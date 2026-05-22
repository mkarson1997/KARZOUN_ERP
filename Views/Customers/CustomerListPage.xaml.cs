using FornixxCRM.ViewModels;
using System.Windows.Controls;

namespace FornixxCRM.Views.Customers;

public partial class CustomerListPage : UserControl
{
    public CustomerListPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CustomerViewModel vm)
                await vm.LoadAsync();
        };
    }
}
