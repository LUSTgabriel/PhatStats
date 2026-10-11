using System.Globalization;
using System.Resources;

namespace PhatStats.Localization;

public static class LocalizationService
{
    private static readonly ResourceManager Resources = new("PhatStats.Resources.Strings", typeof(LocalizationService).Assembly);
    private static readonly string[] Supported = ["en-US", "zh-CN", "ja-JP", "ko-KR"];

    public static event EventHandler? CultureChanged;

    public static CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

    public static IReadOnlyList<string> SupportedCultures => Supported;

    public static void Initialize(string? configuredCulture)
    {
        var name = string.IsNullOrWhiteSpace(configuredCulture)
            ? ResolveSupportedCulture(CultureInfo.CurrentUICulture.Name)
            : ResolveSupportedCulture(configuredCulture);
        Apply(name, raiseEvent: false);
    }

    public static void SetCulture(string cultureName)
    {
        Apply(ResolveSupportedCulture(cultureName), raiseEvent: true);
    }

    public static string T(string key)
    {
        return Resources.GetString(key, CurrentCulture)
            ?? Resources.GetString(key, CultureInfo.GetCultureInfo("en-US"))
            ?? key;
    }

    public static string F(string key, params object?[] args) =>
        string.Format(CurrentCulture, T(key), args);

    public static string DisplayName(string cultureName) => cultureName switch
    {
        "zh-CN" => "简体中文",
        "ja-JP" => "日本語",
        "ko-KR" => "한국어",
        _ => "English"
    };

    private static void Apply(string cultureName, bool raiseEvent)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        if (raiseEvent)
            CultureChanged?.Invoke(null, EventArgs.Empty);
    }

    private static string ResolveSupportedCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return "en-US";
        if (Supported.Contains(cultureName, StringComparer.OrdinalIgnoreCase))
            return Supported.First(x => string.Equals(x, cultureName, StringComparison.OrdinalIgnoreCase));
        if (cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
        if (cultureName.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) return "ja-JP";
        if (cultureName.StartsWith("ko", StringComparison.OrdinalIgnoreCase)) return "ko-KR";
        return "en-US";
    }
}
