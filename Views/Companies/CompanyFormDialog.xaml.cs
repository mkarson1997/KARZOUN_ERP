using KarzounERP.Helpers;
using KarzounERP.ViewModels;
using System.Windows;

namespace KarzounERP.Views.Companies;

public partial class CompanyFormDialog : Window
{
    public CompanyFormDialog()
    {
        InitializeComponent();
        this.FlowDirection = LocalizationManager.FlowDirection;
        Loaded += (_, _) =>
        {
            if (DataContext is CompanyFormViewModel vm)
            {
                vm.RequestClose += (_, _) =>
                {
                    DialogResult = vm.DialogResult;
                    Close();
                };
                vm.RequestFocus += (fieldName) =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var control = this.FindName(fieldName) as System.Windows.UIElement;
                        control?.Focus();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                };
            }
        };
    }
}
