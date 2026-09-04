using KarzounERP.Helpers;
using KarzounERP.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KarzounERP.Data;

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

        MigrateCompanyLocalizedSettings(context);
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

        if (!existingCompanies.Contains("ShowProductImageInQuotation"))
            Exec(conn, "ALTER TABLE Companies ADD COLUMN ShowProductImageInQuotation INTEGER NOT NULL DEFAULT 0");

        if (!existingCompanies.Contains("ShowCustomerContactInPdf"))
            Exec(conn, "ALTER TABLE Companies ADD COLUMN ShowCustomerContactInPdf INTEGER NOT NULL DEFAULT 0");

        if (!existingCompanies.Contains("AutoBackupEnabled"))
            Exec(conn, "ALTER TABLE Companies ADD COLUMN AutoBackupEnabled INTEGER NOT NULL DEFAULT 0");

        if (!existingCompanies.Contains("AutoBackupIntervalMinutes"))
            Exec(conn, "ALTER TABLE Companies ADD COLUMN AutoBackupIntervalMinutes INTEGER NOT NULL DEFAULT 30");

        if (!existingCompanies.Contains("BackupFolder"))
            Exec(conn, "ALTER TABLE Companies ADD COLUMN BackupFolder TEXT");

        var existingProducts = GetColumns(conn, "Products");
        if (!existingProducts.Contains("ImagePath"))
            Exec(conn, "ALTER TABLE Products ADD COLUMN ImagePath TEXT");
        if (!existingProducts.Contains("WeightUnit"))
            Exec(conn, "ALTER TABLE Products ADD COLUMN WeightUnit TEXT NOT NULL DEFAULT 'kg'");

        var existingCustomers = GetColumns(conn, "Customers");
        if (!existingCustomers.Contains("NextFollowUpDate"))
            Exec(conn, "ALTER TABLE Customers ADD COLUMN NextFollowUpDate TEXT");
        if (!existingCustomers.Contains("ColorMarker"))
            Exec(conn, "ALTER TABLE Customers ADD COLUMN ColorMarker TEXT");

        var existingItems = GetColumns(conn, "DocumentItems");
        if (!existingItems.Contains("ProductId"))
            Exec(conn, "ALTER TABLE DocumentItems ADD COLUMN ProductId INTEGER");

        if (!existingItems.Contains("ImagePath"))
            Exec(conn, "ALTER TABLE DocumentItems ADD COLUMN ImagePath TEXT");
        if (!existingItems.Contains("WeightUnit"))
            Exec(conn, "ALTER TABLE DocumentItems ADD COLUMN WeightUnit TEXT NOT NULL DEFAULT 'kg'");

        Exec(conn, "CREATE TABLE IF NOT EXISTS CustomerNotes (Id INTEGER PRIMARY KEY AUTOINCREMENT, CustomerId INTEGER NOT NULL, NoteText TEXT NOT NULL, CreatedAt TEXT NOT NULL, FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE)");

        Exec(conn, "CREATE TABLE IF NOT EXISTS ProductLocalizedTexts (" +
                   "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                   "ProductId INTEGER NOT NULL, " +
                   "LanguageCode TEXT NOT NULL, " +
                   "Name TEXT NOT NULL, " +
                   "Description TEXT, " +
                   "CreatedAt TEXT NOT NULL, " +
                   "UpdatedAt TEXT NOT NULL, " +
                   "FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE)");

        Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_ProductLocalizedTexts_ProductId_LanguageCode ON ProductLocalizedTexts (ProductId, LanguageCode)");

        Exec(conn, "CREATE TABLE IF NOT EXISTS CompanyLocalizedSettings (" +
                   "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                   "CompanyId INTEGER NOT NULL, " +
                   "LanguageCode TEXT NOT NULL, " +
                   "DefaultInvoiceNotes TEXT, " +
                   "DefaultQuotationNotes TEXT, " +
                   "LegalFooterText TEXT, " +
                   "DefaultPaymentDetails TEXT, " +
                   "QrTemplateText TEXT, " +
                   "CreatedAt TEXT NOT NULL, " +
                   "UpdatedAt TEXT NOT NULL, " +
                   "FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE)");

        Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_CompanyLocalizedSettings_CompanyId_LanguageCode ON CompanyLocalizedSettings (CompanyId, LanguageCode)");
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

    private static void MigrateCompanyLocalizedSettings(AppDbContext context)
    {
        var companies = context.Companies.ToList();
        foreach (var company in companies)
        {
            var existingSettings = context.CompanyLocalizedSettings
                .Where(s => s.CompanyId == company.Id)
                .ToList();

            if (existingSettings.Count == 0)
            {
                var texts = new[] { company.DefaultInvoiceNotes, company.DefaultQuotationNotes, company.FooterText, company.PaymentInfo, company.QrCodeTemplate };
                string targetLang = "ar";
                if (HasArabic(texts))
                {
                    targetLang = "ar";
                }
                else if (HasTurkish(texts))
                {
                    targetLang = "tr";
                }
                else if (texts.Any(t => !string.IsNullOrWhiteSpace(t)))
                {
                    targetLang = "en";
                }

                var arNotes = targetLang == "ar" ? company.DefaultInvoiceNotes : null;
                var arQuo = targetLang == "ar" ? company.DefaultQuotationNotes : null;
                var arFooter = targetLang == "ar" ? company.FooterText : null;
                var arPay = targetLang == "ar" ? company.PaymentInfo : null;
                var arQr = targetLang == "ar" ? company.QrCodeTemplate : null;

                var arSetting = new CompanyLocalizedSetting
                {
                    CompanyId = company.Id,
                    LanguageCode = "ar",
                    DefaultInvoiceNotes = !string.IsNullOrWhiteSpace(arNotes) ? arNotes : "شكراً لتعاملكم معنا.",
                    DefaultQuotationNotes = !string.IsNullOrWhiteSpace(arQuo) ? arQuo : "هذا العرض صالح لمدة 30 يوماً من تاريخه.",
                    LegalFooterText = !string.IsNullOrWhiteSpace(arFooter) ? arFooter : "المبلغ لا يشمل الشحن إلا إذا تم ذكر ذلك صراحة.",
                    DefaultPaymentDetails = !string.IsNullOrWhiteSpace(arPay) ? arPay : "بيانات الدفع الافتراضية",
                    QrTemplateText = arQr ?? company.QrCodeTemplate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var trNotes = targetLang == "tr" ? company.DefaultInvoiceNotes : null;
                var trQuo = targetLang == "tr" ? company.DefaultQuotationNotes : null;
                var trFooter = targetLang == "tr" ? company.FooterText : null;
                var trPay = targetLang == "tr" ? company.PaymentInfo : null;
                var trQr = targetLang == "tr" ? company.QrCodeTemplate : null;

                var trSetting = new CompanyLocalizedSetting
                {
                    CompanyId = company.Id,
                    LanguageCode = "tr",
                    DefaultInvoiceNotes = !string.IsNullOrWhiteSpace(trNotes) ? trNotes : "Bizimle çalıştığınız için teşekkür ederiz.",
                    DefaultQuotationNotes = !string.IsNullOrWhiteSpace(trQuo) ? trQuo : "Bu teklif tarihinden itibaren 30 gün geçerlidir.",
                    LegalFooterText = !string.IsNullOrWhiteSpace(trFooter) ? trFooter : "Aksi açıkça belirtilmedikçe tutara kargo dahil değildir.",
                    DefaultPaymentDetails = !string.IsNullOrWhiteSpace(trPay) ? trPay : "Varsayılan ödeme bilgileri",
                    QrTemplateText = trQr ?? company.QrCodeTemplate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var enNotes = targetLang == "en" ? company.DefaultInvoiceNotes : null;
                var enQuo = targetLang == "en" ? company.DefaultQuotationNotes : null;
                var enFooter = targetLang == "en" ? company.FooterText : null;
                var enPay = targetLang == "en" ? company.PaymentInfo : null;
                var enQr = targetLang == "en" ? company.QrCodeTemplate : null;

                var enSetting = new CompanyLocalizedSetting
                {
                    CompanyId = company.Id,
                    LanguageCode = "en",
                    DefaultInvoiceNotes = !string.IsNullOrWhiteSpace(enNotes) ? enNotes : "Thank you for doing business with us.",
                    DefaultQuotationNotes = !string.IsNullOrWhiteSpace(enQuo) ? enQuo : "This quotation is valid for 30 days from its date.",
                    LegalFooterText = !string.IsNullOrWhiteSpace(enFooter) ? enFooter : "The amount does not include shipping unless explicitly stated.",
                    DefaultPaymentDetails = !string.IsNullOrWhiteSpace(enPay) ? enPay : "Default payment details",
                    QrTemplateText = enQr ?? company.QrCodeTemplate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.CompanyLocalizedSettings.AddRange(arSetting, trSetting, enSetting);
            }
            else
            {
                var langCodes = new[] { "ar", "tr", "en" };
                foreach (var code in langCodes)
                {
                    if (!existingSettings.Any(s => s.LanguageCode == code))
                    {
                        var setting = new CompanyLocalizedSetting
                        {
                            CompanyId = company.Id,
                            LanguageCode = code,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        if (code == "ar")
                        {
                            setting.DefaultInvoiceNotes = "شكراً لتعاملكم معنا.";
                            setting.DefaultQuotationNotes = "هذا العرض صالح لمدة 30 يوماً من تاريخه.";
                            setting.LegalFooterText = "المبلغ لا يشمل الشحن إلا إذا تم ذكر ذلك صراحة.";
                            setting.DefaultPaymentDetails = "بيانات الدفع الافتراضية";
                        }
                        else if (code == "tr")
                        {
                            setting.DefaultInvoiceNotes = "Bizimle çalıştığınız için teşekkür ederiz.";
                            setting.DefaultQuotationNotes = "Bu teklif tarihinden itibaren 30 gün geçerlidir.";
                            setting.LegalFooterText = "Aksi açıkça belirtilmedikçe tutara kargo dahil değildir.";
                            setting.DefaultPaymentDetails = "Varsayılan ödeme bilgileri";
                        }
                        else
                        {
                            setting.DefaultInvoiceNotes = "Thank you for doing business with us.";
                            setting.DefaultQuotationNotes = "This quotation is valid for 30 days from its date.";
                            setting.LegalFooterText = "The amount does not include shipping unless explicitly stated.";
                            setting.DefaultPaymentDetails = "Default payment details";
                        }
                        setting.QrTemplateText = company.QrCodeTemplate;
                        context.CompanyLocalizedSettings.Add(setting);
                    }
                }
            }
        }
        context.SaveChanges();
    }

    private static bool HasArabic(string?[] texts)
    {
        foreach (var t in texts)
        {
            if (string.IsNullOrWhiteSpace(t)) continue;
            foreach (var c in t)
            {
                if ((c >= 0x0600 && c <= 0x06FF) || (c >= 0x0750 && c <= 0x077F) || (c >= 0x08A0 && c <= 0x08FF) || (c >= 0xFB50 && c <= 0xFDFF) || (c >= 0xFE70 && c <= 0xFEFF))
                    return true;
            }
        }
        return false;
    }

    private static bool HasTurkish(string?[] texts)
    {
        var trChars = new HashSet<char> { 'ş', 'Ş', 'ğ', 'Ğ', 'ı', 'İ', 'ç', 'Ç', 'ö', 'Ö', 'ü', 'Ü' };
        foreach (var t in texts)
        {
            if (string.IsNullOrWhiteSpace(t)) continue;
            foreach (var c in t)
            {
                if (trChars.Contains(c)) return true;
            }
        }
        return false;
    }

    public static void InitializeRestoredDatabase(string dbPath)
    {
        // Apply schema updates to the restored database using raw ADO.NET SqliteConnection
        ApplySchemaUpdates(dbPath);

        // Run localized settings migration if needed
        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        using (var context = new AppDbContext(optionsBuilder.Options))
        {
            MigrateCompanyLocalizedSettings(context);
        }
    }
}
