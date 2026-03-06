using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SoftSign.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminWorkflowController : Controller
{
    private readonly IWorkflowService _workflowService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AdminWorkflowController> _logger;

    public AdminWorkflowController(
        IWorkflowService workflowService,
        UserManager<User> userManager,
        ILogger<AdminWorkflowController> logger)
    {
        _workflowService = workflowService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var workflows = await _workflowService.GetWorkflowsAsync();
            return View(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workflows");
            TempData["Error"] = "Erreur lors du chargement des workflows.";
            return View(new List<WorkflowListDto>());
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new CreateWorkflowDto
        {
            Steps = new List<WorkflowStepCreateDto>()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Required] CreateWorkflowDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Validate business rules
            var (isValid, errors) = await _workflowService.ValidateWorkflowAsync(model);
            
            if (!isValid)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                return View(model);
            }

            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            model.CreatedBy = user.Id.ToString();

            // Create workflow
            await _workflowService.CreateAsync(model);

            _logger.LogInformation("Workflow created: {Name} by {User}", model.Name, user.Email);
            TempData["Success"] = "Workflow créé avec succès.";
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            ModelState.AddModelError(string.Empty, "Erreur lors de la création du workflow. Veuillez réessayer.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> View(Guid id)
    {
        try
        {
            var workflow = await _workflowService.GetWorkflowDetailAsync(id);
            
            if (workflow == null)
            {
                _logger.LogWarning("Workflow not found: {Id}", id);
                TempData["Error"] = "Workflow introuvable.";
                return RedirectToAction(nameof(Index));
            }

            return View(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workflow details for {Id}", id);
            TempData["Error"] = "Erreur lors du chargement des détails du workflow.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(Guid id)
    {
        try
        {
            await _workflowService.DuplicateWorkflowAsync(id);
            
            _logger.LogInformation("Workflow duplicated: {Id}", id);
            TempData["Success"] = "Workflow dupliqué avec succès.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating workflow {Id}", id);
            TempData["Error"] = "Erreur lors de la duplication du workflow.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _workflowService.DeleteAsync(id);
            
            _logger.LogInformation("Workflow deleted: {Id}", id);
            TempData["Success"] = "Workflow supprimé avec succès.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {Id}", id);
            TempData["Error"] = "Erreur lors de la suppression du workflow.";
        }

        return RedirectToAction(nameof(Index));
    }
}
