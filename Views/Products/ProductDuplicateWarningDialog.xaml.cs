using KarzounERP.Helpers;
using System.Windows;

namespace KarzounERP.Views.Products;

public partial class ProductDuplicateWarningDialog : Window
{
    public bool ContinueAnyway { get; private set; }

    public ProductDuplicateWarningDialog(string message)
    {
        InitializeComponent();
        FlowDirection = LocalizationManager.FlowDirection;
        MessageText.Text = message;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        ContinueAnyway = true;
        DialogResult = true;
        Close();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        ContinueAnyway = false;
        DialogResult = false;
        Close();
    }
}
