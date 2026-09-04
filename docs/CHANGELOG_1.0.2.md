# Changelog: KARZOUN ERP v1.0.2

This release introduces branding visual upgrades, automatic database backup functionality, PDF product image column configuration, and hotfixes localized company settings (Invoice Notes, Quotation Notes, Legal Footer, Payment Details, and QR templates).

## [1.0.2] - 2026-06-13

### Added
- Created `CompanyLocalizedSettings` SQLite database table to store language-specific settings for Arabic, Turkish, and English.
- Added automatic background schema updates to migrate existing shared company settings into their detected language (e.g. Arabic values mapped to the `ar` settings profile).
- Seeded default translation values for empty fields across all three languages.
- Implemented `ICompanyService` methods `GetLocalizedSettingAsync` and `SaveLocalizedSettingAsync`.
- Added localized validation warning strings (`Msg_UnsavedChangesConfirm`).

### Changed
- Configured settings page text fields (Default Invoice Notes, Default Quotation Notes, Legal Footer, default Payment details, QR template) to load and save settings specific to the current active application language.
- Added unsaved settings change prompt dialog when switching the application language in the settings view.
- Configured document creation form (`DocumentFormViewModel`) to automatically load default notes and footers matching the target document language. Changing the document language on a new document updates the defaults dynamically.
- Refactored PDF template builder to pull notes, footers, payment details, and QR templates from localized tables depending on the PDF export language.
- Implemented an automatic override system that replaces default Arabic text in exported English/Turkish PDFs with target-language default texts.
