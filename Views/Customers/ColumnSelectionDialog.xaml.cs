using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace KarzounERP.Views.Customers
{
    public partial class ColumnSelectionDialog : Window
    {
        public List<string> SelectedColumns { get; private set; } = new();

        public ColumnSelectionDialog()
        {
            InitializeComponent();
            this.FlowDirection = KarzounERP.Helpers.LocalizationManager.FlowDirection;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            chkName.IsChecked = true; chkCompany.IsChecked = true;
            chkCountry.IsChecked = true; chkPhone.IsChecked = true;
            chkEmail.IsChecked = true; chkNotes.IsChecked = true;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            chkName.IsChecked = false; chkCompany.IsChecked = false;
            chkCountry.IsChecked = false; chkPhone.IsChecked = false;
            chkEmail.IsChecked = false; chkNotes.IsChecked = false;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (chkName.IsChecked == true) SelectedColumns.Add("Name");
            if (chkCompany.IsChecked == true) SelectedColumns.Add("Company");
            if (chkCountry.IsChecked == true) SelectedColumns.Add("Country");
            if (chkPhone.IsChecked == true) SelectedColumns.Add("Phone");
            if (chkEmail.IsChecked == true) SelectedColumns.Add("Email");
            if (chkNotes.IsChecked == true) SelectedColumns.Add("Notes");

            if (SelectedColumns.Count == 0)
            {
                MessageBox.Show("Please select at least one column", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }
    }
}