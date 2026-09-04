using System.Windows;

namespace KarzounERP.Helpers;

public static class LocalizationManager
{
    private static string _language = "ar";

    public static string Language => _language;

    public static FlowDirection FlowDirection =>
        _language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public static bool IsRtl => _language == "ar";

    /// <summary>Fired after ApplyLanguage completes (live UI + PDF default language).</summary>
    public static event EventHandler? LanguageChanged;

    public static void Initialize(string language) => ApplyLanguage(language, persist: false);

    /// <summary>Switch language at runtime, update resources and FlowDirection, optionally save to disk.</summary>
    public static void ApplyLanguage(string language, bool persist = true)
    {
        if (Application.Current == null) return;

        var valid = language is "ar" or "tr" or "en" ? language : "ar";
        if (valid == _language && persist == false)
        {
            // Still refresh UI when re-applying same language on startup
        }
        else if (valid == _language && persist)
        {
            SaveLanguageToFile(valid);
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                    window.FlowDirection = FlowDirection;
                RefreshDynamicResources();
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            });
            return;
        }

        _language = valid;

        var existing = Application.Current.Resources.MergedDictionaries
            .Where(d => d.Source?.OriginalString.Contains("/Resources/Strings.") == true)
            .ToList();
        foreach (var old in existing)
            Application.Current.Resources.MergedDictionaries.Remove(old);

        var uri = new Uri($"pack://application:,,,/KARZOUN_ERP;component/Resources/Strings.{valid}.xaml");
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });

        Application.Current.Resources["AppFlowDirection"] = FlowDirection;
        Application.Current.Resources["AppTextAlignment"] = IsRtl ? TextAlignment.Right : TextAlignment.Left;
        Application.Current.Resources["AppHorizontalAlignment"] = IsRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        Application.Current.Resources["AppContentAlignment"] = IsRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        Application.Current.Resources["AppDataGridTextAlignment"] = IsRtl ? TextAlignment.Right : TextAlignment.Left;
        Application.Current.Resources["AppOppositeHorizontalAlignment"] = IsRtl ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        Application.Current.Resources["AppFontFamily"] = IsRtl
            ? new System.Windows.Media.FontFamily("Tajawal, Segoe UI, Tahoma, Arial")
            : new System.Windows.Media.FontFamily("Inter, Segoe UI, Arial");
        Application.Current.Resources["AppHeadlineFontFamily"] = IsRtl
            ? new System.Windows.Media.FontFamily("IBM Plex Sans Arabic SemiBold, Tajawal, Segoe UI Semibold, Tahoma, Arial")
            : new System.Windows.Media.FontFamily("Sora SemiBold, Segoe UI Semibold, Segoe UI, Arial");
        Application.Current.Resources["AppIconMargin"] = IsRtl ? new Thickness(8, 0, 0, 0) : new Thickness(0, 0, 8, 0);
        Application.Current.Resources["AppMargin_8_0"] = IsRtl ? new Thickness(0, 0, 8, 0) : new Thickness(8, 0, 0, 0);
        Application.Current.Resources["AppMargin_10_0"] = IsRtl ? new Thickness(0, 0, 10, 0) : new Thickness(10, 0, 0, 0);
        Application.Current.Resources["AppMargin_20_0"] = IsRtl ? new Thickness(0, 0, 20, 0) : new Thickness(20, 0, 0, 0);

        if (persist)
            SaveLanguageToFile(valid);

        AppLogger.LogInfo($"[Localization] ApplyLanguage: {valid} FlowDirection={FlowDirection}");

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            foreach (Window window in Application.Current.Windows)
                window.FlowDirection = FlowDirection;

            RefreshDynamicResources();
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        });
    }

    public static void SaveLanguage(string language) => ApplyLanguage(language, persist: true);

    private static void SaveLanguageToFile(string valid)
    {
        try
        {
            File.WriteAllText(GetLangFile(), valid);
            AppLogger.LogInfo($"[Localization] Saved language preference: {valid} → {GetLangFile()}");
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to save language preference", ex);
        }
    }

    /// <summary>Force DynamicResource bindings to re-read merged dictionaries.</summary>
    private static void RefreshDynamicResources()
    {
        var root = Application.Current.Resources;
        var copy = root.MergedDictionaries.ToList();
        root.MergedDictionaries.Clear();
        foreach (var d in copy)
            root.MergedDictionaries.Add(d);
    }

    public static string Get(string key) =>
        Application.Current.TryFindResource(key) as string ?? key;

    public static string LoadSavedLanguage()
    {
        var file = GetLangFile();
        string loaded = "ar";
        if (File.Exists(file))
        {
            var raw = File.ReadAllText(file).Trim();
            if (raw is "ar" or "tr" or "en")
                loaded = raw;
            else
                AppLogger.LogInfo($"[Localization] language.txt contained invalid value '{raw}', defaulting to ar");
        }
        else
        {
            AppLogger.LogInfo($"[Localization] language.txt not found at {file}, defaulting to ar");
        }
        AppLogger.LogInfo($"[Localization] LoadSavedLanguage() → '{loaded}'");
        return loaded;
    }

    public static string GetMonthName(int month) => (_language, month) switch
    {
        ("tr", 1) => "Ocak",  ("tr", 2) => "Şubat",   ("tr", 3) => "Mart",
        ("tr", 4) => "Nisan", ("tr", 5) => "Mayıs",   ("tr", 6) => "Haziran",
        ("tr", 7) => "Temmuz",("tr", 8) => "Ağustos",  ("tr", 9) => "Eylül",
        ("tr",10) => "Ekim",  ("tr",11) => "Kasım",   ("tr",12) => "Aralık",
        ("en", 1) => "January",  ("en", 2) => "February", ("en", 3) => "March",
        ("en", 4) => "April",    ("en", 5) => "May",      ("en", 6) => "June",
        ("en", 7) => "July",     ("en", 8) => "August",   ("en", 9) => "September",
        ("en",10) => "October",  ("en",11) => "November", ("en",12) => "December",
        _         => month switch
        {
            1=>"يناير", 2=>"فبراير", 3=>"مارس", 4=>"أبريل",
            5=>"مايو",  6=>"يونيو",  7=>"يوليو",8=>"أغسطس",
            9=>"سبتمبر",10=>"أكتوبر",11=>"نوفمبر",12=>"ديسمبر",
            _=>month.ToString()
        }
    };

    public static string FormatMonthYear(int month, int year) =>
        $"{GetMonthName(month)} {year}";

    private static string GetLangFile()
    {
        var dir = AppPaths.DataRoot;
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "language.txt");
    }
}
