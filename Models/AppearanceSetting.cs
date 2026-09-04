using System.ComponentModel.DataAnnotations;

namespace KarzounERP.Models;

public class AppearanceSetting
{
    public int Id { get; set; }

    // Global app theme colors (stored as Hex strings)
    [MaxLength(20)] public string PrimaryColor { get; set; } = Helpers.KarzounBrand.Teal;
    [MaxLength(20)] public string SecondaryColor { get; set; } = Helpers.KarzounBrand.Blue;
    [MaxLength(20)] public string AccentColor { get; set; } = Helpers.KarzounBrand.Emerald;
    [MaxLength(20)] public string SidebarBackground { get; set; } = Helpers.KarzounBrand.Navy;
    [MaxLength(20)] public string SidebarTextColor { get; set; } = Helpers.KarzounBrand.LightGray;
    [MaxLength(20)] public string ButtonColor { get; set; } = Helpers.KarzounBrand.Teal;
    [MaxLength(20)] public string ButtonTextColor { get; set; } = Helpers.KarzounBrand.Navy;
    [MaxLength(20)] public string CardBackground { get; set; } = Helpers.KarzounBrand.LightCard;
    [MaxLength(20)] public string PageBackground { get; set; } = Helpers.KarzounBrand.LightPage;

    // PDF design colors
    [MaxLength(20)] public string PdfPrimaryColor { get; set; } = Helpers.KarzounBrand.Navy;
    [MaxLength(20)] public string PdfHeaderColor { get; set; } = Helpers.KarzounBrand.Navy;
    [MaxLength(20)] public string PdfTableHeaderColor { get; set; } = Helpers.KarzounBrand.PdfTableHeader;
    [MaxLength(20)] public string PdfBorderColor { get; set; } = Helpers.KarzounBrand.PdfBorder;
    [MaxLength(20)] public string PdfAccentColor { get; set; } = Helpers.KarzounBrand.Teal;
    [MaxLength(20)] public string PdfTotalBoxColor { get; set; } = Helpers.KarzounBrand.PdfTotalBox;

    // PDF layout controls
    public double PdfCompanyInfoTopMargin { get; set; } = 0.0;
    public double PdfLogoTopMargin { get; set; } = 0.0;
    public double PdfHeaderSpacing { get; set; } = 8.0;
    public double PdfTableSpacing { get; set; } = 10.0;
    public double PdfFontSize { get; set; } = 9.0;
}
