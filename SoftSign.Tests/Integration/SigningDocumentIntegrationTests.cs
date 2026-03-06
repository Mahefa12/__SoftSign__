using Microsoft.EntityFrameworkCore;
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
/// Integration tests for signing a document with signature zones
/// </summary>
public class SigningDocumentIntegrationTests
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

	#region Signing Zone Tests

	/// <summary>
	/// Test that signing a zone stores the signature correctly in the database
	/// </summary>
	[Fact]
	public async Task SigningZone_StoresSignatureCorrectly()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);

		// Create a document
		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Test Document for Signing",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId,
			Status = DocumentStatus.InProgress
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Create a signature zone
		var signatureImage = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="; // Simple 1x1 transparent PNG base64
		var zone = new SignatureZone
		{
			DocumentId = document.Id,
			PageNumber = 1,
			PositionX = 100,
			PositionY = 200,
			Width = 150,
			Height = 50,
			Level = SignatureLevel.Signature,
			Label = "Signature Zone 1",
			Order = 1,
			IsRequired = true
		};
		await zoneRepo.AddAsync(zone);
		await unitOfWork.SaveChangesAsync();

		// Act - Sign the zone
		var signingUserId = Guid.NewGuid();
		zone.SignatureImage = signatureImage;
		zone.SignedByUserId = signingUserId;
		zone.SignedAt = DateTime.UtcNow;

		zoneRepo.Update(zone);
		await unitOfWork.SaveChangesAsync();

		// Assert - Verify signature was stored correctly
		var signedZone = await zoneRepo.GetByIdAsync(zone.Id);
		Assert.NotNull(signedZone);
		Assert.Equal(signatureImage, signedZone.SignatureImage);
		Assert.Equal(signingUserId, signedZone.SignedByUserId);
		Assert.NotNull(signedZone.SignedAt);
	}

	/// <summary>
	/// Test that zone is marked as signed after signing (IsSigned computed property)
	/// </summary>
	[Fact]
	public async Task SigningZone_MarksZoneAsSigned()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);

		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Test Document",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

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

		// Assert - Before signing, IsSigned should be false
		var unsignedZone = await zoneRepo.GetByIdAsync(zone.Id);
		Assert.NotNull(unsignedZone);
		Assert.False(unsignedZone.IsSigned);
		Assert.Null(unsignedZone.SignedAt);

		// Act - Sign the zone
		unsignedZone.SignatureImage = "base64signature";
		unsignedZone.SignedByUserId = Guid.NewGuid();
		unsignedZone.SignedAt = DateTime.UtcNow;

		zoneRepo.Update(unsignedZone);
		await unitOfWork.SaveChangesAsync();

		// Assert - After signing, IsSigned should be true
		var signedZone = await zoneRepo.GetByIdAsync(zone.Id);
		Assert.NotNull(signedZone);
		Assert.True(signedZone.IsSigned);
		Assert.NotNull(signedZone.SignedAt);
	}

	/// <summary>
	/// Test that signed zones display correctly in the document
	/// </summary>
	[Fact]
	public async Task SigningZone_DisplaysCorrectlyInDocument()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);

		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Test Document with Multiple Zones",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		// Create multiple zones
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
				Label = "Zone 1",
				Order = 1,
				IsRequired = true
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 300,
				PositionY = 200,
				Width = 150,
				Height = 50,
				Label = "Zone 2",
				Order = 2,
				IsRequired = true
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 2,
				PositionX = 100,
				PositionY = 300,
				Width = 150,
				Height = 50,
				Label = "Zone 3",
				Order = 3,
				IsRequired = true
			}
		};

		foreach (var zone in zones)
		{
			await zoneRepo.AddAsync(zone);
		}
		await unitOfWork.SaveChangesAsync();

		// Sign only first two zones
		var signerId = Guid.NewGuid();
		zones[0].SignatureImage = "base64_1";
		zones[0].SignedByUserId = signerId;
		zones[0].SignedAt = DateTime.UtcNow;

		zones[1].SignatureImage = "base64_2";
		zones[1].SignedByUserId = signerId;
		zones[1].SignedAt = DateTime.UtcNow;

		zoneRepo.Update(zones[0]);
		zoneRepo.Update(zones[1]);
		await unitOfWork.SaveChangesAsync();

		// Act - Retrieve document with signature zones
		var documentWithZones = await docRepo.Query()
			.Include(d => d.SignatureZones)
			.FirstOrDefaultAsync(d => d.Id == document.Id);

		// Assert
		Assert.NotNull(documentWithZones);
		Assert.Equal(3, documentWithZones.SignatureZones.Count);

		var signedZones = documentWithZones.SignatureZones.Where(z => z.IsSigned).ToList();
		var unsignedZones = documentWithZones.SignatureZones.Where(z => !z.IsSigned).ToList();

		Assert.Equal(2, signedZones.Count);
		Assert.Single(unsignedZones);

		// Verify signed zones have correct data
		foreach (var signedZone in signedZones)
		{
			Assert.NotNull(signedZone.SignatureImage);
			Assert.Equal(signerId, signedZone.SignedByUserId);
			Assert.NotNull(signedZone.SignedAt);
		}

		// Verify unsigned zone
		var unsigedZone = unsignedZones.First();
		Assert.Null(unsigedZone.SignatureImage);
		Assert.Null(unsigedZone.SignedAt);
	}

	/// <summary>
	/// Test signing with multiple users - each user signs their assigned zone
	/// </summary>
	[Fact]
	public async Task SigningZone_MultipleUsersSignTheirOwnZones()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);

		var userId = Guid.NewGuid();
		var document = new Document
		{
			Title = "Multi-User Signing Document",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf",
			FilePath = "/test.pdf",
			CreatedById = userId
		};
		await docRepo.AddAsync(document);
		await unitOfWork.SaveChangesAsync();

		var user1 = Guid.NewGuid();
		var user2 = Guid.NewGuid();
		var user3 = Guid.NewGuid();

		var zones = new List<SignatureZone>
		{
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 50,
				PositionY = 100,
				Width = 100,
				Height = 40,
				AssignedUserId = user1,
				Order = 1,
				IsRequired = true
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 200,
				PositionY = 100,
				Width = 100,
				Height = 40,
				AssignedUserId = user2,
				Order = 2,
				IsRequired = true
			},
			new()
			{
				DocumentId = document.Id,
				PageNumber = 1,
				PositionX = 350,
				PositionY = 100,
				Width = 100,
				Height = 40,
				AssignedUserId = user3,
				Order = 3,
				IsRequired = true
			}
		};

		foreach (var zone in zones)
		{
			await zoneRepo.AddAsync(zone);
		}
		await unitOfWork.SaveChangesAsync();

		// Act - Each user signs their zone
		zones[0].SignatureImage = "user1_signature";
		zones[0].SignedByUserId = user1;
		zones[0].SignedAt = DateTime.UtcNow;

		zones[1].SignatureImage = "user2_signature";
		zones[1].SignedByUserId = user2;
		zones[1].SignedAt = DateTime.UtcNow;

		// User 3 hasn't signed yet

		zoneRepo.Update(zones[0]);
		zoneRepo.Update(zones[1]);
		await unitOfWork.SaveChangesAsync();

		// Assert
		var allZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id);
		var zoneList = allZones.ToList();

		Assert.Equal(3, zoneList.Count);

		var user1Zone = zoneList.First(z => z.AssignedUserId == user1);
		var user2Zone = zoneList.First(z => z.AssignedUserId == user2);
		var user3Zone = zoneList.First(z => z.AssignedUserId == user3);

		Assert.True(user1Zone.IsSigned);
		Assert.Equal(user1, user1Zone.SignedByUserId);

		Assert.True(user2Zone.IsSigned);
		Assert.Equal(user2, user2Zone.SignedByUserId);

		Assert.False(user3Zone.IsSigned);
		Assert.Null(user3Zone.SignedByUserId);
	}

	/// <summary>
	/// Test that signing updates the document status
	/// </summary>
	[Fact]
	public async Task SigningZone_UpdatesDocumentStatus()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);
		var documentService = new DocumentService(docRepo, unitOfWork);

		var userId = Guid.NewGuid();
		var document = await documentService.CreateAsync(new CreateDocumentDto
		{
			Title = "Document to Sign",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf"
		}, userId);

		// Add a signature zone
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

		// Act - Sign the zone
		zone.SignatureImage = "signature_data";
		zone.SignedByUserId = userId;
		zone.SignedAt = DateTime.UtcNow;
		zoneRepo.Update(zone);

		// Check if all required zones are signed
		var allZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id && z.IsRequired);
		var requiredZones = allZones.ToList();
		var allSigned = requiredZones.All(z => z.IsSigned);

		if (allSigned)
		{
			await documentService.UpdateStatusAsync(document.Id, (int)DocumentStatus.Completed);
		}

		await unitOfWork.SaveChangesAsync();

		// Assert
		var updatedDoc = await documentService.GetByIdAsync(document.Id);
		Assert.NotNull(updatedDoc);
		Assert.Equal(DocumentStatus.Completed, updatedDoc.Status);
		Assert.NotNull(updatedDoc.SignedAt);
	}

	/// <summary>
	/// Test partial signing - not all zones are signed
	/// </summary>
	[Fact]
	public async Task SigningZone_PartialSigningDoesNotComplete()
	{
		// Arrange
		using var context = CreateDbContext();
		var unitOfWork = CreateUnitOfWork(context);

		var zoneRepo = new Repository<SignatureZone>(context);
		var docRepo = new Repository<Document>(context);
		var documentService = new DocumentService(docRepo, unitOfWork);

		var userId = Guid.NewGuid();
		var document = await documentService.CreateAsync(new CreateDocumentDto
		{
			Title = "Document with Multiple Zones",
			FileName = "test.pdf",
			OriginalFileName = "test.pdf"
		}, userId);

		// Add multiple required zones
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

		// Act - Only sign one zone (partial signing)
		zones[0].SignatureImage = "signature_1";
		zones[0].SignedByUserId = userId;
		zones[0].SignedAt = DateTime.UtcNow;
		zoneRepo.Update(zones[0]);
		await unitOfWork.SaveChangesAsync();

		// Check if all required zones are signed
		var allZones = await zoneRepo.FindAsync(z => z.DocumentId == document.Id && z.IsRequired);
		var requiredZones = allZones.ToList();
		var allSigned = requiredZones.All(z => z.IsSigned);

		// Assert - Document should not be completed with partial signing
		var updatedDoc = await documentService.GetByIdAsync(document.Id);
		Assert.NotNull(updatedDoc);
		Assert.NotEqual(DocumentStatus.Completed, updatedDoc.Status);
		Assert.Null(updatedDoc.SignedAt);

		// Verify only one zone is signed
		var signedCount = requiredZones.Count(z => z.IsSigned);
		Assert.Equal(1, signedCount);
	}
	#endregion
}
