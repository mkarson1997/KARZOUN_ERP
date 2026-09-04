using KarzounERP.Models;
using System.IO;
using System.Text.Json;

namespace KarzounERP.Helpers;

public class CompanyThemeData
{
    public string ThemePrimaryColor { get; set; } = KarzounBrand.Teal;
    public string ThemeSecondaryColor { get; set; } = KarzounBrand.Blue;
    public string ThemeAccentColor { get; set; } = KarzounBrand.Emerald;
    public bool ApplyCompanyTheme { get; set; }
}

internal sealed class AppearanceStoreData
{
    public AppearanceSetting Global { get; set; } = new();
    public Dictionary<int, CompanyThemeData> Companies { get; set; } = new();
}

public static class AppearanceSettingsStore
{
    private static readonly string StorePath = AppPaths.AppearanceSettingsPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static AppearanceSetting LoadGlobal()
    {
        var data = Load();
        return CloneAppearance(data.Global);
    }

    public static CompanyThemeData LoadCompanyTheme(int companyId)
    {
        var data = Load();
        if (data.Companies.TryGetValue(companyId, out var theme))
            return CloneCompanyTheme(theme);
        return new CompanyThemeData();
    }

    public static void SaveGlobal(AppearanceSetting setting)
    {
        var data = Load();
        data.Global = CloneAppearance(setting);
        Save(data);
    }

    public static void SaveCompanyTheme(int companyId, CompanyThemeData theme)
    {
        var data = Load();
        data.Companies[companyId] = CloneCompanyTheme(theme);
        Save(data);
    }

    private static AppearanceStoreData Load()
    {
        try
        {
            if (!File.Exists(StorePath))
                return new AppearanceStoreData();

            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<AppearanceStoreData>(json, JsonOptions) ?? new AppearanceStoreData();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[AppearanceSettingsStore] Failed to load appearance settings", ex);
            return new AppearanceStoreData();
        }
    }

    private static void Save(AppearanceStoreData data)
    {
        try
        {
            var folder = Path.GetDirectoryName(StorePath)!;
            Directory.CreateDirectory(folder);
            File.WriteAllText(StorePath, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[AppearanceSettingsStore] Failed to save appearance settings", ex);
            throw;
        }
    }

    private static AppearanceSetting CloneAppearance(AppearanceSetting source) => new()
    {
        Id = source.Id,
        PrimaryColor = source.PrimaryColor,
        SecondaryColor = source.SecondaryColor,
        AccentColor = source.AccentColor,
        SidebarBackground = source.SidebarBackground,
        SidebarTextColor = source.SidebarTextColor,
        ButtonColor = source.ButtonColor,
        ButtonTextColor = source.ButtonTextColor,
        CardBackground = source.CardBackground,
        PageBackground = source.PageBackground,
        PdfPrimaryColor = source.PdfPrimaryColor,
        PdfHeaderColor = source.PdfHeaderColor,
        PdfTableHeaderColor = source.PdfTableHeaderColor,
        PdfBorderColor = source.PdfBorderColor,
        PdfAccentColor = source.PdfAccentColor,
        PdfTotalBoxColor = source.PdfTotalBoxColor,
        PdfCompanyInfoTopMargin = source.PdfCompanyInfoTopMargin,
        PdfLogoTopMargin = source.PdfLogoTopMargin,
        PdfHeaderSpacing = source.PdfHeaderSpacing,
        PdfTableSpacing = source.PdfTableSpacing,
        PdfFontSize = source.PdfFontSize
    };

    private static CompanyThemeData CloneCompanyTheme(CompanyThemeData source) => new()
    {
        ThemePrimaryColor = source.ThemePrimaryColor,
        ThemeSecondaryColor = source.ThemeSecondaryColor,
        ThemeAccentColor = source.ThemeAccentColor,
        ApplyCompanyTheme = source.ApplyCompanyTheme
    };
}
