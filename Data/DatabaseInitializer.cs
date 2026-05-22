using FornixxCRM.Helpers;
using FornixxCRM.Models;
using Microsoft.Data.Sqlite;

namespace FornixxCRM.Data;

public static class DatabaseInitializer
{
    public static void Initialize(AppDbContext context, string dbPath)
    {
        // Let EF Core create the schema for any brand-new database.
        context.Database.EnsureCreated();

        // Apply column additions to existing databases using a fresh ADO.NET connection
        // that is completely independent of EF Core's internal connection management.
        ApplySchemaUpdates(dbPath);

        // Remove companies with empty/null/placeholder names (bad data from before validation was added).
        // Only removes companies with no associated customers, documents, or products.
        var badCompanies = context.Companies
            .AsEnumerable()
            .Where(c => string.IsNullOrWhiteSpace(c.Name) ||
                        c.Name.Trim().Equals("....", StringComparison.OrdinalIgnoreCase))
            .ToList()
            .Where(c => !context.Customers.Any(x => x.CompanyId == c.Id)
                     && !context.Documents.Any(x => x.CompanyId == c.Id)
                     && !context.Products.Any(x => x.CompanyId == c.Id))
            .ToList();
        if (badCompanies.Count > 0)
        {
            context.Companies.RemoveRange(badCompanies);
            context.SaveChanges();
        }

        if (!context.Companies.Any())
        {
            var defaultCompany = new Company
            {
                Name = "شركتي الأولى",
                CommercialName = "شركتي الأولى",
                Currency = "USD",
                InvoicePrefix = "INV",
                QuotationPrefix = "QUO",
                NextInvoiceNumber = 1,
                NextQuotationNumber = 1,
                TaxEnabled = false,
                TaxRate = 0,
                FooterText = "المبلغ لا يشمل الشحن إلا إذا تم ذكر ذلك صراحة.",
                DefaultInvoiceNotes = "شكراً لتعاملكم معنا.",
                DefaultQuotationNotes = "هذا العرض صالح لمدة 30 يوماً من تاريخه.",
                CreatedAt = DateTime.UtcNow
            };
            context.Companies.Add(defaultCompany);
            context.SaveChanges();
        }
    }

    private static void ApplySchemaUpdates(string dbPath)
    {
        // Fresh connection — nothing shared with EF Core's internal pooling.
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        var existing = GetColumns(conn, "Documents");

        if (!existing.Contains("PaidAmount"))
            Exec(conn, "ALTER TABLE Documents ADD COLUMN PaidAmount REAL NOT NULL DEFAULT 0");

        if (!existing.Contains("PaymentDate"))
            Exec(conn, "ALTER TABLE Documents ADD COLUMN PaymentDate TEXT");

        if (!existing.Contains("LanguageCode"))
            Exec(conn, "ALTER TABLE Documents ADD COLUMN LanguageCode TEXT NOT NULL DEFAULT 'ar'");

        var existingCompanies = GetColumns(conn, "Companies");
        if (!existingCompanies.Contains("NumberPadding"))
            Exec(conn, "ALTER TABLE Companies ADD COLUMN NumberPadding INTEGER NOT NULL DEFAULT 4");

        if (!existingCompanies.Contains("StampPath"))
            Exec(conn, "ALTER TABLE Companies ADD COLUMN StampPath TEXT");

        if (!existingCompanies.Contains("QrCodeTemplate"))
            Exec(conn, "ALTER TABLE Companies ADD COLUMN QrCodeTemplate TEXT");

        if (!existingCompanies.Contains("AppPassword"))
            Exec(conn, "ALTER TABLE Companies ADD COLUMN AppPassword TEXT");

        var existingCustomers = GetColumns(conn, "Customers");
        if (!existingCustomers.Contains("NextFollowUpDate"))
            Exec(conn, "ALTER TABLE Customers ADD COLUMN NextFollowUpDate TEXT");

        Exec(conn, "CREATE TABLE IF NOT EXISTS CustomerNotes (Id INTEGER PRIMARY KEY AUTOINCREMENT, CustomerId INTEGER NOT NULL, NoteText TEXT NOT NULL, CreatedAt TEXT NOT NULL, FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE)");
    }

    private static HashSet<string> GetColumns(SqliteConnection conn, string table)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            cols.Add(reader.GetString(1)); // index 1 = column name in PRAGMA table_info
        return cols;
    }

    private static void Exec(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Schema migration failed: {sql}", ex);
        }
    }
}
