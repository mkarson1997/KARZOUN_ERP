# KARZOUN ERP

[![CI](https://github.com/mkarson1997/KARZOUN_ERP/actions/workflows/ci.yml/badge.svg)](https://github.com/mkarson1997/KARZOUN_ERP/actions/workflows/ci.yml)
[![CodeQL](https://github.com/mkarson1997/KARZOUN_ERP/actions/workflows/codeql.yml/badge.svg)](https://github.com/mkarson1997/KARZOUN_ERP/actions/workflows/codeql.yml)
[![Release](https://img.shields.io/github/v/release/mkarson1997/KARZOUN_ERP)](https://github.com/mkarson1997/KARZOUN_ERP/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A multilingual open-source Windows desktop ERP/CRM application for managing companies, customers, products and sales documents, built with .NET 8, WPF and MVVM.

**Version:** 1.1.0 · **Platform:** Windows (x64)

## Overview

KARZOUN ERP is a desktop business-management application that keeps customer relationships, product catalogues and sales paperwork in one place. It stores data locally in SQLite, generates print-ready PDF quotations and invoices, imports and exports Excel workbooks, and presents the whole interface in English, Turkish or Arabic, including full right-to-left layout for Arabic.

The application is designed to run standalone on a single machine: no server, no account, no network dependency. Application data lives in the current user's `%AppData%\KARZOUN ERP` folder.

## Engineering proof points

| Area | What the repository demonstrates |
| --- | --- |
| Desktop architecture | .NET 8/WPF application organized around MVVM view models, dependency injection, services and EF Core. |
| Local persistence | SQLite-backed business data with startup schema evolution and preservation of existing settings. |
| Recovery engineering | Verified restore candidates, temporary-copy migration, emergency backup, rollback and startup corruption recovery. |
| Multilingual product engineering | English, Turkish and Arabic resource dictionaries with full Arabic RTL layout behavior. |
| Language-aware domain settings | Per-company/per-language invoice notes, legal footer, payment details and QR template defaults with uniqueness constraints. |
| Document workflows | QuestPDF quotations/invoices, ClosedXML import/export and validation reporting, plus QR/image support. |
| Configuration model | Global appearance settings with per-company theme overrides and localized document defaults. |
| Release integrity | Versioned Windows installer releases accompanied by `SHA256SUMS.txt`. |
| Verification | Windows CI restores/builds the .NET project and CodeQL runs C# `security-extended` analysis. |
| Supply-chain hygiene | GitHub Actions used by CI and CodeQL are pinned to reviewed immutable commit SHAs. |

## Screenshots

> Screenshots are not committed to the repository yet. This section is intentionally left as a placeholder until real dashboard, document-editor, PDF-output and Appearance & Design captures are available.

## Features

### Business data
- Multi-company support with a company selector, so several businesses can be managed from one installation
- Customer records with contact details, notes, colour markers and per-customer detail view
- Product and service catalogue with weight, weight unit, unit price, default quantity, images and active/inactive state
- Duplicate-product detection that warns on exact and near-identical entries while still allowing legitimate variants
- Bulk selection across customers, companies, products and documents

### Sales documents
- Quotations and invoices with line items, discounts, tax rate, paid/remaining amounts and document status
- Product picker with autocomplete search across all three languages, plus free-text custom line items
- Automatic totals including aggregate quantity and total weight with unit normalisation
- Document status workflow for draft, pending, unpaid, partially paid and other states

### Documents and reporting
- PDF generation for quotations and invoices via QuestPDF, with configurable colours, margins, spacing and base font size
- Optional product images and QR codes in generated documents
- Excel export and import for products and customers via ClosedXML, with imported/updated/skipped/error reporting per row
- Excel sales-report export
- Reports screen with dashboard statistics

### Languages and layout
- Full UI localisation in **English**, **Turkish** and **Arabic**, maintained as separate WPF resource dictionaries
- **Right-to-left (RTL)** layout for Arabic: flow direction, text alignment, data-grid alignment and icon margins flip; Turkish and English render left-to-right
- Arabic and Persian numerals are normalised to English digits on input, and exported documents use English digits consistently

### Appearance & Design
- Built-in Appearance & Design screen for customising primary, secondary and accent colours, sidebar/button colours and card/page backgrounds
- Light and dark surfaces with Material Design base-theme selection from surface luminance
- Separate PDF colour, margin, spacing and font-size settings with a live preview
- **Per-company theme overrides** that take precedence while a company is active and fall back to global settings otherwise

### Backup and recovery
- On-demand database backup to a configurable folder, with fallback to the default backup folder when the configured location is unavailable
- Restore with integrity verification: source validation, temporary-copy migration, second validation, emergency backup and rollback on failure
- Startup recovery that preserves a corrupted database and restores from the most suitable verified safety backup

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

## Architecture

```text
WPF Views / XAML
       |
       v
MVVM ViewModels
       |
       v
Application Services
   |       |       |
   v       v       v
EF Core   PDF     Excel
   |
   v
SQLite
   ^
   |
Backup / restore verification + rollback
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the local trust boundary, localization/data-migration model, backup/restore safety path and explicit scope limits.

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

Download `KARZOUN_ERP_Setup_1.1.0.exe` from the latest release and run it. The installer requires administrator rights and installs to `C:\Program Files\KARZOUN ERP`. Uninstalling removes only the application files; the database and backups in `%AppData%\KARZOUN ERP` remain untouched.

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

## Explicit boundaries

KARZOUN ERP is currently a standalone desktop product, not a multi-user server platform. It does not claim cloud synchronization, cross-machine concurrency, server-side RBAC, or regulated accounting certification. A future networked edition would require separate authentication, authorization, tenancy, transport-security and conflict-resolution boundaries.

## License

Released under the [MIT License](LICENSE). Copyright © 2026 Karzoun.
