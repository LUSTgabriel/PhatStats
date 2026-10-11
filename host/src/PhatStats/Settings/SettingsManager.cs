using System.Text.Json;
using PhatStats.Diagnostics;

namespace PhatStats.Settings;

public sealed class SettingsManager : IDisposable
{
    private readonly object _gate = new();
    private readonly object _saveGate = new();
    private readonly SevereErrorRing _errors;
    private readonly string _directory;
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private AppSettings _current;
    private CancellationTokenSource? _saveDebounce;
    private bool _disposed;

    public SettingsManager(SevereErrorRing errors)
    {
        _errors = errors;
        _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhatStats");
        _path = Path.Combine(_directory, "settings.json");
        _current = Load();
    }

    public event EventHandler<AppSettings>? Changed;

    public AppSettings Current
    {
        get
        {
            lock (_gate)
                return _current;
        }
    }

    public string SettingsPath => _path;

    public AppSettings Update(Func<AppSettings, AppSettings> update, bool persist = true)
    {
        ArgumentNullException.ThrowIfNull(update);
        AppSettings next;
        lock (_gate)
        {
            next = update(_current).Validate();
            _current = next;
        }

        Changed?.Invoke(this, next);
        if (persist)
            ScheduleSave();
        return next;
    }

    public void SaveNow()
    {
        AppSettings snapshot;
        lock (_gate)
            snapshot = _current;

        try
        {
            lock (_saveGate)
            {
                Directory.CreateDirectory(_directory);
                var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
                var temp = _path + ".tmp";
                File.WriteAllText(temp, json);
                File.Move(temp, _path, true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _errors.Add("Settings", "SAVE_FAILED", ex.Message);
        }
    }

    private AppSettings Load()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_path);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            if (loaded is null)
                throw new JsonException("Settings file contained no settings object.");

            var validated = loaded.Validate();
            return validated;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            _errors.Add("Settings", "CORRUPT_FALLBACK", ex.Message);
            return new AppSettings();
        }
    }

    private void ScheduleSave()
    {
        CancellationTokenSource cts;
        lock (_gate)
        {
            _saveDebounce?.Cancel();
            _saveDebounce?.Dispose();
            _saveDebounce = new CancellationTokenSource();
            cts = _saveDebounce;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(450, cts.Token).ConfigureAwait(false);
                if (!cts.IsCancellationRequested)
                    SaveNow();
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        lock (_gate)
        {
            _saveDebounce?.Cancel();
            _saveDebounce?.Dispose();
            _saveDebounce = null;
        }

        SaveNow();
    }
}
