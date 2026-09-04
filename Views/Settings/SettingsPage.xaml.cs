using KarzounERP.ViewModels;
using System.Windows.Controls;

namespace KarzounERP.Views.Settings;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                await vm.LoadAsync();
                vm.RequestFocus += (fieldName) =>
                {
                    this.Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        var control = this.FindName(fieldName) as System.Windows.UIElement;
                        control?.Focus();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                };
            }
        };
    }
}
