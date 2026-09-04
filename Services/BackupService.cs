using KarzounERP.Helpers;
using KarzounERP.Services.Interfaces;

namespace KarzounERP.Services;

public class BackupService : IBackupService
{
    public string GetDatabasePath()
    {
        return AppPaths.DatabasePath;
    }

    public string BackupDatabase(string targetFolder)
    {
        var source = GetDatabasePath();
        if (!File.Exists(source))
            throw new FileNotFoundException(LocalizationManager.Get("Msg_DbFileMissing"), source);

        var backupName = $"KARZOUN_ERP_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db";

        try
        {
            return WriteBackupToFolder(source, targetFolder, backupName);
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        {
            var fallbackFolder = AppPaths.BackupsDirectory;
            AppLogger.LogError(
                $"[Backup] Configured backup folder '{targetFolder}' is unavailable. Falling back to default backup folder: {fallbackFolder}",
                ex);
            var destination = WriteBackupToFolder(source, fallbackFolder, backupName);
            AppLogger.LogInfo($"[Backup] Fallback backup written to default folder: {destination}");
            return destination;
        }
    }

    private static string WriteBackupToFolder(string source, string targetFolder, string backupName)
    {
        Directory.CreateDirectory(targetFolder);
        var destination = Path.Combine(targetFolder, backupName);
        File.Copy(source, destination, overwrite: false);
        return destination;
    }

    public bool RestoreDatabase(string backupFile)
    {
        return RestoreDatabaseToPath(backupFile, GetDatabasePath());
    }

    public bool RestoreDatabaseToPath(string backupFile, string destination)
    {
        if (!File.Exists(backupFile)) return false;
        var destinationFolder = Path.GetDirectoryName(destination);
        if (string.IsNullOrEmpty(destinationFolder))
        {
            destinationFolder = AppContext.BaseDirectory;
        }

        var safetyDir = Path.Combine(destinationFolder, "RestoreSafetyBackups");
        Directory.CreateDirectory(safetyDir);
        var destFileName = Path.GetFileNameWithoutExtension(destination);
        var emergencyBackupPath = Path.Combine(safetyDir, $"{destFileName}_emergency_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db");

        // 1. Create emergency backup of current database
        bool emergencyBackupCreated = false;
        if (File.Exists(destination))
        {
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                File.Copy(destination, emergencyBackupPath, overwrite: true);
                emergencyBackupCreated = true;
                AppLogger.LogInfo($"[RestoreSafety] Emergency backup created at: {emergencyBackupPath}");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("[RestoreSafety] Failed to create emergency backup before restore.", ex);
                return false;
            }
        }

        var tempDbPath = Path.Combine(destinationFolder, $"{destFileName}_temp_restore.db");
        try
        {
            if (File.Exists(tempDbPath))
            {
                File.Delete(tempDbPath);
            }

            // 2. Verify source backup file integrity
            if (!CheckDatabaseIntegrity(backupFile))
            {
                AppLogger.LogError($"[RestoreSafety] Source backup file is corrupted or not a valid sqlite database: {backupFile}");
                return false;
            }

            // 3. Copy source backup to temp path
            File.Copy(backupFile, tempDbPath, overwrite: true);

            // 4. Initialize and migrate the temp database
            KarzounERP.Data.DatabaseInitializer.InitializeRestoredDatabase(tempDbPath);

            // 5. Verify the migrated temp database integrity
            if (!CheckDatabaseIntegrity(tempDbPath))
            {
                AppLogger.LogError("[RestoreSafety] Migrated temporary database failed integrity check.");
                if (File.Exists(tempDbPath)) File.Delete(tempDbPath);
                return false;
            }

            // 6. Copy temp database to destination
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            File.Copy(tempDbPath, destination, overwrite: true);

            if (File.Exists(tempDbPath)) File.Delete(tempDbPath);
            AppLogger.LogInfo("[RestoreSafety] Restore completed successfully.");
            return true;
        }
        catch (Exception restoreEx)
        {
            AppLogger.LogError("[RestoreSafety] Restore failed. Attempting rollback to emergency backup.", restoreEx);
            
            try { if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { }

            // 7. Rollback: restore the emergency backup
            if (emergencyBackupCreated && File.Exists(emergencyBackupPath))
            {
                try
                {
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    File.Copy(emergencyBackupPath, destination, overwrite: true);
                    AppLogger.LogInfo("[RestoreSafety] Rollback to emergency backup succeeded.");
                }
                catch (Exception rollbackEx)
                {
                    AppLogger.LogError("[RestoreSafety] Rollback to emergency backup failed. Main database might be unusable.", rollbackEx);
                }
            }
            return false;
        }
    }


    private static bool CheckDatabaseIntegrity(string file)
    {
        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={file};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            return cmd.ExecuteScalar() as string == "ok";
        }
        catch
        {
            return false;
        }
    }

    public string ResolveBackupFolder(string? savedFolder)
    {
        if (!string.IsNullOrWhiteSpace(savedFolder) && !IsOldDefaultBackupFolder(savedFolder))
        {
            return savedFolder;
        }

        string defaultPath = Path.Combine(AppContext.BaseDirectory, "النسخ الاحتياطي");
        if (IsDirectoryWritable(defaultPath))
        {
            return defaultPath;
        }

        string appDataPath = AppPaths.BackupsDirectory;
        return appDataPath;
    }

    private static bool IsOldDefaultBackupFolder(string folder)
    {
        try
        {
            var fullPath = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var oldDefault = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "backup"))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return string.Equals(fullPath, oldDefault, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileName(fullPath), "backup", StringComparison.OrdinalIgnoreCase)
                    && fullPath.StartsWith(Path.GetFullPath(AppContext.BaseDirectory), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private bool IsDirectoryWritable(string dirPath)
    {
        try
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            string testFile = Path.Combine(dirPath, Guid.NewGuid().ToString() + ".tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
