using KarzounERP.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace KarzounERP.Views.Appearance;

public partial class AppearancePage : UserControl
{
    public AppearancePage()
    {
        InitializeComponent();
        Unloaded += AppearancePage_Unloaded;
    }

    private void AppearancePage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppearanceViewModel vm)
        {
            vm.RevertIfUnsaved();
        }
    }
}
