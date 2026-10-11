using PhatStats.Localization;

namespace PhatStats.UI;

internal sealed class AboutForm : Form
{
    private readonly Label _text = new() { Dock = DockStyle.Fill, Padding = new Padding(18), AutoSize = false };
    private readonly Button _close = new() { Dock = DockStyle.Bottom, Height = 34 };

    public AboutForm()
    {
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(480, 280);
        _close.Click += (_, _) => Hide();
        Controls.Add(_text);
        Controls.Add(_close);
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
        Text = LocalizationService.T("About.Title");
        _close.Text = LocalizationService.T("Common.Close");
        _text.Text = LocalizationService.F("About.Body", Application.ProductVersion);
    }

    private void OnCultureChanged(object? sender, EventArgs e) => ApplyLocalization();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            LocalizationService.CultureChanged -= OnCultureChanged;
        base.Dispose(disposing);
    }
}
