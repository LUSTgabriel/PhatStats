using PhatStats.Localization;
using PhatStats.Models;
using PhatStats.Protocol;
using PhatStats.Serial;
using PhatStats.Settings;

namespace PhatStats.UI;

internal sealed class SettingsForm : Form
{
    private readonly SettingsManager _settings;
    private readonly Func<IReadOnlyList<SerialDeviceInfo>> _ports;
    private readonly Func<IReadOnlyList<GpuDeviceDescriptor>> _gpus;
    private readonly Func<bool, bool> _setStartup;

    private readonly ComboBox _serial = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _gpu = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _profile = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly NumericUpDown _interval = new() { Minimum = 500, Maximum = 5000, Increment = 500, Dock = DockStyle.Fill };
    private readonly NumericUpDown _writeTimeout = new() { Minimum = 500, Maximum = 10000, Increment = 100, Dock = DockStyle.Fill };
    private readonly CheckBox _dtr = new() { Text = "DTR", AutoSize = true };
    private readonly CheckBox _rts = new() { Text = "RTS", AutoSize = true };
    private readonly CheckBox _startup = new() { AutoSize = true };
    private readonly Button _close = new() { AutoSize = true, Anchor = AnchorStyles.Right };

    private readonly Label _serialLabel = NewLabel();
    private readonly Label _gpuLabel = NewLabel();
    private readonly Label _profileLabel = NewLabel();
    private readonly Label _intervalLabel = NewLabel();
    private readonly Label _controlLinesLabel = NewLabel();
    private readonly Label _timeoutLabel = NewLabel();
    private bool _loading;

    public SettingsForm(SettingsManager settings, Func<IReadOnlyList<SerialDeviceInfo>> ports,
        Func<IReadOnlyList<GpuDeviceDescriptor>> gpus, Func<bool, bool> setStartup)
    {
        _settings = settings;
        _ports = ports;
        _gpus = gpus;
        _setStartup = setStartup;

        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 370);
        Size = new Size(620, 430);
        ShowInTaskbar = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 8,
            AutoScroll = true
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(table, 0, _serialLabel, _serial);
        AddRow(table, 1, _gpuLabel, _gpu);
        AddRow(table, 2, _profileLabel, _profile);
        AddRow(table, 3, _intervalLabel, _interval);
        var advanced = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        advanced.Controls.Add(_dtr);
        advanced.Controls.Add(_rts);
        AddRow(table, 4, _controlLinesLabel, advanced);
        AddRow(table, 5, _timeoutLabel, _writeTimeout);
        AddRow(table, 6, NewLabel(), _startup);
        AddRow(table, 7, NewLabel(), _close);
        Controls.Add(table);

        _serial.SelectedIndexChanged += (_, _) => ApplySerial();
        _gpu.SelectedIndexChanged += (_, _) =>
        {
            if (!_loading && _gpu.SelectedItem is Choice choice)
                _settings.Update(s => s with { SelectedGpuIdentifier = choice.Value });
        };
        _profile.SelectedIndexChanged += (_, _) =>
        {
            if (!_loading && _profile.SelectedItem is ProtocolChoice choice)
                _settings.Update(s => s with { ProtocolProfile = choice.Value });
        };
        _interval.ValueChanged += (_, _) =>
        {
            if (!_loading)
                _settings.Update(s => s with { SamplingIntervalMs = (int)_interval.Value });
        };
        _dtr.CheckedChanged += (_, _) =>
        {
            if (!_loading)
                _settings.Update(s => s with { DtrEnable = _dtr.Checked });
        };
        _rts.CheckedChanged += (_, _) =>
        {
            if (!_loading)
                _settings.Update(s => s with { RtsEnable = _rts.Checked });
        };
        _writeTimeout.ValueChanged += (_, _) =>
        {
            if (!_loading)
                _settings.Update(s => s with { WriteTimeoutMs = (int)_writeTimeout.Value });
        };
        _startup.CheckedChanged += (_, _) => ApplyStartup();
        _close.Click += (_, _) => Hide();
        Activated += (_, _) => Reload();
        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
        LocalizationService.CultureChanged += OnCultureChanged;
        ApplyLocalization();
        Reload();
    }

    public void ApplyLocalization()
    {
        Text = LocalizationService.T("Settings.Title");
        _serialLabel.Text = LocalizationService.T("Settings.SerialTarget");
        _gpuLabel.Text = LocalizationService.T("Settings.Gpu");
        _profileLabel.Text = LocalizationService.T("Settings.Protocol");
        _intervalLabel.Text = LocalizationService.T("Settings.SamplingInterval");
        _controlLinesLabel.Text = LocalizationService.T("Settings.SerialControlLines");
        _timeoutLabel.Text = LocalizationService.T("Settings.WriteTimeout");
        _startup.Text = LocalizationService.T("Settings.StartWithWindows");
        _close.Text = LocalizationService.T("Common.Close");
        if (IsHandleCreated)
            Reload();
    }

    public void Reload()
    {
        _loading = true;
        try
        {
            var settings = _settings.Current;

            _serial.Items.Clear();
            _serial.Items.Add(new Choice(LocalizationService.T("Settings.AutoSafeUsb"), null));
            foreach (var port in _ports())
                _serial.Items.Add(new SerialChoice(port));
            if (settings.AutoSerial)
                _serial.SelectedIndex = 0;
            else
                SelectChoice(_serial, settings.SerialIdentity?.LastKnownPort, serialPort: true);

            _gpu.Items.Clear();
            _gpu.Items.Add(new Choice(LocalizationService.T("Settings.Auto"), null));
            foreach (var gpu in _gpus())
                _gpu.Items.Add(new Choice(gpu.Name, gpu.Identifier));
            SelectChoice(_gpu, settings.SelectedGpuIdentifier);

            _profile.Items.Clear();
            foreach (var profile in Enum.GetValues<ProtocolProfile>())
                _profile.Items.Add(new ProtocolChoice(profile, ProfileText(profile)));
            SelectProfile(settings.ProtocolProfile);

            _interval.Value = Math.Clamp(settings.SamplingIntervalMs, 500, 5000);
            _writeTimeout.Value = Math.Clamp(settings.WriteTimeoutMs, 500, 10000);
            _dtr.Checked = settings.DtrEnable;
            _rts.Checked = settings.RtsEnable;
            _startup.Checked = settings.StartWithWindows;
        }
        finally
        {
            _loading = false;
        }
    }

    private static string ProfileText(ProtocolProfile profile) => profile switch
    {
        ProtocolProfile.LegacyWeeStatServer052 => LocalizationService.T("Profile.Legacy"),
        ProtocolProfile.ExtendedCompatibility => LocalizationService.T("Profile.Extended"),
        _ => LocalizationService.T("Profile.CurrentPortrait")
    };

    private void SelectProfile(ProtocolProfile profile)
    {
        for (var i = 0; i < _profile.Items.Count; i++)
        {
            if (_profile.Items[i] is ProtocolChoice choice && choice.Value == profile)
            {
                _profile.SelectedIndex = i;
                return;
            }
        }
        _profile.SelectedIndex = 0;
    }

    private void ApplySerial()
    {
        if (_loading)
            return;
        if (_serial.SelectedItem is SerialChoice serialChoice)
            _settings.Update(s => s with { AutoSerial = false, SerialIdentity = serialChoice.Device.ToIdentity() });
        else
            _settings.Update(s => s with { AutoSerial = true });
    }

    private void ApplyStartup()
    {
        if (_loading)
            return;
        var requested = _startup.Checked;
        if (_setStartup(requested))
            _settings.Update(s => s with { StartWithWindows = requested });
        else
        {
            _loading = true;
            _startup.Checked = _settings.Current.StartWithWindows;
            _loading = false;
        }
    }

    private static void SelectChoice(ComboBox combo, string? value, bool serialPort = false)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            var match = combo.Items[i] switch
            {
                Choice choice => string.Equals(choice.Value, value, StringComparison.OrdinalIgnoreCase),
                SerialChoice serialChoice when serialPort => string.Equals(serialChoice.Device.PortName, value, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
            if (match)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private static Label NewLabel() => new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Padding = new Padding(0, 7, 0, 0)
    };

    private static void AddRow(TableLayoutPanel table, int row, Label label, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(label, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private void OnCultureChanged(object? sender, EventArgs e) => ApplyLocalization();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            LocalizationService.CultureChanged -= OnCultureChanged;
        base.Dispose(disposing);
    }

    private sealed record Choice(string Text, string? Value)
    {
        public override string ToString() => Text;
    }

    private sealed record SerialChoice(SerialDeviceInfo Device)
    {
        public override string ToString() => Device.DisplayName;
    }

    private sealed record ProtocolChoice(ProtocolProfile Value, string Text)
    {
        public override string ToString() => Text;
    }
}
