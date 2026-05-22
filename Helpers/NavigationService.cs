namespace FornixxCRM.Helpers;

public class NavigationService
{
    public event EventHandler<object>? NavigationRequested;
    private object? _currentViewModel;

    public void NavigateTo<T>(Action<T>? configure = null) where T : class
    {
        if (_currentViewModel is T existing)
        {
            if (configure != null)
            {
                // Capture old state to check if it changed (specific to our app logic if needed)
                // To be completely safe and generic, we can just invoke it.
                // But to prevent the clearing bug, we must NOT call LoadAsync if we are just clicking the same menu.
                // For DocumentViewModel, we can check if FilterType changed.
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
            return; // Ignore navigation to the exact same active page
        }

        var vm = App.Services.GetRequiredService<T>();
        configure?.Invoke(vm);
        _currentViewModel = vm;
        NavigationRequested?.Invoke(this, vm);
        if (vm is ViewModels.Base.ILoadableViewModel loadableNew)
            _ = loadableNew.LoadAsync();
    }

    public void NavigateTo(object viewModel)
    {
        _currentViewModel = viewModel;
        NavigationRequested?.Invoke(this, viewModel);
        if (viewModel is ViewModels.Base.ILoadableViewModel loadable)
            _ = loadable.LoadAsync();
    }
}
