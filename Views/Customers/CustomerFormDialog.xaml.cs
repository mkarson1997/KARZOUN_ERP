using FornixxCRM.Helpers;
using FornixxCRM.ViewModels;
using System.Windows;

namespace FornixxCRM.Views.Customers;

public partial class CustomerFormDialog : Window
{
    public CustomerFormDialog()
    {
        InitializeComponent();
        this.FlowDirection = LocalizationManager.FlowDirection;
        Loaded += (_, _) =>
        {
            if (DataContext is CustomerFormViewModel vm)
                vm.RequestClose += (_, _) =>
                {
                    DialogResult = vm.DialogResult;
                    Close();
                };
        };
    }
}
