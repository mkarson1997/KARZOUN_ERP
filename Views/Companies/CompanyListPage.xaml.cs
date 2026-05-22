using FornixxCRM.ViewModels;
using System.Windows.Controls;

namespace FornixxCRM.Views.Companies;

public partial class CompanyListPage : UserControl
{
    public CompanyListPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CompanyViewModel vm)
                await vm.LoadAsync();
        };
    }
}
