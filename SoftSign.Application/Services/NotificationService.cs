using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Domain.Interfaces;

namespace SoftSign.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IRepository<UserNotification> _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IRepository<UserNotification> notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task SendNotificationAsync(Guid userId, string title, string message, NotificationType type = NotificationType.InApp, Guid? documentId = null, string? link = null)
    {
        var notification = new UserNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            DocumentId = documentId,
            Link = link
        };

        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false)
    {
        IEnumerable<UserNotification> notifications;
        if (unreadOnly)
            notifications = await _notificationRepository.FindAsync(n => n.UserId == userId && !n.IsRead);
        else
            notifications = await _notificationRepository.FindAsync(n => n.UserId == userId);

        return notifications.OrderByDescending(n => n.CreatedAt).Select(MapToDto);
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId)
            ?? throw new InvalidOperationException("Notification not found");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await _notificationRepository.FindAsync(n => n.UserId == userId && !n.IsRead);
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _notificationRepository.Update(notification);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _notificationRepository.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    private static NotificationDto MapToDto(UserNotification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            Link = notification.Link,
            DocumentId = notification.DocumentId,
            CreatedAt = notification.CreatedAt
        };
    }
}
