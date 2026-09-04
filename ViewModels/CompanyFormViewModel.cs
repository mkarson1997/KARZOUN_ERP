using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;
using Microsoft.Win32;

namespace KarzounERP.ViewModels;

public partial class CompanyFormViewModel : BaseViewModel
{
    private readonly ICompanyService _companyService;
    private readonly INotificationService _notificationService;
    private readonly AppSession _session;
    private int _editingId = 0;
    private string _companyThemeSecondaryColor = KarzounBrand.Blue;

    [ObservableProperty] private string _windowTitle = LocalizationManager.Get("CompForm_TitleNew");
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _commercialName = string.Empty;
    [ObservableProperty] private string _logoPath = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _country = string.Empty;
    [ObservableProperty] private string _currency = "USD";
    [ObservableProperty] private string _paymentInfo = string.Empty;
    [ObservableProperty] private string _defaultInvoiceNotes = string.Empty;
    [ObservableProperty] private string _defaultQuotationNotes = string.Empty;
    [ObservableProperty] private string _footerText = string.Empty;
    [ObservableProperty] private bool _applyCompanyTheme = true;
    [ObservableProperty] private string _companyThemePrimaryColor = KarzounBrand.Teal;
    [ObservableProperty] private string _companyThemeAccentColor = KarzounBrand.Emerald;
    [ObservableProperty] private bool _taxEnabled;
    [ObservableProperty] private decimal _taxRate;
    [ObservableProperty] private string _invoicePrefix = "INV";
    [ObservableProperty] private string _quotationPrefix = "QUO";
    [ObservableProperty] private string _validationError = string.Empty;
    [ObservableProperty] private bool _isNameInvalid;
    [ObservableProperty] private bool _isCurrencyInvalid;
    [ObservableProperty] private bool _isInvoicePrefixInvalid;
    [ObservableProperty] private bool _isQuotationPrefixInvalid;

    public bool? DialogResult { get; private set; }
    public event EventHandler? RequestClose;

    public CompanyFormViewModel(ICompanyService companyService, INotificationService notificationService, AppSession session)
    {
        _companyService = companyService;
        _notificationService = notificationService;
        _session = session;
    }

    public void PrepareNew()
    {
        _editingId = 0;
        WindowTitle = LocalizationManager.Get("CompForm_TitleNewFull");
        Name = CommercialName = Phone = Email = Address = Country = PaymentInfo =
            DefaultInvoiceNotes = DefaultQuotationNotes = FooterText = LogoPath = string.Empty;
        Currency = "USD"; InvoicePrefix = "INV"; QuotationPrefix = "QUO";
        TaxEnabled = false; TaxRate = 0;
        var global = AppearanceSettingsStore.LoadGlobal();
        ApplyCompanyTheme = true;
        CompanyThemePrimaryColor = global.PrimaryColor;
        CompanyThemeAccentColor = global.AccentColor;
        _companyThemeSecondaryColor = global.SecondaryColor;
    }

    public void LoadFromCompany(Company c)
    {
        _editingId = c.Id;
        WindowTitle = LocalizationManager.Get("CompForm_TitleEdit");
        Name = c.Name; CommercialName = c.CommercialName; LogoPath = c.LogoPath ?? "";
        Phone = c.Phone ?? ""; Email = c.Email ?? ""; Address = c.Address ?? "";
        Country = c.Country ?? ""; Currency = c.Currency;
        PaymentInfo = c.PaymentInfo ?? ""; DefaultInvoiceNotes = c.DefaultInvoiceNotes ?? "";
        DefaultQuotationNotes = c.DefaultQuotationNotes ?? ""; FooterText = c.FooterText ?? "";
        TaxEnabled = c.TaxEnabled; TaxRate = c.TaxRate;
        InvoicePrefix = c.InvoicePrefix; QuotationPrefix = c.QuotationPrefix;

        var theme = AppearanceSettingsStore.LoadCompanyTheme(c.Id);
        var global = AppearanceSettingsStore.LoadGlobal();
        ApplyCompanyTheme = theme.ApplyCompanyTheme;
        CompanyThemePrimaryColor = string.IsNullOrWhiteSpace(theme.ThemePrimaryColor) ? global.PrimaryColor : theme.ThemePrimaryColor;
        CompanyThemeAccentColor = string.IsNullOrWhiteSpace(theme.ThemeAccentColor) ? global.AccentColor : theme.ThemeAccentColor;
        _companyThemeSecondaryColor = string.IsNullOrWhiteSpace(theme.ThemeSecondaryColor) ? global.SecondaryColor : theme.ThemeSecondaryColor;
    }

    [RelayCommand]
    private void BrowseLogo()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Title = LocalizationManager.Get("Dialog_SelectLogoTitle")
        };
        if (dlg.ShowDialog() == true) LogoPath = dlg.FileName;
    }

    [RelayCommand]
    private void PickCompanyPrimaryColor()
    {
        var selected = PickColor(CompanyThemePrimaryColor);
        if (!string.IsNullOrWhiteSpace(selected))
            CompanyThemePrimaryColor = selected;
    }

    [RelayCommand]
    private void PickCompanyAccentColor()
    {
        var selected = PickColor(CompanyThemeAccentColor);
        if (!string.IsNullOrWhiteSpace(selected))
            CompanyThemeAccentColor = selected;
    }

    private static string? PickColor(string currentHex)
    {
        var color = ToMediaColor(currentHex);
        var previewBrush = new System.Windows.Media.SolidColorBrush(color);

        var window = new System.Windows.Window
        {
            Title = LocalizationManager.Get("CompForm_PickColor"),
            Width = 360,
            Height = 300,
            ResizeMode = System.Windows.ResizeMode.NoResize,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Owner = System.Windows.Application.Current?.MainWindow,
            FlowDirection = LocalizationManager.FlowDirection,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
        };

        var root = new System.Windows.Controls.StackPanel
        {
            Margin = new System.Windows.Thickness(18)
        };

        var preview = new System.Windows.Controls.Border
        {
            Height = 54,
            CornerRadius = new System.Windows.CornerRadius(6),
            BorderBrush = System.Windows.Media.Brushes.LightGray,
            BorderThickness = new System.Windows.Thickness(1),
            Background = previewBrush,
            Margin = new System.Windows.Thickness(0, 0, 0, 16)
        };
        root.Children.Add(preview);

        var red = CreateColorSlider("R", color.R);
        var green = CreateColorSlider("G", color.G);
        var blue = CreateColorSlider("B", color.B);
        root.Children.Add(red.Row);
        root.Children.Add(green.Row);
        root.Children.Add(blue.Row);

        void UpdatePreview()
        {
            previewBrush.Color = System.Windows.Media.Color.FromRgb(
                (byte)red.Slider.Value,
                (byte)green.Slider.Value,
                (byte)blue.Slider.Value);
        }

        red.Slider.ValueChanged += (_, _) => UpdatePreview();
        green.Slider.ValueChanged += (_, _) => UpdatePreview();
        blue.Slider.ValueChanged += (_, _) => UpdatePreview();

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new System.Windows.Thickness(0, 18, 0, 0)
        };

        var cancel = new System.Windows.Controls.Button
        {
            Content = LocalizationManager.Get("Btn_Cancel"),
            MinWidth = 88,
            Margin = new System.Windows.Thickness(8, 0, 0, 0)
        };
        cancel.Click += (_, _) => window.DialogResult = false;

        var ok = new System.Windows.Controls.Button
        {
            Content = LocalizationManager.Get("Btn_Save"),
            MinWidth = 88
        };
        ok.Click += (_, _) => window.DialogResult = true;

        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        root.Children.Add(buttons);

        window.Content = root;
        return window.ShowDialog() == true
            ? $"#{previewBrush.Color.R:X2}{previewBrush.Color.G:X2}{previewBrush.Color.B:X2}"
            : null;
    }

    private static (System.Windows.Controls.Grid Row, System.Windows.Controls.Slider Slider) CreateColorSlider(string label, byte value)
    {
        var grid = new System.Windows.Controls.Grid { Margin = new System.Windows.Thickness(0, 0, 0, 10) };
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(28) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(42) });

        var title = new System.Windows.Controls.TextBlock
        {
            Text = label,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            FontWeight = System.Windows.FontWeights.SemiBold
        };
        System.Windows.Controls.Grid.SetColumn(title, 0);

        var slider = new System.Windows.Controls.Slider
        {
            Minimum = 0,
            Maximum = 255,
            Value = value,
            TickFrequency = 1,
            IsSnapToTickEnabled = true
        };
        System.Windows.Controls.Grid.SetColumn(slider, 1);

        var number = new System.Windows.Controls.TextBlock
        {
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        number.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding("Value")
        {
            Source = slider,
            StringFormat = "0"
        });
        System.Windows.Controls.Grid.SetColumn(number, 2);

        grid.Children.Add(title);
        grid.Children.Add(slider);
        grid.Children.Add(number);
        return (grid, slider);
    }

    private static System.Windows.Media.Color ToMediaColor(string? hex)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                var value = hex.Trim().TrimStart('#');
                if (value.Length == 8)
                    value = value[2..];
                if (value.Length == 6)
                    return System.Windows.Media.Color.FromRgb(
                        Convert.ToByte(value[..2], 16),
                        Convert.ToByte(value.Substring(2, 2), 16),
                        Convert.ToByte(value.Substring(4, 2), 16));
            }
        }
        catch
        {
            // Fall through to a professional default color.
        }

        return System.Windows.Media.Color.FromRgb(255, 107, 0);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var trimmedName = Name.Trim();
        IsNameInvalid = string.IsNullOrWhiteSpace(trimmedName) || trimmedName.Equals("....", StringComparison.OrdinalIgnoreCase);
        IsCurrencyInvalid = string.IsNullOrWhiteSpace(Currency);
        IsInvoicePrefixInvalid = string.IsNullOrWhiteSpace(InvoicePrefix);
        IsQuotationPrefixInvalid = string.IsNullOrWhiteSpace(QuotationPrefix);

        if (IsNameInvalid || IsCurrencyInvalid || IsInvoicePrefixInvalid || IsQuotationPrefixInvalid)
        {
            ValidationError = LocalizationManager.Get("Msg_RequiredFields") ?? "Please fill in the required fields.";
            _notificationService.Error(ValidationError);
            if (IsNameInvalid) RaiseRequestFocus(nameof(Name));
            else if (IsCurrencyInvalid) RaiseRequestFocus(nameof(Currency));
            else if (IsInvoicePrefixInvalid) RaiseRequestFocus(nameof(InvoicePrefix));
            else if (IsQuotationPrefixInvalid) RaiseRequestFocus(nameof(QuotationPrefix));
            return;
        }
        ValidationError = string.Empty;

        var company = _editingId > 0
            ? await _companyService.GetCompanyAsync(_editingId) ?? new Company()
            : new Company();

        company.Name = trimmedName;
        // CommercialName defaults to company name if left blank
        company.CommercialName = string.IsNullOrWhiteSpace(CommercialName)
            ? trimmedName
            : CommercialName.Trim();
        company.LogoPath = string.IsNullOrWhiteSpace(LogoPath) ? null : LogoPath.Trim();
        company.Phone = Phone.Trim();
        company.Email = Email.Trim();
        company.Address = Address.Trim();
        company.Country = Country.Trim();
        // Currency defaults to USD if blank
        company.Currency = string.IsNullOrWhiteSpace(Currency) ? "USD" : Currency.Trim();
        company.PaymentInfo = PaymentInfo.Trim();
        company.DefaultInvoiceNotes = DefaultInvoiceNotes.Trim();
        company.DefaultQuotationNotes = DefaultQuotationNotes.Trim();
        company.FooterText = FooterText.Trim();
        company.TaxEnabled = TaxEnabled;
        company.TaxRate = TaxRate;
        company.InvoicePrefix = string.IsNullOrWhiteSpace(InvoicePrefix) ? "INV" : InvoicePrefix.Trim();
        company.QuotationPrefix = string.IsNullOrWhiteSpace(QuotationPrefix) ? "QUO" : QuotationPrefix.Trim();

        bool isEdit = _editingId > 0;
        if (isEdit)
            await _companyService.UpdateCompanyAsync(company);
        else
            await _companyService.AddCompanyAsync(company);

        AppearanceSettingsStore.SaveCompanyTheme(company.Id, new CompanyThemeData
        {
            ThemePrimaryColor = CompanyThemePrimaryColor,
            ThemeSecondaryColor = _companyThemeSecondaryColor,
            ThemeAccentColor = CompanyThemeAccentColor,
            ApplyCompanyTheme = ApplyCompanyTheme
        });

        if (_session.ActiveCompanyId == company.Id)
            ThemeManager.ApplyTheme(company.Id);

        _notificationService.Success(isEdit
            ? string.Format(LocalizationManager.Get("Msg_CompanyUpdated") ?? "Company '{0}' updated successfully.", company.Name)
            : string.Format(LocalizationManager.Get("Msg_CompanyCreated") ?? "Company '{0}' created successfully.", company.Name));

        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
