using PhatStats.Diagnostics;
using PhatStats.Hardware;
using PhatStats.Localization;
using PhatStats.Models;
using PhatStats.Protocol;
using PhatStats.Runtime;
using PhatStats.Serial;
using PhatStats.Settings;
using PhatStats.UI;

namespace PhatStats.App;

internal sealed class PhatStatsApplicationContext : ApplicationContext
{
    private readonly SingleInstanceGate _instance;
    private readonly SevereErrorRing _errors = new();
    private readonly DiagnosticState _diagnostics = new();
    private readonly LatestSnapshotMailbox _mailbox = new();
    private readonly SettingsManager _settings;
    private readonly HardwareTelemetryEngine _hardware;
    private readonly SerialDeviceManager _serial;
    private readonly TelemetryCoordinator _telemetry;
    private readonly TaskSchedulerStartup _startup;
    private readonly SystemMessageWindow _messages = new();
    private readonly NotifyIcon _tray = new();
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _statusItem = new() { Enabled = false };
    private readonly ToolStripMenuItem _serialMenu = new();
    private readonly ToolStripMenuItem _gpuMenu = new();
    private readonly ToolStripMenuItem _intervalMenu = new();
    private readonly ToolStripMenuItem _languageMenu = new();
    private readonly ToolStripMenuItem _pauseItem = new();
    private readonly ToolStripMenuItem _reconnectItem = new();
    private readonly ToolStripMenuItem _settingsItem = new();
    private readonly ToolStripMenuItem _diagnosticsItem = new();
    private readonly ToolStripMenuItem _startupItem = new() { CheckOnClick = false };
    private readonly ToolStripMenuItem _aboutItem = new();
    private readonly ToolStripMenuItem _exitItem = new();
    private readonly Control _marshal = new();
    private readonly System.Windows.Forms.Timer _uiTimer = new() { Interval = 1000 };
    private readonly Dictionary<string, Icon> _icons;
    private SettingsForm? _settingsForm;
    private DiagnosticsForm? _diagnosticsForm;
    private AboutForm? _aboutForm;
    private string? _lastTrayIconKey;
    private string? _lastTrayText;
    private string? _lastStatusText;
    private bool _exiting;
    private bool _disposed;

    public PhatStatsApplicationContext(SingleInstanceGate instance)
    {
        _instance = instance;
        _ = _marshal.Handle;
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        _settings = new SettingsManager(_errors);
        LocalizationService.Initialize(_settings.Current.Language);
        LocalizationService.CultureChanged += OnCultureChanged;

        _hardware = new HardwareTelemetryEngine(_errors);
        _serial = new SerialDeviceManager(_settings, _mailbox, _diagnostics, _errors);
        _telemetry = new TelemetryCoordinator(_hardware, _mailbox, _serial, _settings, _diagnostics, _errors);
        _startup = new TaskSchedulerStartup(_errors);
        if (_settings.Current.StartWithWindows)
            _startup.SetEnabled(true);

        _icons = new Dictionary<string, Icon>
        {
            ["connected"] = TrayIconFactory.Create(Color.SeaGreen, "P"),
            ["working"] = TrayIconFactory.Create(Color.SteelBlue, "…"),
            ["busy"] = TrayIconFactory.Create(Color.DarkOrange, "!"),
            ["offline"] = TrayIconFactory.Create(Color.Firebrick, "×"),
            ["paused"] = TrayIconFactory.Create(Color.DimGray, "Ⅱ"),
            ["degraded"] = TrayIconFactory.Create(Color.MediumPurple, "!")
        };

        BuildStaticMenu();
        ApplyLocalization();
        _tray.ContextMenuStrip = _menu;
        _tray.Icon = _icons["working"];
        _tray.Text = "PhatStats";
        _tray.Visible = true;
        _tray.DoubleClick += (_, _) => ShowDiagnostics();
        _menu.Opening += (_, _) => RebuildDynamicMenus();

        _serial.StatusChanged += OnSerialStatusChanged;
        _serial.NotificationRequested += OnNotificationRequested;
        _messages.SerialDevicesChanged += (_, _) => _serial.NotifyDevicesChanged();
        _messages.SystemResumed += (_, _) => _telemetry.SystemResumed();
        _instance.ActivateRequested += OnActivateRequested;
        _uiTimer.Tick += (_, _) => RefreshTray();
        _uiTimer.Start();
        RefreshTray();
    }

    private void BuildStaticMenu()
    {
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_serialMenu);
        _menu.Items.Add(_gpuMenu);
        _menu.Items.Add(_intervalMenu);
        _menu.Items.Add(_languageMenu);

        _pauseItem.Click += (_, _) =>
        {
            if (_serial.Status.State == SerialConnectionState.Paused) _serial.Resume();
            else _serial.Pause();
        };
        _menu.Items.Add(_pauseItem);

        _reconnectItem.Click += (_, _) => _serial.ReconnectNow();
        _menu.Items.Add(_reconnectItem);
        _menu.Items.Add(new ToolStripSeparator());

        _settingsItem.Click += (_, _) => ShowSettings();
        _menu.Items.Add(_settingsItem);
        _diagnosticsItem.Click += (_, _) => ShowDiagnostics();
        _menu.Items.Add(_diagnosticsItem);
        _startupItem.Click += (_, _) => ToggleStartup();
        _menu.Items.Add(_startupItem);
        _aboutItem.Click += (_, _) => ShowAbout();
        _menu.Items.Add(_aboutItem);
        _menu.Items.Add(new ToolStripSeparator());
        _exitItem.Click += (_, _) => ExitThread();
        _menu.Items.Add(_exitItem);
    }

    private void ApplyLocalization()
    {
        _serialMenu.Text = LocalizationService.T("Menu.SerialPort");
        _gpuMenu.Text = LocalizationService.T("Menu.Gpu");
        _intervalMenu.Text = LocalizationService.T("Menu.SamplingInterval");
        _languageMenu.Text = LocalizationService.T("Menu.Language");
        _reconnectItem.Text = LocalizationService.T("Menu.ReconnectNow");
        _settingsItem.Text = LocalizationService.T("Menu.Settings");
        _diagnosticsItem.Text = LocalizationService.T("Menu.Diagnostics");
        _startupItem.Text = LocalizationService.T("Menu.StartWithWindows");
        _aboutItem.Text = LocalizationService.T("Menu.About");
        _exitItem.Text = LocalizationService.T("Menu.Exit");
        _pauseItem.Text = _serial.Status.State == SerialConnectionState.Paused
            ? LocalizationService.T("Menu.ResumeTelemetry")
            : LocalizationService.T("Menu.PauseTelemetry");
    }

    private void RebuildDynamicMenus()
    {
        var settings = _settings.Current;

        DisposeDropDownItems(_serialMenu);
        var auto = new ToolStripMenuItem(LocalizationService.T("Menu.Auto")) { Checked = settings.AutoSerial };
        auto.Click += (_, _) => _settings.Update(x => x with { AutoSerial = true });
        _serialMenu.DropDownItems.Add(auto);
        foreach (var port in _serial.Ports)
        {
            var item = new ToolStripMenuItem(port.DisplayName)
            {
                Checked = !settings.AutoSerial && settings.SerialIdentity?.IsStrongMatch(port) == true
            };
            var copy = port;
            item.Click += (_, _) => _settings.Update(x => x with { AutoSerial = false, SerialIdentity = copy.ToIdentity() });
            _serialMenu.DropDownItems.Add(item);
        }

        var diagnostics = _diagnostics.Snapshot();
        DisposeDropDownItems(_gpuMenu);
        var gpuAuto = new ToolStripMenuItem(LocalizationService.T("Menu.Auto"))
        {
            Checked = string.IsNullOrWhiteSpace(settings.SelectedGpuIdentifier)
        };
        gpuAuto.Click += (_, _) => _settings.Update(x => x with { SelectedGpuIdentifier = null });
        _gpuMenu.DropDownItems.Add(gpuAuto);
        foreach (var gpu in diagnostics.Gpus)
        {
            var item = new ToolStripMenuItem(gpu.Name)
            {
                Checked = string.Equals(settings.SelectedGpuIdentifier, gpu.Identifier, StringComparison.OrdinalIgnoreCase)
            };
            var id = gpu.Identifier;
            item.Click += (_, _) => _settings.Update(x => x with { SelectedGpuIdentifier = id });
            _gpuMenu.DropDownItems.Add(item);
        }

        DisposeDropDownItems(_intervalMenu);
        foreach (var ms in new[] { 500, 1000, 2000, 3000, 5000 })
        {
            var item = new ToolStripMenuItem($"{ms / 1000d:0.0} s") { Checked = settings.SamplingIntervalMs == ms };
            var value = ms;
            item.Click += (_, _) => _settings.Update(x => x with { SamplingIntervalMs = value });
            _intervalMenu.DropDownItems.Add(item);
        }

        DisposeDropDownItems(_languageMenu);
        foreach (var cultureName in LocalizationService.SupportedCultures)
        {
            var item = new ToolStripMenuItem(LocalizationService.DisplayName(cultureName))
            {
                Checked = string.Equals(LocalizationService.CurrentCulture.Name, cultureName, StringComparison.OrdinalIgnoreCase)
            };
            var selected = cultureName;
            item.Click += (_, _) => LocalizationService.SetCulture(selected);
            _languageMenu.DropDownItems.Add(item);
        }

        _startupItem.Checked = settings.StartWithWindows;
        _pauseItem.Text = _serial.Status.State == SerialConnectionState.Paused
            ? LocalizationService.T("Menu.ResumeTelemetry")
            : LocalizationService.T("Menu.PauseTelemetry");
    }

    private void RefreshTray()
    {
        var status = _serial.Status;
        var diagnostics = _diagnostics.Snapshot();
        var degraded = diagnostics.Telemetry?.Health == TelemetryHealth.Degraded;
        var icon = status.State switch
        {
            SerialConnectionState.Paused => "paused",
            SerialConnectionState.Connected when degraded => "degraded",
            SerialConnectionState.Connected => "connected",
            SerialConnectionState.Connecting or SerialConnectionState.Recovering or SerialConnectionState.Switching => "working",
            SerialConnectionState.Busy => "busy",
            _ => "offline"
        };
        if (!string.Equals(_lastTrayIconKey, icon, StringComparison.Ordinal))
        {
            _tray.Icon = _icons[icon];
            _lastTrayIconKey = icon;
        }

        var stateText = LocalizedSerialState(status.State);
        var statusText = (status.PortName is null ? stateText : $"{status.PortName} · {stateText}")
            + (degraded ? $" · {LocalizationService.T("Status.HardwareDegraded")}" : string.Empty);
        if (!string.Equals(_lastStatusText, statusText, StringComparison.Ordinal))
        {
            _statusItem.Text = statusText;
            _lastStatusText = statusText;
        }

        var gpu = diagnostics.Telemetry?.GpuName;
        if (!string.IsNullOrWhiteSpace(gpu) && gpu.Length > 30)
            gpu = gpu[..30];
        var tip = $"PhatStats · {stateText}"
            + (status.PortName is null ? string.Empty : $" · {status.PortName}")
            + (gpu is null ? string.Empty : $" · {gpu}");
        tip = tip.Length <= 127 ? tip : tip[..127];
        if (!string.Equals(_lastTrayText, tip, StringComparison.Ordinal))
        {
            _tray.Text = tip;
            _lastTrayText = tip;
        }
    }

    private static string LocalizedSerialState(SerialConnectionState state) =>
        LocalizationService.T("Status." + state);

    private static void DisposeDropDownItems(ToolStripMenuItem menu)
    {
        if (menu.DropDownItems.Count == 0)
            return;

        var oldItems = new ToolStripItem[menu.DropDownItems.Count];
        for (var i = 0; i < oldItems.Length; i++)
            oldItems[i] = menu.DropDownItems[i];
        menu.DropDownItems.Clear();
        foreach (var item in oldItems)
            item.Dispose();
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        _settings.Update(s => s with { Language = LocalizationService.CurrentCulture.Name });
        ApplyLocalization();
        RebuildDynamicMenus();
        RefreshTray();
    }

    private void OnSerialStatusChanged(object? sender, SerialStatus e) => MarshalToUi(RefreshTray);

    private void OnNotificationRequested(object? sender, (string Title, string Message) e) => MarshalToUi(() =>
    {
        var message = e.Message;
        if (message.Contains("currently in use", StringComparison.Ordinal))
            message = LocalizationService.F("Notify.SerialBusy", _serial.Status.PortName ?? string.Empty);
        else if (message.StartsWith("The selected serial device was removed", StringComparison.Ordinal))
            message = LocalizationService.T("Notify.DeviceRemoved");
        _tray.ShowBalloonTip(4000, "PhatStats", message, ToolTipIcon.Warning);
    });

    private void OnActivateRequested(object? sender, EventArgs e) => MarshalToUi(ShowSettings);
    private void OnThreadException(object sender, ThreadExceptionEventArgs e) => _errors.Add("UI", "UNHANDLED_UI_EXCEPTION", e.Exception.Message);

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            _errors.Add("Runtime", "UNHANDLED_EXCEPTION", ex.Message);
    }

    private void MarshalToUi(Action action)
    {
        if (_marshal.IsDisposed)
            return;
        try { _marshal.BeginInvoke(action); } catch (InvalidOperationException) { }
    }

    private void ShowSettings()
    {
        _settingsForm ??= new SettingsForm(_settings, () => _serial.Ports, () => _diagnostics.Snapshot().Gpus,
            enabled => _startup.SetEnabled(enabled));
        _settingsForm.Reload();
        _settingsForm.Show();
        if (_settingsForm.WindowState == FormWindowState.Minimized)
            _settingsForm.WindowState = FormWindowState.Normal;
        _settingsForm.Activate();
    }

    private void ShowDiagnostics()
    {
        _diagnosticsForm ??= new DiagnosticsForm(_diagnostics, _errors);
        _diagnosticsForm.Show();
        _diagnosticsForm.Activate();
    }

    private void ShowAbout()
    {
        _aboutForm ??= new AboutForm();
        _aboutForm.Show();
        _aboutForm.Activate();
    }

    private void ToggleStartup()
    {
        var desired = !_settings.Current.StartWithWindows;
        if (_startup.SetEnabled(desired))
            _settings.Update(s => s with { StartWithWindows = desired });
    }

    protected override void ExitThreadCore()
    {
        if (_exiting)
            return;
        _exiting = true;
        _tray.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _uiTimer.Stop();
            _uiTimer.Dispose();
            LocalizationService.CultureChanged -= OnCultureChanged;
            _instance.ActivateRequested -= OnActivateRequested;
            Application.ThreadException -= OnThreadException;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            _serial.StatusChanged -= OnSerialStatusChanged;
            _serial.NotificationRequested -= OnNotificationRequested;
            _settingsForm?.Dispose();
            _diagnosticsForm?.Dispose();
            _aboutForm?.Dispose();
            _telemetry.Dispose();
            _serial.Dispose();
            _hardware.Dispose();
            _settings.Dispose();
            _messages.Dispose();
            _tray.Dispose();
            _menu.Dispose();
            _marshal.Dispose();
            foreach (var icon in _icons.Values)
                icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
