using System.Windows;

namespace KarzounERP.Helpers;

public class NavigationService
{
    public event EventHandler<object>? NavigationRequested;
    private object? _currentViewModel;
    private static bool _isShowingNavigationError;

    public void NavigateTo<T>(Action<T>? configure = null) where T : class
    {
        try
        {
            if (_currentViewModel is T existing)
            {
                if (configure != null)
                {
                    if (existing is ViewModels.DocumentViewModel docVm)
                    {
                        var oldFilter = docVm.FilterType;
                        configure.Invoke(existing);
                        if (oldFilter != docVm.FilterType)
                        {
                            NavigationRequested?.Invoke(this, existing);
                            _ = docVm.LoadAsync();
                        }
                        return;
                    }
                }
                return;
            }

            var vm = App.Services.GetRequiredService<T>();
            configure?.Invoke(vm);
            _currentViewModel = vm;
            NavigationRequested?.Invoke(this, vm);
            if (vm is ViewModels.Base.ILoadableViewModel loadableNew)
                _ = loadableNew.LoadAsync();
        }
        catch (Exception ex)
        {
            HandleNavigationFailure(typeof(T).Name, ex);
        }
    }

    public void NavigateTo(object viewModel)
    {
        try
        {
            _currentViewModel = viewModel;
            NavigationRequested?.Invoke(this, viewModel);
            if (viewModel is ViewModels.Base.ILoadableViewModel loadable)
                _ = loadable.LoadAsync();
        }
        catch (Exception ex)
        {
            HandleNavigationFailure(viewModel.GetType().Name, ex);
        }
    }

    private static void HandleNavigationFailure(string targetName, Exception ex)
    {
        AppLogger.LogCrash($"Navigation to {targetName}", ex);

        var message = targetName.Contains("Appearance", StringComparison.OrdinalIgnoreCase)
            ? LocalizationManager.Get("Msg_AppearanceNavError")
            : LocalizationManager.Get("Msg_NavigationError");

        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_isShowingNavigationError)
                return;

            _isShowingNavigationError = true;
            try
            {
                MessageBox.Show(
                    message,
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isShowingNavigationError = false;
            }
        });
    }
}
