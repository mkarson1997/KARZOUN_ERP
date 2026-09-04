using System.Collections.Generic;

namespace KarzounERP.Models;

public class ProductImportSummary
{
    public int ImportedCount { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

public class ProductImportResult
{
    public ProductImportSummary Summary { get; set; } = new();
    public List<Product> ProductsToSave { get; set; } = new();
}
