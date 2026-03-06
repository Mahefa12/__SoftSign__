using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SoftSign.Application.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IRepository<Workflow> _workflowRepository;
    private readonly IRepository<WorkflowStep> _stepRepository;
    private readonly IRepository<DocumentWorkflowStep> _documentStepRepository;
    private readonly IRepository<Document> _documentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkflowService(
        IRepository<Workflow> workflowRepository,
        IRepository<WorkflowStep> stepRepository,
        IRepository<DocumentWorkflowStep> documentStepRepository,
        IRepository<Document> documentRepository,
        IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _stepRepository = stepRepository;
        _documentStepRepository = documentStepRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkflowDto?> GetByIdAsync(Guid id)
    {
        var workflow = await _workflowRepository.Query()
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (workflow == null) return null;
        return MapToDto(workflow);
    }

    public async Task<IEnumerable<WorkflowDto>> GetAllAsync(Guid? companyId = null)
    {
        IEnumerable<Workflow> workflows;
        if (companyId.HasValue)
        {
            // BUG FIX: Include Steps when filtering by companyId
            workflows = await _workflowRepository.Query()
                .Include(w => w.Steps)
                .Where(w => w.CompanyId == companyId.Value)
                .ToListAsync();
        }
        else
        {
            workflows = await _workflowRepository.Query().Include(w => w.Steps).ToListAsync();
        }

        return workflows.Select(MapToDto);
    }

    public async Task<WorkflowDto> CreateAsync(CreateWorkflowDto dto)
    {
        var workflow = new Workflow
        {
            Name = dto.Name,
            Description = dto.Description,
            CompanyId = dto.CompanyId,
            IsDefault = dto.IsDefault
        };

        foreach (var stepDto in dto.Steps)
        {
            workflow.Steps.Add(new WorkflowStep
            {
                Name = stepDto.Name,
                StepOrder = stepDto.StepOrder,
                StepType = stepDto.StepType,
                Instructions = stepDto.Instructions,
                RequiredSignatureLevel = stepDto.RequiredSignatureLevel,
                AssignedUserId = stepDto.AssignedUserId,
                AssignedRoleId = stepDto.AssignedRoleId,
                RequiredSignatureCount = stepDto.RequiredSignatureCount
            });
        }

        await _workflowRepository.AddAsync(workflow);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(workflow);
    }

    public async Task<WorkflowDto> UpdateAsync(Guid id, CreateWorkflowDto dto)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Workflow not found");

        workflow.Name = dto.Name;
        workflow.Description = dto.Description;
        workflow.IsDefault = dto.IsDefault;
        workflow.UpdatedAt = DateTime.UtcNow;

        _workflowRepository.Update(workflow);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(workflow);
    }

    public async Task DeleteAsync(Guid id)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Workflow not found");

        workflow.IsDeleted = true;
        _workflowRepository.Update(workflow);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<WorkflowDto?> GetDefaultWorkflowAsync(Guid? companyId = null)
    {
        Workflow? workflow;
        if (companyId.HasValue)
            workflow = await _workflowRepository.FirstOrDefaultAsync(w => w.IsDefault && w.CompanyId == companyId.Value);
        else
            workflow = await _workflowRepository.FirstOrDefaultAsync(w => w.IsDefault && w.CompanyId == null);

        return workflow != null ? MapToDto(workflow) : null;
    }

    private static WorkflowDto MapToDto(Workflow workflow)
    {
        return new WorkflowDto
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description,
            Status = workflow.Status,
            CompanyId = workflow.CompanyId,
            Order = workflow.Order,
            IsDefault = workflow.IsDefault,
            CreatedBy = workflow.CreatedBy,
            IsActive = workflow.IsActive,
            CreatedAt = workflow.CreatedAt,
            Steps = workflow.Steps?.Select(s => new WorkflowStepDto
            {
                Id = s.Id,
                Name = s.Name,
                StepOrder = s.StepOrder,
                StepType = s.StepType,
                Instructions = s.Instructions,
                RequiredSignatureLevel = s.RequiredSignatureLevel,
                AssignedUserId = s.AssignedUserId,
                AssignedUserName = s.AssignedUser != null ? $"{s.AssignedUser.FirstName} {s.AssignedUser.LastName}" : null,
                AssignedRoleId = s.AssignedRoleId,
                IsCompleted = s.IsCompleted,
                CompletedAt = s.CompletedAt,
                RequiredSignatureCount = s.RequiredSignatureCount
            }).ToList() ?? new List<WorkflowStepDto>()
        };
    }

    public async Task<IEnumerable<WorkflowListDto>> GetWorkflowsAsync()
    {
        var workflows = await _workflowRepository.GetAllAsync();
        return workflows.Select(w => new WorkflowListDto
        {
            Id = w.Id,
            Name = w.Name,
            CreatedBy = w.CreatedBy,
            CreatedAt = w.CreatedAt,
            StepCount = w.Steps.Count,
            IsActive = w.IsActive
        });
    }

    public async Task<WorkflowDetailDto?> GetWorkflowDetailAsync(Guid id)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id);
        if (workflow == null) return null;

        return new WorkflowDetailDto
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description,
            Status = workflow.Status,
            CompanyId = workflow.CompanyId,
            Order = workflow.Order,
            IsDefault = workflow.IsDefault,
            CreatedBy = workflow.CreatedBy,
            IsActive = workflow.IsActive,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            Steps = workflow.Steps?.OrderBy(s => s.StepOrder).Select(s => new WorkflowStepDetailDto
            {
                Id = s.Id,
                Name = s.Name,
                StepOrder = s.StepOrder,
                StepType = s.StepType,
                Instructions = s.Instructions,
                RequiredSignatureLevel = s.RequiredSignatureLevel,
                AssignedUserId = s.AssignedUserId,
                AssignedUserName = s.AssignedUser != null ? $"{s.AssignedUser.FirstName} {s.AssignedUser.LastName}" : null,
                AssignedRoleId = s.AssignedRoleId,
                IsCompleted = s.IsCompleted,
                CompletedAt = s.CompletedAt,
                Comments = s.Comments,
                RequiredSignatureCount = s.RequiredSignatureCount
            }).ToList() ?? new List<WorkflowStepDetailDto>()
        };
    }

    public async Task<WorkflowDto> DuplicateWorkflowAsync(Guid id)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Workflow not found");

        var newWorkflow = new Workflow
        {
            Name = $"{workflow.Name}_copie",
            Description = workflow.Description,
            CompanyId = workflow.CompanyId,
            IsDefault = false,
            CreatedBy = workflow.CreatedBy,
            Status = WorkflowStatus.Active,
            IsActive = true
        };

        foreach (var step in workflow.Steps.OrderBy(s => s.StepOrder))
        {
            newWorkflow.Steps.Add(new WorkflowStep
            {
                Name = step.Name,
                StepOrder = step.StepOrder,
                StepType = step.StepType,
                Instructions = step.Instructions,
                RequiredSignatureLevel = step.RequiredSignatureLevel,
                AssignedUserId = step.AssignedUserId,
                AssignedRoleId = step.AssignedRoleId
            });
        }

        await _workflowRepository.AddAsync(newWorkflow);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(newWorkflow);
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateWorkflowAsync(CreateWorkflowDto dto)
    {
        var errors = new List<string>();

        // Validate workflow name
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add("Workflow name is required");
        }

        // Validate at least one step exists
        if (dto.Steps == null || dto.Steps.Count == 0)
        {
            errors.Add("At least one step is required");
        }
        else
        {
            // Validate each step
            for (int i = 0; i < dto.Steps.Count; i++)
            {
                var step = dto.Steps[i];
                var stepLabel = $"Step {i + 1} ('{step.Name ?? "unnamed"}')";

                // Check StepType is selected (optional for now)
                // if (step.StepType == default)
                // {
                //     errors.Add($"{stepLabel}: StepType must be selected");
                // }

                // Check either RoleId or UserId is assigned (optional for now)
                // if (!step.AssignedRoleId.HasValue && !step.AssignedUserId.HasValue)
                // {
                //     errors.Add($"{stepLabel}: Either Role or User must be assigned");
                // }
            }

            // Check for duplicate roles in sequence (consecutive steps with same RoleId)
            // This is temporarily disabled for easier testing
            // if (dto.Steps.Count > 1)
            // {
            //     var orderedSteps = dto.Steps.OrderBy(s => s.StepOrder).ToList();
            //     for (int i = 0; i < orderedSteps.Count - 1; i++)
            //     {
            //         var currentStep = orderedSteps[i];
            //         var nextStep = orderedSteps[i + 1];

            //         if (currentStep.AssignedRoleId.HasValue && 
            //             nextStep.AssignedRoleId.HasValue &&
            //             currentStep.AssignedRoleId == nextStep.AssignedRoleId)
            //         {
            //             errors.Add($"Consecutive steps cannot have the same role assigned. Check steps '{currentStep.Name}' and '{nextStep.Name}'");
            //         }
            //     }
            // }
        }

        return (errors.Count == 0, errors);
    }

    public async Task<List<DocumentWorkflowStepDto>> GetDocumentWorkflowStepsAsync(Guid documentId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null || !document.WorkflowId.HasValue)
        {
            return new List<DocumentWorkflowStepDto>();
        }

        // If workflow is customized for this document, return the custom steps
        if (document.IsWorkflowCustomized)
        {
            var customSteps = await _documentStepRepository.FindAsync(s => s.DocumentId == documentId);
            return customSteps.Select(s => new DocumentWorkflowStepDto
            {
                Id = s.Id,
                DocumentId = s.DocumentId,
                WorkflowStepId = s.WorkflowStepId,
                StepOrder = s.StepOrder,
                Name = s.Name,
                StepType = (StepType)s.StepType,
                Instructions = s.Instructions,
                OriginalAssignedUserId = s.OriginalAssignedUserId,
                CustomAssignedUserId = s.CustomAssignedUserId,
                OriginalAssignedRoleId = s.OriginalAssignedRoleId,
                CustomAssignedRoleId = s.CustomAssignedRoleId,
                IsCompleted = s.IsCompleted,
                CompletedAt = s.CompletedAt,
                Comments = s.Comments
            }).OrderBy(s => s.StepOrder).ToList();
        }

        // Otherwise, return the workflow template steps
        var workflowSteps = await _stepRepository.FindAsync(s => s.WorkflowId == document.WorkflowId.Value);
        return workflowSteps.Select(s => new DocumentWorkflowStepDto
        {
            Id = Guid.Empty,
            DocumentId = documentId,
            WorkflowStepId = s.Id,
            StepOrder = s.StepOrder,
            Name = s.Name,
            StepType = s.StepType,
            Instructions = s.Instructions,
            OriginalAssignedUserId = s.AssignedUserId,
            CustomAssignedUserId = null,
            OriginalAssignedRoleId = s.AssignedRoleId,
            CustomAssignedRoleId = null,
            IsCompleted = false,
            CompletedAt = null,
            Comments = null
        }).OrderBy(s => s.StepOrder).ToList();
    }

    public async Task CustomizeWorkflowForDocumentAsync(Guid documentId, List<WorkflowStepCustomizationDto> customizations)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null || !document.WorkflowId.HasValue)
        {
            throw new InvalidOperationException("Document or workflow not found");
        }

        // Get the workflow steps
        var workflowSteps = await _stepRepository.FindAsync(s => s.WorkflowId == document.WorkflowId.Value);
        var stepDict = workflowSteps.ToDictionary(s => s.Id);

        // Clear existing customizations
        var existingSteps = (await _documentStepRepository.FindAsync(s => s.DocumentId == documentId)).ToList();
        foreach (var step in existingSteps)
        {
            _documentStepRepository.Remove(step);
        }

        // Create new document workflow steps with customizations
        int order = 1;
        foreach (var step in workflowSteps.OrderBy(s => s.StepOrder))
        {
            var customization = customizations.FirstOrDefault(c => c.WorkflowStepId == step.Id);
            
            var docStep = new DocumentWorkflowStep
            {
                DocumentId = documentId,
                WorkflowStepId = step.Id,
                StepOrder = order++,
                Name = step.Name,
                StepType = (int)step.StepType,
                Instructions = step.Instructions,
                OriginalAssignedUserId = step.AssignedUserId,
                OriginalAssignedRoleId = step.AssignedRoleId,
                CustomAssignedUserId = customization?.CustomAssignedUserId,
                CustomAssignedRoleId = customization?.CustomAssignedRoleId,
                IsCompleted = false
            };

            await _documentStepRepository.AddAsync(docStep);
        }

        // Mark document as customized
        document.IsWorkflowCustomized = true;
        _documentRepository.Update(document);

        await _unitOfWork.SaveChangesAsync();
    }
}
