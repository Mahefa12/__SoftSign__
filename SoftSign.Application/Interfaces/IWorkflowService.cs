using SoftSign.Application.DTOs;

namespace SoftSign.Application.Interfaces;

public interface IWorkflowService
{
    Task<WorkflowDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<WorkflowDto>> GetAllAsync(Guid? companyId = null);
    Task<WorkflowDto> CreateAsync(CreateWorkflowDto dto);
    Task<WorkflowDto> UpdateAsync(Guid id, CreateWorkflowDto dto);
    Task DeleteAsync(Guid id);
    Task<WorkflowDto?> GetDefaultWorkflowAsync(Guid? companyId = null);
    
    // New methods for Admin Workflow Creation
    Task<IEnumerable<WorkflowListDto>> GetWorkflowsAsync();
    Task<WorkflowDetailDto?> GetWorkflowDetailAsync(Guid id);
    Task<WorkflowDto> DuplicateWorkflowAsync(Guid id);
    Task<(bool IsValid, List<string> Errors)> ValidateWorkflowAsync(CreateWorkflowDto dto);

    // Document-specific workflow customization
    Task<List<DocumentWorkflowStepDto>> GetDocumentWorkflowStepsAsync(Guid documentId);
    Task CustomizeWorkflowForDocumentAsync(Guid documentId, List<WorkflowStepCustomizationDto> customizations);
}
