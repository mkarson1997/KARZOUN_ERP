using FornixxCRM.Helpers;
using FornixxCRM.ViewModels;
using System.Windows;

namespace FornixxCRM.Views.Companies;

public partial class CompanyFormDialog : Window
{
    public CompanyFormDialog()
    {
        InitializeComponent();
        this.FlowDirection = LocalizationManager.FlowDirection;
        Loaded += (_, _) =>
        {
            if (DataContext is CompanyFormViewModel vm)
                vm.RequestClose += (_, _) =>
                {
                    DialogResult = vm.DialogResult;
                    Close();
                };
        };
    }
}
