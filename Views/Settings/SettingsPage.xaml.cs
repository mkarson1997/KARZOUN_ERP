using FornixxCRM.ViewModels;
using System.Windows.Controls;

namespace FornixxCRM.Views.Settings;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
                await vm.LoadAsync();
        };
    }
}
