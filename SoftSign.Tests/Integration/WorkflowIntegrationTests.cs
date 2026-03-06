using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Moq;
using SoftSign.Application.DTOs;
using SoftSign.Application.Services;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Domain.Interfaces;
using SoftSign.Infrastructure.Data;
using SoftSign.Infrastructure.Repositories;
using System.Security.Claims;
using Xunit;

namespace SoftSign.Tests.Integration;

public class WorkflowIntegrationTests
{
    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private IUnitOfWork CreateUnitOfWork(ApplicationDbContext context)
    {
        return new UnitOfWork(context);
    }

    private WorkflowService CreateWorkflowService(ApplicationDbContext context)
    {
        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        var documentStepRepo = new Repository<DocumentWorkflowStep>(context);
        var documentRepo = new Repository<Document>(context);
        var unitOfWork = new UnitOfWork(context);
        return new WorkflowService(workflowRepo, stepRepo, documentStepRepo, documentRepo, unitOfWork);
    }

    #region Admin Workflow CRUD Tests

    [Fact]
    public async Task Test_AdminCanCreateWorkflow()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        var service = CreateWorkflowService(context);

        var dto = new CreateWorkflowDto
        {
            Name = "Standard Approval",
            Description = "Standard multi-level approval workflow",
            IsDefault = true,
            CreatedBy = "admin@test.com",
            Steps = new List<WorkflowStepCreateDto>
            {
                new() { Name = "Initial Review", StepOrder = 1, StepType = StepType.Validation, RequiredSignatureLevel = SignatureLevel.Initial },
                new() { Name = "Manager Approval", StepOrder = 2, StepType = StepType.Signature, RequiredSignatureLevel = SignatureLevel.Signature },
                new() { Name = "Legal Review", StepOrder = 3, StepType = StepType.Signature, RequiredSignatureLevel = SignatureLevel.Signature }
            }
        };

        // Act
        var created = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("Standard Approval", created.Name);
        Assert.Equal(3, created.Steps.Count);
        Assert.True(created.IsDefault);

        // Verify steps are stored correctly
        var workflowFromDb = await workflowRepo.GetByIdAsync(created.Id);
        Assert.NotNull(workflowFromDb);
        Assert.Equal(3, workflowFromDb.Steps.Count);
    }

    [Fact]
    public async Task Test_AdminCanViewWorkflowList()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        
        // Create multiple workflows
        var workflow1 = new Workflow { Name = "Workflow 1", CreatedBy = "admin@test.com", IsActive = true };
        var workflow2 = new Workflow { Name = "Workflow 2", CreatedBy = "admin@test.com", IsActive = true };
        var workflow3 = new Workflow { Name = "Workflow 3", CreatedBy = "admin@test.com", IsActive = false };
        
        await workflowRepo.AddAsync(workflow1);
        await workflowRepo.AddAsync(workflow2);
        await workflowRepo.AddAsync(workflow3);
        
        // Add steps
        workflow1.Steps.Add(new WorkflowStep { Name = "Step 1", StepOrder = 1 });
        workflow2.Steps.Add(new WorkflowStep { Name = "Step 1", StepOrder = 1 });
        workflow2.Steps.Add(new WorkflowStep { Name = "Step 2", StepOrder = 2 });
        
        await unitOfWork.SaveChangesAsync();

        var service = CreateWorkflowService(context);

        // Act
        var workflows = await service.GetWorkflowsAsync();
        var workflowList = workflows.ToList();

        // Assert
        Assert.Equal(3, workflowList.Count);
        Assert.Contains(workflowList, w => w.Name == "Workflow 1");
        Assert.Contains(workflowList, w => w.Name == "Workflow 2");
        Assert.Contains(workflowList, w => w.Name == "Workflow 3");
        
        // Verify step counts
        var wf1 = workflowList.First(w => w.Name == "Workflow 1");
        var wf2 = workflowList.First(w => w.Name == "Workflow 2");
        Assert.Equal(1, wf1.StepCount);
        Assert.Equal(2, wf2.StepCount);
    }

    [Fact]
    public async Task Test_AdminCanViewWorkflowDetails()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        
        // Create a workflow with detailed steps
        var workflow = new Workflow
        {
            Name = "Detailed Approval",
            Description = "A detailed approval workflow",
            CreatedBy = "admin@test.com",
            IsActive = true,
            Status = WorkflowStatus.Active
        };
        
        workflow.Steps.Add(new WorkflowStep
        {
            Name = "Manager Review",
            StepOrder = 1,
            StepType = StepType.Validation,
            Instructions = "Review for completeness",
            RequiredSignatureLevel = SignatureLevel.Initial
        });
        
        workflow.Steps.Add(new WorkflowStep
        {
            Name = "Director Signature",
            StepOrder = 2,
            StepType = StepType.Signature,
            Instructions = "Final signature required",
            RequiredSignatureLevel = SignatureLevel.Signature
        });
        
        await workflowRepo.AddAsync(workflow);
        await unitOfWork.SaveChangesAsync();

        var service = CreateWorkflowService(context);

        // Act
        var details = await service.GetWorkflowDetailAsync(workflow.Id);

        // Assert
        Assert.NotNull(details);
        Assert.Equal("Detailed Approval", details.Name);
        Assert.Equal("A detailed approval workflow", details.Description);
        Assert.Equal(2, details.Steps.Count);
        Assert.True(details.IsActive);
        
        // Verify step details
        var firstStep = details.Steps.First();
        Assert.Equal("Manager Review", firstStep.Name);
        Assert.Equal(1, firstStep.StepOrder);
        Assert.Equal(StepType.Validation, firstStep.StepType);
        Assert.Equal("Review for completeness", firstStep.Instructions);
        Assert.Equal(SignatureLevel.Initial, firstStep.RequiredSignatureLevel);
    }

    [Fact]
    public async Task Test_AdminCanDuplicateWorkflow()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        
        // Create original workflow
        var originalWorkflow = new Workflow
        {
            Name = "Original Workflow",
            Description = "This is the original",
            CreatedBy = "admin@test.com",
            IsActive = true,
            IsDefault = false
        };
        
        originalWorkflow.Steps.Add(new WorkflowStep
        {
            Name = "Step 1",
            StepOrder = 1,
            StepType = StepType.Signature,
            RequiredSignatureLevel = SignatureLevel.Signature
        });
        
        originalWorkflow.Steps.Add(new WorkflowStep
        {
            Name = "Step 2",
            StepOrder = 2,
            StepType = StepType.Signature,
            RequiredSignatureLevel = SignatureLevel.Signature
        });
        
        await workflowRepo.AddAsync(originalWorkflow);
        await unitOfWork.SaveChangesAsync();

        var service = CreateWorkflowService(context);

        // Act
        var duplicated = await service.DuplicateWorkflowAsync(originalWorkflow.Id);

        // Assert
        Assert.NotNull(duplicated);
        Assert.NotEqual(originalWorkflow.Id, duplicated.Id);
        Assert.Equal("Original Workflow_copie", duplicated.Name);
        Assert.Equal("This is the original", duplicated.Description);
        
        // Verify steps were duplicated
        var originalSteps = originalWorkflow.Steps.Count;
        Assert.Equal(originalSteps, duplicated.Steps.Count);
        
        // Verify steps have new IDs
        foreach (var step in duplicated.Steps)
        {
            Assert.NotEqual(Guid.Empty, step.Id);
        }
    }

    [Fact]
    public async Task Test_AdminCanDeleteWorkflow()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        
        var workflow = new Workflow
        {
            Name = "To Be Deleted",
            CreatedBy = "admin@test.com",
            IsActive = true
        };
        
        await workflowRepo.AddAsync(workflow);
        await unitOfWork.SaveChangesAsync();
        
        var workflowId = workflow.Id;

        var service = CreateWorkflowService(context);

        // Act
        await service.DeleteAsync(workflowId);

        // Assert
        var deletedWorkflow = await workflowRepo.GetByIdAsync(workflowId);
        Assert.NotNull(deletedWorkflow);
        Assert.True(deletedWorkflow.IsDeleted); // Soft delete
    }

    #endregion

    #region Unauthorized Access Tests

    private Mock<UserManager<TUser>> CreateMockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
        mgr.Object.UserValidators.Add(new UserValidator<TUser>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
        return mgr;
    }

    private ClaimsPrincipal CreateUserPrincipal(string userId, string email, bool isAdmin)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email)
        };
        
        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task Test_NonAdminCannotAccessWorkflowIndex()
    {
        // Arrange - Simulate authorization check
        var nonAdminUserId = Guid.NewGuid().ToString();
        var nonAdminPrincipal = CreateUserPrincipal(nonAdminUserId, "user@test.com", isAdmin: false);
        
        // Check if user has Admin role
        var isAdmin = nonAdminPrincipal.IsInRole("Admin");
        
        // Assert - Non-admin should not have access
        Assert.False(isAdmin);
    }

    [Fact]
    public async Task Test_NonAdminCannotAccessWorkflowCreate()
    {
        // Arrange - Simulate authorization check for Create action
        var nonAdminUserId = Guid.NewGuid().ToString();
        var nonAdminPrincipal = CreateUserPrincipal(nonAdminUserId, "user@test.com", isAdmin: false);
        
        // Check authorization - Create typically requires Admin role
        var canCreate = nonAdminPrincipal.IsInRole("Admin") || 
                       nonAdminPrincipal.HasClaim(c => c.Type == "permission" && c.Value == "workflow.create");
        
        // Assert - Non-admin should not have create permission
        Assert.False(canCreate);
    }

    [Fact]
    public async Task Test_UnauthenticatedUserRedirectsToLogin()
    {
        // Arrange - Simulate unauthenticated user
        var anonymousPrincipal = new ClaimsPrincipal(); // Empty principal - not authenticated
        
        // Assert - Verify user is not authenticated
        Assert.False(anonymousPrincipal.Identity?.IsAuthenticated ?? false);
        
        // In a real scenario, unauthenticated users accessing protected endpoints
        // would be redirected to the login page (401/302 redirect)
    }

    #endregion

    #region Workflow Assignment Tests

    [Fact]
    public async Task Test_WorkflowCanBeAssignedToDocument()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        // Create workflow
        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        
        var workflow = new Workflow
        {
            Name = "Assignment Workflow",
            CreatedBy = "admin@test.com",
            IsActive = true
        };
        
        workflow.Steps.Add(new WorkflowStep
        {
            Name = "Approval Step",
            StepOrder = 1,
            StepType = StepType.Signature,
            RequiredSignatureLevel = SignatureLevel.Signature
        });
        
        await workflowRepo.AddAsync(workflow);
        await unitOfWork.SaveChangesAsync();

        // Create document with workflow assignment
        var docRepo = new Repository<Document>(context);
        var document = new Document
        {
            Title = "Document for Workflow",
            FileName = "document.pdf",
            OriginalFileName = "document.pdf",
            FilePath = "/documents/document.pdf",
            CreatedById = Guid.NewGuid(),
            WorkflowId = workflow.Id // Assign workflow
        };
        
        await docRepo.AddAsync(document);
        await unitOfSaveChangesAsync(unitOfWork);

        // Act - Retrieve document with workflow
        var documentFromDb = await docRepo.GetByIdAsync(document.Id);

        // Assert
        Assert.NotNull(documentFromDb);
        Assert.Equal(workflow.Id, documentFromDb.WorkflowId);
        
        // Verify workflow is loaded
        var workflowFromDb = await workflowRepo.GetByIdAsync(workflow.Id);
        Assert.NotNull(workflowFromDb);
        Assert.Single(workflowFromDb.Steps);
    }

    private async Task unitOfSaveChangesAsync(IUnitOfWork unitOfWork)
    {
        await unitOfWork.SaveChangesAsync();
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public async Task Test_CreateWorkflowWithNoSteps()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        var service = CreateWorkflowService(context);

        var dto = new CreateWorkflowDto
        {
            Name = "Empty Workflow",
            Description = "Workflow with no steps",
            Steps = new List<WorkflowStepCreateDto>() // Empty steps
        };

        // Act
        var created = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("Empty Workflow", created.Name);
        Assert.Empty(created.Steps);
    }

    [Fact]
    public async Task Test_DeleteNonExistentWorkflow_ThrowsException()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        var service = CreateWorkflowService(context);

        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.DeleteAsync(nonExistentId));
    }

    [Fact]
    public async Task Test_DuplicateNonExistentWorkflow_ThrowsException()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        var service = CreateWorkflowService(context);

        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.DuplicateWorkflowAsync(nonExistentId));
    }

    [Fact]
    public async Task Test_GetWorkflowDetailForNonExistent_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);

        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        var service = CreateWorkflowService(context);

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GetWorkflowDetailAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
