using FornixxCRM.Helpers;
using FornixxCRM.Services.Interfaces;

namespace FornixxCRM.Services;

public class BackupService : IBackupService
{
    public string GetDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "FornixxCRM", "fornixx.db");
    }

    public string BackupDatabase(string targetFolder)
    {
        var source = GetDatabasePath();
        if (!File.Exists(source))
            throw new FileNotFoundException(LocalizationManager.Get("Msg_DbFileMissing"), source);

        Directory.CreateDirectory(targetFolder);
        var backupName = $"fornixx_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var destination = Path.Combine(targetFolder, backupName);
        File.Copy(source, destination, overwrite: false);
        return destination;
    }

    public bool RestoreDatabase(string backupFile)
    {
        if (!File.Exists(backupFile)) return false;
        var destination = GetDatabasePath();
        try
        {
            // Close all EF connections by disposing the app context is handled by caller
            File.Copy(backupFile, destination, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
