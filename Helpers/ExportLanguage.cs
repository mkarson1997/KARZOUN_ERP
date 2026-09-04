namespace KarzounERP.Helpers;

/// <summary>Central rules for PDF/Excel export language selection.</summary>
public static class ExportLanguage
{
    public static string? Normalize(string? code) =>
        code is "ar" or "tr" or "en" ? code : null;

    /// <summary>List/grid export: always current app language (ignore stored LanguageCode).</summary>
    public static string ForListExport() => LocalizationManager.Language;

    /// <summary>Document form export: form selector, else app language.</summary>
    public static string ForFormExport(string? formLanguageCode) =>
        Normalize(formLanguageCode) ?? LocalizationManager.Language;

    /// <summary>New/saved documents: app language unless form explicitly sets a valid code.</summary>
    public static string ForNewDocument(string? formLanguageCode) =>
        ForFormExport(formLanguageCode);
}
