using KarzounERP.Models;

namespace KarzounERP.ViewModels;

public class ProductPickerItem
{
    public bool IsCustomOption { get; init; }
    public Product? Product { get; init; }
    public string DisplayLine { get; init; } = string.Empty;
    public string CustomName { get; init; } = string.Empty;
}