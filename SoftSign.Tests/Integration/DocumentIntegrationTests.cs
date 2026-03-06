using Microsoft.EntityFrameworkCore;
using SoftSign.Application.Services;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Domain.Interfaces;
using SoftSign.Infrastructure.Data;
using SoftSign.Infrastructure.Repositories;
using Xunit;

namespace SoftSign.Tests.Integration;

public class DocumentIntegrationTests
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

    [Fact]
    public async Task Document_CreateAndRetrieve_WorksCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);
        
        var repository = new Repository<Document>(context);
        var service = new DocumentService(repository, unitOfWork);

        var userId = Guid.NewGuid();
        var dto = new Application.DTOs.CreateDocumentDto
        {
            Title = "Test Document",
            Description = "Test Description",
            CompanyId = Guid.NewGuid()
        };

        // Act
        var created = await service.CreateAsync(dto, userId);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("Test Document", created.Title);

        // Retrieve
        var retrieved = await service.GetByIdAsync(created.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public async Task Workflow_CreateWithSteps_WorksCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);
        
        var workflowRepo = new Repository<Workflow>(context);
        var stepRepo = new Repository<WorkflowStep>(context);
        var service = CreateWorkflowService(context);

        var dto = new Application.DTOs.CreateWorkflowDto
        {
            Name = "Approval Workflow",
            Description = "Multi-step approval",
            Steps = new List<Application.DTOs.WorkflowStepCreateDto>
            {
                new() { Name = "Manager Approval", StepOrder = 1, RequiredSignatureLevel = SignatureLevel.Signature },
                new() { Name = "Director Approval", StepOrder = 2, RequiredSignatureLevel = SignatureLevel.Signature }
            }
        };

        // Act
        var created = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(2, created.Steps.Count);
    }

    [Fact]
    public async Task Document_UpdateStatus_SetsSignedAtOnCompletion()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);
        
        var repository = new Repository<Document>(context);
        var service = new DocumentService(repository, unitOfWork);

        var userId = Guid.NewGuid();
        var doc = await service.CreateAsync(new Application.DTOs.CreateDocumentDto
        {
            Title = "Test"
        }, userId);

        // Act
        var updated = await service.UpdateStatusAsync(doc.Id, (int)DocumentStatus.Completed);

        // Assert
        Assert.Equal(DocumentStatus.Completed, updated.Status);
        Assert.NotNull(updated.SignedAt);
    }

    [Fact]
    public async Task Signature_ApplySignature_TracksIpAndUserAgent()
    {
        // Arrange
        using var context = CreateDbContext();
        var unitOfWork = CreateUnitOfWork(context);
        
        var docRepo = new Repository<Document>(context);
        var sigRepo = new Repository<DocumentSignature>(context);
        var service = new SignatureService(sigRepo, docRepo, unitOfWork);

        // Create document first
        var document = new Document
        {
            Title = "Test",
            FileName = "test.pdf",
            OriginalFileName = "test.pdf",
            FilePath = "/test.pdf",
            CreatedById = Guid.NewGuid()
        };
        await docRepo.AddAsync(document);
        await unitOfWork.SaveChangesAsync();

        // Create signature
        var signature = await service.CreateAsync(new Application.DTOs.CreateSignatureDto
        {
            DocumentId = document.Id,
            SignerId = Guid.NewGuid(),
            Level = SignatureLevel.Signature
        });

        // Apply signature
        var applied = await service.ApplySignatureAsync(
            new Application.DTOs.ApplySignatureDto
            {
                SignatureId = signature.Id,
                SignatureData = "base64data",
                Comments = "Approved"
            },
            "192.168.1.100",
            "Mozilla/5.0"
        );

        // Assert
        Assert.True(applied.IsSigned);
        Assert.NotNull(applied.SignedAt);
    }
}
