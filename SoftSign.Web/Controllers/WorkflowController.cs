using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;

namespace SoftSign.Web.Controllers;

[Authorize]
public class WorkflowController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly IWorkflowService _workflowService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IDocumentService documentService,
        IWorkflowService workflowService,
        INotificationService notificationService,
        UserManager<User> userManager,
        ILogger<WorkflowController> logger)
    {
        _documentService = documentService;
        _workflowService = workflowService;
        _notificationService = notificationService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, int page = 1, int pageSize = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var documents = await _documentService.GetAllAsync(user.CompanyId);

        // Filter for pending documents
        documents = documents.Where(d => d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.InProgress);

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<DocumentStatus>(status, out var docStatus))
            {
                documents = documents.Where(d => d.Status == docStatus);
            }
        }

        var documentList = documents.ToList();
        var totalCount = documentList.Count;
        var pagedDocuments = documentList.Skip((page - 1) * pageSize).Take(pageSize);

        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.TotalCount = totalCount;

        return View(pagedDocuments);
    }

    [HttpGet]
    public async Task<IActionResult> Review(Guid id)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Check if user is authorized to review this document
        if (document.CreatedById == user.Id || User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            return View(document);
        }

        return Forbid();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, string? comments)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Update document status
            await _documentService.UpdateStatusAsync(id, (int)DocumentStatus.Completed);

            // Send notification to document creator
            await _notificationService.SendNotificationAsync(
                document.CreatedById,
                "Document Approved",
                $"Your document '{document.Title}' has been approved by {user.FirstName} {user.LastName}.",
                NotificationType.Approval,
                id,
                $"/Document/Details/{id}");

            _logger.LogInformation("Document approved: {Title} by {User}", document.Title, user.Email);

            TempData["Success"] = "Document approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving document");
            TempData["Error"] = "Error approving document.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            ModelState.AddModelError(string.Empty, "Please provide a reason for rejection.");
            return RedirectToAction(nameof(Review), new { id });
        }

        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Update document status
            await _documentService.UpdateStatusAsync(id, (int)DocumentStatus.Rejected);

            // Send notification to document creator
            await _notificationService.SendNotificationAsync(
                document.CreatedById,
                "Document Rejected",
                $"Your document '{document.Title}' has been rejected. Reason: {reason}",
                NotificationType.Rejection,
                id,
                $"/Document/Details/{id}");

            _logger.LogInformation("Document rejected: {Title} by {User}. Reason: {Reason}", document.Title, user.Email, reason);

            TempData["Success"] = "Document rejected.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting document");
            TempData["Error"] = "Error rejecting document.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCorrection(Guid id, string feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback))
        {
            ModelState.AddModelError(string.Empty, "Please provide feedback for correction.");
            return RedirectToAction(nameof(Review), new { id });
        }

        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Update document status back to draft
            await _documentService.UpdateStatusAsync(id, (int)DocumentStatus.Draft);

            // Send notification to document creator
            await _notificationService.SendNotificationAsync(
                document.CreatedById,
                "Correction Requested",
                $"Correction requested for '{document.Title}'. Feedback: {feedback}",
                NotificationType.Correction,
                id,
                $"/Document/Details/{id}");

            _logger.LogInformation("Correction requested for document: {Title} by {User}", document.Title, user.Email);

            TempData["Success"] = "Correction requested.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting correction");
            TempData["Error"] = "Error requesting correction.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> History(Guid id)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        return View(document);
    }
}
