using KarzounERP.ViewModels;
using System.Windows.Controls;

namespace KarzounERP.Views.Documents;

public partial class DocumentFormPage : UserControl
{
    public DocumentFormPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is DocumentFormViewModel vm)
            {
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
