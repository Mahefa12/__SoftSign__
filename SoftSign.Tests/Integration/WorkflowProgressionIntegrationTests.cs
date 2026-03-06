using Microsoft.EntityFrameworkCore;
using SoftSign.Application.DTOs;
using SoftSign.Application.Services;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Domain.Interfaces;
using SoftSign.Infrastructure.Data;
using SoftSign.Infrastructure.Repositories;
using Xunit;

namespace SoftSign.Tests.Integration;

/// <summary>
/// Integration tests for workflow progression after signing
/// </summary>
public class WorkflowProgressionIntegrationTests
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

	#region Workflow Progression Tests

	/// <summary>
	/// Test that workflow advances after document signing - step completion
	/// </summary>
	[Fact]
	public async Task WorkflowProgression_AdvancesAfterSigning_CompletesStep()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var workflowRepo = new Repository<Workflow>(context);
		var stepRepo = new Repository<WorkflowStep>(context);
		var docRepo = new Repository<Document>(context);
		var docStepRepo = new Repository<DocumentWorkflowStep>(context);

		// Create a workflow with multiple steps
		var workflow = new Workflow
		{
			Name = "Approval Workflow",
			Description = "Multi-step approval workflow",
			CreatedBy = "admin@test.com",
			IsActive = true,
			Status = WorkflowStatus.Active
		};

		var step1 = new WorkflowStep
		{
			Name = "Manager Approval",
			StepOrder = 1,
			StepType = StepType.Signature,
			RequiredSignatureLevel = SignatureLevel.Signature
		};

		var step2 = new WorkflowStep
		{
			Name = "Director Approval",
			StepOrder = 2,
			StepType = StepType.Signature,
			RequiredSignatureLevel = SignatureLevel.Signature
		};

		workflow.Steps.Add(step1);
		workflow.Steps.Add(step2);

		await workflowRepo.AddAsync(workflow);
		await unitOfWork.SaveChangesAsync();

		// Create a document with this workflow
		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Document for Workflow Test",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId,
			WorkflowId = workflow.Id,
			Status = DocumentStatus.InProgress
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Create document workflow steps
		var docSteps = new List<DocumentWorkflowStep>
		{
			new()
			{
				DocumentId = document.Id,
				WorkflowStepId = step1.Id,
				StepOrder = 1,
				Name = "Manager Approval",
				StepType = (int)StepType.Signature,
				IsCompleted = false
			},
			new()
			{
				DocumentId = document.Id,
				WorkflowStepId = step2.Id,
				StepOrder = 2,
				Name = "Director Approval",
				StepType = (int)StepType.Signature,
				IsCompleted = false
			}
		};

		foreach (var docStep in docSteps)
		{
			await docStepRepo.AddAsync(docStep);
		}
		await unitOfWork.SaveChangesAsync();

		// Act - Complete first step by signing
		var currentStep = await docStepRepo.FindAsync(ds => ds.DocumentId == document.Id && ds.StepOrder == 1);
		var currentStepEntity = currentStep.First();
		currentStepEntity.IsCompleted = true;
		currentStepEntity.CompletedAt = DateTime.UtcNow;
		currentStepEntity.Comments = "Approved";

		docStepRepo.Update(currentStepEntity);
		await unitOfWork.SaveChangesAsync();

		// Assert - First step should be completed
		var updatedStep = await docStepRepo.GetByIdAsync(currentStepEntity.Id);
		Assert.NotNull(updatedStep);
		Assert.True(updatedStep.IsCompleted);
		Assert.NotNull(updatedStep.CompletedAt);

		// Second step should still be pending
		var nextStep = await docStepRepo.FindAsync(ds => ds.DocumentId == document.Id && ds.StepOrder == 2);
		var nextStepEntity = nextStep.First();
		Assert.False(nextStepEntity.IsCompleted);
	}

	/// <summary>
	/// Test that workflow completes when all steps are done
	/// </summary>
	[Fact]
	public async Task WorkflowProgression_AllStepsComplete_CompletesWorkflow()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var workflowRepo = new Repository<Workflow>(context);
		var docRepo = new Repository<Document>(context);
		var docStepRepo = new Repository<DocumentWorkflowStep>(context);

		// Create workflow
		var workflow = new Workflow
		{
			Name = "Simple Approval",
			Description = "Simple approval workflow",
			CreatedBy = "admin@test.com",
			IsActive = true,
			Status = WorkflowStatus.Active
		};

		var step1 = new WorkflowStep
		{
			Name = "Approval",
			StepOrder = 1,
			StepType = StepType.Signature,
			RequiredSignatureLevel = SignatureLevel.Signature
		};

		workflow.Steps.Add(step1);
		await workflowRepo.AddAsync(workflow);
		await unitOfWork.SaveChangesAsync();

		// Create document
		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Document for Completion Test",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId,
			WorkflowId = workflow.Id,
			Status = DocumentStatus.InProgress
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Create document workflow step
		var docStep = new DocumentWorkflowStep
		{
			DocumentId = document.Id,
			WorkflowStepId = step1.Id,
			StepOrder = 1,
			Name = "Approval",
			StepType = (int)StepType.Signature,
			IsCompleted = false
		};
		await docStepRepo.AddAsync(docStep);
		await unitOfWork.SaveChangesAsync();

		// Act - Complete the step
		docStep.IsCompleted = true;
		docStep.CompletedAt = DateTime.UtcNow;
		docStepRepo.Update(docStep);
		await unitOfWork.SaveChangesAsync();

		// Check if all steps are completed
		var allDocSteps = await docStepRepo.FindAsync(ds => ds.DocumentId == document.Id);
		var allCompleted = allDocSteps.All(ds => ds.IsCompleted);

		// If all steps completed, update document and workflow status
		var docService = new DocumentService(docRepo, unitOfWork);
		if (allCompleted)
		{
			await docService.UpdateStatusAsync(document.Id, (int)DocumentStatus.Completed);
		}

		// Assert
		var updatedDoc = await docService.GetByIdAsync(document.Id);
		Assert.NotNull(updatedDoc);
		Assert.Equal(DocumentStatus.Completed, updatedDoc.Status);
		Assert.NotNull(updatedDoc.SignedAt);

		// Verify workflow is completed
		var updatedWorkflow = await workflowRepo.GetByIdAsync(workflow.Id);
		Assert.NotNull(updatedWorkflow);
		Assert.Equal(WorkflowStatus.Completed, updatedWorkflow.Status);
	}

	/// <summary>
	/// Test partial signing workflow - not all workflow steps complete
	/// </summary>
	[Fact]
	public async Task WorkflowProgression_PartialSigning_WorkflowNotComplete()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var workflowRepo = new Repository<Workflow>(context);
		var docRepo = new Repository<Document>(context);
		var docStepRepo = new Repository<DocumentWorkflowStep>(context);

		// Create workflow with 2 steps
		var workflow = new Workflow
		{
			Name = "Multi-Step Workflow",
			Description = "Two-step approval workflow",
			CreatedBy = "admin@test.com",
			IsActive = true,
			Status = WorkflowStatus.Active
		};

		var step1 = new WorkflowStep { Name = "Step 1", StepOrder = 1, StepType = StepType.Signature };
		var step2 = new WorkflowStep { Name = "Step 2", StepOrder = 2, StepType = StepType.Signature };

		workflow.Steps.Add(step1);
		workflow.Steps.Add(step2);

		await workflowRepo.AddAsync(workflow);
		await unitOfWork.SaveChangesAsync();

		// Create document
		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Document with Partial Workflow",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId,
			WorkflowId = workflow.Id,
			Status = DocumentStatus.InProgress
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Create document workflow steps
		var docStep1 = new DocumentWorkflowStep
		{
			DocumentId = document.Id,
			WorkflowStepId = step1.Id,
			StepOrder = 1,
			Name = "Step 1",
			StepType = (int)StepType.Signature,
			IsCompleted = false
		};
		var docStep2 = new DocumentWorkflowStep
		{
			DocumentId = document.Id,
			WorkflowStepId = step2.Id,
			StepOrder = 2,
			Name = "Step 2",
			StepType = (int)StepType.Signature,
			IsCompleted = false
		};

		await docStepRepo.AddAsync(docStep1);
		await docStepRepo.AddAsync(docStep2);
		await unitOfWork.SaveChangesAsync();

		// Act - Complete only first step
		docStep1.IsCompleted = true;
		docStep1.CompletedAt = DateTime.UtcNow;
		docStepRepo.Update(docStep1);
		await unitOfWork.SaveChangesAsync();

		// Check workflow status
		var allDocSteps = await docStepRepo.FindAsync(ds => ds.DocumentId == document.Id);
		var allCompleted = allDocSteps.All(ds => ds.IsCompleted);

		// Assert - Workflow should NOT be complete
		Assert.False(allCompleted);

		var completedSteps = allDocSteps.Count(ds => ds.IsCompleted);
		Assert.Equal(1, completedSteps);

		var pendingSteps = allDocSteps.Count(ds => !ds.IsCompleted);
		Assert.Equal(1, pendingSteps);

		// Document should still be InProgress
		var docService = new DocumentService(docRepo, unitOfWork);
		var currentDoc = await docService.GetByIdAsync(document.Id);
		Assert.NotNull(currentDoc);
		Assert.Equal(DocumentStatus.InProgress, currentDoc.Status);
	}

	/// <summary>
	/// Test that document with no workflow can still be signed
	/// </summary>
	[Fact]
	public async Task WorkflowProgression_NoWorkflow_CanStillSign()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);
		var docService = new DocumentService(docRepo, unitOfWork);

		// Create document without workflow
		var userId = Guid.NewGuid();
		var document = await docService.CreateAsync(new CreateDocumentDto
		{
			Title = "Document Without Workflow",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf"
		}, userId);

		// Verify document has no workflow
		Assert.Null(document.WorkflowId);

		// Add signature zone and sign it
		var zone = new SignatureZone
		{
			DocumentId = document.Id,
			PageNumber = 1,
			PositionX = 100,
			PositionY = 200,
			Width = 150,
			Height = 50,
			IsRequired = true,
			SignatureImage = "signature_data",
			SignedByUserId = userId,
			SignedAt = DateTime.UtcNow,
			Order = 1
		};
		await zoneRepo.AddAsync(zone);
		await unitOfWork.SaveChangesAsync();

		// Act - Complete document
		var allZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id && z.IsRequired);
		var allSigned = allZones.All(z => z.IsSigned);

		if (allSigned)
		{
			await docService.UpdateStatusAsync(document.Id, (int)DocumentStatus.Completed);
		}

		// Assert
		var completedDoc = await docService.GetByIdAsync(document.Id);
		Assert.NotNull(completedDoc);
		Assert.Equal(DocumentStatus.Completed, completedDoc.Status);
	}

	/// <summary>
	/// Test validation step progression - validation doesn't require signature
	/// </summary>
	[Fact]
	public async Task WorkflowProgression_ValidationStep_AdvancesWithoutSignature()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var workflowRepo = new Repository<Workflow>(context);
		var docRepo = new Repository<Document>(context);
		var docStepRepo = new Repository<DocumentWorkflowStep>(context);

		// Create workflow with validation step followed by signature step
		var workflow = new Workflow
		{
			Name = "Validation then Signature",
			Description = "Validation step before signature",
			CreatedBy = "admin@test.com",
			IsActive = true,
			Status = WorkflowStatus.Active
		};

		var validationStep = new WorkflowStep
		{
			Name = "Document Review",
			StepOrder = 1,
			StepType = (int)StepType.Validation,
			RequiredSignatureLevel = SignatureLevel.Initial
		};

		var signatureStep = new WorkflowStep
		{
			Name = "Final Signature",
			StepOrder = 2,
			StepType = StepType.Signature,
			RequiredSignatureLevel = SignatureLevel.Signature
		};

		workflow.Steps.Add(validationStep);
		workflow.Steps.Add(signatureStep);

		await workflowRepo.AddAsync(workflow);
		await unitOfWork.SaveChangesAsync();

		// Create document
		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Document for Validation Test",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId,
			WorkflowId = workflow.Id,
			Status = DocumentStatus.InProgress
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Create document workflow steps
		var validationDocStep = new DocumentWorkflowStep
		{
			DocumentId = document.Id,
			WorkflowStepId = validationStep.Id,
			StepOrder = 1,
			Name = "Document Review",
			StepType = (int)StepType.Validation,
			IsCompleted = false
		};

		var signatureDocStep = new DocumentWorkflowStep
		{
			DocumentId = document.Id,
			WorkflowStepId = signatureStep.Id,
			StepOrder = 2,
			Name = "Final Signature",
			StepType = (int)StepType.Signature,
			IsCompleted = false
		};

		await docStepRepo.AddAsync(validationDocStep);
		await docStepRepo.AddAsync(signatureDocStep);
		await unitOfWork.SaveChangesAsync();

		// Act - Complete validation step (no signature needed)
		validationDocStep.IsCompleted = true;
		validationDocStep.CompletedAt = DateTime.UtcNow;
		validationDocStep.Comments = "Document reviewed and approved";

		docStepRepo.Update(validationDocStep);
		await unitOfWork.SaveChangesAsync();

		// Assert - Validation step is complete, signature step is pending
		var updatedValidationStep = await docStepRepo.GetByIdAsync(validationDocStep.Id);
		Assert.NotNull(updatedValidationStep);
		Assert.True(updatedValidationStep.IsCompleted);
		Assert.Equal(StepType.Validation, (StepType)updatedValidationStep.StepType);

		var updatedSignatureStep = await docStepRepo.GetByIdAsync(signatureDocStep.Id);
		Assert.NotNull(updatedSignatureStep);
		Assert.False(updatedSignatureStep.IsCompleted);
	}

	/// <summary>
	/// Test workflow step assignment to specific users
	/// </summary>
	[Fact]
	public async Task WorkflowProgression_UserAssigned_CanCompleteStep()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var workflowRepo = new Repository<Workflow>(context);
		var docRepo = new Repository<Document>(context);
		var docStepRepo = new Repository<DocumentWorkflowStep>(context);

		var assignedUserId = Guid.NewGuid();
		var otherUserId = Guid.NewGuid();

		// Create workflow with assigned user step
		var workflow = new Workflow
		{
			Name = "Assigned User Workflow",
			Description = "Step assigned to specific user",
			CreatedBy = "admin@test.com",
			IsActive = true,
			Status = WorkflowStatus.Active
		};

		var step1 = new WorkflowStep
		{
			Name = "Assigned Approval",
			StepOrder = 1,
			StepType = StepType.Signature,
			RequiredSignatureLevel = SignatureLevel.Signature,
			AssignedUserId = assignedUserId
		};

		workflow.Steps.Add(step1);
		await workflowRepo.AddAsync(workflow);
		await unitOfWork.SaveChangesAsync();

		// Create document
		var creatorId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Document for Assignment Test",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = creatorId,
			WorkflowId = workflow.Id,
			Status = DocumentStatus.InProgress
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Create document workflow step with assigned user
		var docStep = new DocumentWorkflowStep
		{
			DocumentId = document.Id,
			WorkflowStepId = step1.Id,
			StepOrder = 1,
			Name = "Assigned Approval",
			StepType = (int)StepType.Signature,
			CustomAssignedUserId = assignedUserId,
			IsCompleted = false
		};
		await docStepRepo.AddAsync(docStep);
		await unitOfWork.SaveChangesAsync();

		// Act - Only assigned user completes the step
		var canCompleteStep = docStep.CustomAssignedUserId == assignedUserId;

		if (canCompleteStep)
		{
			docStep.IsCompleted = true;
			docStep.CompletedAt = DateTime.UtcNow;
			docStep.Comments = "Approved by assigned user";

			docStepRepo.Update(docStep);
			await unitOfWork.SaveChangesAsync();
		}

		// Assert
		var updatedStep = await docStepRepo.GetByIdAsync(docStep.Id);
		Assert.NotNull(updatedStep);
		Assert.Equal(assignedUserId, updatedStep.CustomAssignedUserId);
		Assert.True(updatedStep.IsCompleted);
		Assert.NotNull(updatedStep.CompletedAt);
	}

	/// <summary>
	/// Test workflow with mixed step types
	/// </summary>
	[Fact]
	public async Task WorkflowProgression_MixedStepTypes_ProgressionWorks()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var workflowRepo = new Repository<Workflow>(context);
		var docRepo = new Repository<Document>(context);
		var docStepRepo = new Repository<DocumentWorkflowStep>(context);
		var zoneRepo = new Repository<SignatureZone>(context);

		// Create workflow with mixed steps
		var workflow = new Workflow
		{
			Name = "Mixed Workflow",
			Description = "Validation, signature, and final review",
			CreatedBy = "admin@test.com",
			IsActive = true,
			Status = WorkflowStatus.Active
		};

		var step1 = new WorkflowStep { Name = "Review", StepOrder = 1, StepType = StepType.Validation };
		var step2 = new WorkflowStep { Name = "Sign", StepOrder = 2, StepType = StepType.Signature };
		var step3 = new WorkflowStep { Name = "Final Review", StepOrder = 3, StepType = StepType.Validation };

		workflow.Steps.Add(step1);
		workflow.Steps.Add(step2);
		workflow.Steps.Add(step3);

		await workflowRepo.AddAsync(workflow);
		await unitOfWork.SaveChangesAsync();

		// Create document with signature zone
		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Document for Mixed Steps",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId,
			WorkflowId = workflow.Id,
			Status = DocumentStatus.InProgress
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Add signature zone for step 2
		var zone = new SignatureZone
		{
			DocumentId = document.Id,
			PageNumber = 1,
			PositionX = 100,
			PositionY = 200,
			Width = 150,
			Height = 50,
			IsRequired = true,
			Order = 1
		};
		await zoneRepo.AddAsync(zone);
		await unitOfWork.SaveChangesAsync();

		// Create document workflow steps
		var docSteps = new List<DocumentWorkflowStep>
		{
			new() { DocumentId = document.Id, WorkflowStepId = step1.Id, StepOrder = 1, Name = "Review", StepType = (int)StepType.Validation, IsCompleted = false },
			new() { DocumentId = document.Id, WorkflowStepId = step2.Id, StepOrder = 2, Name = "Sign", StepType = (int)StepType.Signature, IsCompleted = false },
			new() { DocumentId = document.Id, WorkflowStepId = step3.Id, StepOrder = 3, Name = "Final Review", StepType = (int)StepType.Validation, IsCompleted = false }
		};

		foreach (var step in docSteps)
		{
			await docStepRepo.AddAsync(step);
		}
		await unitOfWork.SaveChangesAsync();

		// Act - Complete step 1 (validation)
		docSteps[0].IsCompleted = true;
		docSteps[0].CompletedAt = DateTime.UtcNow;
		docStepRepo.Update(docSteps[0]);
		await unitOfWork.SaveChangesAsync();

		// Complete step 2 (signature) - sign the zone first
		zone.SignatureImage = "signature_data";
		zone.SignedByUserId = userId;
		zone.SignedAt = DateTime.UtcNow;
		zoneRepo.Update(zone);
		await unitOfWork.SaveChangesAsync();

		docSteps[1].IsCompleted = true;
		docSteps[1].CompletedAt = DateTime.UtcNow;
		docSteps[1].Comments = "Document signed";
		docStepRepo.Update(docSteps[1]);
		await unitOfWork.SaveChangesAsync();

		// Assert - Check progression
		var allSteps = await docStepRepo.FindAsync(ds => ds.DocumentId == document.Id);
		var stepList = allSteps.OrderBy(ds => ds.StepOrder).ToList();

		Assert.True(stepList[0].IsCompleted); // Review done
		Assert.True(stepList[1].IsCompleted); // Sign done
		Assert.False(stepList[2].IsCompleted); // Final review pending

		// Verify zone is signed
		var signedZone = await zoneRepo.GetByIdAsync(zone.Id);
		Assert.NotNull(signedZone);
		Assert.True(signedZone.IsSigned);
	}

	#endregion
}
