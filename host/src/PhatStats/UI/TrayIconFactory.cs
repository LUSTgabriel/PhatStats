using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PhatStats.UI;

internal static class TrayIconFactory
{
    public static Icon Create(Color color, string glyph)
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 2, 2, 28, 28);
        using var font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Color.White);
        var size = g.MeasureString(glyph, font);
        g.DrawString(glyph, font, textBrush, (32 - size.Width) / 2, (32 - size.Height) / 2 - 1);
        var h = bmp.GetHicon();
        try { using var tmp = Icon.FromHandle(h); return (Icon)tmp.Clone(); }
        finally { DestroyIcon(h); }
    }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(IntPtr hIcon);
}
