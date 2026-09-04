using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace KarzounERP.Views.Products;

public partial class DuplicateReportWindow : Window
{
    private readonly List<DuplicatePairRow> _rows;

    public DuplicateReportWindow(List<ProductDuplicateHelper.DuplicatePairResult> pairs, IProductService productService, INotificationService notificationService)
    {
        InitializeComponent();
        FlowDirection = LocalizationManager.FlowDirection;
        _rows = pairs.Select(p => new DuplicatePairRow
        {
            ProductA = p.ProductA,
            ProductB = p.ProductB,
            ProductAName = p.ProductA.DisplayName,
            ProductBName = p.ProductB.DisplayName,
            NameSimilarity = (int)Math.Round(p.NameSimilarityPercent),
            IdentitySimilarity = (int)Math.Round(p.IdentitySimilarityPercent),
            Reason = ProductDuplicateHelper.GetLocalizedReason(p.ReasonKey),
            SuggestedAction = ProductDuplicateHelper.GetLocalizedSuggestedAction(p.SuggestedActionKey)
        }).ToList();
        DuplicateGrid.ItemsSource = _rows;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        OpenEditForm();
    }

    private void DuplicateGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenEditForm();
    }

    private void OpenEditForm()
    {
        if (DuplicateGrid.SelectedItem is not DuplicatePairRow selectedRow)
            return;

        var selectedProduct = selectedRow.ProductA;
        var vm = App.Services.GetRequiredService<ProductFormViewModel>();
        vm.LoadFromProduct(selectedProduct);
        var dialog = new ProductFormDialog { DataContext = vm, Owner = this };
        if (dialog.ShowDialog() == true)
            DuplicateGrid.Items.Refresh();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
