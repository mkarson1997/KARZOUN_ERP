# Restore Step 01 - Select All and Document Totals

## Scope

Compared only the requested current files in the repository root against the
corresponding files in an external pre-1.1.0 backup snapshot. The machine-local
paths are intentionally omitted from public documentation.

## Backup Files With Requested Features

None of the requested backup files contained row-selection or Select All grid code.

None of the requested backup files contained `TotalQuantity`, `TotalWeight`, `SumQuantity`, `SumWeight`, `QuantityTotal`, or `WeightTotal`.

The only matching selected-related code in the requested files was customer selected-column export:

- `ViewModels\CustomerViewModel.cs`
- `Views\Customers\CustomerListPage.xaml`

That is column export, not row selection, and it already exists in current.

## What Was Missing In Current

Current is missing:

- Select All header checkbox and row checkbox columns for products, customers, and documents.
- Row-selection state such as `IsSelected`, `SelectedProducts`, `SelectedCustomers`, or `SelectedDocuments`.
- Bulk selected-row actions such as `DeleteSelected` or selected-row export.
- Document total quantity and total weight properties/calculation/display.

The same items are also missing from the specified backup files.

## What Was Restored

No feature code was restored because the specified backup files are byte-for-byte identical to current for all requested files and do not contain the requested missing features.

## Files Changed

- `docs\RESTORE_STEP_01_SELECT_TOTALS.md`

## Files Compared

- `ViewModels\ProductViewModel.cs`
- `ViewModels\CustomerViewModel.cs`
- `ViewModels\DocumentViewModel.cs`
- `ViewModels\DocumentFormViewModel.cs`
- `Views\Products\ProductListPage.xaml`
- `Views\Customers\CustomerListPage.xaml`
- `Views\Documents\DocumentListPage.xaml`
- `Views\Documents\DocumentFormPage.xaml`
- `Models\SalesDocument.cs`
- `Models\SalesDocumentItem.cs`
- `Services\DocumentService.cs`

## What Was Not Touched

- Arabic RTL
- Text centering
- Appearance
- PDF behavior/layout
- Excel behavior
- Installer creation
- ZIP creation
- `bin`
- `obj`
- `.db` files
- Unrelated features
