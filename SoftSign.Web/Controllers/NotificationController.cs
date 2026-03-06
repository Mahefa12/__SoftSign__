using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;

namespace SoftSign.Web.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        UserManager<User> userManager,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);
        var notificationList = notifications.ToList();
        
        var totalCount = notificationList.Count;
        var pagedNotifications = notificationList.Skip((page - 1) * pageSize).Take(pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.TotalCount = totalCount;

        return View(pagedNotifications);
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(user.Id);
        return Json(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var notifications = await _notificationService.GetUserNotificationsAsync(user.Id, unreadOnly: true);
        var recent = notifications.Take(5).Select(n => new
        {
            id = n.Id,
            title = n.Title,
            message = n.Message,
            type = n.Type.ToString(),
            isRead = n.IsRead,
            link = n.Link,
            documentId = n.DocumentId,
            createdAt = n.CreatedAt
        });

        return Json(recent);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { message = "Error marking notification as read" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(user.Id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { message = "Error marking all notifications as read" });
        }
    }
}
