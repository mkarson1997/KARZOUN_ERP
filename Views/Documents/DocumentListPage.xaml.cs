using FornixxCRM.ViewModels;
using System.Windows.Controls;

namespace FornixxCRM.Views.Documents;

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
