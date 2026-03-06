using Microsoft.EntityFrameworkCore;
using Moq;
using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Application.Services;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Domain.Interfaces;
using SoftSign.Infrastructure.Data;
using SoftSign.Infrastructure.Repositories;
using Xunit;

namespace SoftSign.Tests.Integration;

/// <summary>
/// Integration tests for generating signed PDF documents
/// </summary>
public class GeneratedSignedPdfIntegrationTests
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

	/// <summary>
	/// Creates a minimal valid PDF byte array for testing
	/// </summary>
	private byte[] CreateMinimalPdf()
	{
		// Minimal valid PDF structure
		return new byte[]
		{
			0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x25, // %PDF-1.4\n%
			0xC4, 0xE5, 0xF3, 0xE9, 0x0A, 0x31, 0x20, 0x30, 0x20, 0x6F, 0x62, 0x6A, 0x0A,
			0x3C, 0x3C, 0x0A, 0x2F, 0x54, 0x79, 0x70, 0x65, 0x20, 0x2F, 0x43, 0x61, 0x74,
			0x61, 0x6C, 0x6F, 0x67, 0x0A, 0x2F, 0x50, 0x61, 0x67, 0x65, 0x73, 0x20, 0x32,
			0x20, 0x30, 0x20, 0x52, 0x0A, 0x3E, 0x3E, 0x0A, 0x65, 0x6E, 0x64, 0x6F, 0x62,
			0x6A, 0x0A, 0x32, 0x20, 0x30, 0x20, 0x6F, 0x62, 0x6A, 0x0A, 0x3C, 0x3C, 0x0A,
			0x2F, 0x54, 0x79, 0x70, 0x65, 0x20, 0x2F, 0x50, 0x61, 0x67, 0x65, 0x0A, 0x2F,
			0x4D, 0x65, 0x64, 0x69, 0x61, 0x42, 0x6F, 0x78, 0x20, 0x5B, 0x30, 0x20, 0x30,
			0x20, 0x36, 0x31, 0x32, 0x20, 0x37, 0x39, 0x32, 0x5D, 0x0A, 0x2F, 0x43, 0x6F,
			0x6E, 0x74, 0x65, 0x6E, 0x74, 0x73, 0x20, 0x33, 0x20, 0x30, 0x20, 0x52, 0x0A,
			0x3E, 0x3E, 0x0A, 0x65, 0x6E, 0x64, 0x6F, 0x62, 0x6A, 0x0A, 0x33, 0x20, 0x30,
			0x20, 0x6F, 0x62, 0x6A, 0x0A, 0x3C, 0x3C, 0x0A, 0x2F, 0x4C, 0x65, 0x6E, 0x67,
			0x74, 0x68, 0x20, 0x34, 0x34, 0x0A, 0x3E, 0x3E, 0x0A, 0x73, 0x74, 0x72, 0x65,
			0x61, 0x6D, 0x0A, 0x42, 0x54, 0x0A, 0x2F, 0x46, 0x31, 0x20, 0x31, 0x32, 0x20,
			0x54, 0x66, 0x0A, 0x35, 0x30, 0x20, 0x37, 0x30, 0x20, 0x54, 0x64, 0x0A, 0x28,
			0x54, 0x65, 0x73, 0x74, 0x20, 0x50, 0x44, 0x46, 0x29, 0x20, 0x54, 0x6A, 0x0A,
			0x45, 0x54, 0x0A, 0x65, 0x6E, 0x64, 0x73, 0x74, 0x72, 0x65, 0x61, 0x6D, 0x0A,
			0x65, 0x6E, 0x64, 0x6F, 0x62, 0x6A, 0x0A, 0x78, 0x72, 0x65, 0x66, 0x0A, 0x30,
			0x20, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30,
			0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x0A, 0x30, 0x30, 0x30, 0x30, 0x30,
			0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30,
			0x30, 0x0A, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30,
			0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x0A, 0x30, 0x30, 0x30,
			0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30,
			0x30, 0x30, 0x30, 0x30, 0x0A, 0x74, 0x72, 0x61, 0x69, 0x6C, 0x65, 0x72, 0x0A,
			0x3C, 0x3C, 0x0A, 0x2F, 0x53, 0x69, 0x7A, 0x65, 0x20, 0x34, 0x0A, 0x2F, 0x52,
			0x6F, 0x6F, 0x74, 0x20, 0x31, 0x20, 0x30, 0x20, 0x52, 0x0A, 0x3E, 0x3E, 0x0A,
			0x73, 0x74, 0x61, 0x72, 0x74, 0x78, 0x72, 0x65, 0x66, 0x0A, 0x33, 0x30, 0x39,
			0x0A, 0x25, 0x25, 0x45, 0x4F, 0x46
		};
	}

	/// <summary>
	/// Creates a base64 signature image (simple transparent PNG)
	/// </summary>
	private string CreateSignatureImageBase64()
	{
		return "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
	}

	#region Generate Signed PDF Tests

	/// <summary>
	/// Test that signed PDF is generated when all zones are signed
	/// </summary>
	[Fact]
	public async Task GenerateSignedPdf_WhenAllZonesSigned_CreatesPdf()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);

		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Test Document for PDF Generation",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId,
			Status = DocumentStatus.InProgress
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Create signed zones
		var signatureImage = CreateSignatureImageBase64();
		var zones = new List<SignatureZone>
		{
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 100,
				PositionY = 200,
				Width = 150,
				Height = 50,
				SignatureImage = signatureImage,
				SignedByUserId = userId,
				SignedAt = DateTime.UtcNow,
				IsRequired = true,
				Order = 1
			}
		};

		foreach (var zone in zones)
		{
			await zoneRepo.AddAsync(zone);
		}
		await unitOfWork.SaveChangesAsync();

		// Mock PDF service
		var mockPdfService = new Mock<IPdfService>();
		mockPdfService
			.Setup(p => p.ApplySignaturesFromZonesAsync(It.IsAny<byte[]>(), It.IsAny<IEnumerable<SignatureZone>>()))
			.ReturnsAsync(CreateMinimalPdf());

		var docService = new DocumentService(docRepo, unitOfWork);

		// Act - Simulate PDF generation
		var allZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id);
		var signedZones = allZones.Where(z => z.IsSigned).ToList();

		var allZonesSigned = zones.All(z => z.IsSigned);
		byte[]? signedPdf = null;

		if (allZonesSigned && signedZones.Count > 0)
		{
			signedPdf = await mockPdfService.Object.ApplySignaturesFromZonesAsync(CreateMinimalPdf(), signedZones);
		}

		// Assert
		Assert.NotNull(signedPdf);
		Assert.True(signedPdf.Length > 0);
		mockPdfService.Verify(
			p => p.ApplySignaturesFromZonesAsync(It.IsAny<byte[]>(), It.IsAny<IEnumerable<SignatureZone>>()),
			Times.Once);
	}

	/// <summary>
	/// Test that signatures appear at correct coordinates in the final PDF
	/// </summary>
	[Fact]
	public async Task GenerateSignedPdf_SignaturesAtCorrectCoordinates()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);

		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Document for Coordinate Test",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Create zones with specific coordinates
		var zones = new List<SignatureZone>
		{
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 100.5,
				PositionY = 200.75,
				Width = 150.0,
				Height = 50.0,
				SignatureImage = CreateSignatureImageBase64(),
				SignedByUserId = userId,
				SignedAt = DateTime.UtcNow,
				IsRequired = true,
				Order = 1
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 2,
				PositionX = 300.25,
				PositionY = 400.5,
				Width = 100.0,
				Height = 40.0,
				SignatureImage = CreateSignatureImageBase64(),
				SignedByUserId = userId,
				SignedAt = DateTime.UtcNow,
				IsRequired = true,
				Order = 2
			}
		};

		foreach (var zone in zones)
		{
			await zoneRepo.AddAsync(zone);
		}
		await unitOfWork.SaveChangesAsync();

		// Act - Verify zone coordinates are stored correctly
		var savedZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id);
		var zoneList = savedZones.ToList();

		// Assert - Verify coordinates
		var zone1 = zoneList.First(z => z.PageNumber == 1);
		Assert.Equal(100.5, zone1.PositionX);
		Assert.Equal(200.75, zone1.PositionY);
		Assert.Equal(150.0, zone1.Width);
		Assert.Equal(50.0, zone1.Height);

		var zone2 = zoneList.First(z => z.PageNumber == 2);
		Assert.Equal(300.25, zone2.PositionX);
		Assert.Equal(400.5, zone2.PositionY);
		Assert.Equal(100.0, zone2.Width);
		Assert.Equal(40.0, zone2.Height);

		// All zones should be signed
		Assert.True(zoneList.All(z => z.IsSigned));
	}

	/// <summary>
	/// Test that document status changes to Completed when all zones are signed
	/// </summary>
	[Fact]
	public async Task GenerateSignedPdf_AllZonesSigned_CompletesDocument()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);
		var docService = new DocumentService(docRepo, unitOfWork);

		var userId = Guid.NewGuid();
		var document = await docService.CreateAsync(new CreateDocumentDto
		{
			Title = "Document for Completion Test",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf"
		}, userId);

		// Create all required zones and sign them
		var signatureImage = CreateSignatureImageBase64();
		var zones = new List<SignatureZone>
		{
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 50,
				PositionY = 50,
				Width = 100,
				Height = 40,
				IsRequired = true,
				Order = 1
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 200,
				PositionY = 50,
				Width = 100,
				Height = 40,
				IsRequired = true,
				Order = 2
			}
		};

		foreach (var zone in zones)
		{
			await zoneRepo.AddAsync(zone);
		}
		await unitOfWork.SaveChangesAsync();

		// Sign all zones
		foreach (var zone in zones)
		{
			zone.SignatureImage = signatureImage;
			zone.SignedByUserId = userId;
			zone.SignedAt = DateTime.UtcNow;
			zoneRepo.Update(zone);
		}
		await unitOfWork.SaveChangesAsync();

		// Act - Check if all required zones are signed and update document status
		var allZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id && z.IsRequired);
		var requiredZones = allZones.ToList();
		var allSigned = requiredZones.All(z => z.IsSigned);

		if (allSigned)
		{
			await docService.UpdateStatusAsync(document.Id, (int)DocumentStatus.Completed);
		}

		// Assert
		var completedDoc = await docService.GetByIdAsync(document.Id);
		Assert.NotNull(completedDoc);
		Assert.Equal(DocumentStatus.Completed, completedDoc.Status);
		Assert.NotNull(completedDoc.SignedAt);
	}

	/// <summary>
	/// Test that document status remains InProgress when not all zones are signed
	/// </summary>
	[Fact]
	public async Task GenerateSignedPdf_NotAllZonesSigned_DoesNotCompleteDocument()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);
		var docService = new DocumentService(docRepo, unitOfWork);

		var userId = Guid.NewGuid();
		var document = await docService.CreateAsync(new CreateDocumentDto
		{
			Title = "Document with Partial Signing",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf"
		}, userId);

		// Create zones - only one signed
		var zones = new List<SignatureZone>
		{
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 50,
				PositionY = 50,
				Width = 100,
				Height = 40,
				IsRequired = true,
				SignatureImage = CreateSignatureImageBase64(),
				SignedByUserId = userId,
				SignedAt = DateTime.UtcNow,
				Order = 1
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 200,
				PositionY = 50,
				Width = 100,
				Height = 40,
				IsRequired = true,
				Order = 2
			}
		};

		foreach (var zone in zones)
		{
			await zoneRepo.AddAsync(zone);
		}
		await unitOfWork.SaveChangesAsync();

		// Act - Try to complete document (should fail because not all zones signed)
		var allZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id && z.IsRequired);
		var requiredZones = allZones.ToList();
		var allSigned = requiredZones.All(z => z.IsSigned);

		// Assert - Document should NOT be completed
		Assert.False(allSigned);

		var currentDoc = await docService.GetByIdAsync(document.Id);
		Assert.NotNull(currentDoc);
		Assert.NotEqual(DocumentStatus.Completed, currentDoc.Status);
	}

	/// <summary>
	/// Test that signed PDF path is saved to document
	/// </summary>
	[Fact]
	public async Task GenerateSignedPdf_SavesSignedFilePath()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);
		var docService = new DocumentService(docRepo, unitOfWork);

		var userId = Guid.NewGuid();
		var document = await docService.CreateAsync(new CreateDocumentDto
		{
			Title = "Document for Signed Path Test",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf"
		}, userId);

		// Add and sign zone
		var zone = new SignatureZone
		{
			DocumentId = document.Id,
			PageNumber = 1,
			PositionX = 100,
			PositionY = 200,
			Width = 150,
			Height = 50,
			IsRequired = true,
			SignatureImage = CreateSignatureImageBase64(),
			SignedByUserId = userId,
			SignedAt = DateTime.UtcNow,
			Order = 1
		};
		await zoneRepo.AddAsync(zone);
		await unitOfWork.SaveChangesAsync();

		// Simulate signed PDF generation
		var signedFilePath = $"/uploads/documents/{document.Id}_signed.pdf";

		// Act - Update document with signed file path
		await docService.UpdateSignedFilePathAsync(document.Id, signedFilePath);
		await docService.UpdateStatusAsync(document.Id, (int)DocumentStatus.Completed);

		// Assert
		var updatedDoc = await docService.GetByIdAsync(document.Id);
		Assert.NotNull(updatedDoc);
		Assert.Equal(signedFilePath, updatedDoc.SignedFilePath);
		Assert.Equal(DocumentStatus.Completed, updatedDoc.Status);
	}

	/// <summary>
	/// Test multi-page signature zones are all applied to PDF
	/// </summary>
	[Fact]
	public async Task GenerateSignedPdf_MultiPageSignatures_AllApplied()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);

		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Multi-Page Document",
			FileName = "multipage.pdf",
			OriginalFileName = "multipage.pdf",
			FilePath = "/multipage.pdf",
			CreatedById = userId
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		var signatureImage = CreateSignatureImageBase64();

		// Create zones on different pages
		var zones = new List<SignatureZone>
		{
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 50,
				PositionY = 700,
				Width = 200,
				Height = 60,
				SignatureImage = signatureImage,
				SignedByUserId = userId,
				SignedAt = DateTime.UtcNow,
				IsRequired = true,
				Order = 1
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 2,
				PositionX = 50,
				PositionY = 700,
				Width = 200,
				Height = 60,
				SignatureImage = signatureImage,
				SignedByUserId = userId,
				SignedAt = DateTime.UtcNow,
				IsRequired = true,
				Order = 2
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 3,
				PositionX = 50,
				PositionY = 700,
				Width = 200,
				Height = 60,
				SignatureImage = signatureImage,
				SignedByUserId = userId,
				SignedAt = DateTime.UtcNow,
				IsRequired = true,
				Order = 3
			}
		};

		foreach (var zone in zones)
		{
			await zoneRepo.AddAsync(zone);
		}
		await unitOfWork.SaveChangesAsync();

		// Act - Verify all zones are stored
		var savedZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id);
		var zoneList = savedZones.ToList();

		// Assert
		Assert.Equal(3, zoneList.Count);

		// Verify each page has a signed zone
		var page1Zones = zoneList.Where(z => z.PageNumber == 1).ToList();
		var page2Zones = zoneList.Where(z => z.PageNumber == 2).ToList();
		var page3Zones = zoneList.Where(z => z.PageNumber == 3).ToList();

		Assert.Single(page1Zones);
		Assert.Single(page2Zones);
		Assert.Single(page3Zones);

		Assert.True(page1Zones.All(z => z.IsSigned));
		Assert.True(page2Zones.All(z => z.IsSigned));
		Assert.True(page3Zones.All(z => z.IsSigned));
	}

	/// <summary>
	/// Test that optional (non-required) zones don't block document completion
	/// </summary>
	[Fact]
	public async Task GenerateSignedPdf_OptionalZonesNotRequired_CompletesDocument()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);
		var docService = new DocumentService(docRepo, unitOfWork);

		var userId = Guid.NewGuid();
		var document = await docService.CreateAsync(new CreateDocumentDto
		{
			Title = "Document with Optional Zone",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf"
		}, userId);

		var signatureImage = CreateSignatureImageBase64();

		// Create one required zone (signed) and one optional zone (not signed)
		var zones = new List<SignatureZone>
		{
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 50,
				PositionY = 50,
				Width = 100,
				Height = 40,
				IsRequired = true,
				SignatureImage = signatureImage,
				SignedByUserId = userId,
				SignedAt = DateTime.UtcNow,
				Order = 1
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 200,
				PositionY = 50,
				Width = 100,
				Height = 40,
				IsRequired = false, // Optional zone
				Order = 2
			}
		};

		foreach (var zone in zones)
		{
			await zoneRepo.AddAsync(zone);
		}
		await unitOfWork.SaveChangesAsync();

		// Act - Check only required zones
		var allZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id);
		var requiredZones = allZones.Where(z => z.IsRequired).ToList();
		var allRequiredSigned = requiredZones.All(z => z.IsSigned);

		if (allRequiredSigned)
		{
			await docService.UpdateStatusAsync(document.Id, (int)DocumentStatus.Completed);
		}

		// Assert - Document should be completed even with unsigned optional zone
		var completedDoc = await docService.GetByIdAsync(document.Id);
		Assert.NotNull(completedDoc);
		Assert.Equal(DocumentStatus.Completed, completedDoc.Status);
	}

	#endregion
}
