# Changelog v1.1.0 (KARZOUN ERP)

## New Features
- **Appearance & Design Center**: Customize global theme colors (Primary, Accent, Sidebar Background, Buttons) and per-company/customer styling live.
- **PDF Customization Controls**: Adjust margins, spacing, and colors of Invoice/Quotation PDFs.
- **Autocomplete Product Search**: Search and select products by main/localized names, weight, type, and description. Normalizes Arabic/Persian numbers.
- **Total Weight and Quantity Calculations**: Automatic line item accumulation and display in document totals section on the UI and generated PDFs.
- **Separation of Document Type & Status**: Strictly separates Quotations and Invoices, allowing individual document statuses (Draft, Sent, Pending, Accepted, Rejected, Paid, etc.).
- **Multi-Select and Bulk Actions**: Apply bulk delete, status change, and excel export actions across Products, Customers, Companies, and Invoices.
- **Duplicate Product Similarity warning**: Implemented smart Levenshtein similarity warning above 95% threshold instead of hard blocking variants.
- **Retroactive Product Images**: QuestPDF export dynamically fetches current product image at export time rather than storing snapshot.
- **Approved Karzoun Identity**: Added the production K application/setup icons, compact sidebar lockup, semantic Karzoun palette, and theme-aware light/dark surfaces while preserving company appearance overrides.
- **Language Button Styling**: Standardized settings language buttons to be theme-aware.
- **English Digits Everywhere**: Ensures all numeric outputs (totals, dates, invoices) utilize standard English digits (0-9) regardless of system culture.

## Candidate Build Notes
- Final installer, final ZIP, and customer delivery folder must not be created until explicit release approval.
- Candidate includes UI/PDF totals, status/type separation, selection checkboxes, current-setting product image resolution for PDFs, and duplicate similarity warning behavior.
