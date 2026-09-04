using System.Globalization;
using KarzounERP.Models;

namespace KarzounERP.Helpers;

public static class ProductSearchHelper
{
    public static string GetPreferredName(Product product)
    {
        var lang = LocalizationManager.Language;
        var localized = product.LocalizedTexts?.FirstOrDefault(t => t.LanguageCode == lang)?.Name;
        if (!string.IsNullOrWhiteSpace(localized))
            return localized.Trim();
        return (product.Name ?? string.Empty).Trim();
    }

    public static string FormatPickerLine(Product product, string? currency = null)
    {
        var parts = new List<string> { GetPreferredName(product) };

        if (product.Weight.HasValue)
        {
            var weight = product.Weight.Value.ToString("G29", CultureInfo.InvariantCulture);
            parts.Add($"{weight} {product.WeightUnit ?? "kg"}");
        }

        parts.Add(ArabicEnumHelper.GetProductTypeLabel(product.Type));
        parts.Add(MoneyFormatter.FormatMoney(product.UnitPrice, currency));

        return string.Join(" — ", parts);
    }

    public static int CalculateRelevanceScore(Product product, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            return 0;

        var names = GetSearchableNames(product);
        var exact = 100;
        var starts = 50;
        var contains = 30;

        foreach (var name in names)
        {
            if (name.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                return exact;
        }

        foreach (var name in names)
        {
            if (name.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                return starts;
        }

        foreach (var name in names)
        {
            if (name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                return contains;
        }

        var typeLabel = ArabicEnumHelper.GetProductTypeLabel(product.Type);
        var description = DigitNormalizer.ToEnglishDigits(product.Description);
        var weightStr = product.Weight.HasValue
            ? DigitNormalizer.ToEnglishDigits(product.Weight.Value.ToString("G29", CultureInfo.InvariantCulture))
            : string.Empty;
        var weightUnit = DigitNormalizer.ToEnglishDigits(product.WeightUnit);

        if (typeLabel.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            description.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            weightStr.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            weightUnit.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }

        if (typeLabel.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            description.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            weightStr.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            weightUnit.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        return 0;
    }

    public static List<Product> SearchProducts(IEnumerable<Product> products, string? query)
    {
        var active = products.Where(p => p.IsActive).ToList();
        if (string.IsNullOrWhiteSpace(query))
        {
            return active
                .OrderBy(GetPreferredName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        var normalized = DigitNormalizer.ToEnglishDigits(query).Trim();
        return active
            .Select(p => new { Product = p, Score = CalculateRelevanceScore(p, normalized) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => GetPreferredName(x.Product), StringComparer.CurrentCultureIgnoreCase)
            .Select(x => x.Product)
            .ToList();
    }

    public static bool HasExactNameMatch(string query, IEnumerable<Product> products)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var normalized = DigitNormalizer.ToEnglishDigits(query).Trim();
        return products.Any(p =>
            GetSearchableNames(p).Any(name => name.Equals(normalized, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> GetSearchableNames(Product product)
    {
        yield return (product.Name ?? string.Empty).Trim();
        if (product.LocalizedTexts == null)
            yield break;

        foreach (var text in product.LocalizedTexts)
        {
            if (!string.IsNullOrWhiteSpace(text.Name))
                yield return text.Name.Trim();
        }
    }
}