using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.ViewModels.Base;

namespace KarzounERP.ViewModels;

public partial class LogViewModel : BaseViewModel, ILoadableViewModel
{
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _statusColor = "#2E7D32";
    [ObservableProperty] private string _latestLogPath = string.Empty;
    [ObservableProperty] private string _logContent = string.Empty;
    [ObservableProperty] private int _errorCount;

    public async Task LoadAsync()
    {
        await Task.Run(LoadLogs);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    private void LoadLogs()
    {
        var logsDir = AppPaths.LogsDirectory;

        if (!Directory.Exists(logsDir))
        {
            ApplyResult(LocalizationManager.Get("Logs_StatusOk"), "#2E7D32", string.Empty, string.Empty, 0);
            return;
        }

        var files = Directory.GetFiles(logsDir, "*.log")
            .Concat(Directory.GetFiles(logsDir, "crash_*.txt"))
            .OrderByDescending(File.GetLastWriteTime)
            .ToList();

        if (files.Count == 0)
        {
            ApplyResult(LocalizationManager.Get("Logs_StatusOk"), "#2E7D32", string.Empty, string.Empty, 0);
            return;
        }

        var latest = files.First();
        var allRecentLines = files.Take(5)
            .SelectMany(ReadTail)
            .ToList();

        var errors = allRecentLines.Count(line =>
            line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("crash", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("خطأ", StringComparison.OrdinalIgnoreCase));

        var status = errors > 0
            ? string.Format(LocalizationManager.Get("Logs_StatusIssues"), errors)
            : LocalizationManager.Get("Logs_StatusOk");

        var color = errors > 0 ? "#D32F2F" : "#2E7D32";
        ApplyResult(status, color, latest, string.Join(Environment.NewLine, ReadTail(latest, 180)), errors);
    }

    private static IEnumerable<string> ReadTail(string path, int count = 80)
    {
        try
        {
            return File.ReadLines(path).TakeLast(count);
        }
        catch (Exception ex)
        {
            return new[] { ex.Message };
        }
    }

    private void ApplyResult(string status, string color, string path, string content, int errors)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            StatusText = status;
            StatusColor = color;
            LatestLogPath = path;
            LogContent = content;
            ErrorCount = errors;
        });
    }
}
