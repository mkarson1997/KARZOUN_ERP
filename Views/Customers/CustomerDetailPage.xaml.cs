using KarzounERP.ViewModels;
using System.Windows.Controls;

namespace KarzounERP.Views.Customers;

public partial class CustomerDetailPage : UserControl
{
    public CustomerDetailPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CustomerDetailViewModel vm)
                await vm.LoadAsync();
        };
    }
}
