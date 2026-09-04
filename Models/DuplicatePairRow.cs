namespace KarzounERP.Models;

public class DuplicatePairRow
{
    public Product ProductA { get; set; } = null!;
    public Product ProductB { get; set; } = null!;
    public string ProductAName { get; set; } = string.Empty;
    public string ProductBName { get; set; } = string.Empty;
    public int NameSimilarity { get; set; }
    public int IdentitySimilarity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
}