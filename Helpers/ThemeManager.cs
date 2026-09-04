using KarzounERP.Models;
using MaterialDesignThemes.Wpf;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace KarzounERP.Helpers;

public static class ThemeManager
{
    private static readonly Dictionary<string, string> DefaultHex = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AppPrimaryBrush"] = KarzounBrand.Teal,
        ["AppSecondaryBrush"] = KarzounBrand.Blue,
        ["AppAccentBrush"] = KarzounBrand.Emerald,
        ["AppSidebarBackgroundBrush"] = KarzounBrand.Navy,
        ["AppSidebarTextColorBrush"] = KarzounBrand.LightGray,
        ["AppButtonBrush"] = KarzounBrand.Teal,
        ["AppButtonTextColorBrush"] = KarzounBrand.Navy,
        ["AppCardBackgroundBrush"] = KarzounBrand.LightCard,
        ["AppPageBackgroundBrush"] = KarzounBrand.LightPage,
    };

    public static string GetDefaultHex(string resourceKey)
        => DefaultHex.TryGetValue(resourceKey, out var hex) ? hex : "#000000";

    public static Color ParseColorOrDefault(string? hex, string fallbackHex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch { }
        }

        try
        {
            return (Color)ColorConverter.ConvertFromString(fallbackHex);
        }
        catch
        {
            return Colors.Gray;
        }
    }

    public static void EnsureDefaultBrushes()
    {
        if (Application.Current == null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var pair in DefaultHex)
            {
                if (!Application.Current.Resources.Contains(pair.Key))
                    SetBrush(pair.Key, pair.Value);
            }
            EnsureDerivedBrushes(
                ParseColorOrDefault(GetBrushHex("AppSecondaryBrush"), DefaultHex["AppSecondaryBrush"]));
        });
    }

    private static string GetBrushHex(string key)
    {
        if (Application.Current?.Resources[key] is SolidColorBrush brush)
            return brush.Color.ToString();
        return DefaultHex.TryGetValue(key, out var hex) ? hex : "#000000";
    }

    private static void EnsureDerivedBrushes(Color secondary)
    {
        if (Application.Current == null) return;
        Application.Current.Resources["AppSecondaryMutedBrush"] = new SolidColorBrush(Color.FromArgb(20, secondary.R, secondary.G, secondary.B));
        Application.Current.Resources["AppSecondaryHighlightBrush"] = new SolidColorBrush(Color.FromArgb(38, secondary.R, secondary.G, secondary.B));
        Application.Current.Resources["AppAccentMutedBrush"] = new SolidColorBrush(Color.FromArgb(30, GetAccentColor().R, GetAccentColor().G, GetAccentColor().B));
    }

    private static Color GetAccentColor()
    {
        if (Application.Current?.Resources["AppAccentBrush"] is SolidColorBrush brush)
            return brush.Color;
        return ParseColorOrDefault(null, DefaultHex["AppAccentBrush"]);
    }
    public static void ApplyTheme(int companyId)
    {
        try
        {
            var setting = AppearanceSettingsStore.LoadGlobal();
            CompanyThemeData? companyTheme = companyId > 0
                ? AppearanceSettingsStore.LoadCompanyTheme(companyId)
                : null;
            ApplyThemeColors(setting, companyTheme);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[ThemeManager] Failed to load or apply theme", ex);
        }
    }

    public static void ApplyThemeColors(AppearanceSetting setting, CompanyThemeData? companyTheme = null)
    {
        if (Application.Current == null) return;

        string primary = setting.PrimaryColor;
        string secondary = setting.SecondaryColor;
        string accent = setting.AccentColor;
        string sidebarBg = setting.SidebarBackground;
        string sidebarText = setting.SidebarTextColor;
        string buttonBg = setting.ButtonColor;
        string buttonText = setting.ButtonTextColor;
        string cardBg = setting.CardBackground;
        string pageBg = setting.PageBackground;

        if (companyTheme != null && companyTheme.ApplyCompanyTheme)
        {
            if (!string.IsNullOrWhiteSpace(companyTheme.ThemePrimaryColor))
                primary = companyTheme.ThemePrimaryColor;
            if (!string.IsNullOrWhiteSpace(companyTheme.ThemeSecondaryColor))
                secondary = companyTheme.ThemeSecondaryColor;
            if (!string.IsNullOrWhiteSpace(companyTheme.ThemeAccentColor))
                accent = companyTheme.ThemeAccentColor;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            SetBrush("AppPrimaryBrush", primary);
            SetBrush("AppSecondaryBrush", secondary);
            SetBrush("AppAccentBrush", accent);
            SetBrush("AppSidebarBackgroundBrush", sidebarBg);
            SetBrush("AppSidebarTextColorBrush", sidebarText);
            SetBrush("AppButtonBrush", buttonBg);
            SetBrush("AppButtonTextColorBrush", buttonText);
            SetBrush("AppCardBackgroundBrush", cardBg);
            SetBrush("AppPageBackgroundBrush", pageBg);
            var primaryColor = ParseColorOrDefault(primary, DefaultHex["AppPrimaryBrush"]);
            var secondaryColor = ParseColorOrDefault(secondary, DefaultHex["AppSecondaryBrush"]);
            var sidebarColor = ParseColorOrDefault(sidebarBg, DefaultHex["AppSidebarBackgroundBrush"]);
            var pageColor = ParseColorOrDefault(pageBg, DefaultHex["AppPageBackgroundBrush"]);
            var cardColor = ParseColorOrDefault(cardBg, DefaultHex["AppCardBackgroundBrush"]);
            var isDark = RelativeLuminance(pageColor) < 0.24 || RelativeLuminance(cardColor) < 0.24;

            // Material Design owns the control templates. Apply its base palette first,
            // then publish our semantic brushes so custom appearance colors win.
            ApplyMaterialColors(primary, secondary, isDark);
            SetBrush("BrandBackground", pageBg);
            SetBrush("BrandSurface", cardBg);
            SetBrush("BrandSurfaceSecondary", ToHex(Blend(cardColor, pageColor, 0.55)));
            SetBrush("BrandTextPrimary", isDark ? KarzounBrand.DarkText : KarzounBrand.Navy);
            SetBrush("BrandTextSecondary", isDark ? "#B5BDCA" : "#5E6877");
            SetBrush("BrandBorder", isDark ? "#344054" : KarzounBrand.PdfBorder);
            SetBrush("BrandAccent", ToHex(primaryColor));
            SetBrush("BrandAccentHover", ToHex(Adjust(primaryColor, isDark ? 0.12 : -0.12)));
            SetBrush("BrandAccentPressed", ToHex(Adjust(primaryColor, isDark ? 0.20 : -0.22)));
            SetBrush("BrandNavigationBackground", ToHex(sidebarColor));
            SetBrush("BrandNavigationSelected", ToHex(Blend(sidebarColor, primaryColor, 0.22)));
            SetBrush("BrandNavigationHover", ToHex(Blend(sidebarColor, Colors.White, 0.07)));
            SetBrush("BrandNavigationText", sidebarText);
            SetBrush("BrandNavigationTextMuted", ToHex(Blend(sidebarColor, ParseColorOrDefault(sidebarText, KarzounBrand.LightGray), 0.68)));
            SetBrush("BrandFocus", KarzounBrand.Blue);

            // Root-level overrides keep Material cards, dialogs and standard text
            // consistent with the selected light/dark surfaces.
            SetBrush("MaterialDesignPaper", cardBg);
            SetBrush("MaterialDesignCardBackground", cardBg);
            SetBrush("MaterialDesignBackground", pageBg);
            SetBrush("MaterialDesignBody", isDark ? KarzounBrand.DarkText : KarzounBrand.Navy);
            SetBrush("MaterialDesignBodyLight", isDark ? "#B5BDCA" : "#5E6877");
            SetBrush("MaterialDesignDivider", isDark ? "#344054" : KarzounBrand.PdfBorder);
            Application.Current.Resources["AppIsDarkTheme"] = isDark;
            EnsureDerivedBrushes(secondaryColor);
        });
    }

    private static void SetBrush(string name, string hex)
    {
        if (Application.Current == null) return;
        var fallback = DefaultHex.TryGetValue(name, out var defaultHex) ? defaultHex : "#000000";
        var color = ParseColorOrDefault(hex, fallback);
        Application.Current.Resources[name] = new SolidColorBrush(color);
    }

    private static void ApplyMaterialColors(string primaryHex, string secondaryHex, bool isDark)
    {
        try
        {
            var paletteHelper = new MaterialDesignThemes.Wpf.PaletteHelper();
            var theme = paletteHelper.GetTheme();
            
            var primaryCol = (Color)ColorConverter.ConvertFromString(primaryHex);
            var secondaryCol = (Color)ColorConverter.ConvertFromString(secondaryHex);
            
            theme.SetPrimaryColor(primaryCol);
            theme.SetSecondaryColor(secondaryCol);
            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
            
            paletteHelper.SetTheme(theme);
        }
        catch { }
    }

    private static double RelativeLuminance(Color color)
    {
        static double Channel(byte value)
        {
            var s = value / 255d;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
    }

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte Mix(byte a, byte b) => (byte)Math.Round(a + ((b - a) * amount));
        return Color.FromRgb(Mix(from.R, to.R), Mix(from.G, to.G), Mix(from.B, to.B));
    }

    private static Color Adjust(Color color, double amount)
        => Blend(color, amount >= 0 ? Colors.White : Colors.Black, Math.Abs(amount));

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
