using KarzounERP.ViewModels;
using System.Windows.Controls;

namespace KarzounERP.Views.Documents;

public partial class DocumentListPage : UserControl
{
    public DocumentListPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DocumentViewModel vm)
                await vm.LoadAsync();
        };
    }
}
