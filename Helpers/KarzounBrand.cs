namespace KarzounERP.Helpers;

/// <summary>
/// Code-side defaults for the approved visual identity. The matching WPF resources
/// live in Resources/Brand/KarzounBrand.xaml; keeping these values named here avoids
/// scattering raw brand colors through view-model and persistence code.
/// </summary>
public static class KarzounBrand
{
    public const string Navy = "#0B1324";
    public const string Graphite = "#1F2937";
    public const string Silver = "#A6ADB7";
    public const string LightGray = "#EBEBEF";
    public const string Teal = "#00B8C5";
    public const string Emerald = "#00C896";
    public const string Blue = "#0D6EFD";

    public const string LightPage = "#F6F7F9";
    public const string LightCard = "#FFFFFF";
    public const string LightText = Navy;

    public const string DarkPage = Navy;
    public const string DarkCard = "#172033";
    public const string DarkText = "#F3F6FA";

    public const string PdfTableHeader = LightGray;
    public const string PdfBorder = "#D8DDE5";
    public const string PdfTotalBox = "#F0FBFC";
}
