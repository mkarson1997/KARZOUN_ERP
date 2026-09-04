using KarzounERP.ViewModels;
using System.Windows.Controls;

namespace KarzounERP.Views.Products;

public partial class ProductListPage : UserControl
{
    public ProductListPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ProductViewModel vm)
                await vm.LoadAsync();
        };
    }
}
