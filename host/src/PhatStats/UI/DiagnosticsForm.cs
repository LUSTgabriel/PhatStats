using System.Text;
using PhatStats.Diagnostics;
using PhatStats.Localization;

namespace PhatStats.UI;

internal sealed class DiagnosticsForm : Form
{
    private readonly DiagnosticState _state;
    private readonly SevereErrorRing _errors;
    private readonly TextBox _text = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Font = new Font(FontFamily.GenericMonospace, 9)
    };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly Button _close = new() { AutoSize = true };
    private readonly Button _copy = new() { AutoSize = true };
    private readonly Button _export = new() { AutoSize = true };

    public DiagnosticsForm(DiagnosticState state, SevereErrorRing errors)
    {
        _state = state;
        _errors = errors;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(900, 650);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(6)
        };
        _close.Click += (_, _) => Hide();
        _copy.Click += (_, _) => Clipboard.SetText(Render());
        _export.Click += (_, _) => Export();
        buttons.Controls.Add(_close);
        buttons.Controls.Add(_export);
        buttons.Controls.Add(_copy);
        Controls.Add(_text);
        Controls.Add(buttons);

        _timer.Tick += (_, _) => RefreshText();
        Shown += (_, _) =>
        {
            RefreshText();
            _timer.Start();
        };
        VisibleChanged += (_, _) =>
        {
            if (Visible) _timer.Start();
            else _timer.Stop();
        };
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
    }

    public void ApplyLocalization()
    {
        Text = LocalizationService.T("Diagnostics.Title");
        _close.Text = LocalizationService.T("Common.Close");
        _copy.Text = LocalizationService.T("Diagnostics.Copy");
        _export.Text = LocalizationService.T("Diagnostics.Export");
    }

    private void RefreshText()
    {
        if (Visible)
            _text.Text = Render();
    }

    // Diagnostic payload intentionally remains English for support/debug portability.
    private string Render()
    {
        var diagnostics = _state.Snapshot();
        var sb = new StringBuilder(4096);
        sb.AppendLine("PHATSTATS DIAGNOSTIC SNAPSHOT")
          .AppendLine($"Generated: {DateTimeOffset.Now:O}")
          .AppendLine();
        sb.AppendLine("SERIAL")
          .AppendLine($"  State: {diagnostics.SerialState}")
          .AppendLine($"  Port: {diagnostics.SerialPort ?? "-"}")
          .AppendLine($"  Retries: {diagnostics.RetryCount}")
          .AppendLine($"  Last send: {diagnostics.LastSuccessfulSend?.ToString("O") ?? "-"}")
          .AppendLine($"  Last error: {diagnostics.LastTransportError ?? "-"}")
          .AppendLine($"  Last batch: {diagnostics.LastEncodedBatch}")
          .AppendLine();

        sb.AppendLine("PORTS");
        foreach (var port in diagnostics.Ports)
            sb.AppendLine($"  {port.PortName} | {port.FriendlyName} | VID:{port.Vid ?? "-"} PID:{port.Pid ?? "-"} | serial:{port.UsbSerialNumber ?? "-"} | {port.InstanceId ?? "-"}");

        sb.AppendLine().AppendLine("TELEMETRY");
        if (diagnostics.Telemetry is { } telemetry)
        {
            sb.AppendLine($"  Health: {telemetry.Health} [{string.Join(", ", telemetry.DegradedFields)}]")
              .AppendLine($"  CPU: {telemetry.CpuName} | {telemetry.CpuTemperature:0.#} C | {telemetry.CpuLoad:0.#}% | {telemetry.CpuClock:0} MHz | {telemetry.CpuPower:0.#} W")
              .AppendLine($"  GPU: {telemetry.GpuName} | {telemetry.GpuTemperature:0.#} C | {telemetry.GpuLoad:0.#}% | {telemetry.GpuCoreClock:0} MHz | VRAM {telemetry.GpuMemoryUsed:0}/{telemetry.GpuMemoryTotal:0} MB | {telemetry.GpuPower:0.#} W")
              .AppendLine($"  RAM: {telemetry.RamUsed:0.0} used + {telemetry.RamAvailable:0.0} available GiB | {telemetry.RamLoad:0.#}%");
        }

        sb.AppendLine().AppendLine("GPU DEVICES");
        foreach (var gpu in diagnostics.Gpus)
            sb.AppendLine($"  {(gpu.IsSelected ? "*" : " ")} {gpu.Identifier} | {gpu.Name} | {gpu.HardwareType} | VRAM {gpu.DedicatedMemoryMb:0} MB");

        sb.AppendLine().AppendLine("SENSOR MAPPINGS");
        foreach (var mapping in diagnostics.SensorMappings)
            sb.AppendLine($"  {mapping.Field}: {(mapping.Healthy ? "OK" : "MISSING")} | {mapping.SensorName ?? "-"} | {mapping.SensorIdentifier ?? "-"} | {mapping.Reason}");

        sb.AppendLine().AppendLine("SEVERE ERROR RING (memory only, newest 5 maximum)");
        foreach (var error in _errors.Snapshot())
            sb.AppendLine($"  {error.LastSeen:O} x{error.OccurrenceCount} [{error.Component}/{error.Code}] {error.Message}");
        return sb.ToString();
    }

    private void Export()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt",
            FileName = $"PhatStats-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            File.WriteAllText(dialog.FileName, Render());
    }

    private void OnCultureChanged(object? sender, EventArgs e) => ApplyLocalization();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LocalizationService.CultureChanged -= OnCultureChanged;
            _timer.Stop();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
