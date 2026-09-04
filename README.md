# KARZOUN ERP

A multilingual open-source Windows desktop ERP/CRM application for managing companies, customers, products and sales documents — built with .NET 8, WPF and MVVM.

**Version:** 1.1.0 · **License:** MIT · **Platform:** Windows (x64)

---

## Overview

KARZOUN ERP is a desktop business-management application that keeps customer relationships, product catalogues and sales paperwork in one place. It stores data locally in SQLite, generates print-ready PDF quotations and invoices, imports and exports Excel workbooks, and presents the whole interface in English, Turkish or Arabic — including full right-to-left layout for Arabic.

The application is designed to run standalone on a single machine: no server, no account, no network dependency. Application data lives in the current user's `%AppData%\KARZOUN ERP` folder.

## Screenshots

> Screenshots are not committed to the repository yet. This section is a placeholder and will be populated with dashboard, document-editor, PDF-output and Appearance & Design captures in a future update.

## Features

### Business data
- Multi-company support with a company selector, so several businesses can be managed from one installation
- Customer records with contact details, notes, colour markers and per-customer detail view
- Product and service catalogue with weight, weight unit, unit price, default quantity, images and active/inactive state
- Duplicate-product detection that warns on exact and near-identical entries while still allowing legitimate variants (different weight, different flavour, and so on)
- Bulk selection across customers, companies, products and documents

### Sales documents
- Quotations and invoices with line items, discounts, tax rate, paid/remaining amounts and document status
- Product picker with autocomplete search across all three languages, plus free-text custom line items
- Automatic totals including aggregate quantity and total weight with unit normalisation
- Document status workflow (draft, pending, unpaid, partially paid and others)

### Documents and reporting
- PDF generation for quotations and invoices via QuestPDF, with configurable colours, margins, spacing and base font size
- Optional product images and QR codes in generated documents
- Excel export **and import** for products and customers via ClosedXML, with validation reporting (imported / updated / skipped / error counts per row)
- Excel sales-report export
- Reports screen with dashboard statistics

### Languages and layout
- Full UI localisation in **English**, **Turkish** and **Arabic**, maintained as separate WPF resource dictionaries
- **Right-to-left (RTL)** layout for Arabic — flow direction, text alignment, data-grid alignment and icon margins all flip; Turkish and English render left-to-right
- Arabic and Persian numerals are normalised to English digits on input, and exported documents use English digits consistently

### Appearance & Design
- Built-in Appearance & Design screen for customising primary, secondary and accent colours, sidebar and button colours, and card/page backgrounds
- Light and dark surfaces are supported; the matching Material Design base theme is selected automatically from surface luminance
- Separate PDF colour, margin, spacing and font-size settings with a live preview
- **Per-company theme overrides** — an individual company can carry its own primary/secondary/accent colours that take precedence while that company is active, falling back to the global appearance settings otherwise

### Backup and recovery
- On-demand database backup to a configurable folder, with automatic fallback to the default backup folder if the configured location is unavailable (disconnected drive, missing folder, permission denied)
- Restore with integrity verification: the source file is checked, migrated in a temporary copy, verified again, and only then applied — with an automatic emergency backup and rollback if anything fails
- Startup recovery that detects a corrupted database, preserves the damaged file, and restores from the most suitable verified safety backup

## Tech stack

| Area | Technology |
|---|---|
| Runtime | .NET 8 (`net8.0-windows`) / C# |
| UI | WPF / XAML |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| Dependency injection | Microsoft.Extensions.DependencyInjection |
| Data | Entity Framework Core 8 + SQLite |
| PDF | QuestPDF |
| Excel | ClosedXML |
| QR codes | QRCoder |
| UI components | MaterialDesignThemes |
| Installer | Inno Setup 6 |

## Project structure

```text
KarzounERP/
├── Models/              Domain entities and settings models
├── Data/                DbContext and database initialisation
├── ViewModels/          MVVM view models
├── Views/               WPF screens
│   ├── Dashboard/  Companies/  Customers/  Products/
│   ├── Documents/  Reports/    Settings/   Appearance/  Logs/
├── Services/            Company, customer, product, document, PDF, Excel, backup
├── Helpers/             Theming, localisation, paths, formatting, search
├── Pdf/                 Document templates and formatters
├── Resources/
│   ├── Brand/           Application icon, installer icon, logo mark, brand palette
│   ├── Strings.en.xaml  Strings.tr.xaml  Strings.ar.xaml
├── installer/           Inno Setup script
├── docs/                Changelogs, user guides, developer notes
└── KarzounERP.csproj
```

## Build

Requirements:

- Windows
- .NET 8 SDK

```powershell
dotnet restore KarzounERP.csproj
dotnet build KarzounERP.csproj --configuration Release
```

## Run

```powershell
dotnet run --project KarzounERP.csproj
```

The application creates its data folder on first launch at:

```text
%AppData%\KARZOUN ERP\
```

## Download

Prebuilt Windows installers are published on the [GitHub Releases](https://github.com/mkarson1997/KARZOUN_ERP/releases) page.

Download `KARZOUN_ERP_Setup_1.1.0.exe` from the latest release and run it. The installer requires administrator rights and installs to `C:\Program Files\KARZOUN ERP`. Uninstalling removes only the application files — your database and backups in `%AppData%\KARZOUN ERP` are left untouched.

Each release includes a `SHA256SUMS.txt` file so the installer can be verified before running:

```powershell
Get-FileHash .\KARZOUN_ERP_Setup_1.1.0.exe -Algorithm SHA256
```

## Build reliability

The project file clears stale WPF markup-compiler cache files before `MarkupCompilePass1`. This prevents intermittent `BG1002` failures caused by stale BAML state after an interrupted build or a Debug/Release switch.

## Localisation

UI strings live in separate WPF resource dictionaries, keeping translations independent of business logic:

```text
Resources/Strings.en.xaml
Resources/Strings.tr.xaml
Resources/Strings.ar.xaml
```

`scripts/ValidateResources.ps1` validates that the dictionaries are well-formed and reports keys missing from any language.

## License

Released under the [MIT License](LICENSE). Copyright © 2026 Karzoun.
