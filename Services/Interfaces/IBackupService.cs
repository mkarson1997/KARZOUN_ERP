namespace FornixxCRM.Services.Interfaces;

public interface IBackupService
{
    string BackupDatabase(string targetFolder);
    bool RestoreDatabase(string backupFile);
    string GetDatabasePath();
}
