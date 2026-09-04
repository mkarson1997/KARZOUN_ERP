using KarzounERP.Models;
using System.Globalization;

namespace KarzounERP.Helpers;

public static class ProductDuplicateHelper
{
    public const double WarningThreshold = 95.0;
    public const double LiveSoftThreshold = 70.0;
    public const double ScanNameThreshold = 90.0;

    public sealed class ProductIdentity
    {
        public string ComparableKey { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string NormalizedName { get; init; } = string.Empty;
    }

    public sealed class RichDuplicateMatchResult
    {
        public Product? ClosestProduct { get; init; }
        public double NameSimilarityPercent { get; init; }
        public double IdentitySimilarityPercent { get; init; }
        public bool SameName { get; init; }
        public bool SameWeight { get; init; }
        public bool SameUnit { get; init; }
        public bool SameType { get; init; }
        public bool IsExactDuplicate { get; init; }
        public string ReasonKey { get; init; } = string.Empty;
        public bool IsPotentialDuplicate => ClosestProduct != null && (
            IsExactDuplicate ||
            (SameWeight && SameUnit && SameType && NameSimilarityPercent >= WarningThreshold) ||
            NameSimilarityPercent >= ScanNameThreshold);
        public bool ShouldWarn => ClosestProduct != null && (
            IsExactDuplicate ||
            (SameWeight && SameUnit && SameType && NameSimilarityPercent >= WarningThreshold));
        public double Similarity => IsExactDuplicate ? 100.0 : NameSimilarityPercent;
        public Product? MatchedProduct => ClosestProduct;
    }

    public sealed class DuplicateMatchResult
    {
        public Product? MatchedProduct { get; init; }
        public double Similarity { get; init; }
        public bool IsExactDuplicate { get; init; }
        public bool ShouldWarn => MatchedProduct != null && Similarity >= WarningThreshold;
    }

    public sealed class DuplicatePairResult
    {
        public Product ProductA { get; init; } = null!;
        public Product ProductB { get; init; } = null!;
        public double NameSimilarityPercent { get; init; }
        public double IdentitySimilarityPercent { get; init; }
        public bool IsExactDuplicate { get; init; }
        public bool SameName { get; init; }
        public bool SameWeight { get; init; }
        public bool SameUnit { get; init; }
        public bool SameType { get; init; }
        public string ReasonKey { get; init; } = string.Empty;
        public string SuggestedActionKey { get; init; } = string.Empty;
        public bool IsReportable { get; init; }
    }

    public static string GetNormalizedProductName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var (baseName, _, _) = DigitNormalizer.ParseNameWeightUnit(name, null, null);
        return DigitNormalizer.NormalizeText(string.IsNullOrWhiteSpace(baseName) ? name : baseName);
    }

    public static ProductIdentity BuildIdentity(string? name, decimal? weight, string? weightUnit, ProductType type)
    {
        var rawName = name?.Trim() ?? string.Empty;
        var (baseName, parsedWeight, parsedUnit) = DigitNormalizer.ParseNameWeightUnit(rawName, weight, weightUnit);
        var normName = DigitNormalizer.NormalizeText(string.IsNullOrWhiteSpace(baseName) ? rawName : baseName);
        var unit = DigitNormalizer.NormalizeUnit(parsedUnit);
        var w = parsedWeight ?? 0m;
        var key = string.Create(CultureInfo.InvariantCulture, $"{normName}|{w:F6}|{unit}|{type}");
        return new ProductIdentity
        {
            ComparableKey = key,
            DisplayName = rawName,
            NormalizedName = normName
        };
    }

    public static IEnumerable<ProductIdentity> GetAllIdentities(Product product)
    {
        yield return BuildIdentity(product.Name, product.Weight, product.WeightUnit, product.Type);
        if (product.LocalizedTexts == null) yield break;
        foreach (var loc in product.LocalizedTexts)
        {
            if (!string.IsNullOrWhiteSpace(loc.Name))
                yield return BuildIdentity(loc.Name, product.Weight, product.WeightUnit, product.Type);
        }
    }

    public static IEnumerable<ProductIdentity> GetEnteredIdentities(
        string name, string arName, string trName, string enName,
        decimal? weight, string? weightUnit, ProductType type)
    {
        foreach (var n in new[] { name, arName, trName, enName })
        {
            if (!string.IsNullOrWhiteSpace(n))
                yield return BuildIdentity(n, weight, weightUnit, type);
        }
    }

    public static IEnumerable<string> GetProductNames(Product product)
    {
        yield return product.Name;
        if (product.LocalizedTexts == null) yield break;
        foreach (var loc in product.LocalizedTexts)
        {
            if (!string.IsNullOrWhiteSpace(loc.Name))
                yield return loc.Name;
        }
    }

    public static RichDuplicateMatchResult FindClosestMatch(
        string name,
        decimal? weight,
        string? weightUnit,
        ProductType type,
        IEnumerable<Product> existingProducts,
        int excludeProductId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new RichDuplicateMatchResult();
        }

        Product? closest = null;
        RichDuplicateMatchResult? best = null;

        foreach (var existing in existingProducts)
        {
            if (existing.Id == excludeProductId) continue;

            var result = CompareEnteredAgainstProduct(
                new[] { name },
                weight,
                weightUnit,
                type,
                existing);

            if (best == null || result.NameSimilarityPercent > best.NameSimilarityPercent ||
                (Math.Abs(result.NameSimilarityPercent - best.NameSimilarityPercent) < 0.01 &&
                 result.IdentitySimilarityPercent > best.IdentitySimilarityPercent))
            {
                best = result;
                closest = existing;
            }
        }

        return best ?? new RichDuplicateMatchResult();
    }

    public static RichDuplicateMatchResult FindBestRichMatch(
        IEnumerable<ProductIdentity> enteredIdentities,
        IEnumerable<string> enteredNames,
        decimal? weight,
        string? weightUnit,
        ProductType type,
        IEnumerable<Product> existingProducts,
        int excludeProductId)
    {
        Product? bestProduct = null;
        RichDuplicateMatchResult? best = null;

        foreach (var existing in existingProducts)
        {
            if (existing.Id == excludeProductId) continue;

            var result = CompareEnteredAgainstProduct(enteredNames, weight, weightUnit, type, existing);

            foreach (var enteredIdentity in enteredIdentities)
            {
                foreach (var existingIdentity in GetAllIdentities(existing))
                {
                    if (enteredIdentity.ComparableKey == existingIdentity.ComparableKey)
                    {
                        return new RichDuplicateMatchResult
                        {
                            ClosestProduct = existing,
                            NameSimilarityPercent = 100.0,
                            IdentitySimilarityPercent = 100.0,
                            SameName = true,
                            SameWeight = true,
                            SameUnit = true,
                            SameType = true,
                            IsExactDuplicate = true,
                            ReasonKey = "DupReason_ExactMatch"
                        };
                    }
                }
            }

            if (best == null ||
                result.IdentitySimilarityPercent > best.IdentitySimilarityPercent ||
                (Math.Abs(result.IdentitySimilarityPercent - best.IdentitySimilarityPercent) < 0.01 &&
                 result.NameSimilarityPercent > best.NameSimilarityPercent))
            {
                best = result;
                bestProduct = existing;
            }
        }

        return best ?? new RichDuplicateMatchResult();
    }

    public static DuplicateMatchResult FindBestMatch(
        IEnumerable<ProductIdentity> enteredIdentities,
        IEnumerable<Product> existingProducts,
        int excludeProductId)
    {
        var names = enteredIdentities.Select(i => i.DisplayName).Distinct().ToList();
        decimal? weight = null;
        string? weightUnit = null;
        var type = ProductType.Physical;

        if (enteredIdentities.Any())
        {
            var first = enteredIdentities.First();
            var parts = first.ComparableKey.Split('|');
            if (parts.Length >= 4)
            {
                type = Enum.TryParse<ProductType>(parts[3], out var parsedType) ? parsedType : ProductType.Physical;
                weightUnit = parts[2];
                if (decimal.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
                    weight = w;
            }
        }

        var rich = FindBestRichMatch(enteredIdentities, names, weight, weightUnit, type, existingProducts, excludeProductId);
        return new DuplicateMatchResult
        {
            MatchedProduct = rich.ClosestProduct,
            Similarity = rich.IdentitySimilarityPercent,
            IsExactDuplicate = rich.IsExactDuplicate
        };
    }

    public static List<DuplicatePairResult> ScanDuplicatePairs(IEnumerable<Product> products)
    {
        var list = products.ToList();
        var pairs = new List<DuplicatePairResult>();

        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                var pair = CompareProductPair(list[i], list[j]);
                if (pair.IsReportable)
                    pairs.Add(pair);
            }
        }

        return pairs
            .OrderByDescending(p => p.IsExactDuplicate)
            .ThenByDescending(p => p.IdentitySimilarityPercent)
            .ThenByDescending(p => p.NameSimilarityPercent)
            .ToList();
    }

    public static DuplicatePairResult CompareProductPair(Product productA, Product productB)
    {
        var namesA = GetProductNames(productA).ToList();
        var namesB = GetProductNames(productB).ToList();

        double maxNameSim = 0;
        double maxIdentitySim = 0;
        var isExact = false;
        var sameName = false;
        var sameWeight = false;
        var sameUnit = false;
        var sameType = productA.Type == productB.Type;

        var (weightA, unitA) = GetCanonicalWeightUnit(productA.Weight, productA.WeightUnit, productA.Name);
        var (weightB, unitB) = GetCanonicalWeightUnit(productB.Weight, productB.WeightUnit, productB.Name);
        sameUnit = unitA == unitB;
        sameWeight = Math.Abs((weightA ?? 0m) - (weightB ?? 0m)) < 0.0001m;

        foreach (var identityA in GetAllIdentities(productA))
        {
            foreach (var identityB in GetAllIdentities(productB))
            {
                if (identityA.ComparableKey == identityB.ComparableKey)
                    isExact = true;
            }
        }

        foreach (var nameA in namesA)
        {
            var normA = GetNormalizedProductName(nameA);
            foreach (var nameB in namesB)
            {
                var normB = GetNormalizedProductName(nameB);
                var nameSim = GetSimilarity(normA, normB);
                if (nameSim > maxNameSim)
                {
                    maxNameSim = nameSim;
                    sameName = normA == normB;
                }
            }
        }

        maxIdentitySim = CalculateIdentitySimilarity(maxNameSim, sameWeight, sameUnit, sameType);
        var reasonKey = ResolveReasonKey(isExact, maxNameSim, maxIdentitySim, sameName, sameWeight, sameUnit, sameType);
        var suggestedActionKey = ResolveSuggestedActionKey(isExact, maxIdentitySim, sameName, sameWeight, sameUnit, sameType);
        var isReportable = isExact ||
                           maxIdentitySim >= WarningThreshold ||
                           maxNameSim >= ScanNameThreshold ||
                           (sameName && sameWeight && sameUnit && sameType);

        return new DuplicatePairResult
        {
            ProductA = productA,
            ProductB = productB,
            NameSimilarityPercent = maxNameSim,
            IdentitySimilarityPercent = maxIdentitySim,
            IsExactDuplicate = isExact,
            SameName = sameName,
            SameWeight = sameWeight,
            SameUnit = sameUnit,
            SameType = sameType,
            ReasonKey = reasonKey,
            SuggestedActionKey = suggestedActionKey,
            IsReportable = isReportable
        };
    }

    public static string GetLocalizedReason(string reasonKey)
    {
        if (string.IsNullOrWhiteSpace(reasonKey)) return string.Empty;
        var text = LocalizationManager.Get(reasonKey);
        return string.IsNullOrWhiteSpace(text) || text == reasonKey ? reasonKey : text;
    }

    public static string GetLocalizedSuggestedAction(string actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey)) return string.Empty;
        var text = LocalizationManager.Get(actionKey);
        return string.IsNullOrWhiteSpace(text) || text == actionKey ? actionKey : text;
    }

    private static RichDuplicateMatchResult CompareEnteredAgainstProduct(
        IEnumerable<string> enteredNames,
        decimal? enteredWeight,
        string? enteredWeightUnit,
        ProductType enteredType,
        Product existing)
    {
        var enteredNameList = enteredNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        if (enteredNameList.Count == 0)
            return new RichDuplicateMatchResult { ClosestProduct = existing };

        var existingNames = GetProductNames(existing).ToList();
        double maxNameSim = 0;
        double maxIdentitySim = 0;
        var isExact = false;
        var sameName = false;

        var primaryEntered = enteredNameList[0];
        var (enteredW, enteredU) = GetCanonicalWeightUnit(enteredWeight, enteredWeightUnit, primaryEntered);
        var (existingW, existingU) = GetCanonicalWeightUnit(existing.Weight, existing.WeightUnit, existing.Name);
        var sameWeight = Math.Abs((enteredW ?? 0m) - (existingW ?? 0m)) < 0.0001m;
        var sameUnit = enteredU == existingU;
        var sameType = enteredType == existing.Type;

        foreach (var enteredName in enteredNameList)
        {
            var enteredNorm = GetNormalizedProductName(enteredName);
            var enteredIdentity = BuildIdentity(enteredName, enteredWeight, enteredWeightUnit, enteredType);

            foreach (var existingName in existingNames)
            {
                var existingNorm = GetNormalizedProductName(existingName);
                var nameSim = GetSimilarity(enteredNorm, existingNorm);
                if (nameSim > maxNameSim)
                {
                    maxNameSim = nameSim;
                    sameName = enteredNorm == existingNorm;
                }

                var existingIdentity = BuildIdentity(existingName, existing.Weight, existing.WeightUnit, existing.Type);
                if (enteredIdentity.ComparableKey == existingIdentity.ComparableKey)
                    isExact = true;
            }
        }

        maxIdentitySim = CalculateIdentitySimilarity(maxNameSim, sameWeight, sameUnit, sameType);
        return new RichDuplicateMatchResult
        {
            ClosestProduct = existing,
            NameSimilarityPercent = maxNameSim,
            IdentitySimilarityPercent = maxIdentitySim,
            SameName = sameName,
            SameWeight = sameWeight,
            SameUnit = sameUnit,
            SameType = sameType,
            IsExactDuplicate = isExact,
            ReasonKey = ResolveReasonKey(isExact, maxNameSim, maxIdentitySim, sameName, sameWeight, sameUnit, sameType)
        };
    }

    private static (decimal? weight, string unit) GetCanonicalWeightUnit(decimal? weight, string? weightUnit, string? name)
    {
        var (_, parsedWeight, parsedUnit) = DigitNormalizer.ParseNameWeightUnit(name ?? string.Empty, weight, weightUnit);
        return (parsedWeight, DigitNormalizer.NormalizeUnit(parsedUnit));
    }

    private static double CalculateIdentitySimilarity(double nameSimilarity, bool sameWeight, bool sameUnit, bool sameType)
    {
        if (!sameType)
            return Math.Min(nameSimilarity, 70.0);
        if (!sameWeight || !sameUnit)
            return Math.Min(nameSimilarity, 80.0);
        return nameSimilarity;
    }

    private static string ResolveReasonKey(
        bool isExact,
        double nameSimilarity,
        double identitySimilarity,
        bool sameName,
        bool sameWeight,
        bool sameUnit,
        bool sameType)
    {
        if (isExact || identitySimilarity >= 99.9)
            return "DupReason_ExactMatch";
        if (sameName && sameWeight && sameUnit && sameType)
            return "DupReason_SameNameWeightUnit";
        if (identitySimilarity >= WarningThreshold)
            return "DupReason_NearExact";
        if (nameSimilarity >= ScanNameThreshold && (!sameWeight || !sameUnit))
            return "DupReason_NameSimilarDifferentWeight";
        if (nameSimilarity >= ScanNameThreshold)
            return "DupReason_HighNameSimilarity";
        return "DupReason_PotentialDuplicate";
    }

    private static string ResolveSuggestedActionKey(
        bool isExact,
        double identitySimilarity,
        bool sameName,
        bool sameWeight,
        bool sameUnit,
        bool sameType)
    {
        if (isExact || identitySimilarity >= WarningThreshold)
            return "DupAction_ReviewEdit";
        if (sameName && sameWeight && sameUnit && sameType)
            return "DupAction_ReviewEdit";
        if (sameName && (!sameWeight || !sameUnit))
            return "DupAction_OkToKeepVariants";
        return "DupAction_ReviewMerge";
    }

    public static double GetSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 100.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;

        s1 = s1.Trim().ToLowerInvariant();
        s2 = s2.Trim().ToLowerInvariant();

        var distance = GetLevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        if (maxLength == 0) return 100.0;

        return (1.0 - (double)distance / maxLength) * 100.0;
    }

    private static int GetLevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; d[i, 0] = i++) { }
        for (var j = 0; j <= m; d[0, j] = j++) { }

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = t[j - 1] == s[i - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
