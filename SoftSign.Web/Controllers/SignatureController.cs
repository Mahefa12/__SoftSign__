using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Domain.Interfaces;
using System.IO;
using System.Text.Json;

namespace SoftSign.Web.Controllers;

[Authorize]
public class SignatureController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly ISignatureService _signatureService;
    private readonly IPdfService _pdfService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IRepository<SignatureZone> _zoneRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SignatureController> _logger;
    private readonly IWebHostEnvironment _env;

    public SignatureController(
        IDocumentService documentService,
        ISignatureService signatureService,
        IPdfService pdfService,
        IFileStorageService fileStorageService,
        IRepository<SignatureZone> zoneRepository,
        IUnitOfWork unitOfWork,
        ILogger<SignatureController> logger,
        IWebHostEnvironment env)
    {
        _documentService = documentService;
        _signatureService = signatureService;
        _pdfService = pdfService;
        _fileStorageService = fileStorageService;
        _zoneRepository = zoneRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetZones(Guid documentId)
    {
        var zones = await _zoneRepository.FindAsync(z => z.DocumentId == documentId);
        var zoneList = zones.OrderBy(z => z.Order).Select(z => new
        {
            z.Id,
            z.DocumentId,
            z.PageNumber,
            z.PositionX,
            z.PositionY,
            z.Width,
            z.Height,
            Level = z.Level.ToString(),
            z.Label,
            z.AssignedUserId,
            z.IsRequired,
            z.Order,
            z.IsSigned,
            z.SignedAt
        });

        return Json(zoneList);
    }

    [HttpGet]
    public async Task<IActionResult> CreateZone(Guid documentId)
    {
        var documentEntity = await _documentService.GetEntityByIdAsync(documentId);
        if (documentEntity == null)
        {
            return NotFound();
        }

        var document = await _documentService.GetByIdAsync(documentId);
        
        // Get actual page size from PDF
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
        
        ViewBag.Document = document;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateZone([FromForm] CreateZoneDto model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var zone = new SignatureZone
                {
                    DocumentId = model.DocumentId,
                    PageNumber = model.PageNumber,
                    PositionX = model.PositionX,
                    PositionY = model.PositionY,
                    Width = model.Width,
                    Height = model.Height,
                    Level = Enum.Parse<SignatureLevel>(model.Level),
                    Label = model.Label,
                    AssignedUserId = model.AssignedUserId,
                    IsRequired = model.IsRequired,
                    Order = model.Order
                };

                await _zoneRepository.AddAsync(zone);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Signature zone created for document: {DocumentId}", model.DocumentId);

                return Ok(new { success = true, zoneId = zone.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating signature zone");
                return StatusCode(500, new { message = "Error creating signature zone" });
            }
        }

        return BadRequest(ModelState);
    }

    [HttpPost]
    public async Task<IActionResult> CreateZoneJson([FromBody] CreateZoneDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var zone = new SignatureZone
            {
                DocumentId = model.DocumentId,
                PageNumber = model.PageNumber,
                PositionX = model.PositionX,
                PositionY = model.PositionY,
                Width = model.Width,
                Height = model.Height,
                Level = Enum.Parse<SignatureLevel>(model.Level),
                Label = model.Label,
                AssignedUserId = model.AssignedUserId,
                IsRequired = model.IsRequired,
                Order = model.Order
            };

            await _zoneRepository.AddAsync(zone);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Signature zone created for document: {DocumentId}", model.DocumentId);

            return Ok(new { success = true, zoneId = zone.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating signature zone");
            return StatusCode(500, new { message = "Error creating signature zone" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Draw(Guid documentId)
    {
        var document = await _documentService.GetByIdAsync(documentId);
        if (document == null)
        {
            return NotFound();
        }

        // Get actual page size from PDF
        var documentEntity = await _documentService.GetEntityByIdAsync(documentId);
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

        ViewBag.Document = document;
        
        // Get signature zones for this document
        var zones = await _zoneRepository.FindAsync(z => z.DocumentId == documentId);
        ViewBag.Zones = zones.OrderBy(z => z.Order).ToList();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Submit(Guid documentId, Guid? zoneId, string signatureData, string? comments)
    {
        // DIAGNOSTIC LOG: Log what parameters were received
        _logger.LogInformation("=== SUBMIT SIGNATURE DIAGNOSTIC ===");
        _logger.LogInformation("documentId: {DocumentId}", documentId);
        _logger.LogInformation("zoneId parameter: {ZoneId}", zoneId);
        _logger.LogInformation("signatureData provided: {HasSignatureData}", !string.IsNullOrEmpty(signatureData));
        _logger.LogInformation("signatureData length: {Length}", signatureData?.Length ?? 0);
        
        // Try to read from Request.Form if parameters are not in query string
        if (Request.Method == "POST")
        {
            _logger.LogWarning("Parameters not found in query string, checking Request.Form...");
            
            // Read documentId from Form if not in query string
            if (documentId == default(Guid))
            {
                if (Request.Form.ContainsKey("documentId"))
                {
                    var formDocumentId = Request.Form["documentId"].FirstOrDefault();
                    _logger.LogInformation("Found documentId in Form: {DocumentId}", formDocumentId);
                    if (Guid.TryParse(formDocumentId, out var parsedDocId))
                    {
                        documentId = parsedDocId;
                    }
                }
            }
            
            if (zoneId == null || string.IsNullOrEmpty(signatureData))
            {
                if (Request.Form.ContainsKey("zoneId"))
                {
                    var formZoneId = Request.Form["zoneId"].FirstOrDefault();
                    _logger.LogInformation("Found zoneId in Form: {ZoneId}", formZoneId);
                    if (Guid.TryParse(formZoneId, out var parsedZoneId))
                    {
                        zoneId = parsedZoneId;
                    }
                }
                if (Request.Form.ContainsKey("signatureData"))
                {
                    var formSignatureData = Request.Form["signatureData"].FirstOrDefault();
                    _logger.LogInformation("Found signatureData in Form, length: {Length}", formSignatureData?.Length ?? 0);
                    signatureData = formSignatureData;
                }
            }
        }
        
        // Log final parsed values
        _logger.LogInformation("FINAL VALUES - documentId: {DocumentId}, zoneId: {ZoneId}, signatureData length: {Length}", 
            documentId, zoneId, signatureData?.Length ?? 0);
        
        var document = await _documentService.GetByIdAsync(documentId);
        if (document == null)
        {
            _logger.LogWarning("Document not found: {DocumentId}", documentId);
            return Json(new { success = false, message = "Document not found" });
        }
        if (document == null)
        {
            return NotFound();
        }

        try
        {
            // Validate signature data
            if (string.IsNullOrEmpty(signatureData))
            {
                _logger.LogWarning("Signature data is empty for document: {DocumentId}", documentId);
                return Json(new { success = false, message = "Please provide your signature." });
            }

            // Get the current user ID
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                _logger.LogWarning("User claim not found for document: {DocumentId}", documentId);
                return Json(new { success = false, message = "Unable to identify user." });
            }
            var currentUserId = Guid.Parse(userIdClaim.Value);

            // Get document entity for file path
            var documentEntity = await _documentService.GetEntityByIdAsync(documentId);
            if (documentEntity == null)
            {
                return NotFound();
            }

            // Get signature zones from database
            var zones = await _zoneRepository.FindAsync(z => z.DocumentId == documentId);
            var zoneList = zones.OrderBy(z => z.Order).ToList();

            // Get the PDF file
            var fileStream = await _fileStorageService.GetFileAsync(documentEntity.FileName);
            if (fileStream == null)
            {
                _logger.LogError("File not found for FileName: {FileName}", documentEntity.FileName);
                return NotFound("Document file not found");
            }

            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            fileStream.Dispose();
            var pdfBytes = ms.ToArray();

            // Get page size for logging
            double pageWidth = 595.28;
            double pageHeight = 841.89;
            (pageWidth, pageHeight) = await _pdfService.GetPageSizeAsync(pdfBytes, 1);

            // If a specific zone is provided, only sign that zone
            if (zoneId.HasValue)
            {
                var targetZone = zoneList.FirstOrDefault(z => z.Id == zoneId.Value);
                if (targetZone == null)
                {
                    _logger.LogWarning("Signature zone not found: {ZoneId}", zoneId.Value);
                    return Json(new { success = false, message = "Signature zone not found." });
                }

                // Check if zone already has a signature
                if (targetZone.IsSigned)
                {
                    _logger.LogWarning("Zone already signed: {ZoneId}", zoneId.Value);
                    return Json(new { success = false, message = "This zone has already been signed." });
                }

                // Save signature to the specific zone
                targetZone.SignatureImage = signatureData;
                targetZone.SignedByUserId = currentUserId;
                targetZone.SignedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Signature applied to zone {ZoneId} for document {DocumentId} by user {UserId}", 
                    zoneId.Value, documentId, currentUserId);

                // Check if all required zones are signed
                var requiredZones = zoneList.Where(z => z.IsRequired).ToList();
                var allRequiredSigned = requiredZones.All(z => z.IsSigned);

                if (allRequiredSigned)
                {
                    // All required zones are signed - generate final PDF using ApplySignaturesFromZonesAsync
                    _logger.LogInformation("All required zones signed for document {DocumentId}. Generating final signed PDF.", documentId);

                    // Reload zones with updated signature data - use explicit SignatureImage check for EF Core
                    var signedZones = await _zoneRepository.FindAsync(z => z.DocumentId == documentId && z.SignatureImage != null && z.SignatureImage != "");
                    var signedZoneList = signedZones.ToList();
                    
                    _logger.LogInformation("Found {Count} signed zones to apply to PDF", signedZoneList.Count);
                    foreach (var z in signedZoneList)
                    {
                        _logger.LogInformation("Zone {ZoneId}: Page={Page}, X={X}, Y={Y}, HasSignature={HasSig}", 
                            z.Id, z.PageNumber, z.PositionX, z.PositionY, !string.IsNullOrEmpty(z.SignatureImage));
                    }

                    // Use ApplySignaturesFromZonesAsync to apply all signatures
                    var signedPdf = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, signedZoneList);

                    // Save signed PDF
                    var signedFileName = $"signed_{Path.GetFileName(documentEntity.FileName)}";
                    var signedFilePath = await _fileStorageService.SaveFileAsync(new MemoryStream(signedPdf), signedFileName, "documents");

                    // Update document with signed file path and status
                    await _documentService.UpdateSignedFilePathAsync(documentId, signedFilePath);
                    await _documentService.UpdateStatusAsync(documentId, (int)DocumentStatus.Completed);

                    _logger.LogInformation("Document {DocumentId} completed. Final signed PDF generated with {Count} signatures.", 
                        documentId, signedZoneList.Count);
                }
                else
                {
                    // Partial signing - don't generate final PDF yet
                    _logger.LogInformation("Partial signing for document {DocumentId}. {Signed}/{Total} required zones signed.", 
                        documentId, requiredZones.Count(z => z.IsSigned), requiredZones.Count);

                    // Update document status to InProgress if it's still Pending
                    if (documentEntity.Status == DocumentStatus.Pending)
                    {
                        await _documentService.UpdateStatusAsync(documentId, (int)DocumentStatus.InProgress);
                    }
                }

                _logger.LogInformation("Signature saved successfully for zone {ZoneId}", zoneId.Value);
                return Json(new { success = true, message = "Signature applied successfully!", redirectUrl = $"/Document/Details/{documentId}" });
            }

            // Original behavior: apply same signature to all zones
            // Convert base64 to bytes (kept for backwards compatibility)
            var signatureBytes = Convert.FromBase64String(signatureData.Replace("data:image/png;base64,", ""));

            _logger.LogDebug("FileStream acquired for {FileName}, disposing after copy", documentEntity.FileName);
            
            _logger.LogDebug("FileStream disposed for {FileName}", documentEntity.FileName);
            
            _logger.LogInformation("Found {Count} signature zones for document {DocumentId}. PDF page size: {Width}x{Height}", 
                zoneList.Count, documentId, pageWidth, pageHeight);
            foreach (var zone in zoneList)
            {
                _logger.LogInformation("Zone: Page={Page}, X={X}, Y={Y}, Width={Width}, Height={Height}", 
                    zone.PageNumber, zone.PositionX, zone.PositionY, zone.Width, zone.Height);
            }
            
            // Save signature to all zones
            foreach (var zone in zoneList)
            {
                _logger.LogInformation("=== SIGNATURE PLACEMENT DEBUG ===");
                _logger.LogInformation("Zone from DB: Page={Page}, X={X}, Y={Y}, Width={Width}, Height={Height}",
                    zone.PageNumber, zone.PositionX, zone.PositionY, zone.Width, zone.Height);
                
                // Coordinates are already stored in PDF coordinate space (bottom-left origin)
                // from the CreateZone.cshtml fix - just use them directly
                var drawX = zone.PositionX;
                var drawY = zone.PositionY;
                
                _logger.LogInformation("Final placement: Page={Page}, X={X}, Y={Y}, Width={W}, Height={H}",
                    zone.PageNumber, drawX, drawY, zone.Width, zone.Height);
                
                // Save signature to zone
                zone.SignatureImage = signatureData;
                zone.SignedByUserId = currentUserId;
                zone.SignedAt = DateTime.UtcNow;
            }

            // Save all zone changes
            await _unitOfWork.SaveChangesAsync();

            // Check if all required zones are signed
            var allRequiredZones = zoneList.Where(z => z.IsRequired).ToList();
            var allRequiredZonesSigned = allRequiredZones.All(z => z.IsSigned);

            if (allRequiredZonesSigned)
            {
                // All required zones are signed - generate final PDF using ApplySignaturesFromZonesAsync
                _logger.LogInformation("All required zones signed for document {DocumentId}. Generating final signed PDF using ApplySignaturesFromZonesAsync.", documentId);

                // Get all signed zones - use explicit SignatureImage check for EF Core
                var allSignedZones = zoneList.Where(z => !string.IsNullOrEmpty(z.SignatureImage)).ToList();

                _logger.LogInformation("Found {Count} signed zones to apply to PDF", allSignedZones.Count);

                if (allSignedZones.Count > 0)
                {
                    // Use ApplySignaturesFromZonesAsync to apply all signatures at once
                    var signedPdf = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, allSignedZones);

                    // Save signed PDF using FileStorageService
                    var signedFileNameLegacy = $"signed_{Path.GetFileName(documentEntity.FileName)}";
                    var signedFilePathLegacy = await _fileStorageService.SaveFileAsync(new MemoryStream(signedPdf), signedFileNameLegacy, "documents");
                    
                    // Update document with signed file path and status
                    await _documentService.UpdateSignedFilePathAsync(documentId, signedFilePathLegacy);
                }

                await _documentService.UpdateStatusAsync(documentId, (int)DocumentStatus.Completed);

                _logger.LogInformation("Document signed: {Title}", document.Title);

                TempData["Success"] = "Document signed successfully!";
            }
            else
            {
                // Not all required zones are signed
                _logger.LogInformation("Document {DocumentId} has {Signed}/{Total} required zones signed. Status remains unchanged.",
                    documentId, allRequiredZones.Count(z => z.IsSigned), allRequiredZones.Count);

                TempData["Success"] = "Signatures applied to zones!";
            }

            return RedirectToAction("Details", "Document", new { id = documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting signature for document {DocumentId}", documentId);
            return Json(new { success = false, message = "Error submitting signature. Please try again." });
        }
    }

    [HttpPost]
    public IActionResult ValidateZone(ValidateZoneDto model)
    {
        // Validate that signature is within zone bounds
        if (model.SignatureX < model.ZoneX || 
            model.SignatureX + model.SignatureWidth > model.ZoneX + model.ZoneWidth ||
            model.SignatureY < model.ZoneY ||
            model.SignatureY + model.SignatureHeight > model.ZoneY + model.ZoneHeight)
        {
            return Json(new { valid = false, message = "Signature must be within the designated zone" });
        }

        return Json(new { valid = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteZone(Guid id)
    {
        var zone = await _zoneRepository.GetByIdAsync(id);
        if (zone == null)
        {
            return NotFound();
        }

        _zoneRepository.Remove(zone);
        await _unitOfWork.SaveChangesAsync();

        return Ok();
    }
}
