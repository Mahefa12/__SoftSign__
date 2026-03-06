using FluentAssertions;
using Moq;
using SoftSign.Application.DTOs;
using SoftSign.Application.Services;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Domain.Interfaces;
using Xunit;

namespace SoftSign.Tests.Services;

public class WorkflowServiceTests
{
    private readonly Mock<IRepository<Workflow>> _mockWorkflowRepository;
    private readonly Mock<IRepository<WorkflowStep>> _mockStepRepository;
    private readonly Mock<IRepository<DocumentWorkflowStep>> _mockDocumentStepRepository;
    private readonly Mock<IRepository<Document>> _mockDocumentRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly WorkflowService _workflowService;

    public WorkflowServiceTests()
    {
        _mockWorkflowRepository = new Mock<IRepository<Workflow>>();
        _mockStepRepository = new Mock<IRepository<WorkflowStep>>();
        _mockDocumentStepRepository = new Mock<IRepository<DocumentWorkflowStep>>();
        _mockDocumentRepository = new Mock<IRepository<Document>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        
        _workflowService = new WorkflowService(
            _mockWorkflowRepository.Object,
            _mockStepRepository.Object,
            _mockDocumentStepRepository.Object,
            _mockDocumentRepository.Object,
            _mockUnitOfWork.Object);
    }

    #region ValidateWorkflowAsync Tests

    [Fact]
    public async Task Test_ValidateWorkflowAsync_ValidWorkflow_ReturnsTrue()
    {
        // Arrange
        var dto = new CreateWorkflowDto
        {
            Name = "Valid Workflow",
            Steps = new List<WorkflowStepCreateDto>
            {
                new WorkflowStepCreateDto
                {
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = StepType.Signature,
                    AssignedRoleId = Guid.NewGuid()
                }
            }
        };

        // Act
        var (isValid, errors) = await _workflowService.ValidateWorkflowAsync(dto);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_ValidateWorkflowAsync_EmptyName_ReturnsFalse()
    {
        // Arrange
        var dto = new CreateWorkflowDto
        {
            Name = "",
            Steps = new List<WorkflowStepCreateDto>
            {
                new WorkflowStepCreateDto
                {
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = StepType.Signature,
                    AssignedRoleId = Guid.NewGuid()
                }
            }
        };

        // Act
        var (isValid, errors) = await _workflowService.ValidateWorkflowAsync(dto);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Workflow name is required");
    }

    [Fact]
    public async Task Test_ValidateWorkflowAsync_NoSteps_ReturnsFalse()
    {
        // Arrange
        var dto = new CreateWorkflowDto
        {
            Name = "Workflow without steps",
            Steps = new List<WorkflowStepCreateDto>()
        };

        // Act
        var (isValid, errors) = await _workflowService.ValidateWorkflowAsync(dto);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("At least one step is required");
    }

    [Fact]
    public async Task Test_ValidateWorkflowAsync_StepWithoutType_ReturnsFalse()
    {
        // Arrange
        var dto = new CreateWorkflowDto
        {
            Name = "Valid Workflow",
            Steps = new List<WorkflowStepCreateDto>
            {
                new WorkflowStepCreateDto
                {
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = default, // Validation = 0 (default)
                    AssignedRoleId = Guid.NewGuid()
                }
            }
        };

        // Act
        var (isValid, errors) = await _workflowService.ValidateWorkflowAsync(dto);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainMatch("*StepType must be selected*");
    }

    [Fact]
    public async Task Test_ValidateWorkflowAsync_StepWithoutRoleOrUser_ReturnsFalse()
    {
        // Arrange
        var dto = new CreateWorkflowDto
        {
            Name = "Valid Workflow",
            Steps = new List<WorkflowStepCreateDto>
            {
                new WorkflowStepCreateDto
                {
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = StepType.Signature,
                    AssignedRoleId = null,
                    AssignedUserId = null
                }
            }
        };

        // Act
        var (isValid, errors) = await _workflowService.ValidateWorkflowAsync(dto);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainMatch("*Either Role or User must be assigned*");
    }

    [Fact]
    public async Task Test_ValidateWorkflowAsync_DuplicateRolesInSequence_ReturnsFalse()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var dto = new CreateWorkflowDto
        {
            Name = "Valid Workflow",
            Steps = new List<WorkflowStepCreateDto>
            {
                new WorkflowStepCreateDto
                {
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = StepType.Signature,
                    AssignedRoleId = roleId
                },
                new WorkflowStepCreateDto
                {
                    Name = "Step 2",
                    StepOrder = 2,
                    StepType = StepType.Signature,
                    AssignedRoleId = roleId // Same role as Step 1
                }
            }
        };

        // Act
        var (isValid, errors) = await _workflowService.ValidateWorkflowAsync(dto);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainMatch("*Consecutive steps cannot have the same role assigned*");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task Test_CreateWorkflow_WithSteps_CreatesSuccessfully()
    {
        // Arrange
        var dto = new CreateWorkflowDto
        {
            Name = "Test Workflow",
            Description = "Test Description",
            CompanyId = Guid.NewGuid(),
            IsDefault = false,
            Steps = new List<WorkflowStepCreateDto>
            {
                new WorkflowStepCreateDto
                {
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = StepType.Signature,
                    AssignedRoleId = Guid.NewGuid()
                },
                new WorkflowStepCreateDto
                {
                    Name = "Step 2",
                    StepOrder = 2,
                    StepType = StepType.Validation,
                    AssignedUserId = Guid.NewGuid()
                }
            }
        };

        Workflow? capturedWorkflow = null;
        var addCalled = false;
        var saveCalled = false;
        
        _mockWorkflowRepository
            .Setup(x => x.AddAsync(It.IsAny<Workflow>()))
            .Callback<Workflow>(w => 
            {
                addCalled = true;
                capturedWorkflow = w;
            })
            .Returns((Workflow w) => Task.FromResult(w));
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => saveCalled = true)
            .Returns(Task.FromResult(1));

        // Act
        var result = await _workflowService.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        result.Description.Should().Be(dto.Description);
        result.Steps.Should().HaveCount(2);
        
        addCalled.Should().BeTrue();
        saveCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Test_CreateWorkflow_SetsCreatedAt()
    {
        // Arrange
        var dto = new CreateWorkflowDto
        {
            Name = "Test Workflow",
            Steps = new List<WorkflowStepCreateDto>
            {
                new WorkflowStepCreateDto
                {
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = StepType.Signature,
                    AssignedRoleId = Guid.NewGuid()
                }
            }
        };

        _mockWorkflowRepository
            .Setup(x => x.AddAsync(It.IsAny<Workflow>()))
            .Returns((Workflow w) => Task.FromResult(w));
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var beforeCreate = DateTime.UtcNow;
        var result = await _workflowService.CreateAsync(dto);
        var afterCreate = DateTime.UtcNow;

        // Assert
        result.CreatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
        result.CreatedAt.Should().BeBefore(afterCreate.AddSeconds(1));
    }

    #endregion

    #region GetWorkflowsAsync Tests

    [Fact]
    public async Task Test_GetWorkflows_ReturnsList()
    {
        // Arrange
        var workflows = new List<Workflow>
        {
            new Workflow
            {
                Id = Guid.NewGuid(),
                Name = "Workflow 1",
                CreatedAt = DateTime.UtcNow,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep { Name = "Step 1", StepOrder = 1 }
                }
            },
            new Workflow
            {
                Id = Guid.NewGuid(),
                Name = "Workflow 2",
                CreatedAt = DateTime.UtcNow,
                Steps = new List<WorkflowStep>()
            }
        };

        _mockWorkflowRepository.Setup(x => x.GetAllAsync()).Returns(Task.FromResult(workflows.AsEnumerable()));

        // Act
        var result = await _workflowService.GetWorkflowsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetWorkflowDetailAsync Tests

    [Fact]
    public async Task Test_GetWorkflowDetail_WithSteps_ReturnsDetail()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new Workflow
        {
            Id = workflowId,
            Name = "Test Workflow",
            Description = "Test Description",
            Status = WorkflowStatus.Active,
            CompanyId = Guid.NewGuid(),
            IsDefault = false,
            CreatedBy = "testuser",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Id = Guid.NewGuid(),
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = StepType.Signature,
                    AssignedRoleId = Guid.NewGuid(),
                    WorkflowId = workflowId
                },
                new WorkflowStep
                {
                    Id = Guid.NewGuid(),
                    Name = "Step 2",
                    StepOrder = 2,
                    StepType = StepType.Validation,
                    AssignedUserId = Guid.NewGuid(),
                    WorkflowId = workflowId
                }
            }
        };

        _mockWorkflowRepository.Setup(x => x.GetByIdAsync(workflowId)).Returns(Task.FromResult<Workflow?>(workflow));

        // Act
        var result = await _workflowService.GetWorkflowDetailAsync(workflowId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(workflowId);
        result.Name.Should().Be("Test Workflow");
        result.Steps.Should().HaveCount(2);
        result.Steps.Should().BeInAscendingOrder(s => s.StepOrder);
    }

    #endregion

    #region DuplicateWorkflowAsync Tests

    [Fact]
    public async Task Test_DuplicateWorkflow_CreatesCopy()
    {
        // Arrange
        var originalWorkflowId = Guid.NewGuid();
        var originalWorkflow = new Workflow
        {
            Id = originalWorkflowId,
            Name = "Original Workflow",
            Description = "Original Description",
            CompanyId = Guid.NewGuid(),
            IsDefault = true,
            CreatedBy = "testuser",
            Status = WorkflowStatus.Active,
            IsActive = true,
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = StepType.Signature,
                    AssignedRoleId = Guid.NewGuid()
                }
            }
        };

        _mockWorkflowRepository.Setup(x => x.GetByIdAsync(originalWorkflowId)).Returns(Task.FromResult<Workflow?>(originalWorkflow));

        Workflow? capturedWorkflow = null;
        _mockWorkflowRepository
            .Setup(x => x.AddAsync(It.IsAny<Workflow>()))
            .Callback<Workflow>(w => capturedWorkflow = w)
            .Returns((Workflow w) => Task.FromResult(w));
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _workflowService.DuplicateWorkflowAsync(originalWorkflowId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Contain("_copie");
        capturedWorkflow.Should().NotBeNull();
        capturedWorkflow!.Name.Should().Be("Original Workflow_copie");
    }

    [Fact]
    public async Task Test_DuplicateWorkflow_ClonesAllSteps()
    {
        // Arrange
        var originalWorkflowId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var originalWorkflow = new Workflow
        {
            Id = originalWorkflowId,
            Name = "Original Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "Step 1",
                    StepOrder = 1,
                    StepType = StepType.Signature,
                    AssignedRoleId = roleId,
                    Instructions = "Sign here"
                },
                new WorkflowStep
                {
                    Name = "Step 2",
                    StepOrder = 2,
                    StepType = StepType.Validation,
                    AssignedUserId = userId,
                    Instructions = "Validate"
                },
                new WorkflowStep
                {
                    Name = "Step 3",
                    StepOrder = 3,
                    StepType = StepType.Paraphe,
                    AssignedRoleId = roleId
                }
            }
        };

        _mockWorkflowRepository.Setup(x => x.GetByIdAsync(originalWorkflowId)).Returns(Task.FromResult<Workflow?>(originalWorkflow));

        Workflow? capturedWorkflow = null;
        _mockWorkflowRepository
            .Setup(x => x.AddAsync(It.IsAny<Workflow>()))
            .Callback<Workflow>(w => capturedWorkflow = w)
            .Returns((Workflow w) => Task.FromResult(w));
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _workflowService.DuplicateWorkflowAsync(originalWorkflowId);

        // Assert
        result.Steps.Should().HaveCount(3);
        
        capturedWorkflow.Should().NotBeNull();
        var duplicatedSteps = capturedWorkflow!.Steps.ToList();
        
        duplicatedSteps[0].Name.Should().Be("Step 1");
        duplicatedSteps[0].StepType.Should().Be(StepType.Signature);
        duplicatedSteps[0].AssignedRoleId.Should().Be(roleId);
        
        duplicatedSteps[1].Name.Should().Be("Step 2");
        duplicatedSteps[1].StepType.Should().Be(StepType.Validation);
        duplicatedSteps[1].AssignedUserId.Should().Be(userId);
        
        duplicatedSteps[2].Name.Should().Be("Step 3");
        duplicatedSteps[2].StepType.Should().Be(StepType.Paraphe);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task Test_DeleteWorkflow_SoftDeletes()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new Workflow
        {
            Id = workflowId,
            Name = "Workflow to delete",
            IsDeleted = false
        };

        _mockWorkflowRepository.Setup(x => x.GetByIdAsync(workflowId)).Returns(Task.FromResult<Workflow?>(workflow));

        Workflow? capturedWorkflow = null;
        var updateCalled = false;
        
        _mockWorkflowRepository
            .Setup(x => x.Update(It.IsAny<Workflow>()))
            .Callback<Workflow>(w => 
            {
                updateCalled = true;
                capturedWorkflow = w;
            });
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        await _workflowService.DeleteAsync(workflowId);

        // Assert
        capturedWorkflow.Should().NotBeNull();
        capturedWorkflow!.IsDeleted.Should().BeTrue();
        updateCalled.Should().BeTrue();
    }

    #endregion
}
