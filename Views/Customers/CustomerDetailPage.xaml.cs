using FornixxCRM.ViewModels;
using System.Windows.Controls;

namespace FornixxCRM.Views.Customers;

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
