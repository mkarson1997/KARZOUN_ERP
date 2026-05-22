using FornixxCRM.ViewModels;
using System.Windows;

namespace FornixxCRM;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        // Apply FlowDirection based on selected language AFTER InitializeComponent
        // so it overrides the XAML attribute value.
        var fd = Helpers.LocalizationManager.FlowDirection;
        this.FlowDirection = fd;
        Helpers.AppLogger.LogInfo($"[MainWindow] Applied FlowDirection={fd} for language={Helpers.LocalizationManager.Language}");

        Helpers.LocalizationManager.LanguageChanged += (_, _) =>
        {
            FlowDirection = Helpers.LocalizationManager.FlowDirection;
            Helpers.AppLogger.LogInfo($"[MainWindow] LanguageChanged → FlowDirection={FlowDirection}");
        };
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await _viewModel.InitializeAsync();
    }
}