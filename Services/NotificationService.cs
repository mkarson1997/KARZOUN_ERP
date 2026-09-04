using KarzounERP.Services.Interfaces;
using System;

namespace KarzounERP.Services;

public class NotificationService : INotificationService
{
    public event Action<string, NotificationType, int>? NotificationTriggered;

    public void Show(string message, NotificationType type = NotificationType.Info, int durationMs = 3000)
    {
        NotificationTriggered?.Invoke(message, type, durationMs);
    }

    public void Success(string message, int durationMs = 3000) => Show(message, NotificationType.Success, durationMs);
    public void Error(string message, int durationMs = 5000) => Show(message, NotificationType.Error, durationMs);
    public void Warning(string message, int durationMs = 4000) => Show(message, NotificationType.Warning, durationMs);
    public void Info(string message, int durationMs = 3000) => Show(message, NotificationType.Info, durationMs);
}
