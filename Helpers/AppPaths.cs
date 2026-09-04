namespace KarzounERP.Helpers;

/// <summary>
/// Central product and per-user storage paths. Keeping these values in one place
/// prevents future branding changes from silently splitting application data.
/// </summary>
public static class AppPaths
{
    public const string ProductName = "KARZOUN ERP";
    public const string DataFolderName = "KARZOUN ERP";
    public const string DatabaseFileName = "karzoun_erp.db";

    // Compatibility identifiers from releases up to 1.1.0. These must remain so
    // existing customers can be migrated without renaming or deleting old data.
    public const string LegacyDataFolderName = "FornixxCRM";
    public const string LegacyDatabaseFileName = "fornixx.db";

    public static string ApplicationDataRoot =>
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static string DataRoot => GetDataRoot(ApplicationDataRoot);
    public static string LegacyDataRoot => GetLegacyDataRoot(ApplicationDataRoot);
    public static string DatabasePath => Path.Combine(DataRoot, DatabaseFileName);
    public static string LegacyDatabasePath => Path.Combine(LegacyDataRoot, LegacyDatabaseFileName);
    public static string LogsDirectory => Path.Combine(DataRoot, "Logs");
    public static string AppearanceSettingsPath => Path.Combine(DataRoot, "appearance.json");
    public static string ProductImagesDirectory => Path.Combine(DataRoot, "ProductImages");
    public static string BackupsDirectory => Path.Combine(DataRoot, "Backups");

    public static string GetDataRoot(string applicationDataRoot) =>
        Path.Combine(applicationDataRoot, DataFolderName);

    public static string GetLegacyDataRoot(string applicationDataRoot) =>
        Path.Combine(applicationDataRoot, LegacyDataFolderName);
}
