using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;
using System.Windows.Media;

namespace KarzounERP.ViewModels;

public partial class AppearanceViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly ICompanyService _companyService;
    private readonly AppSession _session;
    private readonly INotificationService _notificationService;
    private bool _isSyncingColor = false;
    private bool _isSaved = false;

    [ObservableProperty] private Company? _company;

    [ObservableProperty] private string _primaryColor = KarzounBrand.Teal;
    [ObservableProperty] private string _secondaryColor = KarzounBrand.Blue;
    [ObservableProperty] private string _accentColor = KarzounBrand.Emerald;
    [ObservableProperty] private string _sidebarBackground = KarzounBrand.Navy;
    [ObservableProperty] private string _sidebarTextColor = KarzounBrand.LightGray;
    [ObservableProperty] private string _buttonColor = KarzounBrand.Teal;
    [ObservableProperty] private string _buttonTextColor = KarzounBrand.Navy;
    [ObservableProperty] private string _cardBackground = KarzounBrand.LightCard;
    [ObservableProperty] private string _pageBackground = KarzounBrand.LightPage;

    [ObservableProperty] private string _companyThemePrimaryColor = KarzounBrand.Teal;
    [ObservableProperty] private string _companyThemeSecondaryColor = KarzounBrand.Blue;
    [ObservableProperty] private string _companyThemeAccentColor = KarzounBrand.Emerald;
    [ObservableProperty] private bool _applyCompanyTheme;

    [ObservableProperty] private string _pdfPrimaryColor = KarzounBrand.Navy;
    [ObservableProperty] private string _pdfHeaderColor = KarzounBrand.Navy;
    [ObservableProperty] private string _pdfTableHeaderColor = KarzounBrand.PdfTableHeader;
    [ObservableProperty] private string _pdfBorderColor = KarzounBrand.PdfBorder;
    [ObservableProperty] private string _pdfAccentColor = KarzounBrand.Teal;
    [ObservableProperty] private string _pdfTotalBoxColor = KarzounBrand.PdfTotalBox;

    [ObservableProperty] private double _pdfCompanyInfoTopMargin;
    [ObservableProperty] private double _pdfLogoTopMargin;
    [ObservableProperty] private double _pdfHeaderSpacing;
    [ObservableProperty] private double _pdfTableSpacing;
    [ObservableProperty] private double _pdfFontSize = 9.0;

    public double PdfPreviewNormalFontSize => PdfFontSizeHelper.PreviewNormal(PdfFontSize);
    public double PdfPreviewTitleFontSize => PdfFontSizeHelper.PreviewTitle(PdfFontSize);
    public double PdfPreviewSmallFontSize => PdfFontSizeHelper.PreviewSmall(PdfFontSize);
    public double PdfPreviewTinyFontSize => PdfFontSizeHelper.PreviewTiny(PdfFontSize);

    partial void OnPdfFontSizeChanged(double value)
    {
        OnPropertyChanged(nameof(PdfPreviewNormalFontSize));
        OnPropertyChanged(nameof(PdfPreviewTitleFontSize));
        OnPropertyChanged(nameof(PdfPreviewSmallFontSize));
        OnPropertyChanged(nameof(PdfPreviewTinyFontSize));
    }

    [ObservableProperty] private string _selectedColorKey = "PrimaryColor";
    [ObservableProperty] private string _selectedColorHelperText = string.Empty;
    [ObservableProperty] private string _editorHex = KarzounBrand.Teal;
    [ObservableProperty] private int _editorR;
    [ObservableProperty] private int _editorG;
    [ObservableProperty] private int _editorB;

    public AppearanceViewModel(ICompanyService companyService, AppSession session, INotificationService notificationService)
    {
        _companyService = companyService;
        _session = session;
        _notificationService = notificationService;
        _session.ActiveCompanyChanged += async (_, _) => await LoadAsync();
    }

    public async Task LoadAsync()
    {
        _isSaved = false;

        if (!_session.HasActiveCompany) return;
        Company = await _companyService.GetCompanyAsync(_session.ActiveCompanyId);
        if (Company == null) return;

        var companyTheme = AppearanceSettingsStore.LoadCompanyTheme(_session.ActiveCompanyId);
        CompanyThemePrimaryColor = companyTheme.ThemePrimaryColor;
        CompanyThemeSecondaryColor = companyTheme.ThemeSecondaryColor;
        CompanyThemeAccentColor = companyTheme.ThemeAccentColor;
        ApplyCompanyTheme = companyTheme.ApplyCompanyTheme;

        var appearance = AppearanceSettingsStore.LoadGlobal();
        PrimaryColor = appearance.PrimaryColor;
        SecondaryColor = appearance.SecondaryColor;
        AccentColor = appearance.AccentColor;
        SidebarBackground = appearance.SidebarBackground;
        SidebarTextColor = appearance.SidebarTextColor;
        ButtonColor = appearance.ButtonColor;
        ButtonTextColor = appearance.ButtonTextColor;
        CardBackground = appearance.CardBackground;
        PageBackground = appearance.PageBackground;

        PdfPrimaryColor = appearance.PdfPrimaryColor;
        PdfHeaderColor = appearance.PdfHeaderColor;
        PdfTableHeaderColor = appearance.PdfTableHeaderColor;
        PdfBorderColor = appearance.PdfBorderColor;
        PdfAccentColor = appearance.PdfAccentColor;
        PdfTotalBoxColor = appearance.PdfTotalBoxColor;

        PdfCompanyInfoTopMargin = appearance.PdfCompanyInfoTopMargin;
        PdfLogoTopMargin = appearance.PdfLogoTopMargin;
        PdfHeaderSpacing = appearance.PdfHeaderSpacing;
        PdfTableSpacing = appearance.PdfTableSpacing;
        PdfFontSize = appearance.PdfFontSize;

        LoadColorToEditor(SelectedColorKey);
        UpdateSelectedColorHelperText(SelectedColorKey);
        OnAppearancePropertyChanged();
        await Task.CompletedTask;
    }

    private void LoadColorToEditor(string key)
    {
        _isSyncingColor = true;
        try
        {
            var hex = GetColorValue(key);
            EditorHex = hex;
            var color = (Color)ColorConverter.ConvertFromString(hex);
            EditorR = color.R;
            EditorG = color.G;
            EditorB = color.B;
        }
        catch { }
        finally { _isSyncingColor = false; }
    }

    private string GetColorValue(string key) => key switch
    {
        "PrimaryColor" => PrimaryColor,
        "SecondaryColor" => SecondaryColor,
        "AccentColor" => AccentColor,
        "SidebarBackground" => SidebarBackground,
        "SidebarTextColor" => SidebarTextColor,
        "ButtonColor" => ButtonColor,
        "ButtonTextColor" => ButtonTextColor,
        "CardBackground" => CardBackground,
        "PageBackground" => PageBackground,
        "CompanyThemePrimaryColor" => CompanyThemePrimaryColor,
        "CompanyThemeSecondaryColor" => CompanyThemeSecondaryColor,
        "CompanyThemeAccentColor" => CompanyThemeAccentColor,
        "PdfPrimaryColor" => PdfPrimaryColor,
        "PdfHeaderColor" => PdfHeaderColor,
        "PdfTableHeaderColor" => PdfTableHeaderColor,
        "PdfBorderColor" => PdfBorderColor,
        "PdfAccentColor" => PdfAccentColor,
        "PdfTotalBoxColor" => PdfTotalBoxColor,
        _ => "#000000"
    };

    private void SetColorValue(string key, string hex)
    {
        switch (key)
        {
            case "PrimaryColor": PrimaryColor = hex; break;
            case "SecondaryColor": SecondaryColor = hex; break;
            case "AccentColor": AccentColor = hex; break;
            case "SidebarBackground": SidebarBackground = hex; break;
            case "SidebarTextColor": SidebarTextColor = hex; break;
            case "ButtonColor": ButtonColor = hex; break;
            case "ButtonTextColor": ButtonTextColor = hex; break;
            case "CardBackground": CardBackground = hex; break;
            case "PageBackground": PageBackground = hex; break;
            case "CompanyThemePrimaryColor": CompanyThemePrimaryColor = hex; break;
            case "CompanyThemeSecondaryColor": CompanyThemeSecondaryColor = hex; break;
            case "CompanyThemeAccentColor": CompanyThemeAccentColor = hex; break;
            case "PdfPrimaryColor": PdfPrimaryColor = hex; break;
            case "PdfHeaderColor": PdfHeaderColor = hex; break;
            case "PdfTableHeaderColor": PdfTableHeaderColor = hex; break;
            case "PdfBorderColor": PdfBorderColor = hex; break;
            case "PdfAccentColor": PdfAccentColor = hex; break;
            case "PdfTotalBoxColor": PdfTotalBoxColor = hex; break;
        }
        OnAppearancePropertyChanged();
    }

    partial void OnSelectedColorKeyChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            LoadColorToEditor(value);
            UpdateSelectedColorHelperText(value);
        }
    }

    private void UpdateSelectedColorHelperText(string key)
    {
        var helperKey = $"Color_Helper_{key}";
        var text = LocalizationManager.Get(helperKey);
        SelectedColorHelperText = string.IsNullOrWhiteSpace(text) || text == helperKey
            ? LocalizationManager.Get("Color_Helper_Default") ?? string.Empty
            : text;
    }

    partial void OnEditorHexChanged(string value)
    {
        if (_isSyncingColor) return;
        _isSyncingColor = true;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(value);
            EditorR = color.R;
            EditorG = color.G;
            EditorB = color.B;
            SetColorValue(SelectedColorKey, value);
        }
        catch { }
        finally { _isSyncingColor = false; }
    }

    private void UpdateHexFromRgb()
    {
        if (_isSyncingColor) return;
        _isSyncingColor = true;
        try
        {
            var r = Math.Clamp(EditorR, 0, 255);
            var g = Math.Clamp(EditorG, 0, 255);
            var b = Math.Clamp(EditorB, 0, 255);
            var hex = $"#{r:X2}{g:X2}{b:X2}";
            EditorHex = hex;
            SetColorValue(SelectedColorKey, hex);
        }
        catch { }
        finally { _isSyncingColor = false; }
    }

    partial void OnEditorRChanged(int value) => UpdateHexFromRgb();
    partial void OnEditorGChanged(int value) => UpdateHexFromRgb();
    partial void OnEditorBChanged(int value) => UpdateHexFromRgb();

    partial void OnApplyCompanyThemeChanged(bool value) => OnAppearancePropertyChanged();

    [RelayCommand]
    private void SetPresetColor(string hex) => EditorHex = hex;

    [RelayCommand]
    private void ResetToDefault()
    {
        var defaultValue = SelectedColorKey switch
        {
            "PrimaryColor" => KarzounBrand.Teal,
            "SecondaryColor" => KarzounBrand.Blue,
            "AccentColor" => KarzounBrand.Emerald,
            "SidebarBackground" => KarzounBrand.Navy,
            "SidebarTextColor" => KarzounBrand.LightGray,
            "ButtonColor" => KarzounBrand.Teal,
            "ButtonTextColor" => KarzounBrand.Navy,
            "CardBackground" => KarzounBrand.LightCard,
            "PageBackground" => KarzounBrand.LightPage,
            "CompanyThemePrimaryColor" => KarzounBrand.Teal,
            "CompanyThemeSecondaryColor" => KarzounBrand.Blue,
            "CompanyThemeAccentColor" => KarzounBrand.Emerald,
            "PdfPrimaryColor" => KarzounBrand.Navy,
            "PdfHeaderColor" => KarzounBrand.Navy,
            "PdfTableHeaderColor" => KarzounBrand.PdfTableHeader,
            "PdfBorderColor" => KarzounBrand.PdfBorder,
            "PdfAccentColor" => KarzounBrand.Teal,
            "PdfTotalBoxColor" => KarzounBrand.PdfTotalBox,
            _ => "#000000"
        };
        EditorHex = defaultValue;
    }

    [RelayCommand]
    private void ApplyKarzounLightPreset()
    {
        ApplyKarzounAppPreset(KarzounBrand.LightPage, KarzounBrand.LightCard);
    }

    [RelayCommand]
    private void ApplyKarzounDarkPreset()
    {
        ApplyKarzounAppPreset(KarzounBrand.DarkPage, KarzounBrand.DarkCard);
    }

    private void ApplyKarzounAppPreset(string page, string card)
    {
        PrimaryColor = KarzounBrand.Teal;
        SecondaryColor = KarzounBrand.Blue;
        AccentColor = KarzounBrand.Emerald;
        SidebarBackground = KarzounBrand.Navy;
        SidebarTextColor = KarzounBrand.LightGray;
        ButtonColor = KarzounBrand.Teal;
        ButtonTextColor = KarzounBrand.Navy;
        CardBackground = card;
        PageBackground = page;
        LoadColorToEditor(SelectedColorKey);
        OnAppearancePropertyChanged();
    }

    [RelayCommand]
    private void ResetAllToDefault()
    {
        PrimaryColor = KarzounBrand.Teal;
        SecondaryColor = KarzounBrand.Blue;
        AccentColor = KarzounBrand.Emerald;
        SidebarBackground = KarzounBrand.Navy;
        SidebarTextColor = KarzounBrand.LightGray;
        ButtonColor = KarzounBrand.Teal;
        ButtonTextColor = KarzounBrand.Navy;
        CardBackground = KarzounBrand.LightCard;
        PageBackground = KarzounBrand.LightPage;

        PdfPrimaryColor = KarzounBrand.Navy;
        PdfHeaderColor = KarzounBrand.Navy;
        PdfTableHeaderColor = KarzounBrand.PdfTableHeader;
        PdfBorderColor = KarzounBrand.PdfBorder;
        PdfAccentColor = KarzounBrand.Teal;
        PdfTotalBoxColor = KarzounBrand.PdfTotalBox;

        PdfCompanyInfoTopMargin = 0.0;
        PdfLogoTopMargin = 0.0;
        PdfHeaderSpacing = 8.0;
        PdfTableSpacing = 10.0;
        PdfFontSize = 9.0;

        CompanyThemePrimaryColor = KarzounBrand.Teal;
        CompanyThemeSecondaryColor = KarzounBrand.Blue;
        CompanyThemeAccentColor = KarzounBrand.Emerald;
        ApplyCompanyTheme = false;

        LoadColorToEditor(SelectedColorKey);
        OnAppearancePropertyChanged();
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        AppearanceSettingsStore.SaveCompanyTheme(_session.ActiveCompanyId, new CompanyThemeData
        {
            ThemePrimaryColor = CompanyThemePrimaryColor,
            ThemeSecondaryColor = CompanyThemeSecondaryColor,
            ThemeAccentColor = CompanyThemeAccentColor,
            ApplyCompanyTheme = ApplyCompanyTheme
        });

        AppearanceSettingsStore.SaveGlobal(new AppearanceSetting
        {
            PrimaryColor = PrimaryColor,
            SecondaryColor = SecondaryColor,
            AccentColor = AccentColor,
            SidebarBackground = SidebarBackground,
            SidebarTextColor = SidebarTextColor,
            ButtonColor = ButtonColor,
            ButtonTextColor = ButtonTextColor,
            CardBackground = CardBackground,
            PageBackground = PageBackground,
            PdfPrimaryColor = PdfPrimaryColor,
            PdfHeaderColor = PdfHeaderColor,
            PdfTableHeaderColor = PdfTableHeaderColor,
            PdfBorderColor = PdfBorderColor,
            PdfAccentColor = PdfAccentColor,
            PdfTotalBoxColor = PdfTotalBoxColor,
            PdfCompanyInfoTopMargin = PdfCompanyInfoTopMargin,
            PdfLogoTopMargin = PdfLogoTopMargin,
            PdfHeaderSpacing = PdfHeaderSpacing,
            PdfTableSpacing = PdfTableSpacing,
            PdfFontSize = PdfFontSize
        });

        _isSaved = true;
        ThemeManager.ApplyTheme(_session.ActiveCompanyId);
        _notificationService.Success(LocalizationManager.Get("Msg_SettingsSaved") ?? "Appearance settings saved successfully.");
        await Task.CompletedTask;
    }

    public void RevertIfUnsaved()
    {
        if (!_isSaved)
            ThemeManager.ApplyTheme(_session.ActiveCompanyId);
    }

    private void OnAppearancePropertyChanged()
    {
        var tempAppearance = new AppearanceSetting
        {
            PrimaryColor = PrimaryColor,
            SecondaryColor = SecondaryColor,
            AccentColor = AccentColor,
            SidebarBackground = SidebarBackground,
            SidebarTextColor = SidebarTextColor,
            ButtonColor = ButtonColor,
            ButtonTextColor = ButtonTextColor,
            CardBackground = CardBackground,
            PageBackground = PageBackground
        };

        var tempCompanyTheme = new CompanyThemeData
        {
            ThemePrimaryColor = CompanyThemePrimaryColor,
            ThemeSecondaryColor = CompanyThemeSecondaryColor,
            ThemeAccentColor = CompanyThemeAccentColor,
            ApplyCompanyTheme = ApplyCompanyTheme
        };

        ThemeManager.ApplyThemeColors(tempAppearance, tempCompanyTheme);
    }
}
