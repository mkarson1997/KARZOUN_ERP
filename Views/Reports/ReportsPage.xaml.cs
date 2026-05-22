using FornixxCRM.ViewModels;
using System.Windows.Controls;

namespace FornixxCRM.Views.Reports;

public partial class ReportsPage : UserControl
{
    public ReportsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ReportsViewModel vm)
                await vm.LoadAsync();
        };
    }
}
