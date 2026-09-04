# Fornixx CRM Pro

A multilingual Windows desktop CRM and sales-management application built with .NET 8, WPF, MVVM and Entity Framework Core.

The project combines customer/company/product management with document workflows, reporting/export features and a localized desktop UI in English, Turkish and Arabic.

## Engineering highlights

- .NET 8 WPF desktop application
- MVVM architecture with CommunityToolkit.Mvvm
- Dependency injection
- Entity Framework Core with SQLite
- Company, customer and product domain models
- Sales/document workflow screens
- PDF document generation with QuestPDF
- Excel export with ClosedXML
- QR code generation with QRCoder
- Material Design WPF components
- English, Turkish and Arabic localization resources
- RTL-capable Arabic UI resources
- Application-level logging and session state

## Tech stack

| Area | Technology |
|---|---|
| Runtime | .NET 8 / C# |
| UI | WPF / XAML |
| Architecture | MVVM |
| Data | Entity Framework Core 8 + SQLite |
| PDF | QuestPDF |
| Excel | ClosedXML |
| QR | QRCoder |
| MVVM toolkit | CommunityToolkit.Mvvm |
| UI system | MaterialDesignThemes |

## Application structure

```text
Fornixx_CRM_Pro/
├── Models/
│   ├── Company.cs
│   ├── Customer.cs
│   ├── Product.cs
│   └── Enums.cs
├── Data/
│   └── AppDbContext.cs
├── ViewModels/
├── Views/
│   ├── Dashboard/
│   ├── Companies/
│   ├── Customers/
│   ├── Products/
│   ├── Documents/
│   ├── Reports/
│   └── Settings/
├── Services/
├── Helpers/
├── Pdf/
├── Resources/
│   ├── Strings.en.xaml
│   ├── Strings.tr.xaml
│   └── Strings.ar.xaml
├── App.xaml
├── MainWindow.xaml
└── FornixxCRM.csproj
```

## Product areas

### Customer and company management

The application contains separate company and customer models and screens, allowing CRM data to remain organized around business entities.

### Product management

Products are modeled through Entity Framework Core and surfaced through dedicated WPF views and forms.

### Business documents

The project includes document list/form views and PDF generation infrastructure, making it suitable for invoice and sales-document workflows.

### Reporting and export

The application includes report views plus PDF and spreadsheet tooling for business-facing exports.

### Localization

UI strings are maintained as separate WPF resource dictionaries:

```text
Resources/Strings.en.xaml
Resources/Strings.tr.xaml
Resources/Strings.ar.xaml
```

This keeps localization separate from business logic and supports English, Turkish and Arabic experiences.

## Build

Requirements:

- Windows
- .NET 8 SDK

Restore and build:

```powershell
dotnet restore FornixxCRM.csproj
dotnet build FornixxCRM.csproj --configuration Release
```

Run:

```powershell
dotnet run --project FornixxCRM.csproj
```

## Build reliability

The project contains an explicit WPF XAML rebuild safeguard that clears stale markup-compiler cache files before `MarkupCompilePass1`. This addresses intermittent `BG1002` failures caused by stale BAML state after interrupted or configuration-switched builds.

## Portfolio value

This repository demonstrates a different engineering surface from my web/API projects:

- desktop application architecture,
- XAML/WPF UI engineering,
- MVVM state separation,
- local relational persistence,
- business-document generation,
- multilingual desktop UX.

## Roadmap

- Add automated domain and ViewModel tests
- Add database migration/versioning documentation
- Add structured audit logging for important CRM operations
- Add import/export validation tests
- Add installer/release packaging
- Add screenshots and a short product demo

---

Built by [Mahmoud Karzoun](https://github.com/mkarson1997).