using System;

namespace KarzounERP.Services.Interfaces;

public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info
}

public interface INotificationService
{
    event Action<string, NotificationType, int>? NotificationTriggered;
    void Show(string message, NotificationType type = NotificationType.Info, int durationMs = 3000);
    void Success(string message, int durationMs = 3000);
    void Error(string message, int durationMs = 5000);
    void Warning(string message, int durationMs = 4000);
    void Info(string message, int durationMs = 3000);
}
