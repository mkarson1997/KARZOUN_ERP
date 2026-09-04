using KarzounERP.Models;
using QuestPDF.Infrastructure;
using System.Collections.Generic;

namespace KarzounERP.Pdf;

/// <summary>QuestPDF document entry point — delegates to <see cref="PdfTemplateBuilder"/>.</summary>
public class InvoiceDocument : IDocument
{
    private readonly PdfTemplateBuilder _builder;
    private readonly SalesDocument _document;

    public InvoiceDocument(SalesDocument doc, Company company, Customer customer, string language, CompanyLocalizedSetting? localizedSetting = null, Dictionary<int, string>? localizedProductNames = null)
    {
        var lang = language is "ar" or "tr" or "en" ? language : "en";
        _document = doc;
        _builder = new PdfTemplateBuilder(doc, company, customer, lang, localizedSetting, localizedProductNames);
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"KARZOUN ERP - {_document.DocumentNumber}",
        Author = "Karzoun",
        Subject = "KARZOUN ERP business document",
        Keywords = "KARZOUN ERP, invoice, quotation",
        Creator = "KARZOUN ERP 1.1.0",
        Producer = "KARZOUN ERP"
    };

    public void Compose(IDocumentContainer container) => _builder.Compose(container);
}
