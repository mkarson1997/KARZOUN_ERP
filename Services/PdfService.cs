using KarzounERP.Data;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Pdf;
using KarzounERP.Services.Interfaces;
using QuestPDF.Fluent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KarzounERP.Services;

public class PdfService : IPdfService
{
    private readonly ICompanyService _companyService;
    private readonly AppDbContext _context;

    public PdfService(ICompanyService companyService, AppDbContext context)
    {
        _companyService = companyService;
        _context = context;
    }

    public byte[] GeneratePdf(SalesDocument document, Company company, Customer customer, string language = "ar")
    {
        var finalLanguage = language;
        if (string.IsNullOrWhiteSpace(finalLanguage) || (finalLanguage != "ar" && finalLanguage != "tr" && finalLanguage != "en"))
        {
            finalLanguage = LocalizationManager.Language;
        }
        var localizedSetting = _companyService.GetLocalizedSettingAsync(company.Id, finalLanguage).GetAwaiter().GetResult();

        // Fallback translation logic: fetch localized product names
        var localizedProductNames = new Dictionary<int, string>();
        var productIds = document.Items
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        if (productIds.Count > 0)
        {
            var productTranslations = _context.ProductLocalizedTexts
                .Where(lt => productIds.Contains(lt.ProductId))
                .ToList();

            var productsMap = _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionary(p => p.Id);

            foreach (var productId in productIds)
            {
                if (productsMap.TryGetValue(productId, out var prod))
                {
                    // 1. Target language translation
                    var targetText = productTranslations.FirstOrDefault(lt => lt.ProductId == productId && lt.LanguageCode.Equals(finalLanguage, StringComparison.OrdinalIgnoreCase));
                    if (targetText != null && !string.IsNullOrWhiteSpace(targetText.Name))
                    {
                        localizedProductNames[productId] = targetText.Name.Trim();
                        continue;
                    }

                    // 2. Main product name
                    if (!string.IsNullOrWhiteSpace(prod.Name))
                    {
                        localizedProductNames[productId] = prod.Name.Trim();
                    }
                }
            }
        }

        if (company.ShowProductImageInQuotation)
        {
            foreach (var item in document.Items)
            {
                Product? product = null;
                if (item.ProductId.HasValue)
                {
                    product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId.Value);
                }

                if (product == null)
                {
                    var normalizedItem = DigitNormalizer.GetCanonicalProductInfo(item.ProductName, item.Weight, item.WeightUnit);
                    product = _context.Products
                        .Where(p => p.CompanyId == company.Id)
                        .AsEnumerable()
                        .FirstOrDefault(p =>
                        {
                            var normalizedProduct = DigitNormalizer.GetCanonicalProductInfo(p.Name, p.Weight, p.WeightUnit);
                            return normalizedProduct.canonicalName == normalizedItem.canonicalName
                                && normalizedProduct.canonicalWeightInKg == normalizedItem.canonicalWeightInKg;
                        });
                }

                if (product != null)
                {
                    item.ProductId ??= product.Id;
                    item.ImagePath = product.ImagePath;
                }
            }
        }

        var doc = new InvoiceDocument(document, company, customer, finalLanguage, localizedSetting, localizedProductNames);
        return doc.GeneratePdf();
    }

    public void SaveAndOpenPdf(SalesDocument document, Company company, Customer customer, string language = "ar")
    {
        var finalLanguage = language;
        if (string.IsNullOrWhiteSpace(finalLanguage) || (finalLanguage != "ar" && finalLanguage != "tr" && finalLanguage != "en"))
        {
            finalLanguage = LocalizationManager.Language;
        }
        var tempPath = Path.Combine(Path.GetTempPath(),
            $"{document.DocumentNumber.Replace("/", "-").Replace("\\", "-")}.pdf");
        var bytes = GeneratePdf(document, company, customer, finalLanguage);
        File.WriteAllBytes(tempPath, bytes);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = tempPath,
            UseShellExecute = true
        });
    }
}
