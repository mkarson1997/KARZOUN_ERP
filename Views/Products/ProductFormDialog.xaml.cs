using FornixxCRM.Helpers;
using FornixxCRM.ViewModels;
using System.Windows;

namespace FornixxCRM.Views.Products;

public partial class ProductFormDialog : Window
{
    public ProductFormDialog()
    {
        InitializeComponent();
        this.FlowDirection = LocalizationManager.FlowDirection;
        Loaded += (_, _) =>
        {
            if (DataContext is ProductFormViewModel vm)
                vm.RequestClose += (_, _) =>
                {
                    DialogResult = vm.DialogResult;
                    Close();
                };
        };
    }
}
