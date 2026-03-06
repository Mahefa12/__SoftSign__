using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Web.ViewModels;
using SoftSign.Infrastructure.Data;
using System;
using System.IO;

namespace SoftSign.Web.Controllers;

[Authorize]
public class DocumentController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPdfService _pdfService;
    private readonly IWorkflowService _workflowService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DocumentController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly ApplicationDbContext _dbContext;

    public DocumentController(
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        IPdfService pdfService,
        IWorkflowService workflowService,
        INotificationService notificationService,
        UserManager<User> userManager,
        ILogger<DocumentController> logger,
        IWebHostEnvironment env,
        ApplicationDbContext dbContext)
    {
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _pdfService = pdfService;
        _workflowService = workflowService;
        _notificationService = notificationService;
        _userManager = userManager;
        _logger = logger;
        _env = env;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, DocumentStatus? status, int page = 1, int pageSize = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var documents = await _documentService.GetAllAsync(user.CompanyId);

        // Apply filters
        if (!string.IsNullOrEmpty(search))
        {
            documents = documents.Where(d => d.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           d.OriginalFileName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (status.HasValue)
        {
            documents = documents.Where(d => d.Status == status.Value);
        }

        var documentList = documents.ToList();
        var totalCount = documentList.Count;
        var pagedDocuments = documentList.Skip((page - 1) * pageSize).Take(pageSize);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.TotalCount = totalCount;

        return View(pagedDocuments);
    }

    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        var workflows = await _workflowService.GetAllAsync();
        ViewBag.Workflows = workflows;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string title, string? description, Guid? workflowId, DateTime? expiresAt, string? signatureZones)
    {
        // DEBUG: Log all incoming parameters
        _logger.LogInformation("=== UPLOAD POST DEBUG ===");
        _logger.LogInformation("file: {File}", file?.FileName);
        _logger.LogInformation("title: {Title}", title);
        _logger.LogInformation("workflowId: {WorkflowId}", workflowId);
        _logger.LogInformation("signatureZones: {SignatureZones}", signatureZones ?? "NULL");
        _logger.LogInformation("=== END UPLOAD POST DEBUG ===");
        
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a file to upload.");
            var workflows = await _workflowService.GetAllAsync();
            ViewBag.Workflows = workflows;
            return View();
        }

        // Validate file type
        var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".txt" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(string.Empty, "Invalid file type. Allowed types: PDF, Word, Excel, PowerPoint, Text");
            var workflows = await _workflowService.GetAllAsync();
            ViewBag.Workflows = workflows;
            return View();
        }

        // Validate file size (max 50MB)
        if (file.Length > 50 * 1024 * 1024)
        {
            ModelState.AddModelError(string.Empty, "File size exceeds 50MB limit.");
            var workflows = await _workflowService.GetAllAsync();
            ViewBag.Workflows = workflows;
            return View();
        }
        
        // Parse signature zones with proper JSON deserialization
        bool hasSignatureZones = false;
        List<System.Text.Json.JsonElement>? parsedZones = null;
        if (!string.IsNullOrEmpty(signatureZones))
        {
            _logger.LogInformation("DEBUG: signatureZones raw value: {SignatureZones}", signatureZones);
            try 
            {
                parsedZones = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(signatureZones);
                hasSignatureZones = parsedZones != null && parsedZones.Count > 0;
                _logger.LogInformation("DEBUG: Successfully parsed {Count} signature zones", parsedZones?.Count ?? 0);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "DEBUG: Failed to parse signatureZones JSON");
                hasSignatureZones = false;
            }
        }
        else
        {
            _logger.LogWarning("DEBUG: signatureZones parameter is NULL or EMPTY");
        }
        
        _logger.LogInformation("DEBUG: hasSignatureZones: {HasSignatureZones}", hasSignatureZones);
        
        // Validate signature zones if workflow requires them
        bool workflowRequiresSignatures = false;
        if (workflowId.HasValue)
        {
            _logger.LogInformation("DEBUG: Checking signature zones for workflow {WorkflowId}", workflowId.Value);
            _logger.LogInformation("DEBUG: signatureZones parameter: '{SignatureZones}'", signatureZones ?? "null");
            
            var workflow = await _workflowService.GetByIdAsync(workflowId.Value);
            if (workflow != null && workflow.Steps != null)
            {
                var requiredSignatures = workflow.Steps.Sum(s => s.RequiredSignatureCount);
                _logger.LogInformation("DEBUG: Required signatures: {RequiredSignatures}", requiredSignatures);
                
                if (requiredSignatures > 0)
                {
                    workflowRequiresSignatures = true;
                    // Use already-parsed zones for workflow validation
                    _logger.LogInformation("DEBUG: hasZones: {HasZones}", hasSignatureZones);
                    
                    if (!hasSignatureZones)
                    {
                        ModelState.AddModelError(string.Empty, $"Ce flux de travail nécessite {requiredSignatures} zone(s) de signature. Veuillez placer les zones de signature sur le document avant de téléverser.");
                        var workflows = await _workflowService.GetAllAsync();
                        ViewBag.Workflows = workflows;
                        return View();
                    }
                }
            }
        }

        try
        {
            // Only require signature zones if the workflow specifically requires them
            // (not when no workflow is selected or workflow has 0 required signatures)
            if (workflowRequiresSignatures && !hasSignatureZones)
            {
                ModelState.AddModelError(string.Empty, "Veuillez placer au moins une zone de signature avant d'enregistrer le document.");
                var workflows = await _workflowService.GetAllAsync();
                ViewBag.Workflows = workflows;
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Save file to storage
            string filePath;
            byte[] pdfBytes;
            using (var stream = file.OpenReadStream())
            {
                // Convert to PDF if not already PDF
                if (file.ContentType != "application/pdf")
                {
                    pdfBytes = await _pdfService.ConvertToPdfAsync(stream, file.ContentType);
                }
                else
                {
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    pdfBytes = ms.ToArray();
                }

                // Save the PDF
                var fileName = $"{Guid.NewGuid()}.pdf";
                filePath = await _fileStorageService.SaveFileAsync(new MemoryStream(pdfBytes), fileName, "documents");
            }

            // Create document record with file info
            var createDto = new CreateDocumentDto
            {
                Title = string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(file.FileName) : title,
                Description = description,
                WorkflowId = workflowId,
                CompanyId = user.CompanyId,
                ExpiresAt = expiresAt,
                // Store the full relative path from FileStorageService (includes 'documents\' prefix)
                FileName = filePath,
                OriginalFileName = file.FileName,
                ContentType = "application/pdf",
                // Use PDF byte count for file size
                FileSize = pdfBytes.Length
            };

            var document = await _documentService.CreateAsync(createDto, user.Id);

            // Debug: Log signature zones received
            _logger.LogInformation("=== SIGNATURE ZONES DEBUG ===");
            _logger.LogInformation("signatureZones parameter: '{SignatureZones}'", signatureZones ?? "NULL/EMPTY");
            _logger.LogInformation("signatureZones length: {Length}", signatureZones?.Length ?? 0);
            _logger.LogInformation("document.Id: {DocumentId}", document.Id);
            _logger.LogInformation("=== END SIGNATURE ZONES DEBUG ===");

            // Save signature zones if provided
            if (!string.IsNullOrEmpty(signatureZones))
            {
                try
                {
                    // Parse zones from array format: [{"zoneId":"...","stepId":"...","signerRole":"...","x":100,"y":200,"width":200,"height":80}]
                    var zones = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(signatureZones);
                    if (zones != null && zones.Any())
                    {
                        int order = 1;
                        foreach (var zone in zones)
                        {
                            double x = 0, y = 0;
                            double width = 200, height = 80;
                            Guid stepId = Guid.Empty;
                            string? label = null;
                            
                            if (zone.TryGetProperty("x", out var xElement))
                                x = xElement.GetDouble();
                            if (zone.TryGetProperty("y", out var yElement))
                                y = yElement.GetDouble();
                            if (zone.TryGetProperty("width", out var widthElement))
                                width = widthElement.GetDouble();
                            if (zone.TryGetProperty("height", out var heightElement))
                                height = heightElement.GetDouble();
                            if (zone.TryGetProperty("stepId", out var stepIdElement) && stepIdElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                            {
                                if (Guid.TryParse(stepIdElement.GetString(), out var parsedStepId))
                                    stepId = parsedStepId;
                            }
                            if (zone.TryGetProperty("signerRole", out var labelElement) && labelElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                                label = labelElement.GetString();
                            
                            var entity = new SignatureZone
                            {
                                DocumentId = document.Id,
                                PageNumber = 1,
                                PositionX = x,
                                PositionY = y,
                                Width = width,
                                Height = height,
                                IsRequired = true,
                                Order = order++,
                                Label = label ?? $"Zone {order}"
                            };
                            
                            _dbContext.SignatureZones.Add(entity);
                        }
                        
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("SUCCESS: Saved {Count} signature zones for document {DocumentId}", zones.Count, document.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Parsed zones list is null or empty after deserialization");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ERROR: Failed to parse/save signature zones JSON: {Zones}", signatureZones);
                }
            }

            _logger.LogInformation("Document uploaded: {Title} by {User}", document.Title, user.Email);

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(Details), new { id = document.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            ModelState.AddModelError(string.Empty, "Error uploading document. Please try again.");
            var workflows = await _workflowService.GetAllAsync();
            ViewBag.Workflows = workflows;
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        // DEBUG: Log signature zones loaded
        _logger.LogInformation("=== DETAILS PAGE DEBUG ===");
        _logger.LogInformation("Document ID: {DocumentId}", document.Id);
        _logger.LogInformation("Document Title: {Title}", document.Title);
        _logger.LogInformation("SignatureZones count: {Count}", document.SignatureZones?.Count ?? 0);
        if (document.SignatureZones != null && document.SignatureZones.Any())
        {
            foreach (var zone in document.SignatureZones)
            {
                _logger.LogInformation("  Zone: Id={ZoneId}, X={X}, Y={Y}, Width={Width}, Height={Height}", 
                    zone.Id, zone.PositionX, zone.PositionY, zone.Width, zone.Height);
            }
        }
        _logger.LogInformation("=== END DETAILS PAGE DEBUG ===");

        // Get actual page size from PDF
        var documentEntity = await _documentService.GetEntityByIdAsync(id);
        if (documentEntity != null)
        {
            var pdfStream = await _fileStorageService.GetFileAsync(documentEntity.FileName);
            if (pdfStream != null)
            {
                using (pdfStream)
                {
                    using var ms = new MemoryStream();
                    await pdfStream.CopyToAsync(ms);
                    var pdfBytes = ms.ToArray();
                    var (pageWidth, pageHeight) = await _pdfService.GetPageSizeAsync(pdfBytes, 1);
                    
                    ViewBag.PageWidth = pageWidth;
                    ViewBag.PageHeight = pageHeight;
                }
            }
            else
            {
                ViewBag.PageWidth = 595.28;
                ViewBag.PageHeight = 841.89;
            }
        }
        else
        {
            ViewBag.PageWidth = 595.28;
            ViewBag.PageHeight = 841.89;
        }

        return View(document);
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id, bool signed = false)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        // Check authorization
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Only creator or approvers can download
        if (document.CreatedById != user.Id && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            return Forbid();
        }

        string? filePath;
        string fileName;
        
        // If signed=true and there's a signed file, use it
        if (signed && !string.IsNullOrEmpty(document.SignedFilePath))
        {
            filePath = document.SignedFilePath;
            fileName = "signed_" + document.OriginalFileName;
        }
        else
        {
            filePath = document.FileName;
            fileName = document.OriginalFileName;
        }

        // DEBUG: Log path resolution details
        _logger.LogInformation("DEBUG: Document.FileName = {FileName}, SignedFilePath = {SignedFilePath}, signed = {signed}", 
            document.FileName, document.SignedFilePath, signed);

        // Use FileStorageService to get the file - this ensures correct path resolution
        var fileStream = await _fileStorageService.GetFileAsync(filePath);
        if (fileStream == null)
        {
            _logger.LogError("File not found for FileName: {FileName}", filePath);
            return NotFound("File not found: " + filePath);
        }

        // Use the stored content type (should be application/pdf for converted documents)
        var contentType = string.IsNullOrEmpty(document.ContentType) ? "application/pdf" : document.ContentType;

        // Return file with inline content-disposition for preview in iframe
        Response.Headers.Add("Content-Type", contentType);
        Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");

        return File(fileStream, contentType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Delete action called for document ID: {Id}", id);
        
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            _logger.LogWarning("Document not found: {Id}", id);
            return NotFound();
        }

        _logger.LogInformation("Document found: {Title}, Status: {Status}, CreatedById: {CreatedById}, FileName: {FileName}", 
            document.Title, document.Status, document.CreatedById, document.FileName);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) 
        {
            _logger.LogWarning("User not found for delete operation");
            return Unauthorized();
        }

        _logger.LogInformation("Current user: {UserId}, IsAdmin: {IsAdmin}", user.Id, User.IsInRole("Admin"));

        // Only creator or admin can delete
        if (document.CreatedById != user.Id && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            _logger.LogWarning("User {UserId} not authorized to delete document {Id}. Creator: {CreatedById}", 
                user.Id, id, document.CreatedById);
            return Forbid();
        }

        try
        {
            _logger.LogInformation("Attempting to permanently delete document: {Id}, Status: {Status}, FileName: {FileName}", 
                id, document.Status, document.FileName);

            // Delete file from storage using FileStorageService
            var fileDeleted = false;
            
            if (!string.IsNullOrEmpty(document.FileName))
            {
                _logger.LogInformation("Attempting to delete file: {FileName}", document.FileName);
                await _fileStorageService.DeleteFileAsync(document.FileName);
                fileDeleted = true;
            }
            
            _logger.LogInformation("File deletion attempted for document {Id}, FileDeleted: {FileDeleted}", id, fileDeleted);

            // Permanently delete from database (including all related records)
            await _documentService.DeletePermanentlyAsync(id);
            _logger.LogInformation("Document permanently deleted: {Title} by {User}", document.Title, user.Email);

            TempData["Success"] = "Document deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {Id}", id);
            TempData["Error"] = "Error deleting document. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartWorkflow(Guid id)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        try
        {
            await _documentService.UpdateStatusAsync(id, (int)DocumentStatus.Pending);
            _logger.LogInformation("Document workflow started: {Title}", document.Title);

            TempData["Success"] = "Document workflow started.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow");
            TempData["Error"] = "Error starting workflow.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Versions(Guid id)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        return View(document);
    }

    [HttpGet]
    public async Task<IActionResult> Completed(string? search, int page = 1, int pageSize = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var documents = await _documentService.GetAllAsync(user.CompanyId);
        
        // Filter for completed documents only
        documents = documents.Where(d => d.Status == DocumentStatus.Completed);

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            documents = documents.Where(d => d.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           d.OriginalFileName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var documentList = documents.ToList();
        var totalCount = documentList.Count;
        var pagedDocuments = documentList.Skip((page - 1) * pageSize).Take(pageSize);

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.TotalCount = totalCount;

        return View(pagedDocuments);
    }

    // API endpoint to get workflow steps for a document
    [HttpGet]
    public async Task<IActionResult> GetWorkflowSteps(Guid documentId)
    {
        try
        {
            var steps = await _workflowService.GetDocumentWorkflowStepsAsync(documentId);
            return Json(steps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow steps for document {DocumentId}", documentId);
            return StatusCode(500, new { error = "Error loading workflow steps" });
        }
    }

    // API endpoint to customize workflow for a document
    [HttpPost]
    //[ValidateAntiForgeryToken] - Removed for AJAX simplicity
    public async Task<IActionResult> CustomizeWorkflow([FromBody] List<WorkflowStepCustomizationDto> customizations)
    {
        try
        {
            if (customizations == null || !customizations.Any())
            {
                return BadRequest(new { error = "No customizations provided" });
            }

            var documentId = customizations.First().DocumentId;
            await _workflowService.CustomizeWorkflowForDocumentAsync(documentId, customizations);
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error customizing workflow");
            return StatusCode(500, new { error = "Error customizing workflow" });
        }
    }

    // API endpoint to search users
    [HttpGet]
    public async Task<IActionResult> SearchUsers(string query)
    {
        try
        {
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
            var users = await userManager.Users
                .Where(u => u.IsDeleted == false && (
                    u.FirstName.Contains(query) ||
                    u.LastName.Contains(query) ||
                    u.Email.Contains(query)))
                .Take(20)
                .Select(u => new { id = u.Id, name = u.FirstName + " " + u.LastName + " (" + u.Email + ")" })
                .ToListAsync();
            
            return Json(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, new { error = "Error searching users" });
        }
    }

    // API endpoint to search roles
    [HttpGet]
    public async Task<IActionResult> SearchRoles(string query)
    {
        try
        {
            var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>>();
            var roles = await roleManager.Roles
                .Where(r => r.Name != null && r.Name.Contains(query))
                .Take(20)
                .Select(r => new { id = r.Id, name = r.Name })
                .ToListAsync();
            
            return Json(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching roles");
            return StatusCode(500, new { error = "Error searching roles" });
        }
    }

    // API endpoint to generate signature zones based on workflow
    [HttpGet("Document/generate-zones")]
    public async Task<ActionResult<List<SignatureZoneViewModel>>> GenerateZones(Guid workflowId)
    {
        if (workflowId == Guid.Empty)
            return BadRequest("L'ID du workflow est requis");

        var workflow = await _workflowService.GetByIdAsync(workflowId);
        if (workflow == null)
            return NotFound("Workflow non trouvé");

        // Get steps from workflow DTO
        var steps = workflow.Steps;

        var zones = new List<SignatureZoneViewModel>();
        int zoneIndex = 1;

        foreach (var step in steps.OrderBy(s => s.StepOrder))
        {
            // Use RequiredSignatureCount if available, otherwise default to 1
            int signatureCount = step.RequiredSignatureCount > 0 ? step.RequiredSignatureCount : 1;
            
            for (int i = 0; i < signatureCount; i++)
            {
                zones.Add(new SignatureZoneViewModel
                {
                    ZoneId = Guid.NewGuid(),
                    StepId = step.Id,
                    SignerRole = step.Name,
                    ZoneWidth = 200,
                    ZoneHeight = 80
                });
                zoneIndex++;
            }
        }

        return Ok(zones);
    }
}
