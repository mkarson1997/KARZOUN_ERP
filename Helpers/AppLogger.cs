namespace FornixxCRM.Helpers;

public static class AppLogger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FornixxCRM", "logs");

    private static readonly string LogFile = Path.Combine(LogDir, "app.log");

    public static void LogError(string message, Exception? ex = null)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}");
            if (ex != null)
            {
                lines.AppendLine($"  Type: {ex.GetType().Name}");
                lines.AppendLine($"  Message: {ex.Message}");
                if (ex.InnerException != null)
                    lines.AppendLine($"  Inner: {ex.InnerException.Message}");
                lines.AppendLine($"  Stack: {ex.StackTrace}");
            }
            File.AppendAllText(LogFile, lines.ToString());
        }
        catch { /* logging must never crash the app */ }
    }

    public static void LogInfo(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\n");
        }
        catch { }
    }

    public static void Info(string message) => LogInfo(message);
}
