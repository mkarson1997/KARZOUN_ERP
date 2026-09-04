# Developer Notes: Company Settings Localization (v1.0.2 Hotfix)

This document explains the design, database schema, data migration, and logic flow of the localized company settings implemented in v1.0.2.

## 1. Database Schema
We created a new table `CompanyLocalizedSettings` to support language-specific document defaults (Arabic, Turkish, and English):

```sql
CREATE TABLE IF NOT EXISTS CompanyLocalizedSettings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyId INTEGER NOT NULL,
    LanguageCode TEXT NOT NULL,
    DefaultInvoiceNotes TEXT,
    DefaultQuotationNotes TEXT,
    LegalFooterText TEXT,
    DefaultPaymentDetails TEXT,
    QrTemplateText TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_CompanyLocalizedSettings_CompanyId_LanguageCode ON CompanyLocalizedSettings (CompanyId, LanguageCode);
```

- **Unique Constraint:** A company can only have one configuration per `LanguageCode` (allowed: `ar`, `tr`, `en`).
- **Cascade Delete:** Deleting a company profile automatically cleans up its localized settings records.

## 2. Safe SQLite Migration
In `DatabaseInitializer.cs`:
1. `ApplySchemaUpdates(dbPath)` ensures the new table and unique index are created if they do not exist.
2. `MigrateCompanyLocalizedSettings(context)` runs on startup:
   - For every existing company, it checks if any localized settings exist.
   - If empty, it inspects the old shared fields in the `Companies` table (notes, footers, payment info) to see if they appear Arabic (using character unicode range checking) or Turkish/English.
   - It migrates the existing user configurations to the detected language row and seeds the other languages with predefined translation defaults.

## 3. Settings View & ViewModel Logic
In `SettingsViewModel.cs`:
- Implements `ILoadableViewModel` so it reloads automatically when the application language changes.
- `LoadAsync()` queries the `CompanyLocalizedSettings` table for the current app language (`LocalizationManager.Language`) and populates the bound UI textboxes.
- `SaveSettingsAsync()` updates the database table *only* for the current app language, leaving other languages untouched.
- `SaveLanguageAsync()` checks if there are unsaved settings changes. If so, it displays a warning prompt asking the user whether they want to save changes, discard changes, or cancel the language change.

## 4. Document Form Defaults
In `DocumentFormViewModel.cs`:
- In `PrepareNew()`, default notes, payment info, and footer text are loaded asynchronously from `CompanyLocalizedSettings` based on the current application language.
- `OnLanguageCodeChanged(string value)` reloads localized defaults dynamically when a user changes the target document language dropdown on a new document.

## 5. PDF Export & Arabic Default Overrides
In `PdfTemplateBuilder.cs`:
- Reads QR template text from `CompanyLocalizedSetting` for the selected output PDF language.
- If the document contains Arabic default notes/footers/payment info but is being exported in English or Turkish, it automatically overrides these fields with the target language's default translations to maintain presentation consistency.
