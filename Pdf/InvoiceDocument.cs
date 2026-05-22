using FornixxCRM.Models;
using QuestPDF.Infrastructure;

namespace FornixxCRM.Pdf;

/// <summary>QuestPDF document entry point — delegates to <see cref="PdfTemplateBuilder"/>.</summary>
public class InvoiceDocument : IDocument
{
    private readonly PdfTemplateBuilder _builder;

    public InvoiceDocument(SalesDocument doc, Company company, Customer customer, string language)
    {
        var lang = language is "ar" or "tr" or "en" ? language : "en";
        _builder = new PdfTemplateBuilder(doc, company, customer, lang);
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container) => _builder.Compose(container);
}
