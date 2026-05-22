using FornixxCRM.ViewModels;
using System.Windows.Controls;

namespace FornixxCRM.Views.Dashboard;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DashboardViewModel vm)
                await vm.LoadAsync();
        };
    }
}
