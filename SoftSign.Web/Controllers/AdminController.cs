using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;

namespace SoftSign.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IWorkflowService _workflowService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IWorkflowService workflowService,
        IDocumentService documentService,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _workflowService = workflowService;
        _documentService = documentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var documents = await _documentService.GetAllAsync();
        var documentList = documents.ToList();
        
        var model = new AdminDashboardViewModel
        {
            TotalDocuments = documentList.Count,
            PendingDocuments = documentList.Count(d => d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.InProgress),
            CompletedDocuments = documentList.Count(d => d.Status == DocumentStatus.Completed),
            RejectedDocuments = documentList.Count(d => d.Status == DocumentStatus.Rejected),
            TotalUsers = _userManager.Users.Count(),
            ActiveUsers = _userManager.Users.Count(u => u.IsActive),
            RecentDocuments = documentList.OrderByDescending(d => d.CreatedAt).Take(5).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ManageUsers(string? search, string? role, int page = 1, int pageSize = 20)
    {
        var users = _userManager.Users.ToList();
        
        // Filter by search
        if (!string.IsNullOrEmpty(search))
        {
            users = users.Where(u => 
                (u.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.FirstName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.LastName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        // Filter by role
        if (!string.IsNullOrEmpty(role))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            var userIds = usersInRole.Select(u => u.Id).ToHashSet();
            users = users.Where(u => userIds.Contains(u.Id)).ToList();
        }

        var totalCount = users.Count;
        var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // Get roles for each user
        var userViewModels = new List<UserManagementViewModel>();
        foreach (var user in pagedUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserManagementViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt
            });
        }

        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.TotalCount = totalCount;
        ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();

        return View(userViewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"User status updated to {(user.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(ManageUsers));
    }

    [HttpGet]
    public async Task<IActionResult> ManageRoles()
    {
        var roles = _roleManager.Roles.ToList();
        var roleViewModels = new List<RoleManagementViewModel>();

        foreach (var role in roles)
        {
            var users = await _userManager.GetUsersInRoleAsync(role.Name!);
            roleViewModels.Add(new RoleManagementViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                UserCount = users.Count,
                Description = role.Name switch
                {
                    "SuperAdmin" => "Full system access",
                    "Admin" => "Administrative access",
                    "Manager" => "Manage documents and workflows",
                    "User" => "Standard user access",
                    _ => "User role"
                }
            });
        }

        return View(roleViewModels);
    }

    [HttpGet]
    public IActionResult ManageWorkflows()
    {
        return RedirectToAction("Index", "Workflow");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Role name is required.");
            return RedirectToAction(nameof(ManageRoles));
        }

        if (await _roleManager.RoleExistsAsync(name))
        {
            ModelState.AddModelError(string.Empty, "Role already exists.");
            return RedirectToAction(nameof(ManageRoles));
        }

        var result = await _roleManager.CreateAsync(new IdentityRole<Guid>(name));
        if (result.Succeeded)
        {
            _logger.LogInformation("Role created: {RoleName}", name);
            TempData["Success"] = $"Role '{name}' created successfully.";
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return RedirectToAction(nameof(ManageRoles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRole(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return NotFound();
        }

        var role = await _roleManager.FindByNameAsync(name);
        if (role == null) return NotFound();

        var result = await _roleManager.DeleteAsync(role);
        if (result.Succeeded)
        {
            _logger.LogInformation("Role deleted: {RoleName}", name);
            TempData["Success"] = $"Role '{name}' deleted successfully.";
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return RedirectToAction(nameof(ManageRoles));
    }
}

public class AdminDashboardViewModel
{
    public int TotalDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int CompletedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public List<DocumentDto> RecentDocuments { get; set; } = new();
}

public class UserManagementViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class RoleManagementViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public string Description { get; set; } = string.Empty;
}
