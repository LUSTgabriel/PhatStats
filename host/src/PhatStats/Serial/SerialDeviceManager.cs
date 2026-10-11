using System.IO;
using PhatStats.Diagnostics;
using PhatStats.Protocol;
using PhatStats.Runtime;
using PhatStats.Settings;

namespace PhatStats.Serial;

public sealed class SerialDeviceManager : IDisposable
{
    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)
    ];

    private readonly object _gate = new();
    private readonly SettingsManager _settings;
    private readonly LatestSnapshotMailbox _mailbox;
    private readonly DiagnosticState _diagnostics;
    private readonly SevereErrorRing _errors;
    private readonly SetupApiPortEnumerator _enumerator = new();
    private readonly ProtocolSession _protocol = new(new ProtocolEncoder());
    private readonly SerialTransport _transport = new();
    private readonly SemaphoreSlim _wake = new(0, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    private SerialDeviceInfo[] _ports = [];
    private SerialStatus _status = new(SerialConnectionState.Unconfigured, null, "Not configured", 0, null);
    private bool _devicesDirty = true;
    private bool _settingsDirty = true;
    private bool _forceReconnect;
    private bool _forceSeed;
    private bool _paused;
    private int _retryIndex;
    private DateTimeOffset _nextRetry = DateTimeOffset.MinValue;
    private long _lastVersion;
    private string _lastBatch = string.Empty;
    private DateTimeOffset? _lastSuccessfulSend;
    private string? _lastError;
    private bool _disposed;
    private (bool Dtr, bool Rts, int WriteTimeout)? _appliedLink;
    private ProtocolProfile? _appliedProfile;

    public SerialDeviceManager(SettingsManager settings, LatestSnapshotMailbox mailbox, DiagnosticState diagnostics, SevereErrorRing errors)
    {
        _settings = settings; _mailbox = mailbox; _diagnostics = diagnostics; _errors = errors;
        _settings.Changed += OnSettingsChanged;
        _worker = Task.Run(WorkerAsync);
    }

    public event EventHandler<SerialStatus>? StatusChanged;
    public event EventHandler<(string Title, string Message)>? NotificationRequested;

    public SerialStatus Status { get { lock (_gate) return _status; } }
    public IReadOnlyList<SerialDeviceInfo> Ports { get { lock (_gate) return _ports; } }

    public void NotifySnapshotAvailable() => Signal();
    public void NotifyDevicesChanged() { lock (_gate) _devicesDirty = true; Signal(); }
    public void ReconnectNow() { lock (_gate) { _forceReconnect = true; _devicesDirty = true; _nextRetry = DateTimeOffset.MinValue; } Signal(); }
    public void Pause() { lock (_gate) _paused = true; Signal(); }
    public void Resume() { lock (_gate) { _paused = false; _forceSeed = true; } Signal(); }
    public void SystemResumed() { lock (_gate) { _devicesDirty = true; _forceReconnect = true; _forceSeed = true; _nextRetry = DateTimeOffset.MinValue; } Signal(); }

    private async Task WorkerAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync().ConfigureAwait(false);
                var delay = GetWaitDelay();
                if (delay == Timeout.InfiniteTimeSpan)
                    await _wake.WaitAsync(_cts.Token).ConfigureAwait(false);
                else
                    await _wake.WaitAsync(delay, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _errors.Add("Serial", "STATE_MACHINE_FAILURE", ex.Message);
                _lastError = ex.Message;
                _transport.Close();
                Transition(SerialConnectionState.Recovering, null, "Recovering", ex.Message);
                ScheduleRetry();
            }
        }
        _transport.Close();
    }

    private Task ProcessAsync()
    {
        AppSettings settings = _settings.Current;
        bool paused, dirty, settingsDirty, forceReconnect;
        lock (_gate)
        {
            paused = _paused; dirty = _devicesDirty; settingsDirty = _settingsDirty; forceReconnect = _forceReconnect;
            _devicesDirty = false; _settingsDirty = false; _forceReconnect = false;
        }

        if (dirty || settingsDirty || forceReconnect || (_nextRetry != DateTimeOffset.MinValue && DateTimeOffset.UtcNow >= _nextRetry))
            RefreshPorts();

        if (_appliedProfile != settings.ProtocolProfile)
        {
            _appliedProfile = settings.ProtocolProfile;
            _protocol.Reset();
            lock (_gate) _forceSeed = true;
        }

        if (paused)
        {
            Transition(SerialConnectionState.Paused, _transport.PortName, "Paused · display will sleep", null);
            return Task.CompletedTask;
        }

        var target = ResolveTarget(settings, _ports, out var targetState);
        if (target is null)
        {
            _transport.Close(); _appliedLink = null;
            _protocol.Reset();
            Transition(targetState, null, targetState switch
            {
                SerialConnectionState.Ambiguous => "Multiple serial devices · select one",
                SerialConnectionState.NoDevice => "Selected device not present",
                _ => "Select a serial device"
            }, null);
            return Task.CompletedTask;
        }

        if (forceReconnect && _transport.IsOpen)
        {
            _transport.Close(); _appliedLink = null; _protocol.Reset();
            lock (_gate) _forceSeed = true;
        }

        var desiredLink = (settings.DtrEnable, settings.RtsEnable, settings.WriteTimeoutMs);
        if (_transport.IsOpen && _appliedLink.HasValue && _appliedLink.Value != desiredLink)
        {
            _transport.Close(); _protocol.Reset(); _appliedLink = null;
            forceReconnect = true;
        }

        if (_transport.IsOpen && !string.Equals(_transport.PortName, target.PortName, StringComparison.OrdinalIgnoreCase))
        {
            TryTransactionalSwitch(target, settings);
            return Task.CompletedTask;
        }

        if (!_transport.IsOpen)
        {
            if (!forceReconnect && _nextRetry != DateTimeOffset.MinValue && DateTimeOffset.UtcNow < _nextRetry)
                return Task.CompletedTask;
            TryConnect(target, settings);
        }

        if (!_transport.IsOpen) return Task.CompletedTask;

        var version = _mailbox.Version;
        var snapshot = _mailbox.Latest;
        bool forceSeed;
        lock (_gate) { forceSeed = _forceSeed; _forceSeed = false; }
        if (snapshot is null || (version == _lastVersion && !forceSeed)) return Task.CompletedTask;

        var now = DateTimeOffset.UtcNow;
        var batch = _protocol.Encode(snapshot, settings.ProtocolProfile, now, forceSeed,
            TimeSpan.FromMilliseconds(settings.DynamicRefreshMs), TimeSpan.FromMilliseconds(settings.FullSeedRefreshMs));
        _lastVersion = version; // Empty deltas are intentionally consumed, never queued.
        if (batch.Length == 0) return Task.CompletedTask;

        try
        {
            _transport.WriteAscii(batch);
            _lastBatch = batch;
            _lastSuccessfulSend = now;
            _lastError = null;
            _retryIndex = 0; _nextRetry = DateTimeOffset.MinValue;
            Transition(SerialConnectionState.Connected, target.PortName, $"{target.PortName} · Connected", null);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException or TimeoutException)
        {
            _lastError = ex.Message;
            _transport.Close(); _appliedLink = null; _protocol.Reset();
            Transition(SerialConnectionState.Recovering, target.PortName, $"{target.PortName} · Recovering", ex.Message);
            ScheduleRetry();
        }
        return Task.CompletedTask;
    }

    private void TryConnect(SerialDeviceInfo target, AppSettings settings)
    {
        Transition(SerialConnectionState.Connecting, target.PortName, $"{target.PortName} · Connecting", null);
        try
        {
            _transport.Open(target.PortName, settings);
            _appliedLink = (settings.DtrEnable, settings.RtsEnable, settings.WriteTimeoutMs);
            _protocol.Reset(); _lastVersion = 0; _forceSeed = true; _retryIndex = 0; _nextRetry = DateTimeOffset.MinValue; _lastError = null;
            PersistResolvedIdentity(target);
            Transition(SerialConnectionState.Connected, target.PortName, $"{target.PortName} · Connected", null);
        }
        catch (UnauthorizedAccessException ex)
        {
            _lastError = ex.Message;
            Transition(SerialConnectionState.Busy, target.PortName, $"{target.PortName} · In use", ex.Message);
            ScheduleRetry();
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException)
        {
            _lastError = ex.Message;
            Transition(SerialConnectionState.Recovering, target.PortName, $"{target.PortName} · Recovering", ex.Message);
            ScheduleRetry();
        }
    }

    private void TryTransactionalSwitch(SerialDeviceInfo target, AppSettings settings)
    {
        var oldPort = _transport.PortName;
        Transition(SerialConnectionState.Switching, oldPort, $"Switching to {target.PortName}", null);
        using var candidate = new SerialTransport();
        try
        {
            candidate.Open(target.PortName, settings);
            _transport.AdoptFrom(candidate);
            _appliedLink = (settings.DtrEnable, settings.RtsEnable, settings.WriteTimeoutMs);
            _protocol.Reset(); _lastVersion = 0; _forceSeed = true;
            PersistResolvedIdentity(target);
            Transition(SerialConnectionState.Connected, target.PortName, $"{target.PortName} · Connected", null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            _lastError = ex.Message;
            // Candidate failure leaves the old connection alive and restores the previous working selection.
            var old = _ports.FirstOrDefault(p => string.Equals(p.PortName, _transport.PortName, StringComparison.OrdinalIgnoreCase));
            if (old is not null)
                _settings.Update(s => s with { AutoSerial = false, SerialIdentity = old.ToIdentity() }, persist: true);
            NotificationRequested?.Invoke(this, ("PhatStats", $"Could not switch to {target.PortName}. The previous serial connection remains active."));
            Transition(_transport.IsOpen ? SerialConnectionState.Connected : SerialConnectionState.Recovering,
                _transport.PortName, _transport.IsOpen ? $"{_transport.PortName} · Connected" : "Recovering", ex.Message);
        }
    }

    private void RefreshPorts()
    {
        try { lock (_gate) _ports = _enumerator.Enumerate(); }
        catch (Exception ex) { _errors.Add("Serial", "ENUMERATION_FAILED", ex.Message); lock (_gate) _ports = []; }
        UpdateDiagnostics();
    }

    private static SerialDeviceInfo? ResolveTarget(AppSettings settings, IReadOnlyList<SerialDeviceInfo> ports, out SerialConnectionState noTargetState)
    {
        noTargetState = SerialConnectionState.Unconfigured;
        if (settings.SerialIdentity is { } saved)
        {
            var strong = ports.Where(saved.IsStrongMatch).OrderByDescending(saved.Score).ToArray();
            if (strong.Length > 0) return strong[0];

            if (!settings.AutoSerial)
            {
                // A user-explicit weak target may use its current COM number only when no stable identity metadata exists.
                if (!saved.HasStableIdentity && !string.IsNullOrWhiteSpace(saved.LastKnownPort))
                {
                    var explicitPort = ports.FirstOrDefault(p => string.Equals(p.PortName, saved.LastKnownPort, StringComparison.OrdinalIgnoreCase));
                    if (explicitPort is not null) return explicitPort;
                }
                noTargetState = SerialConnectionState.NoDevice; return null;
            }

            // Never auto-fall-through from a missing known USB identity to an unrelated device.
            if (saved.HasStableIdentity) { noTargetState = SerialConnectionState.NoDevice; return null; }
        }
        if (!settings.AutoSerial) { noTargetState = SerialConnectionState.Unconfigured; return null; }
        var suitable = ports.Where(p => p.IsPhysicalUsb && !p.IsBluetooth).ToArray();
        if (suitable.Length == 1) return suitable[0];
        noTargetState = suitable.Length == 0 ? SerialConnectionState.NoDevice : SerialConnectionState.Ambiguous;
        return null;
    }

    private void PersistResolvedIdentity(SerialDeviceInfo target)
    {
        var current = _settings.Current.SerialIdentity;
        var identity = target.ToIdentity();
        if (current == identity) return;
        _settings.Update(s => s with { SerialIdentity = identity }, persist: true);
    }

    private void ScheduleRetry()
    {
        var idx = Math.Min(_retryIndex, Backoff.Length - 1);
        _nextRetry = DateTimeOffset.UtcNow + Backoff[idx];
        _retryIndex = Math.Min(_retryIndex + 1, Backoff.Length);
        UpdateDiagnostics();
    }

    private TimeSpan GetWaitDelay()
    {
        if (_nextRetry == DateTimeOffset.MinValue) return Timeout.InfiniteTimeSpan;
        var d = _nextRetry - DateTimeOffset.UtcNow;
        return d <= TimeSpan.Zero ? TimeSpan.Zero : d;
    }

    private void Transition(SerialConnectionState state, string? port, string summary, string? error)
    {
        SerialStatus previous, next;
        bool changed;
        lock (_gate)
        {
            previous = _status;
            changed = previous.State != state
                || !string.Equals(previous.PortName, port, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(previous.Summary, summary, StringComparison.Ordinal)
                || previous.RetryCount != _retryIndex
                || !string.Equals(previous.LastError, error, StringComparison.Ordinal);

            if (changed)
            {
                next = new SerialStatus(state, port, summary, _retryIndex, error);
                _status = next;
            }
            else
            {
                next = previous;
            }
        }

        if (changed)
        {
            StatusChanged?.Invoke(this, next);
            if (state == SerialConnectionState.Busy && previous.State != SerialConnectionState.Busy)
                NotificationRequested?.Invoke(this, ("PhatStats", $"{port} exists but is currently in use. Connection will retry automatically."));
            if (state == SerialConnectionState.NoDevice && previous.State == SerialConnectionState.Connected)
                NotificationRequested?.Invoke(this, ("PhatStats", "The selected serial device was removed. Reconnection will be automatic when it returns."));
        }

        // Diagnostics still receives the newest batch/send timestamp even when the connection
        // state itself is unchanged.
        UpdateDiagnostics();
    }

    private void UpdateDiagnostics()
    {
        IReadOnlyList<SerialDeviceInfo> ports; SerialStatus status;
        lock (_gate) { ports = _ports; status = _status; }
        _diagnostics.UpdateSerial(status.State, status.PortName, ports, _lastBatch, _lastSuccessfulSend, _retryIndex, _lastError);
    }

    private void OnSettingsChanged(object? sender, AppSettings e) { lock (_gate) _settingsDirty = true; Signal(); }
    private void Signal() { try { _wake.Release(); } catch (SemaphoreFullException) { } }

    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        _settings.Changed -= OnSettingsChanged;
        _cts.Cancel(); Signal();
        try { _worker.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
        _transport.Dispose(); _wake.Dispose(); _cts.Dispose();
    }
}
