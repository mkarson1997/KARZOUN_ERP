using System;
using System.Globalization;

namespace KarzounERP.Helpers;

public static class DigitNormalizer
{
    public static string ToEnglishDigits(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        char[] chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (c >= '٠' && c <= '٩')
            {
                chars[i] = (char)('0' + (c - '٠'));
            }
            else if (c >= '۰' && c <= '۹')
            {
                chars[i] = (char)('0' + (c - '۰'));
            }
        }
        return new string(chars);
    }

    public static string FormatDecimal(decimal value, string format = "N2")
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string FormatDouble(double value, string format = "N2")
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string FormatDate(DateTime date, string format = "yyyy-MM-dd")
    {
        return date.ToString(format, CultureInfo.InvariantCulture);
    }

    public static decimal ParseDecimal(string? input, decimal defaultValue = 0m)
    {
        if (string.IsNullOrWhiteSpace(input)) return defaultValue;
        var normalized = ToEnglishDigits(input);
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
        {
            return val;
        }
        return defaultValue;
    }

    public static int ParseInt(string? input, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(input)) return defaultValue;
        var normalized = ToEnglishDigits(input);
        if (int.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
        {
            return val;
        }
        return defaultValue;
    }

    public static string NormalizeUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return "kg";
        var u = unit.Trim().ToLowerInvariant();
        if (u is "g" or "gram" or "grams" or "غرام" or "غ" or "جرام" or "غرامات")
            return "g";
        if (u is "ton" or "tons" or "tonne" or "tonnes" or "طن" or "اطنان" or "أطنان")
            return "ton";
        return "kg";
    }

    public static decimal? NormalizeWeightToKg(decimal? weight, string unit)
    {
        if (!weight.HasValue) return null;
        var normUnit = NormalizeUnit(unit);
        if (normUnit == "g")
            return weight.Value / 1000m;
        if (normUnit == "ton")
            return weight.Value * 1000m;
        return weight.Value;
    }

    public static (string baseName, decimal? weight, string unit) ParseNameWeightUnit(string name, decimal? weight, string? unit)
    {
        string baseName = name ?? string.Empty;
        decimal? parsedWeight = weight;
        string parsedUnit = unit ?? "kg";

        var pattern = @"(?:-\s*|,\s*|\s+)([0-9\.,٠-٩۱۲-۹]+)\s*(g|gram|grams|غرام|غ|جرام|غرامات|kg|kilogram|kilograms|kilo|كغ|كيلو|كيلوغرام|كيلو\s*جرام|ton|tons|tonne|tonnes|طن|اطنان|أطنان)\s*$";
        var match = System.Text.RegularExpressions.Regex.Match(baseName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var weightStr = match.Groups[1].Value;
            var unitStr = match.Groups[2].Value;
            
            baseName = baseName.Substring(0, match.Index).Trim();
            if (baseName.EndsWith("-") || baseName.EndsWith(","))
            {
                baseName = baseName.Substring(0, baseName.Length - 1).Trim();
            }

            if (!parsedWeight.HasValue || parsedWeight.Value == 0)
            {
                parsedWeight = ParseDecimal(weightStr);
            }
            parsedUnit = NormalizeUnit(unitStr);
        }
        else
        {
            parsedUnit = NormalizeUnit(parsedUnit);
        }

        return (baseName, parsedWeight, parsedUnit);
    }

    public static (string canonicalName, decimal? canonicalWeightInKg) GetCanonicalProductInfo(string name, decimal? weight, string? unit)
    {
        var (baseName, parsedWeight, parsedUnit) = ParseNameWeightUnit(name, weight, unit);
        var canonicalName = NormalizeText(baseName);
        var canonicalWeightInKg = NormalizeWeightToKg(parsedWeight, parsedUnit);
        return (canonicalName, canonicalWeightInKg);
    }

    public static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var t = ToEnglishDigits(text).ToLowerInvariant();
        
        char[] chars = t.Select(c => char.IsPunctuation(c) || c == '-' || c == ',' ? ' ' : c).ToArray();
        var temp = new string(chars);
        
        char[] arr = temp.ToCharArray();
        arr = Array.FindAll<char>(arr, c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c));
        var clean = new string(arr);
        
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", " ").Trim();
        return clean;
    }

    public static bool NameContainsUnitWord(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var pattern = @"\b(g|gram|grams|غرام|غ|جرام|غرامات|kg|kilogram|kilograms|kilo|كغ|كيلو|كيلوغرام|كيلو\s*جرام|ton|tons|tonne|tonnes|طن|اطنان|أطنان)\b";
        var t = name.ToLowerInvariant();
        if (System.Text.RegularExpressions.Regex.IsMatch(t, pattern))
            return true;
            
        string[] arabicUnits = { "غرام", "جرام", "كيلو", "كغ", "كيلوغرام", "طن", "أطنان" };
        foreach (var unitWord in arabicUnits)
        {
            if (t.Contains(unitWord))
                return true;
        }

        return false;
    }
}
