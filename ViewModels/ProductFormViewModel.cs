using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;

namespace KarzounERP.ViewModels;

public partial class ProductFormViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    private readonly INotificationService _notificationService;
    private readonly AppSession _session;
    private int _editingId = 0;
    private int _companyId;

    [ObservableProperty] private string _windowTitle = LocalizationManager.Get("ProdForm_TitleNew") ?? "New Product";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private ProductType _type = ProductType.Physical;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private decimal _weight;
    [ObservableProperty] private string _weightUnit = "kg"; // "g", "kg", "ton"
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private int _defaultQuantity = 1;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _validationError = string.Empty;
    [ObservableProperty] private string? _imagePath;
    [ObservableProperty] private bool _isNameInvalid;
    [ObservableProperty] private bool _isUnitPriceInvalid;
    [ObservableProperty] private bool _isDefaultQuantityInvalid;
    [ObservableProperty] private bool _isWeightInvalid;
    [ObservableProperty] private bool _showDuplicateIndicator;
    [ObservableProperty] private string _duplicateSimilarityText = string.Empty;
    [ObservableProperty] private string _duplicateClosestProductText = string.Empty;
    [ObservableProperty] private string _duplicateDifferenceText = string.Empty;
    [ObservableProperty] private string _duplicateIndicatorBrush = "#607D8B";
    [ObservableProperty] private string _currencyCode = "USD";
    private List<Product> _existingProducts = new();

    public string UnitPriceHint
    {
        get
        {
            var raw = LocalizationManager.Get("ProdForm_UnitPrice") ?? "Unit Price *";
            var clean = raw.Replace("*", "").Trim();
            return $"{clean} ({CurrencyCode}) *";
        }
    }

    partial void OnCurrencyCodeChanged(string value)
    {
        OnPropertyChanged(nameof(UnitPriceHint));
    }

    // Translation fields (Phase 6)
    [ObservableProperty] private string _nameAr = string.Empty;
    [ObservableProperty] private string _descriptionAr = string.Empty;
    [ObservableProperty] private string _nameTr = string.Empty;
    [ObservableProperty] private string _descriptionTr = string.Empty;
    [ObservableProperty] private string _nameEn = string.Empty;
    [ObservableProperty] private string _descriptionEn = string.Empty;

    public IEnumerable<ProductType> AllProductTypes => ArabicEnumHelper.AllProductTypes;

    public bool? DialogResult { get; private set; }
    public event EventHandler? RequestClose;

    public ProductFormViewModel(IProductService productService, INotificationService notificationService, AppSession session)
    {
        _productService = productService;
        _notificationService = notificationService;
        _session = session;
    }

    public void PrepareNew(int companyId)
    {
        _editingId = 0; _companyId = companyId;
        WindowTitle = LocalizationManager.Get("ProdForm_TitleNewFull") ?? "New Product";
        Name = Description = string.Empty; Type = ProductType.Physical;
        Weight = 0; UnitPrice = 0; DefaultQuantity = 1; IsActive = true;
        ImagePath = null;
        WeightUnit = "kg";
        CurrencyCode = _session.ActiveCompanyCurrency;
        NameAr = DescriptionAr = NameTr = DescriptionTr = NameEn = DescriptionEn = string.Empty;
        ResetValidation();
        _ = LoadExistingProductsForDuplicateCheckAsync();
    }

    public void LoadFromProduct(Product p)
    {
        _editingId = p.Id; _companyId = p.CompanyId;
        WindowTitle = LocalizationManager.Get("ProdForm_TitleEdit") ?? "Edit Product";
        Name = p.Name; Type = p.Type; Description = p.Description ?? "";
        Weight = p.Weight ?? 0; UnitPrice = p.UnitPrice;
        DefaultQuantity = p.DefaultQuantity; IsActive = p.IsActive;
        ImagePath = p.ImagePath;
        WeightUnit = p.WeightUnit ?? "kg";
        CurrencyCode = _session.ActiveCompanyCurrency;

        NameAr = p.LocalizedTexts?.FirstOrDefault(x => x.LanguageCode == "ar")?.Name ?? string.Empty;
        DescriptionAr = p.LocalizedTexts?.FirstOrDefault(x => x.LanguageCode == "ar")?.Description ?? string.Empty;
        NameTr = p.LocalizedTexts?.FirstOrDefault(x => x.LanguageCode == "tr")?.Name ?? string.Empty;
        DescriptionTr = p.LocalizedTexts?.FirstOrDefault(x => x.LanguageCode == "tr")?.Description ?? string.Empty;
        NameEn = p.LocalizedTexts?.FirstOrDefault(x => x.LanguageCode == "en")?.Name ?? string.Empty;
        DescriptionEn = p.LocalizedTexts?.FirstOrDefault(x => x.LanguageCode == "en")?.Description ?? string.Empty;

        ResetValidation();
        _ = LoadExistingProductsForDuplicateCheckAsync();
    }

    private void ResetValidation()
    {
        IsNameInvalid = false;
        IsUnitPriceInvalid = false;
        IsDefaultQuantityInvalid = false;
        IsWeightInvalid = false;
        ValidationError = string.Empty;
    }

    partial void OnNameChanged(string value) => UpdateLiveDuplicateIndicator();
    partial void OnNameArChanged(string value) => UpdateLiveDuplicateIndicator();
    partial void OnNameTrChanged(string value) => UpdateLiveDuplicateIndicator();
    partial void OnNameEnChanged(string value) => UpdateLiveDuplicateIndicator();
    partial void OnWeightChanged(decimal value) => UpdateLiveDuplicateIndicator();
    partial void OnWeightUnitChanged(string value) => UpdateLiveDuplicateIndicator();
    partial void OnTypeChanged(ProductType value) => UpdateLiveDuplicateIndicator();

    private async Task LoadExistingProductsForDuplicateCheckAsync()
    {
        if (_companyId <= 0 || _productService == null) return;
        _existingProducts = await _productService.GetProductsAsync(_companyId);
        UpdateLiveDuplicateIndicator();
    }

    private void UpdateLiveDuplicateIndicator()
    {
        if (string.IsNullOrWhiteSpace(Name) || _existingProducts.Count == 0)
        {
            ShowDuplicateIndicator = false;
            DuplicateSimilarityText = DuplicateClosestProductText = DuplicateDifferenceText = string.Empty;
            return;
        }

        var enteredNames = new[] { Name, NameAr, NameTr, NameEn }
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToList();
        var enteredIdentities = ProductDuplicateHelper.GetEnteredIdentities(Name, NameAr, NameTr, NameEn, Weight, WeightUnit, Type).ToList();
        var match = ProductDuplicateHelper.FindBestRichMatch(
            enteredIdentities, enteredNames, Weight, WeightUnit, Type, _existingProducts, _editingId);

        if (match.ClosestProduct == null || match.NameSimilarityPercent < ProductDuplicateHelper.LiveSoftThreshold)
        {
            ShowDuplicateIndicator = false;
            DuplicateSimilarityText = DuplicateClosestProductText = DuplicateDifferenceText = string.Empty;
            return;
        }

        var percent = Math.Round(match.NameSimilarityPercent).ToString("0", CultureInfo.InvariantCulture);
        DuplicateSimilarityText = string.Format(
            LocalizationManager.Get("DupLive_ClosestSimilarity") ?? "Closest product similarity: {0}%",
            percent);
        DuplicateClosestProductText = string.Format(
            LocalizationManager.Get("DupLive_ClosestProduct") ?? "Closest product: {0}",
            match.ClosestProduct.DisplayName);
        DuplicateDifferenceText = !match.SameWeight || !match.SameUnit || !match.SameType
            ? (LocalizationManager.Get("DupLive_NameSimilarDifferentIdentity") ?? "Name is similar, but weight or type is different.")
            : string.Empty;
        DuplicateIndicatorBrush = match.NameSimilarityPercent >= ProductDuplicateHelper.WarningThreshold
            ? "#D32F2F"
            : "#F57C00";
        ShowDuplicateIndicator = true;
    }

    [RelayCommand]
    private void CopyMainToLanguages()
    {
        if (!string.IsNullOrWhiteSpace(Name))
        {
            NameAr = Name.Trim();
            NameTr = Name.Trim();
            NameEn = Name.Trim();
            DescriptionAr = Description.Trim();
            DescriptionTr = Description.Trim();
            DescriptionEn = Description.Trim();
            _notificationService.Success(LocalizationManager.Get("Msg_TranslationFieldsSaved") ?? "Translation fields populated.");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ResetValidation();

        // 1. Positive Number and Required Validation (Phase 4)
        IsNameInvalid = string.IsNullOrWhiteSpace(Name);
        IsUnitPriceInvalid = UnitPrice < 0;
        IsDefaultQuantityInvalid = DefaultQuantity <= 0;
        IsWeightInvalid = Weight < 0;

        if (IsNameInvalid)
        {
            ValidationError = LocalizationManager.Get("Msg_ValidationProductName") ?? "Product name is required.";
            _notificationService.Error(ValidationError);
            RaiseRequestFocus(nameof(Name));
            return;
        }

        if (IsUnitPriceInvalid || IsDefaultQuantityInvalid || IsWeightInvalid)
        {
            ValidationError = LocalizationManager.Get("Msg_ValidationPositiveNumber") ?? "Please enter a valid positive number.";
            _notificationService.Error(ValidationError);
            if (IsUnitPriceInvalid) RaiseRequestFocus(nameof(UnitPrice));
            else if (IsWeightInvalid) RaiseRequestFocus(nameof(Weight));
            else if (IsDefaultQuantityInvalid) RaiseRequestFocus(nameof(DefaultQuantity));
            return;
        }

        // 2. Duplicate warning: never block variants automatically.
        var existingProducts = await _productService.GetProductsAsync(_companyId);
        var enteredNames = new[] { Name, NameAr, NameTr, NameEn }
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToList();
        var enteredIdentities = ProductDuplicateHelper.GetEnteredIdentities(Name, NameAr, NameTr, NameEn, Weight, WeightUnit, Type).ToList();
        var duplicateMatch = ProductDuplicateHelper.FindBestRichMatch(
            enteredIdentities, enteredNames, Weight, WeightUnit, Type, existingProducts, _editingId);
        if (duplicateMatch.ShouldWarn)
        {
            var similarity = Math.Round(duplicateMatch.IdentitySimilarityPercent >= ProductDuplicateHelper.WarningThreshold
                ? duplicateMatch.IdentitySimilarityPercent
                : duplicateMatch.NameSimilarityPercent);
            var template = LocalizationManager.Get("Msg_ProductMayExistContinue")
                ?? "This product may already exist. Similarity: {0}%. Do you want to continue anyway?";
            var dialog = new KarzounERP.Views.Products.ProductDuplicateWarningDialog(
                string.Format(template, similarity.ToString("0", System.Globalization.CultureInfo.InvariantCulture)));
            if (dialog.ShowDialog() != true || !dialog.ContinueAnyway)
            {
                RaiseRequestFocus(nameof(Name));
                return;
            }
        }

        // 3. Repeated Words Warning (Phase 8)
        var repeatedWord = FindRepeatedWord(Name);
        if (repeatedWord != null)
        {
            _notificationService.Warning(LocalizationManager.Get("Msg_WarningRepeatedWords") ?? "Repeated words were found in the product name.");
        }

        if (DigitNormalizer.NameContainsUnitWord(Name))
        {
            _notificationService.Warning(LocalizationManager.Get("Msg_WarningNameContainsUnit") ?? "Warning: The product name contains weight unit words. Product name should remain separate from weight and unit fields.");
        }

        // 4. Save Logic
        var product = _editingId > 0
            ? await _productService.GetProductAsync(_editingId) ?? new Product()
            : new Product();

        product.CompanyId = _companyId; 
        product.Name = Name.Trim();
        product.Type = Type; 
        product.Description = Description.Trim();
        product.Weight = Weight > 0 ? Weight : null;
        product.WeightUnit = WeightUnit;
        product.UnitPrice = UnitPrice; 
        product.DefaultQuantity = DefaultQuantity;
        product.IsActive = IsActive;
        product.ImagePath = ImagePath;

        // Save translations
        product.LocalizedTexts.Clear();
        if (!string.IsNullOrWhiteSpace(NameAr))
            product.LocalizedTexts.Add(new ProductLocalizedText { LanguageCode = "ar", Name = NameAr.Trim(), Description = DescriptionAr?.Trim(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        if (!string.IsNullOrWhiteSpace(NameTr))
            product.LocalizedTexts.Add(new ProductLocalizedText { LanguageCode = "tr", Name = NameTr.Trim(), Description = DescriptionTr?.Trim(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        if (!string.IsNullOrWhiteSpace(NameEn))
            product.LocalizedTexts.Add(new ProductLocalizedText { LanguageCode = "en", Name = NameEn.Trim(), Description = DescriptionEn?.Trim(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        bool isEdit = _editingId > 0;
        if (isEdit) await _productService.UpdateProductAsync(product);
        else await _productService.AddProductAsync(product);

        _notificationService.Success(isEdit
            ? string.Format(LocalizationManager.Get("Msg_ProductUpdated") ?? "Product '{0}' updated successfully.", product.Name)
            : string.Format(LocalizationManager.Get("Msg_ProductCreated") ?? "Product '{0}' created successfully.", product.Name));

        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private string? FindRepeatedWord(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            if (w.Length <= 1) continue;
            if (!seen.Add(w.ToLowerInvariant()))
                return w;
        }
        return null;
    }

    [RelayCommand]
    private void SelectImage()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.webp",
            Title = "Select Product Image"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            var sourcePath = openFileDialog.FileName;
            var ext = System.IO.Path.GetExtension(sourcePath).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp")
            {
                _notificationService.Error(LocalizationManager.Get("Msg_InvalidImageFormat") ?? "Invalid image format.");
                return;
            }

            try
            {
                var targetDir = AppPaths.ProductImagesDirectory;
                System.IO.Directory.CreateDirectory(targetDir);

                var targetFileName = $"product_{Guid.NewGuid()}{ext}";
                var targetPath = System.IO.Path.Combine(targetDir, targetFileName);

                System.IO.File.Copy(sourcePath, targetPath, overwrite: true);
                ImagePath = targetPath;
                _notificationService.Success(LocalizationManager.Get("Msg_ProductImageCopiedLocal") ?? "Product image copied locally.");
            }
            catch (Exception ex)
            {
                _notificationService.Error((LocalizationManager.Get("Msg_Error") ?? "Error") + ": " + ex.Message);
            }
        }
    }

    [RelayCommand]
    private void ClearImage()
    {
        var result = System.Windows.MessageBox.Show(
            LocalizationManager.Get("Msg_ConfirmClearProductImage") ?? "Are you sure you want to clear the product image?",
            LocalizationManager.Get("App_Title") ?? AppPaths.ProductName,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            ImagePath = null;
        }
    }

    [RelayCommand]
    private void Cancel() { DialogResult = false; RequestClose?.Invoke(this, EventArgs.Empty); }
}
