using System.Windows.Controls;
using System.Windows.Threading;
using KarzounERP.ViewModels;

namespace KarzounERP.Views.Documents;

public partial class ProductPickerControl : UserControl
{
    public ProductPickerControl() => InitializeComponent();

    private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not LineItemViewModel item || string.IsNullOrWhiteSpace(item.ProductName))
            return;

        Dispatcher.BeginInvoke(() =>
        {
            if (!string.IsNullOrWhiteSpace(item.ProductName))
                ProductComboBox.Text = item.ProductName;
        }, DispatcherPriority.Background);
    }
}
