using ClosedXML.Excel;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using System.Globalization;

namespace KarzounERP.Services;

public class ExcelService : IExcelService
{
    private const string SheetProducts = "Products";
    private const string SheetCustomers = "Customers";
    private const string SheetDocuments = "Documents";
    private const string SheetSalesReport = "Sales Report";

    private static readonly string[] ProductHeaders =
    {
        "Product Name",
        "Type",
        "Description",
        "Weight",
        "Weight Unit",
        "Unit Price",
        "Default Quantity",
        "Image Path",
        "Arabic Name",
        "Arabic Description",
        "Turkish Name",
        "Turkish Description",
        "English Name",
        "English Description",
        "Active"
    };

    private static readonly string[] CustomerHeaders =
    {
        "Full Name",
        "Company Name",
        "Country",
        "Phone",
        "Email",
        "Importance",
        "Follow Up Stage",
        "Commercial Mindset",
        "Notes",
        "Color Marker",
        "Created At"
    };

    private static readonly Dictionary<string, string[]> ProductHeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = new[] { "product name", "name", "display name", "اسم المنتج", "الاسم", "اسم", "urun adi", "urun adı", "ad", "product" },
        ["type"] = new[] { "type", "product type", "نوع", "نوع المنتج", "tur", "tür", "urun turu", "ürün türü" },
        ["description"] = new[] { "description", "desc", "وصف", "الوصف", "aciklama", "açıklama" },
        ["weight"] = new[] { "weight", "وزن", "الوزن", "agirlik", "ağırlık" },
        ["weightunit"] = new[] { "weight unit", "unit", "وحدة الوزن", "وحدة", "agirlik birimi", "ağırlık birimi", "birim" },
        ["unitprice"] = new[] { "unit price", "price", "سعر الوحدة", "السعر", "سعر", "birim fiyat", "fiyat" },
        ["defaultquantity"] = new[] { "default quantity", "default qty", "quantity", "qty", "الكمية الافتراضية", "كمية", "الكمية", "varsayilan adet", "varsayılan adet", "adet", "miktar" },
        ["imagepath"] = new[] { "image path", "image", "صورة", "مسار الصورة", "gorsel", "görsel", "resim yolu" },
        ["arabicname"] = new[] { "arabic name", "name arabic", "name (arabic)", "ar name", "اسم عربي", "الاسم العربي" },
        ["arabicdescription"] = new[] { "arabic description", "description arabic", "description (arabic)", "ar description", "وصف عربي", "الوصف العربي" },
        ["turkishname"] = new[] { "turkish name", "name turkish", "name (turkish)", "tr name", "turkce ad", "türkçe ad" },
        ["turkishdescription"] = new[] { "turkish description", "description turkish", "description (turkish)", "tr description", "turkce aciklama", "türkçe açıklama" },
        ["englishname"] = new[] { "english name", "name english", "name (english)", "en name" },
        ["englishdescription"] = new[] { "english description", "description english", "description (english)", "en description" },
        ["active"] = new[] { "active", "is active", "نشط", "aktif" }
    };

    private static readonly Dictionary<string, string[]> CustomerHeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fullname"] = new[] { "full name", "name", "customer name", "الاسم الكامل", "اسم العميل", "الاسم", "ad soyad", "musteri adi", "müşteri adı" },
        ["companyname"] = new[] { "company name", "company", "شركة", "اسم الشركة", "şirket", "sirket", "firma" },
        ["country"] = new[] { "country", "دولة", "البلد", "بلد", "ülke", "ulke" },
        ["phone"] = new[] { "phone", "whatsapp", "mobile", "هاتف", "جوال", "واتساب", "telefon" },
        ["email"] = new[] { "email", "e-mail", "بريد", "البريد", "eposta", "e posta" },
        ["importance"] = new[] { "importance", "أهمية", "الاهمية", "önem", "onem" },
        ["followupstage"] = new[] { "follow up stage", "stage", "follow-up stage", "مرحلة المتابعة", "المرحلة", "aşama", "asama" },
        ["commercialmindset"] = new[] { "commercial mindset", "mindset", "العقلية التجارية", "ticari yaklaşım", "ticari yaklasim" },
        ["notes"] = new[] { "notes", "note", "ملاحظات", "ملاحظة", "notlar", "not" },
        ["colormarker"] = new[] { "color marker", "marker", "color", "لون", "مؤشر اللون", "renk" }
    };

    public void ExportSelectedColumns(List<Customer> customers, List<string> selectedColumns, string filePath)
    {
        using var wb = new XLWorkbook();
        SetWorkbookMetadata(wb);
        var ws = wb.Worksheets.Add(LocalizeSheetName("ExcelCustomers", SheetCustomers));

        var columnMap = new List<(string Key, string Header, Action<IXLCell, Customer> Writer)>();
        if (selectedColumns.Contains("Name")) columnMap.Add(("fullname", L("Name", "Full Name"), (cell, c) => cell.Value = Clean(c.FullName)));
        if (selectedColumns.Contains("Company")) columnMap.Add(("companyname", L("Company", "Company Name"), (cell, c) => cell.Value = Clean(c.CompanyName)));
        if (selectedColumns.Contains("Country")) columnMap.Add(("country", L("Country", "Country"), (cell, c) => cell.Value = Clean(c.Country)));
        if (selectedColumns.Contains("Phone")) columnMap.Add(("phone", L("Phone", "Phone"), (cell, c) => cell.Value = Clean(c.Phone)));
        if (selectedColumns.Contains("Email")) columnMap.Add(("email", L("Email", "Email"), (cell, c) => cell.Value = Clean(c.Email)));
        if (selectedColumns.Contains("Notes")) columnMap.Add(("notes", L("CustNotes", "Notes"), (cell, c) => cell.Value = Clean(c.Notes)));

        WriteHeaderRow(ws, columnMap.Select(c => c.Header).ToArray());
        WriteRows(customers, ws, columnMap.Select(c => c.Writer).ToArray());
        FinishSheet(ws);
        wb.SaveAs(filePath);
    }

    public void ExportCustomers(List<Customer> customers, string filePath)
    {
        using var wb = new XLWorkbook();
        SetWorkbookMetadata(wb);
        var ws = wb.Worksheets.Add(LocalizeSheetName("ExcelCustomers", SheetCustomers));

        var headers = new[]
        {
            L("Name", CustomerHeaders[0]),
            L("Company", CustomerHeaders[1]),
            L("Country", CustomerHeaders[2]),
            L("Phone", CustomerHeaders[3]),
            L("Email", CustomerHeaders[4]),
            L("CustImportance", CustomerHeaders[5]),
            L("CustStage", CustomerHeaders[6]),
            L("CustMindset", CustomerHeaders[7]),
            L("CustNotes", CustomerHeaders[8]),
            "Color Marker",
            L("CustCreatedAt", CustomerHeaders[10])
        };
        WriteHeaderRow(ws, headers);

        for (var r = 0; r < customers.Count; r++)
        {
            var c = customers[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = Clean(c.FullName);
            ws.Cell(row, 2).Value = Clean(c.CompanyName);
            ws.Cell(row, 3).Value = Clean(c.Country);
            ws.Cell(row, 4).Value = Clean(c.Phone);
            ws.Cell(row, 5).Value = Clean(c.Email);
            ws.Cell(row, 6).Value = Clean(ArabicEnumHelper.GetImportanceLevelLabel(c.Importance));
            ws.Cell(row, 7).Value = Clean(ArabicEnumHelper.GetFollowUpStageLabel(c.FollowUpStage));
            ws.Cell(row, 8).Value = Clean(ArabicEnumHelper.GetCommercialMindsetLabel(c.CommercialMindset));
            ws.Cell(row, 9).Value = Clean(c.Notes);
            ws.Cell(row, 10).Value = Clean(c.ColorMarker);
            ws.Cell(row, 11).Value = DigitNormalizer.FormatDate(c.CreatedAt);
        }

        FinishSheet(ws);
        wb.SaveAs(filePath);
    }

    public CustomerImportResult ImportCustomers(string filePath, int companyId, List<Customer> existingCustomers)
    {
        var result = new CustomerImportResult();
        try
        {
            ValidateExcelFile(filePath);

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.First();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            var headers = BuildHeaderMap(ws, CustomerHeaderAliases);
            var hasHeaders = headers.Count > 0;
            if (hasHeaders && !headers.ContainsKey("fullname"))
                throw MissingNameColumnException();

            var defaultColumns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["fullname"] = 1,
                ["companyname"] = 2,
                ["country"] = 3,
                ["phone"] = 4,
                ["email"] = 5,
                ["importance"] = 6,
                ["followupstage"] = 7,
                ["commercialmindset"] = 8,
                ["notes"] = 9,
                ["colormarker"] = 10
            };

            var columns = MergeHeaderMapWithDefaults(headers, defaultColumns, hasHeaders);
            var startRow = hasHeaders ? 2 : 1;

            for (var row = startRow; row <= lastRow; row++)
            {
                if (IsBlankRow(ws, row))
                {
                    result.Summary.SkippedCount++;
                    continue;
                }

                var name = CellText(ws, row, columns, "fullname");
                if (string.IsNullOrWhiteSpace(name))
                {
                    AddCustomerError(result, row, "Full Name is required.");
                    continue;
                }

                var email = CellText(ws, row, columns, "email");
                var phone = CellText(ws, row, columns, "phone");
                if (IsDuplicateCustomer(existingCustomers, result.CustomersToSave, phone, email))
                {
                    result.Summary.DuplicateCount++;
                    result.Summary.SkippedCount++;
                    continue;
                }

                var customer = new Customer
                {
                    CompanyId = companyId,
                    FullName = name.Trim(),
                    CompanyName = EmptyToNull(CellText(ws, row, columns, "companyname")),
                    Country = EmptyToNull(CellText(ws, row, columns, "country")),
                    Phone = EmptyToNull(phone),
                    Email = EmptyToNull(email),
                    Notes = EmptyToNull(CellText(ws, row, columns, "notes")),
                    ColorMarker = EmptyToNull(CellText(ws, row, columns, "colormarker")),
                    Importance = ParseImportance(CellText(ws, row, columns, "importance")),
                    FollowUpStage = ParseFollowUpStage(CellText(ws, row, columns, "followupstage")),
                    CommercialMindset = ParseCommercialMindset(CellText(ws, row, columns, "commercialmindset")),
                    CreatedAt = DateTime.UtcNow
                };

                result.CustomersToSave.Add(customer);
                result.Summary.ImportedCount++;
                result.Summary.InsertedCount++;
            }
        }
        catch (Exception ex)
        {
            result.Summary.ErrorCount++;
            result.Summary.Message = ex.Message;
            result.Summary.Errors.Add(ex.Message);
        }

        return result;
    }

    public void ExportDocuments(List<SalesDocument> documents, string filePath)
    {
        using var wb = new XLWorkbook();
        SetWorkbookMetadata(wb);
        var ws = wb.Worksheets.Add(LocalizeSheetName("ExcelDocuments", SheetDocuments));

        string? currency = null;
        try
        {
            var session = App.Services?.GetService(typeof(AppSession)) as AppSession;
            currency = session?.ActiveCompanyCurrency;
        }
        catch { }
        if (string.IsNullOrWhiteSpace(currency))
        {
            currency = documents.FirstOrDefault()?.Company?.Currency ?? "USD";
        }

        var headers = new[]
        {
            L("DocNo", "Document Number"),
            L("DocType", "Document Type"),
            L("Name", "Customer"),
            L("Date", "Date"),
            L("Status", "Status"),
            MoneyFormatter.FormatHeaderWithCurrency(L("ColTotal", "Total"), currency)
        };
        WriteHeaderRow(ws, headers);

        for (var r = 0; r < documents.Count; r++)
        {
            var d = documents[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = Clean(d.DocumentNumber);
            ws.Cell(row, 2).Value = Clean(ArabicEnumHelper.GetDocumentTypeLabel(d.Type));
            ws.Cell(row, 3).Value = Clean(d.Customer?.FullName);
            ws.Cell(row, 4).Value = DigitNormalizer.FormatDate(d.Date);
            ws.Cell(row, 5).Value = Clean(ArabicEnumHelper.GetStatusLabel(d.Status));
            WriteDecimal(ws.Cell(row, 6), d.GrandTotal);
        }

        FinishSheet(ws);
        wb.SaveAs(filePath);
    }

    public void ExportSalesReport(List<SalesDocument> documents, string filePath)
    {
        using var wb = new XLWorkbook();
        SetWorkbookMetadata(wb);
        var ws = wb.Worksheets.Add(LocalizeSheetName("ExcelSalesReport", SheetSalesReport));

        string? currency = null;
        try
        {
            var session = App.Services?.GetService(typeof(AppSession)) as AppSession;
            currency = session?.ActiveCompanyCurrency;
        }
        catch { }
        if (string.IsNullOrWhiteSpace(currency))
        {
            currency = documents.FirstOrDefault()?.Company?.Currency ?? "USD";
        }

        var headers = new[]
        {
            L("RepMonth", "Month"),
            L("RepInvoiceCount", "Invoice Count"),
            MoneyFormatter.FormatHeaderWithCurrency(L("RepTotal", "Total"), currency),
            MoneyFormatter.FormatHeaderWithCurrency(L("RepPaid", "Paid"), currency),
            MoneyFormatter.FormatHeaderWithCurrency(L("RepUnpaid", "Unpaid"), currency)
        };
        WriteHeaderRow(ws, headers);

        var monthly = documents
            .Where(d => d.Type == DocumentType.Invoice)
            .GroupBy(d => new { d.Date.Year, d.Date.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .ToList();

        for (var r = 0; r < monthly.Count; r++)
        {
            var g = monthly[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = $"{g.Key.Year:0000}-{g.Key.Month:00}";
            ws.Cell(row, 2).Value = g.Count().ToString(CultureInfo.InvariantCulture);
            WriteDecimal(ws.Cell(row, 3), g.Sum(d => d.GrandTotal));
            WriteDecimal(ws.Cell(row, 4), g.Sum(d => d.PaidAmount));
            WriteDecimal(ws.Cell(row, 5), g.Where(d => d.Status != DocumentStatus.Cancelled).Sum(d => d.GrandTotal - d.PaidAmount));
        }

        FinishSheet(ws);
        wb.SaveAs(filePath);
    }

    public ProductImportResult ImportProducts(string filePath, int companyId, List<Product> existingProducts)
    {
        var result = new ProductImportResult();
        try
        {
            ValidateExcelFile(filePath);

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.First();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            var headers = BuildHeaderMap(ws, ProductHeaderAliases);
            var hasHeaders = headers.Count > 0;
            if (hasHeaders && !headers.ContainsKey("name"))
                throw MissingNameColumnException();

            var defaultColumns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = 1,
                ["type"] = 2,
                ["description"] = 3,
                ["weight"] = 4,
                ["weightunit"] = 5,
                ["unitprice"] = 6,
                ["defaultquantity"] = 7,
                ["imagepath"] = 8,
                ["arabicname"] = 9,
                ["arabicdescription"] = 10,
                ["turkishname"] = 11,
                ["turkishdescription"] = 12,
                ["englishname"] = 13,
                ["englishdescription"] = 14,
                ["active"] = 15
            };

            var columns = MergeHeaderMapWithDefaults(headers, defaultColumns, hasHeaders);
            var startRow = hasHeaders ? 2 : 1;

            for (var row = startRow; row <= lastRow; row++)
            {
                if (IsBlankRow(ws, row))
                {
                    result.Summary.SkippedCount++;
                    continue;
                }

                var name = CellText(ws, row, columns, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    AddProductError(result, row, "Product Name is required.");
                    continue;
                }

                if (!TryReadDecimal(ws, row, columns, "weight", false, out var weight, out var weightError))
                {
                    AddProductError(result, row, weightError);
                    continue;
                }

                if (!TryReadDecimal(ws, row, columns, "unitprice", false, out var unitPrice, out var priceError))
                {
                    AddProductError(result, row, priceError);
                    continue;
                }

                if (!TryReadInt(ws, row, columns, "defaultquantity", 1, out var defaultQuantity, out var quantityError))
                {
                    AddProductError(result, row, quantityError);
                    continue;
                }

                var weightUnit = DigitNormalizer.NormalizeUnit(CellText(ws, row, columns, "weightunit"));
                if (IsDuplicateProduct(existingProducts, result.ProductsToSave, name, weight, weightUnit))
                {
                    result.Summary.DuplicateCount++;
                    result.Summary.SkippedCount++;
                    continue;
                }

                var product = new Product
                {
                    CompanyId = companyId,
                    Name = name.Trim(),
                    Type = ParseProductType(CellText(ws, row, columns, "type")),
                    Description = EmptyToNull(CellText(ws, row, columns, "description")),
                    Weight = weight,
                    WeightUnit = weightUnit,
                    UnitPrice = unitPrice,
                    DefaultQuantity = defaultQuantity,
                    ImagePath = EmptyToNull(CellText(ws, row, columns, "imagepath")),
                    IsActive = ParseBoolean(CellText(ws, row, columns, "active"), true)
                };

                AddLocalizedText(product, "ar", CellText(ws, row, columns, "arabicname"), CellText(ws, row, columns, "arabicdescription"));
                AddLocalizedText(product, "tr", CellText(ws, row, columns, "turkishname"), CellText(ws, row, columns, "turkishdescription"));
                AddLocalizedText(product, "en", CellText(ws, row, columns, "englishname"), CellText(ws, row, columns, "englishdescription"));

                result.ProductsToSave.Add(product);
                result.Summary.ImportedCount++;
                result.Summary.InsertedCount++;
            }
        }
        catch (Exception ex)
        {
            result.Summary.ErrorCount++;
            result.Summary.Message = ex.Message;
            result.Summary.Errors.Add(ex.Message);
        }

        return result;
    }

    public void ExportProducts(List<Product> products, string filePath)
    {
        using var wb = new XLWorkbook();
        SetWorkbookMetadata(wb);
        var ws = wb.Worksheets.Add(LocalizeSheetName("ExcelProducts", SheetProducts));

        string? currency = null;
        try
        {
            var session = App.Services?.GetService(typeof(AppSession)) as AppSession;
            currency = session?.ActiveCompanyCurrency;
        }
        catch { }
        if (string.IsNullOrWhiteSpace(currency))
        {
            currency = products.FirstOrDefault()?.Company?.Currency ?? "USD";
        }

        var headers = new[]
        {
            L("Prod_ColName", ProductHeaders[0]),
            L("Prod_ColType", ProductHeaders[1]),
            "Description",
            L("Prod_ColWeight", ProductHeaders[3]),
            L("Prod_ColWeightUnit", ProductHeaders[4]),
            MoneyFormatter.FormatHeaderWithCurrency(L("Prod_ColPrice", ProductHeaders[5]), currency),
            L("Prod_ColDefaultQty", ProductHeaders[6]),
            L("Prod_ColImagePath", ProductHeaders[7]),
            ProductHeaders[8],
            ProductHeaders[9],
            ProductHeaders[10],
            ProductHeaders[11],
            ProductHeaders[12],
            ProductHeaders[13],
            ProductHeaders[14]
        };
        WriteHeaderRow(ws, headers);

        for (var r = 0; r < products.Count; r++)
        {
            var p = products[r];
            var row = r + 2;
            var ar = p.LocalizedTexts?.FirstOrDefault(x => x.LanguageCode == "ar");
            var tr = p.LocalizedTexts?.FirstOrDefault(x => x.LanguageCode == "tr");
            var en = p.LocalizedTexts?.FirstOrDefault(x => x.LanguageCode == "en");

            ws.Cell(row, 1).Value = Clean(p.Name);
            ws.Cell(row, 2).Value = Clean(ArabicEnumHelper.GetProductTypeLabel(p.Type));
            ws.Cell(row, 3).Value = Clean(p.Description);
            if (p.Weight.HasValue) WriteDecimal(ws.Cell(row, 4), p.Weight.Value);
            else ws.Cell(row, 4).Value = "";
            ws.Cell(row, 5).Value = Clean(DigitNormalizer.NormalizeUnit(p.WeightUnit));
            WriteDecimal(ws.Cell(row, 6), p.UnitPrice);
            ws.Cell(row, 7).Value = p.DefaultQuantity.ToString(CultureInfo.InvariantCulture);
            ws.Cell(row, 8).Value = Clean(p.ImagePath);
            ws.Cell(row, 9).Value = Clean(ar?.Name);
            ws.Cell(row, 10).Value = Clean(ar?.Description);
            ws.Cell(row, 11).Value = Clean(tr?.Name);
            ws.Cell(row, 12).Value = Clean(tr?.Description);
            ws.Cell(row, 13).Value = Clean(en?.Name);
            ws.Cell(row, 14).Value = Clean(en?.Description);
            ws.Cell(row, 15).Value = p.IsActive ? "TRUE" : "FALSE";
        }

        FinishSheet(ws);
        wb.SaveAs(filePath);
    }

    private static void WriteRows<T>(List<T> rows, IXLWorksheet ws, Action<IXLCell, T>[] writers)
    {
        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < writers.Length; c++)
                writers[c](ws.Cell(r + 2, c + 1), rows[r]);
        }
    }

    private static void WriteHeaderRow(IXLWorksheet ws, string[] headers)
    {
        ExcelExportHelper.WriteHeaderRow(ws, headers, headers.Length);
    }

    private static void FinishSheet(IXLWorksheet ws)
    {
        ws.RangeUsed()?.SetAutoFilter();
        ExcelExportHelper.FinishSheet(ws);
    }

    private static void WriteDecimal(IXLCell cell, decimal value)
    {
        cell.Value = value.ToString("0.##", CultureInfo.InvariantCulture);
        cell.Style.NumberFormat.Format = "@";
    }

    private static string L(string key, string fallback)
    {
        var value = ExcelExportHelper.L(key);
        return string.IsNullOrWhiteSpace(value) || value == key ? fallback : DigitNormalizer.ToEnglishDigits(value);
    }

    private static string LocalizeSheetName(string key, string fallback)
    {
        var value = L(key, fallback);
        return SanitizeSheetName(value);
    }

    private static string SanitizeSheetName(string name)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        foreach (var ch in invalid)
            name = name.Replace(ch, ' ');
        return name.Length > 31 ? name[..31] : name;
    }

    private static void ValidateExcelFile(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
            throw new ArgumentException(LocalizationManager.Get("Msg_ExcelImportInvalidFile") ?? "Invalid file format. Please select an Excel file (.xlsx or .xls).");
    }

    private static ArgumentException MissingNameColumnException()
    {
        return new ArgumentException(LocalizationManager.Get("Msg_ExcelImportMissingNameCol") ?? "The required 'Name' or 'Full Name' column is missing from the Excel sheet.");
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet ws, Dictionary<string, string[]> aliases)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastColumn = ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var col = 1; col <= lastColumn; col++)
        {
            var header = NormalizeHeader(ws.Cell(1, col).GetValue<string>());
            if (string.IsNullOrWhiteSpace(header))
                continue;

            foreach (var (key, values) in aliases)
            {
                if (result.ContainsKey(key))
                    continue;

                if (values.Any(alias => NormalizeHeader(alias) == header))
                {
                    result[key] = col;
                    break;
                }
            }
        }

        return result;
    }

    private static Dictionary<string, int> MergeHeaderMapWithDefaults(
        Dictionary<string, int> headers,
        Dictionary<string, int> defaults,
        bool hasHeaders)
    {
        if (!hasHeaders)
            return defaults;

        var merged = new Dictionary<string, int>(defaults, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in headers)
            merged[key] = value;
        return merged;
    }

    private static string NormalizeHeader(string? value)
    {
        var normalized = DigitNormalizer.NormalizeText(value);
        var paren = normalized.IndexOf('(');
        if (paren >= 0)
            normalized = normalized[..paren].Trim();

        return normalized
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal);
    }

    private static bool IsBlankRow(IXLWorksheet ws, int row)
    {
        return !ws.Row(row).CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetValue<string>()));
    }

    private static string CellText(IXLWorksheet ws, int row, Dictionary<string, int> columns, string key)
    {
        if (!columns.TryGetValue(key, out var col))
            return string.Empty;
        return DigitNormalizer.ToEnglishDigits(ws.Cell(row, col).GetValue<string>()).Trim();
    }

    private static bool TryReadDecimal(IXLWorksheet ws, int row, Dictionary<string, int> columns, string key, bool required, out decimal value, out string error)
    {
        value = 0m;
        error = string.Empty;
        var raw = CellText(ws, row, columns, key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (required)
            {
                error = $"{ReadableKey(key)} is required.";
                return false;
            }
            return true;
        }

        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            error = $"{ReadableKey(key)} must be a valid number.";
            return false;
        }

        if (value < 0)
        {
            error = $"{ReadableKey(key)} cannot be negative.";
            return false;
        }

        return true;
    }

    private static bool TryReadInt(IXLWorksheet ws, int row, Dictionary<string, int> columns, string key, int defaultValue, out int value, out string error)
    {
        value = defaultValue;
        error = string.Empty;
        var raw = CellText(ws, row, columns, key);
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            error = $"{ReadableKey(key)} must be a whole number.";
            return false;
        }

        if (value <= 0)
        {
            error = $"{ReadableKey(key)} must be greater than 0.";
            return false;
        }

        return true;
    }

    private static string ReadableKey(string key)
    {
        return key switch
        {
            "unitprice" => "Unit Price",
            "defaultquantity" => "Default Quantity",
            "weight" => "Weight",
            _ => key
        };
    }

    private static void AddProductError(ProductImportResult result, int row, string message)
    {
        result.Summary.ErrorCount++;
        result.Summary.SkippedCount++;
        var rowMessage = $"Row {row}: {message}";
        result.Summary.Errors.Add(rowMessage);
        result.Summary.Message = string.Join(Environment.NewLine, result.Summary.Errors);
    }

    private static void AddCustomerError(CustomerImportResult result, int row, string message)
    {
        result.Summary.ErrorCount++;
        result.Summary.SkippedCount++;
        var rowMessage = $"Row {row}: {message}";
        result.Summary.Errors.Add(rowMessage);
        result.Summary.Message = string.Join(Environment.NewLine, result.Summary.Errors);
    }

    private static string Clean(string? value)
    {
        return DigitNormalizer.ToEnglishDigits(value ?? string.Empty);
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ProductType ParseProductType(string? value)
    {
        var normalized = NormalizeHeader(value);
        if (normalized.Contains("service", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("hizmet", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("خدمة", StringComparison.OrdinalIgnoreCase))
            return ProductType.Service;
        return ProductType.Physical;
    }

    private static ImportanceLevel ParseImportance(string? value)
    {
        var normalized = NormalizeHeader(value);
        if (normalized.Contains("very", StringComparison.OrdinalIgnoreCase) || normalized.Contains("جدا", StringComparison.OrdinalIgnoreCase) || normalized.Contains("cok", StringComparison.OrdinalIgnoreCase))
            return ImportanceLevel.VeryImportant;
        if (normalized.Contains("important", StringComparison.OrdinalIgnoreCase) || normalized.Contains("مهم", StringComparison.OrdinalIgnoreCase) || normalized.Contains("onemli", StringComparison.OrdinalIgnoreCase))
            return ImportanceLevel.Important;
        return ImportanceLevel.Normal;
    }

    private static FollowUpStage ParseFollowUpStage(string? value)
    {
        var normalized = NormalizeHeader(value);
        if (normalized.Contains("contact", StringComparison.OrdinalIgnoreCase)) return FollowUpStage.Contacted;
        if (normalized.Contains("interest", StringComparison.OrdinalIgnoreCase)) return FollowUpStage.Interested;
        if (normalized.Contains("quotation", StringComparison.OrdinalIgnoreCase)) return FollowUpStage.QuotationSent;
        if (normalized.Contains("negotiation", StringComparison.OrdinalIgnoreCase)) return FollowUpStage.Negotiation;
        if (normalized.Contains("won", StringComparison.OrdinalIgnoreCase)) return FollowUpStage.Won;
        if (normalized.Contains("lost", StringComparison.OrdinalIgnoreCase)) return FollowUpStage.Lost;
        return FollowUpStage.New;
    }

    private static CommercialMindset ParseCommercialMindset(string? value)
    {
        var normalized = NormalizeHeader(value);
        if (normalized.Contains("professional", StringComparison.OrdinalIgnoreCase)) return CommercialMindset.Professional;
        if (normalized.Contains("new", StringComparison.OrdinalIgnoreCase)) return CommercialMindset.New;
        return CommercialMindset.Simple;
    }

    private static bool ParseBoolean(string? value, bool defaultValue)
    {
        var normalized = NormalizeHeader(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return defaultValue;
        return normalized is "true" or "yes" or "1" or "active" or "نشط" or "aktif";
    }

    private static bool IsDuplicateCustomer(List<Customer> existing, List<Customer> pending, string? phone, string? email)
    {
        return HasCustomerMatch(existing, phone, email) || HasCustomerMatch(pending, phone, email);
    }

    private static bool HasCustomerMatch(IEnumerable<Customer> customers, string? phone, string? email)
    {
        return customers.Any(c =>
            (!string.IsNullOrWhiteSpace(phone) && string.Equals(c.Phone, phone, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(email) && string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsDuplicateProduct(List<Product> existing, List<Product> pending, string name, decimal weight, string weightUnit)
    {
        return HasProductMatch(existing, name, weight, weightUnit) || HasProductMatch(pending, name, weight, weightUnit);
    }

    private static bool HasProductMatch(IEnumerable<Product> products, string name, decimal weight, string weightUnit)
    {
        var incomingName = DigitNormalizer.NormalizeText(name);
        var incomingWeightKg = DigitNormalizer.NormalizeWeightToKg(weight, weightUnit);

        foreach (var product in products)
        {
            var names = new List<string> { product.Name };
            if (product.LocalizedTexts != null)
                names.AddRange(product.LocalizedTexts.Select(t => t.Name));

            var nameMatches = names.Any(n => DigitNormalizer.NormalizeText(n) == incomingName);
            if (!nameMatches)
                continue;

            var productWeightKg = DigitNormalizer.NormalizeWeightToKg(product.Weight, product.WeightUnit);
            if (!productWeightKg.HasValue && !incomingWeightKg.HasValue)
                return true;
            if (productWeightKg.HasValue && incomingWeightKg.HasValue && Math.Abs(productWeightKg.Value - incomingWeightKg.Value) < 0.0001m)
                return true;
        }

        return false;
    }

    private static void SetWorkbookMetadata(XLWorkbook workbook)
    {
        workbook.Properties.Title = "KARZOUN ERP Export";
        workbook.Properties.Subject = "KARZOUN ERP business data export";
        workbook.Properties.Author = "Karzoun";
        workbook.Properties.Company = "Karzoun";
        workbook.Properties.Comments = "Generated by KARZOUN ERP 1.1.0";
    }

    private static void AddLocalizedText(Product product, string languageCode, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description))
            return;

        product.LocalizedTexts.Add(new ProductLocalizedText
        {
            LanguageCode = languageCode,
            Name = string.IsNullOrWhiteSpace(name) ? product.Name : name.Trim(),
            Description = EmptyToNull(description),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }
}
