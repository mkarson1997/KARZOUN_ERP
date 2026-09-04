using Microsoft.Data.Sqlite;

namespace KarzounERP.Helpers;

public sealed record AppDataMigrationResult(
    bool LegacyDataFound,
    bool DatabaseCopied,
    int ArchivedDuplicateRows,
    int FilesCopied,
    string SourceDirectory,
    string DestinationDirectory);

/// <summary>
/// Copies legacy per-user data into the KARZOUN ERP location without modifying
/// the source. The operation is idempotent: existing destination files always win.
/// </summary>
public static class AppDataMigration
{
    public static AppDataMigrationResult EnsureMigrated(string? applicationDataRoot = null)
    {
        var root = applicationDataRoot ?? AppPaths.ApplicationDataRoot;
        var legacyRoot = AppPaths.GetLegacyDataRoot(root);
        var destinationRoot = AppPaths.GetDataRoot(root);
        var legacyDatabase = Path.Combine(legacyRoot, AppPaths.LegacyDatabaseFileName);
        var destinationDatabase = Path.Combine(destinationRoot, AppPaths.DatabaseFileName);

        Directory.CreateDirectory(destinationRoot);

        if (!Directory.Exists(legacyRoot))
            return new AppDataMigrationResult(false, false, 0, 0, legacyRoot, destinationRoot);

        var databaseCopied = false;
        var archivedDuplicateRows = 0;
        if (!File.Exists(destinationDatabase) && File.Exists(legacyDatabase))
        {
            archivedDuplicateRows = CopyDatabaseAtomically(legacyDatabase, destinationDatabase);
            databaseCopied = true;
        }

        var filesCopied = CopyMissingFiles(legacyRoot, destinationRoot);
        return new AppDataMigrationResult(
            true,
            databaseCopied,
            archivedDuplicateRows,
            filesCopied,
            legacyRoot,
            destinationRoot);
    }

    private static int CopyMissingFiles(string sourceRoot, string destinationRoot)
    {
        var copied = 0;
        foreach (var sourceFile in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
            if (IsPrimaryLegacyDatabaseFile(relativePath))
                continue;

            var destinationFile = Path.Combine(destinationRoot, relativePath);
            if (File.Exists(destinationFile))
                continue;

            var destinationDirectory = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            File.Copy(sourceFile, destinationFile, overwrite: false);
            copied++;
        }

        return copied;
    }

    private static bool IsPrimaryLegacyDatabaseFile(string relativePath)
    {
        if (relativePath.Contains(Path.DirectorySeparatorChar) ||
            relativePath.Contains(Path.AltDirectorySeparatorChar))
        {
            return false;
        }

        return string.Equals(relativePath, AppPaths.LegacyDatabaseFileName, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(relativePath, AppPaths.LegacyDatabaseFileName + "-wal", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(relativePath, AppPaths.LegacyDatabaseFileName + "-shm", StringComparison.OrdinalIgnoreCase);
    }

    private static int CopyDatabaseAtomically(string sourcePath, string destinationPath)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("The KARZOUN ERP database directory could not be resolved.");
        Directory.CreateDirectory(destinationDirectory);

        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".{AppPaths.DatabaseFileName}.{Guid.NewGuid():N}.migration.tmp");

        var archivedDuplicateRows = 0;
        try
        {
            using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = sourcePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString()))
            using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = temporaryPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString()))
            {
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);
                archivedDuplicateRows = NormalizeAndRebuildIndexes(destination);
            }

            using (var verification = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = temporaryPath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString()))
            {
                verification.Open();
                EnsureIntegrity(verification, "migrated");
            }

            File.Move(temporaryPath, destinationPath);
            return archivedDuplicateRows;
        }
        catch
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // A failed cleanup must never hide the migration failure.
            }

            throw;
        }
    }

    private static void EnsureIntegrity(SqliteConnection connection, string label)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var result = command.ExecuteScalar() as string;
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"The {label} database failed SQLite integrity validation.");
    }

    private static int NormalizeAndRebuildIndexes(SqliteConnection connection)
    {
        if (!TableExists(connection, "CompanyLocalizedSettings"))
        {
            using var reindexOnly = connection.CreateCommand();
            reindexOnly.CommandText = "REINDEX;";
            reindexOnly.ExecuteNonQuery();
            return 0;
        }

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = """
            SELECT COALESCE(SUM(DuplicateCount - 1), 0)
            FROM (
                SELECT COUNT(*) AS DuplicateCount
                FROM CompanyLocalizedSettings NOT INDEXED
                GROUP BY CompanyId, LanguageCode
                HAVING COUNT(*) > 1
            );
            """;
        var duplicateCount = Convert.ToInt32(countCommand.ExecuteScalar());

        if (duplicateCount > 0)
        {
            using var transaction = connection.BeginTransaction();
            using var repair = connection.CreateCommand();
            repair.Transaction = transaction;
            repair.CommandText = """
                DROP INDEX IF EXISTS IX_CompanyLocalizedSettings_CompanyId_LanguageCode;

                CREATE TABLE IF NOT EXISTS DataMigrationArchive_CompanyLocalizedSettings (
                    ArchiveId INTEGER PRIMARY KEY AUTOINCREMENT,
                    OriginalId INTEGER NOT NULL UNIQUE,
                    CompanyId INTEGER NOT NULL,
                    LanguageCode TEXT NOT NULL,
                    DefaultInvoiceNotes TEXT,
                    DefaultQuotationNotes TEXT,
                    LegalFooterText TEXT,
                    DefaultPaymentDetails TEXT,
                    QrTemplateText TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    ArchivedAtUtc TEXT NOT NULL,
                    ArchiveReason TEXT NOT NULL
                );

                INSERT OR IGNORE INTO DataMigrationArchive_CompanyLocalizedSettings (
                    OriginalId, CompanyId, LanguageCode, DefaultInvoiceNotes,
                    DefaultQuotationNotes, LegalFooterText, DefaultPaymentDetails,
                    QrTemplateText, CreatedAt, UpdatedAt, ArchivedAtUtc, ArchiveReason
                )
                SELECT duplicate.Id, duplicate.CompanyId, duplicate.LanguageCode,
                       duplicate.DefaultInvoiceNotes, duplicate.DefaultQuotationNotes,
                       duplicate.LegalFooterText, duplicate.DefaultPaymentDetails,
                       duplicate.QrTemplateText, duplicate.CreatedAt, duplicate.UpdatedAt,
                       CURRENT_TIMESTAMP, 'Duplicate localized setting preserved during KARZOUN ERP migration'
                FROM CompanyLocalizedSettings AS duplicate NOT INDEXED
                WHERE EXISTS (
                    SELECT 1
                    FROM CompanyLocalizedSettings AS keeper NOT INDEXED
                    WHERE keeper.CompanyId = duplicate.CompanyId
                      AND keeper.LanguageCode = duplicate.LanguageCode
                      AND keeper.Id < duplicate.Id
                );

                DELETE FROM CompanyLocalizedSettings
                WHERE Id IN (
                    SELECT duplicate.Id
                    FROM CompanyLocalizedSettings AS duplicate NOT INDEXED
                    WHERE EXISTS (
                        SELECT 1
                        FROM CompanyLocalizedSettings AS keeper NOT INDEXED
                        WHERE keeper.CompanyId = duplicate.CompanyId
                          AND keeper.LanguageCode = duplicate.LanguageCode
                          AND keeper.Id < duplicate.Id
                    )
                );

                CREATE UNIQUE INDEX IX_CompanyLocalizedSettings_CompanyId_LanguageCode
                ON CompanyLocalizedSettings (CompanyId, LanguageCode);
                """;
            repair.ExecuteNonQuery();
            transaction.Commit();
        }

        using var reindex = connection.CreateCommand();
        reindex.CommandText = "REINDEX;";
        reindex.ExecuteNonQuery();
        return duplicateCount;
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}
