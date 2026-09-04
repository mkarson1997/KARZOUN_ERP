namespace KarzounERP.Helpers;

public static class AppLogger
{
    private static readonly string LogDir = AppPaths.LogsDirectory;

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

    public static void LogCrash(string context, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            var crashFile = Path.Combine(LogDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            lines.AppendLine($"Context: {context}");
            lines.AppendLine($"Exception: {ex.GetType().FullName}");
            lines.AppendLine($"Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                lines.AppendLine($"Inner Exception: {ex.InnerException.GetType().FullName}");
                lines.AppendLine($"Inner Message: {ex.InnerException.Message}");
            }
            lines.AppendLine("Stack Trace:");
            lines.AppendLine(ex.StackTrace);
            File.WriteAllText(crashFile, lines.ToString());
            LogError($"[Crash] {context}", ex);
        }
        catch { }
    }
}
