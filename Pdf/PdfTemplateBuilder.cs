using FornixxCRM.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FornixxCRM.Pdf;

/// <summary>Clean A4 invoice/quotation layout — LTR page base, separate label/value cells.</summary>
public sealed class PdfTemplateBuilder
{
    private const string Font = "Arial";
    private const string Orange = "#FF6B00";
    private const string Dark = "#1A2332";
    private const string Gray = "#555555";
    private const string LightBg = "#F5F5F5";
    private const string Border = "#DDDDDD";
    private const float ValueColWidth = 200f;
    private const float PageMargin = 30f;

    private readonly SalesDocument _doc;
    private readonly Company _company;
    private readonly Customer _customer;
    private readonly string _lang;
    private readonly bool _isAr;
    private readonly string _currency;

    public PdfTemplateBuilder(SalesDocument doc, Company company, Customer customer, string language)
    {
        _doc = doc;
        _company = company;
        _customer = customer;
        _lang = language is "ar" or "tr" or "en" ? language : "en";
        _isAr = PdfLabels.IsArabic(_lang);
        _currency = string.IsNullOrWhiteSpace(company.Currency) ? "USD" : company.Currency.Trim();
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(PageMargin);
            page.DefaultTextStyle(s => s.FontFamily(Font).FontSize(9f).FontColor(Dark).DirectionFromLeftToRight());
            page.Content().Column(ComposeBody);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeBody(ColumnDescriptor col)
    {
        if (_customer.CommercialMindset == CommercialMindset.Professional)
        {
            ComposeProfessionalBody(col);
        }
        else
        {
            ComposeSimpleBody(col);
        }
    }

    private void ComposeSimpleBody(ColumnDescriptor col)
    {
        ComposeHeader(col);
        col.Item().PaddingVertical(8);
        ComposeDocInfo(col);
        col.Item().PaddingVertical(8);
        ComposeCustomer(col);
        col.Item().PaddingVertical(10);
        ComposeItems(col);
        col.Item().PaddingVertical(10);
        ComposeTotals(col);
        col.Item().PaddingVertical(8);
        ComposeNotes(col);
        ComposeSignatures(col);
    }

    private void ComposeProfessionalBody(ColumnDescriptor col)
    {
        // 1. Professional Header Banner: Deep Dark Blue / Charcoal Background
        col.Item().Background("#101D33").PaddingVertical(12).PaddingHorizontal(16).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Element(x => ValueText(x, _company.Name, 15f, "#FFFFFF", bold: true, rtl: _isAr));
                if (!string.IsNullOrWhiteSpace(_company.CommercialName) && _company.CommercialName != _company.Name)
                    c.Item().Element(x => ValueText(x, _company.CommercialName, 9f, "#B0BEC5", rtl: _isAr));
                if (!string.IsNullOrWhiteSpace(_company.Phone))
                    c.Item().Element(x => LtrValueText(x, _company.Phone, 8.5f, "#CFD8DC"));
                if (!string.IsNullOrWhiteSpace(_company.Email))
                    c.Item().Element(x => LtrValueText(x, _company.Email, 8.5f, "#CFD8DC"));
            });

            row.ConstantItem(130).AlignRight().Column(c =>
            {
                if (!string.IsNullOrWhiteSpace(_company.LogoPath) && File.Exists(_company.LogoPath))
                    c.Item().Height(48).Image(_company.LogoPath).FitHeight();
                else
                    c.Item().Background(Orange).Padding(8)
                        .Element(x => ValueText(x, DocTitle(), 10f, "#FFFFFF", bold: true, rtl: _isAr));
            });
        });

        col.Item().PaddingVertical(10);

        // 2. Info blocks side by side
        col.Item().Row(row =>
        {
            // Left Card: Customer Details
            row.RelativeItem(1.2f).Background(LightBg).BorderLeft(3f).BorderColor("#00897B").Padding(10).Column(c =>
            {
                c.Item().Element(x => LabelText(x, PdfLabels.Get("CustInfo", _lang), 9f, "#00897B", bold: true, rtl: _isAr));
                c.Item().PaddingTop(2).Element(x => ValueText(x, _customer.FullName, 9.5f, Dark, bold: true, rtl: _isAr));
                if (!string.IsNullOrWhiteSpace(_customer.CompanyName))
                    AddInfoRowSmall(c, PdfLabels.Get("Company", _lang), _customer.CompanyName, ltrValue: false);
                if (!string.IsNullOrWhiteSpace(_customer.Phone))
                    AddInfoRowSmall(c, PdfLabels.Get("Phone", _lang), _customer.Phone, ltrValue: true);
                if (!string.IsNullOrWhiteSpace(_customer.Email))
                    AddInfoRowSmall(c, PdfLabels.Get("Email", _lang), _customer.Email, ltrValue: true);
            });

            row.ConstantItem(16);

            // Right Card: Document Metadata
            row.RelativeItem(1f).Background(LightBg).BorderLeft(3f).BorderColor(Orange).Padding(10).Column(c =>
            {
                c.Item().Element(x => LabelText(x, DocTitle(), 9f, Orange, bold: true, rtl: _isAr));
                AddInfoRowSmall(c, PdfLabels.Get("DocNo", _lang), PdfFormatters.FormatDocumentNumber(_doc.DocumentNumber), ltrValue: true);
                AddInfoRowSmall(c, PdfLabels.Get("Date", _lang), PdfFormatters.FormatDate(_doc.Date, _lang), ltrValue: true);
                if (_doc.DueDate.HasValue)
                {
                    var dueKey = _doc.Type == DocumentType.Invoice ? "DueDate" : "ValidUntil";
                    AddInfoRowSmall(c, PdfLabels.Get(dueKey, _lang), PdfFormatters.FormatDate(_doc.DueDate.Value, _lang), ltrValue: true);
                }
                AddInfoRowSmall(c, PdfLabels.Get("Status", _lang), StatusText(_doc.Status), ltrValue: false);
            });
        });

        col.Item().PaddingVertical(12);

        // 3. Grid Items table
        ComposeProfessionalItems(col);

        col.Item().PaddingVertical(10);

        // 4. Totals block
        ComposeProfessionalTotals(col);

        col.Item().PaddingVertical(8);
        ComposeNotes(col);
        ComposeSignatures(col);
    }

    private void AddInfoRowSmall(ColumnDescriptor parent, string label, string? value, bool ltrValue)
    {
        var val = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        parent.Item().PaddingVertical(1).Table(t =>
        {
            if (_isAr)
            {
                t.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); });
                t.Cell().Padding(1).AlignLeft().Element(c => ltrValue ? LtrValueText(c, val, 8f, Dark, bold: true) : ValueText(c, val, 8f, Dark, bold: true, rtl: true));
                t.Cell().Padding(1).AlignRight().Element(c => LabelText(c, label, 8f, Gray, rtl: true));
            }
            else
            {
                t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(120); });
                t.Cell().Padding(1).AlignLeft().Element(c => LabelText(c, label, 8f, Gray, rtl: false));
                t.Cell().Padding(1).AlignRight().Element(c => ltrValue ? LtrValueText(c, val, 8f, Dark, bold: true) : ValueText(c, val, 8f, Dark, bold: true, rtl: false));
            }
        });
    }

    private void ComposeProfessionalItems(ColumnDescriptor col)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3.2f);
                c.RelativeColumn(0.9f);
                c.RelativeColumn(0.75f);
                c.RelativeColumn(0.6f);
                c.RelativeColumn(1.4f);
                c.RelativeColumn(1.5f);
            });

            table.Header(h =>
            {
                HeaderCell(h, PdfLabels.Get("ColProduct", _lang));
                HeaderCell(h, PdfLabels.Get("ColType", _lang));
                HeaderCell(h, PdfLabels.Get("ColWeight", _lang));
                HeaderCell(h, PdfLabels.Get("ColQty", _lang));
                HeaderCell(h, PdfLabels.Get("ColUnitPrice", _lang));
                HeaderCell(h, PdfLabels.Get("ColTotal", _lang));
            });

            var items = _doc.Items.OrderBy(i => i.SortOrder).ToList();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var bg = i % 2 == 0 ? "#FFFFFF" : "#F8F9FA";
                DataCell(bg).Element(c => ValueText(c, item.ProductName, 8f, Dark, rtl: _isAr));
                DataCell(bg).AlignCenter().Element(c => ValueText(c, TypeText(item.ProductType), 8f, Dark, rtl: _isAr));
                var w = item.Weight.HasValue ? item.Weight.Value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) : "-";
                DataCell(bg).AlignCenter().Element(c => LtrValueText(c, w, 8f, Dark));
                DataCell(bg).AlignCenter().Element(c => LtrValueText(c, item.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture), 8f, Dark));
                DataCell(bg).AlignRight().Element(c => LtrValueText(c, PdfFormatters.FormatMoney(item.UnitPrice, _currency), 8f, Dark));
                DataCell(bg).AlignRight().Element(c => LtrValueText(c, PdfFormatters.FormatMoney(item.LineTotal, _currency), 8f, Dark, bold: true));

                IContainer DataCell(string bgColor) => table.Cell().Background(bgColor)
                    .BorderBottom(0.3f).BorderColor("#E0E0E0").PaddingVertical(5).PaddingHorizontal(3);
            }

            void HeaderCell(TableCellDescriptor h, string text) =>
                h.Cell().Background("#101D33").Padding(6).AlignCenter()
                    .Element(c => ValueText(c, text, 8f, "#FFFFFF", bold: true, rtl: _isAr));
        });
    }

    private void ComposeProfessionalTotals(ColumnDescriptor col)
    {
        col.Item().AlignRight().Width(300).Border(1f).BorderColor("#101D33").Background("#F8F9FA").Padding(10).Table(table =>
        {
            if (_isAr)
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(120);
                    c.RelativeColumn();
                });
            }
            else
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(120);
                });
            }

            AddTotalRow(table, PdfLabels.Get("Subtotal", _lang), PdfFormatters.FormatMoney(_doc.Subtotal, _currency));
            if (_doc.DiscountAmount > 0)
                AddTotalRow(table, PdfLabels.Get("Discount", _lang), "-" + PdfFormatters.FormatMoney(_doc.DiscountAmount, _currency));
            if (_doc.TaxRate > 0)
            {
                var taxLbl = $"{PdfLabels.Get("Tax", _lang)} ({_doc.TaxRate.ToString("N1", System.Globalization.CultureInfo.InvariantCulture)}%)";
                AddTotalRow(table, taxLbl, PdfFormatters.FormatMoney(_doc.TaxAmount, _currency));
            }
            table.Cell().ColumnSpan(2).PaddingVertical(4).LineHorizontal(1).LineColor("#101D33");
            AddTotalRow(table, PdfLabels.Get("GrandTotal", _lang), PdfFormatters.FormatMoney(_doc.GrandTotal, _currency), bold: true, color: "#101D33");
            if (_doc.Type == DocumentType.Invoice)
            {
                AddTotalRow(table, PdfLabels.Get("Paid", _lang), PdfFormatters.FormatMoney(_doc.PaidAmount, _currency));
                var rem = Math.Max(0, _doc.GrandTotal - _doc.PaidAmount);
                AddTotalRow(table, PdfLabels.Get("Remaining", _lang), PdfFormatters.FormatMoney(rem, _currency), bold: true, color: rem > 0 ? "#D32F2F" : "#388E3C");
            }
        });
    }

    private void ComposeSignatures(ColumnDescriptor col)
    {
        var hasStamp = !string.IsNullOrWhiteSpace(_company.StampPath) && File.Exists(_company.StampPath);
        
        byte[]? qrBytes = null;
        try
        {
            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            {
                var template = _company.QrCodeTemplate;
                string qrText;
                if (string.IsNullOrWhiteSpace(template))
                {
                    qrText = $"{_company.Name}\n" +
                             $"{(_doc.Type == DocumentType.Invoice ? "Invoice" : "Quotation")}: {_doc.DocumentNumber}\n" +
                             $"Date: {_doc.Date:yyyy-MM-dd}\n" +
                             $"Total: {PdfFormatters.FormatMoney(_doc.GrandTotal, _currency)}";
                }
                else
                {
                    qrText = template
                        .Replace("{DocumentNumber}", _doc.DocumentNumber ?? "")
                        .Replace("{Total}", _doc.GrandTotal.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        .Replace("{CompanyName}", _company.Name ?? "")
                        .Replace("{CustomerName}", _customer.FullName ?? "")
                        .Replace("{DocumentDate}", _doc.Date.ToString("yyyy-MM-dd"));
                }
                
                using (var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCoder.QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
                {
                    qrBytes = qrCode.GetGraphic(20);
                }
            }
        }
        catch
        {
            // Fail-safe
        }

        if (hasStamp || qrBytes != null)
        {
            col.Item().PaddingTop(12).Row(row =>
            {
                if (_isAr)
                {
                    if (hasStamp)
                        row.ConstantItem(120).AlignLeft().Height(80).Image(_company.StampPath!).FitHeight();
                    
                    row.RelativeItem();
                    
                    if (qrBytes != null)
                        row.ConstantItem(80).AlignRight().Height(80).Image(qrBytes).FitHeight();
                }
                else
                {
                    if (qrBytes != null)
                        row.ConstantItem(80).AlignLeft().Height(80).Image(qrBytes).FitHeight();
                    
                    row.RelativeItem();
                    
                    if (hasStamp)
                        row.ConstantItem(120).AlignRight().Height(80).Image(_company.StampPath!).FitHeight();
                }
            });
        }
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Element(x => ValueText(x, _company.Name, 14f, Orange, bold: true, rtl: _isAr));
                if (!string.IsNullOrWhiteSpace(_company.CommercialName) && _company.CommercialName != _company.Name)
                    c.Item().Element(x => ValueText(x, _company.CommercialName, 9f, Gray, rtl: _isAr));
                if (!string.IsNullOrWhiteSpace(_company.Phone))
                    c.Item().Element(x => LtrValueText(x, _company.Phone, 8.5f, Gray));
                if (!string.IsNullOrWhiteSpace(_company.Email))
                    c.Item().Element(x => LtrValueText(x, _company.Email, 8.5f, Gray));
                if (!string.IsNullOrWhiteSpace(_company.Address))
                    c.Item().Element(x => ValueText(x, _company.Address, 8.5f, Gray, rtl: _isAr));
            });

            row.ConstantItem(130).AlignRight().Column(c =>
            {
                if (!string.IsNullOrWhiteSpace(_company.LogoPath) && File.Exists(_company.LogoPath))
                    c.Item().Height(48).Image(_company.LogoPath).FitHeight();
                else
                    c.Item().Background(Orange).Padding(8)
                        .Element(x => ValueText(x, DocTitle(), 10f, "#FFFFFF", bold: true, rtl: _isAr));
            });
        });
        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Orange);
    }

    private void ComposeDocInfo(ColumnDescriptor col)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Element(x => ValueText(x, DocTitle(), 14f, Dark, bold: true, rtl: _isAr));
            row.ConstantItem(280).Column(c =>
            {
                AddInfoRow(c, PdfLabels.Get("DocNo", _lang),
                    PdfFormatters.FormatDocumentNumber(_doc.DocumentNumber), ltrValue: true);
                AddInfoRow(c, PdfLabels.Get("Date", _lang),
                    PdfFormatters.FormatDate(_doc.Date, _lang), ltrValue: true);
                if (_doc.DueDate.HasValue)
                {
                    var dueKey = _doc.Type == DocumentType.Invoice ? "DueDate" : "ValidUntil";
                    AddInfoRow(c, PdfLabels.Get(dueKey, _lang),
                        PdfFormatters.FormatDate(_doc.DueDate.Value, _lang), ltrValue: true);
                }
                AddInfoRow(c, PdfLabels.Get("Status", _lang), StatusText(_doc.Status), ltrValue: false);
            });
        });
    }

    private void ComposeCustomer(ColumnDescriptor col)
    {
        col.Item().Background(LightBg).Border(0.5f).BorderColor(Border).Padding(10).Column(c =>
        {
            c.Item().Element(x => LabelText(x, PdfLabels.Get("CustInfo", _lang), 10f, Orange, bold: true, rtl: _isAr));
            c.Item().PaddingTop(4).Element(x => ValueText(x, _customer.FullName, 10f, Dark, bold: true, rtl: _isAr));
            if (!string.IsNullOrWhiteSpace(_customer.CompanyName))
                AddInfoRow(c, PdfLabels.Get("Company", _lang), _customer.CompanyName, ltrValue: false);
            if (!string.IsNullOrWhiteSpace(_customer.Phone))
                AddInfoRow(c, PdfLabels.Get("Phone", _lang), _customer.Phone, ltrValue: true);
            if (!string.IsNullOrWhiteSpace(_customer.Email))
                AddInfoRow(c, PdfLabels.Get("Email", _lang), _customer.Email, ltrValue: true);
            if (!string.IsNullOrWhiteSpace(_customer.Country))
                AddInfoRow(c, PdfLabels.Get("Country", _lang), _customer.Country, ltrValue: false);
        });
    }

    private void ComposeItems(ColumnDescriptor col)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3.2f);
                c.RelativeColumn(0.9f);
                c.RelativeColumn(0.75f);
                c.RelativeColumn(0.6f);
                c.RelativeColumn(1.4f);
                c.RelativeColumn(1.5f);
            });

            table.Header(h =>
            {
                HeaderCell(h, PdfLabels.Get("ColProduct", _lang));
                HeaderCell(h, PdfLabels.Get("ColType", _lang));
                HeaderCell(h, PdfLabels.Get("ColWeight", _lang));
                HeaderCell(h, PdfLabels.Get("ColQty", _lang));
                HeaderCell(h, PdfLabels.Get("ColUnitPrice", _lang));
                HeaderCell(h, PdfLabels.Get("ColTotal", _lang));
            });

            var items = _doc.Items.OrderBy(i => i.SortOrder).ToList();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var bg = i % 2 == 0 ? "#FFFFFF" : "#FAFAFA";
                DataCell(bg).Element(c => ValueText(c, item.ProductName, 8f, Dark, rtl: _isAr));
                DataCell(bg).AlignCenter().Element(c => ValueText(c, TypeText(item.ProductType), 8f, Dark, rtl: _isAr));
                var w = item.Weight.HasValue ? item.Weight.Value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) : "-";
                DataCell(bg).AlignCenter().Element(c => LtrValueText(c, w, 8f, Dark));
                DataCell(bg).AlignCenter().Element(c => LtrValueText(c, item.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture), 8f, Dark));
                DataCell(bg).AlignRight().Element(c => LtrValueText(c, PdfFormatters.FormatMoney(item.UnitPrice, _currency), 8f, Dark));
                DataCell(bg).AlignRight().Element(c => LtrValueText(c, PdfFormatters.FormatMoney(item.LineTotal, _currency), 8f, Dark, bold: true));

                IContainer DataCell(string bgColor) => table.Cell().Background(bgColor)
                    .BorderBottom(0.3f).BorderColor("#EEEEEE").PaddingVertical(4).PaddingHorizontal(3);
            }

            void HeaderCell(TableCellDescriptor h, string text) =>
                h.Cell().Background(Orange).Padding(5).AlignCenter()
                    .Element(c => ValueText(c, text, 8f, "#FFFFFF", bold: true, rtl: _isAr));
        });
    }

    private void ComposeTotals(ColumnDescriptor col)
    {
        col.Item().Border(0.8f).BorderColor(Border).Background("#FAFAFA").Padding(10).Table(table =>
        {
            if (_isAr)
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(ValueColWidth);
                    c.RelativeColumn(2f);
                });
            }
            else
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2f);
                    c.ConstantColumn(ValueColWidth);
                });
            }

            AddTotalRow(table, PdfLabels.Get("Subtotal", _lang), PdfFormatters.FormatMoney(_doc.Subtotal, _currency));
            if (_doc.DiscountAmount > 0)
                AddTotalRow(table, PdfLabels.Get("Discount", _lang), "-" + PdfFormatters.FormatMoney(_doc.DiscountAmount, _currency));
            if (_doc.TaxRate > 0)
            {
                var taxLbl = $"{PdfLabels.Get("Tax", _lang)} ({_doc.TaxRate.ToString("N1", System.Globalization.CultureInfo.InvariantCulture)}%)";
                AddTotalRow(table, taxLbl, PdfFormatters.FormatMoney(_doc.TaxAmount, _currency));
            }
            table.Cell().ColumnSpan(2).PaddingVertical(4).LineHorizontal(1).LineColor(Orange);
            AddTotalRow(table, PdfLabels.Get("GrandTotal", _lang), PdfFormatters.FormatMoney(_doc.GrandTotal, _currency), bold: true, color: Orange);
            if (_doc.Type == DocumentType.Invoice)
            {
                AddTotalRow(table, PdfLabels.Get("Paid", _lang), PdfFormatters.FormatMoney(_doc.PaidAmount, _currency));
                var rem = Math.Max(0, _doc.GrandTotal - _doc.PaidAmount);
                AddTotalRow(table, PdfLabels.Get("Remaining", _lang), PdfFormatters.FormatMoney(rem, _currency));
            }
        });
    }

    private void ComposeNotes(ColumnDescriptor col)
    {
        if (!string.IsNullOrWhiteSpace(_doc.Notes))
        {
            col.Item().PaddingTop(4).Column(c =>
            {
                c.Item().Element(x => LabelText(x, PdfLabels.Get("Notes", _lang), 9f, Gray, bold: true, rtl: _isAr));
                c.Item().PaddingTop(2).Element(x => ValueText(x, _doc.Notes, 8.5f, Dark, rtl: _isAr));
            });
        }
        if (!string.IsNullOrWhiteSpace(_doc.PaymentAddress))
        {
            col.Item().PaddingTop(6).Column(c =>
            {
                c.Item().Element(x => LabelText(x, PdfLabels.Get("PaymentInfo", _lang), 9f, Gray, bold: true, rtl: _isAr));
                c.Item().PaddingTop(2).Element(x => ValueText(x, _doc.PaymentAddress, 8.5f, Dark, rtl: _isAr));
            });
        }
        if (!string.IsNullOrWhiteSpace(_doc.ShippingNote))
        {
            col.Item().PaddingTop(6).Background("#FFF8E1").Padding(6)
                .Element(x => ValueText(x, $"{PdfLabels.Get("ShippingNote", _lang)}: {_doc.ShippingNote}", 8f, "#E65100", rtl: _isAr));
        }
    }

    private void ComposeFooter(IContainer container)
    {
        var footer = !string.IsNullOrWhiteSpace(_doc.FooterText) ? _doc.FooterText : PdfLabels.Get("FooterDefault", _lang);
        container.PaddingTop(4).Row(row =>
        {
            if (_isAr)
            {
                row.RelativeItem().AlignRight().Element(c => LabelText(c, footer, 7.5f, "#777777"));
                row.ConstantItem(120).AlignLeft().Element(c => PageNumberText(c));
            }
            else
            {
                row.RelativeItem().Element(c => ValueText(c, footer, 7.5f, "#777777", rtl: false));
                row.ConstantItem(100).AlignRight().Element(c => PageNumberText(c));
            }
        });
    }

    private void AddInfoRow(ColumnDescriptor parent, string label, string? value, bool ltrValue)
    {
        var val = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        parent.Item().PaddingVertical(2).Table(t =>
        {
            if (_isAr)
            {
                t.ColumnsDefinition(c => { c.ConstantColumn(ValueColWidth); c.RelativeColumn(); });
                t.Cell().Padding(2).AlignLeft().Element(c => ltrValue ? LtrValueText(c, val, 9f, Dark, bold: true) : ValueText(c, val, 9f, Dark, bold: true, rtl: true));
                t.Cell().Padding(2).AlignRight().Element(c => LabelText(c, label, 9f, Gray, rtl: true));
            }
            else
            {
                t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(ValueColWidth); });
                t.Cell().Padding(2).AlignLeft().Element(c => LabelText(c, label, 9f, Gray, rtl: false));
                t.Cell().Padding(2).AlignRight().Element(c => ltrValue ? LtrValueText(c, val, 9f, Dark, bold: true) : ValueText(c, val, 9f, Dark, bold: true, rtl: false));
            }
        });
    }

    private void AddTotalRow(TableDescriptor table, string label, string value, bool bold = false, string color = Dark)
    {
        if (_isAr)
        {
            table.Cell().PaddingVertical(3).AlignLeft().Element(c => LtrValueText(c, value, 9.5f, color, bold));
            table.Cell().PaddingVertical(3).AlignRight().Element(c => LabelText(c, label, 9.5f, color, bold, rtl: true));
        }
        else
        {
            table.Cell().PaddingVertical(3).AlignLeft().Element(c => LabelText(c, label, 9.5f, color, bold, rtl: false));
            table.Cell().PaddingVertical(3).AlignRight().Element(c => LtrValueText(c, value, 9.5f, color, bold));
        }
    }

    private IContainer PageNumberText(IContainer container)
    {
        container.Text(t =>
        {
            t.DefaultTextStyle(s => s.FontFamily(Font).FontSize(7.5f).FontColor("#777777").DirectionFromLeftToRight());
            t.Span(PdfLabels.Get("Page", _lang) + " ");
            t.CurrentPageNumber();
            t.Span(" / ");
            t.TotalPages();
        });
        return container;
    }

    private static IContainer LtrValueText(IContainer c, string? text, float size, string color, bool bold = false)
    {
        c.Text(t =>
        {
            t.DefaultTextStyle(s => s.FontFamily(Font).FontSize(size).FontColor(color).DirectionFromLeftToRight());
            var sp = t.Span(string.IsNullOrWhiteSpace(text) ? "" : text);
            if (bold) sp.Bold();
        });
        return c;
    }

    private static IContainer ValueText(IContainer c, string? text, float size, string color, bool bold = false, bool rtl = false)
    {
        if (rtl)
            c.Text(t =>
            {
                t.DefaultTextStyle(s => s.FontFamily(Font).FontSize(size).FontColor(color).DirectionFromRightToLeft());
                var sp = t.Span(Safe(text));
                if (bold) sp.Bold();
            });
        else
            LtrValueText(c, text, size, color, bold);
        return c;
    }

    private static IContainer LabelText(IContainer c, string? text, float size, string color, bool bold = false, bool rtl = true)
    {
        c.Text(t =>
        {
            if (rtl)
                t.DefaultTextStyle(st => st.FontFamily(Font).FontSize(size).FontColor(color).DirectionFromRightToLeft());
            else
                t.DefaultTextStyle(st => st.FontFamily(Font).FontSize(size).FontColor(color).DirectionFromLeftToRight());
            var sp = t.Span(Safe(text));
            if (bold) sp.Bold();
        });
        return c;
    }

    private string DocTitle() =>
        _doc.Type == DocumentType.Invoice
            ? PdfLabels.Get("DocTitleInvoice", _lang)
            : PdfLabels.Get("DocTitleQuotation", _lang);

    private string StatusText(DocumentStatus s) => s switch
    {
        DocumentStatus.Draft => PdfLabels.Get("StDraft", _lang),
        DocumentStatus.Sent => PdfLabels.Get("StSent", _lang),
        DocumentStatus.Accepted => PdfLabels.Get("StAccepted", _lang),
        DocumentStatus.Rejected => PdfLabels.Get("StRejected", _lang),
        DocumentStatus.Paid => PdfLabels.Get("StPaid", _lang),
        DocumentStatus.Cancelled => PdfLabels.Get("StCancelled", _lang),
        DocumentStatus.PartiallyPaid => PdfLabels.Get("StPartial", _lang),
        DocumentStatus.Converted => PdfLabels.Get("StConverted", _lang),
        _ => s.ToString()
    };

    private string TypeText(ProductType t) =>
        t == ProductType.Service ? PdfLabels.Get("TyService", _lang) : PdfLabels.Get("TyPhysical", _lang);

    private static string Safe(string? v) => string.IsNullOrWhiteSpace(v) ? "" : v.Trim();
}
