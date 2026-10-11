using System.Text;

namespace PhatStats.Protocol;

public static class NameSanitizer
{
    public const int MaxNameBytes = 48;

    public static string Cpu(string? value)
    {
        var text = Sanitize(value, "AMD CPU", MaxNameBytes);
        if (text.Contains("Ryzen", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("AMD", StringComparison.OrdinalIgnoreCase))
        {
            text = Sanitize("AMD " + text, "AMD CPU", MaxNameBytes);
        }

        return text;
    }

    public static string Gpu(string? value)
    {
        var text = Sanitize(value, "NVIDIA GeForce GPU", MaxNameBytes);

        if (text.StartsWith("NVIDIA NVIDIA ", StringComparison.OrdinalIgnoreCase))
            text = text[7..];

        if (text.Contains("GeForce", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            text = Sanitize("NVIDIA " + text, "NVIDIA GeForce GPU", MaxNameBytes);
        }

        return text;
    }

    public static string Sanitize(string? value, string fallback, int maxBytes)
    {
        if (string.IsNullOrWhiteSpace(value))
            value = fallback;

        var builder = new StringBuilder(Math.Min(value.Length, maxBytes));
        foreach (var ch in value)
        {
            if (ch is '|' or '\r' or '\n' or '\0')
                continue;

            builder.Append(ch is >= ' ' and <= '~' ? ch : '?');
            if (builder.Length >= maxBytes)
                break;
        }

        var result = builder.ToString().Trim();
        return result.Length == 0 ? fallback : result;
    }
}
