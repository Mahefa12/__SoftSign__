using SoftSign.Domain.Enums;

namespace SoftSign.Application.DTOs;

public class WorkflowDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowStatus Status { get; set; }
    public Guid? CompanyId { get; set; }
    public int Order { get; set; }
    public bool IsDefault { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkflowStepDto> Steps { get; set; } = new();
    
    // Computed property for total required signatures
    public int TotalRequiredSignatures => Steps?.Sum(s => s.RequiredSignatureCount) ?? 0;
}

public class WorkflowStepDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public StepType StepType { get; set; }
    public string? Instructions { get; set; }
    public SignatureLevel RequiredSignatureLevel { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public Guid? AssignedRoleId { get; set; }
    public string? AssignedRoleName { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RequiredSignatureCount { get; set; } = 1;
}

public class WorkflowListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int StepCount { get; set; }
    public bool IsActive { get; set; }
}

public class WorkflowDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowStatus Status { get; set; }
    public Guid? CompanyId { get; set; }
    public int Order { get; set; }
    public bool IsDefault { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<WorkflowStepDetailDto> Steps { get; set; } = new();
}

public class WorkflowStepDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public StepType StepType { get; set; }
    public string? Instructions { get; set; }
    public SignatureLevel RequiredSignatureLevel { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public Guid? AssignedRoleId { get; set; }
    public string? AssignedRoleName { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Comments { get; set; }
    public int RequiredSignatureCount { get; set; } = 1;
}

public class CreateWorkflowDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CompanyId { get; set; }
    public bool IsDefault { get; set; }
    public string? CreatedBy { get; set; }
    public List<WorkflowStepCreateDto> Steps { get; set; } = new();
}

public class WorkflowStepCreateDto
{
    public string Name { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public StepType StepType { get; set; } = StepType.Signature;
    public string? Instructions { get; set; }
    public SignatureLevel RequiredSignatureLevel { get; set; } = SignatureLevel.Signature;
    public Guid? AssignedUserId { get; set; }
    public Guid? AssignedRoleId { get; set; }
    public int RequiredSignatureCount { get; set; } = 1;
}

// DTOs for document-specific workflow customization
public class DocumentWorkflowStepDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid WorkflowStepId { get; set; }
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public StepType StepType { get; set; }
    public string? Instructions { get; set; }
    public Guid? OriginalAssignedUserId { get; set; }
    public string? OriginalAssignedUserName { get; set; }
    public Guid? CustomAssignedUserId { get; set; }
    public string? CustomAssignedUserName { get; set; }
    public Guid? OriginalAssignedRoleId { get; set; }
    public string? OriginalAssignedRoleName { get; set; }
    public Guid? CustomAssignedRoleId { get; set; }
    public string? CustomAssignedRoleName { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Comments { get; set; }
    public bool IsCustomized => CustomAssignedUserId.HasValue || CustomAssignedRoleId.HasValue;
}

public class WorkflowStepCustomizationDto
{
    public Guid DocumentId { get; set; }
    public Guid WorkflowStepId { get; set; }
    public Guid? CustomAssignedUserId { get; set; }
    public Guid? CustomAssignedRoleId { get; set; }
}
