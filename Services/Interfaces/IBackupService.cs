namespace KarzounERP.Services.Interfaces;

public interface IBackupService
{
    string BackupDatabase(string targetFolder);
    bool RestoreDatabase(string backupFile);
    bool RestoreDatabaseToPath(string backupFile, string destination);
    string GetDatabasePath();
    string ResolveBackupFolder(string? savedFolder);
}

