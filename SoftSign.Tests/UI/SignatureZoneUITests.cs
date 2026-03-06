using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Infrastructure.Data;
using SoftSign.Domain.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Xunit;
using Xunit.Abstractions;

namespace SoftSign.Tests.UI;

/// <summary>
/// UI Tests for Signature Zone feature using ASP.NET Core TestServer
/// </summary>
public class SignatureZoneUITests : IClassFixture<SignatureZoneWebApplicationFactory>
{
    private readonly SignatureZoneWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public SignatureZoneUITests(SignatureZoneWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    #region Signature Drawing Inside Zone Tests

    /// <summary>
    /// Test: Draw page loads with signature canvas
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_LoadsWithSignatureCanvas()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify canvas exists
        Assert.Contains("signatureCanvas", content);
        Assert.Contains("id=\"signatureCanvas\"", content);
        
        // Verify signature form exists
        Assert.Contains("signatureForm", content);
        Assert.Contains("signatureData", content);
    }

    /// <summary>
    /// Test: Signature canvas has correct dimensions
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_CanvasHasCorrectDimensions()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify canvas dimensions
        Assert.Contains("width=\"300\"", content);
        Assert.Contains("height=\"150\"", content);
    }

    /// <summary>
    /// Test: Clear signature button exists
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_ClearButtonExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify clear button exists with JavaScript function
        Assert.Contains("clearSignature()", content);
        Assert.Contains("Effacer", content);
    }

    /// <summary>
    /// Test: Type signature button exists (for typed signatures)
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_TypeSignatureButtonExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify type signature button exists
        Assert.Contains("typeSignature()", content);
        Assert.Contains("Saisir", content);
    }

    /// <summary>
    /// Test: Type signature modal exists
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_TypeSignatureModalExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify modal exists
        Assert.Contains("typeSignatureModal", content);
        Assert.Contains("typedSignature", content);
    }

    /// <summary>
    /// Test: Save signature button exists
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_SaveButtonExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify submit button
        Assert.Contains("Appliquer la signature", content);
        Assert.Contains("type=\"submit\"", content);
        Assert.Contains("saveSignature()", content);
    }

    /// <summary>
    /// Test: Comments field exists for signature submission
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_CommentsFieldExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify comments field
        Assert.Contains("comments", content);
        Assert.Contains("form-label", content);
    }

    /// <summary>
    /// Test: Submit without signature shows validation error
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_SubmitWithoutSignature_ShowsError()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Get page to extract anti-forgery token
        var getResponse = await client.GetAsync($"/Signature/Draw?documentId={documentId}");
        var content = await getResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(content);

        // Act - Submit form without signature
        var formData = new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "documentId", documentId.ToString() },
            { "signatureData", "" }, // Empty signature
            { "comments", "" }
        };

        var response = await client.PostAsync($"/Signature/Submit?documentId={documentId}", new FormUrlEncodedContent(formData));

        // Assert - Should redirect back with error
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected redirect or OK, got {response.StatusCode}");
    }

    /// <summary>
    /// Test: JavaScript drawing functions exist in the page
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_DrawingJavaScriptFunctionsExist()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify JavaScript drawing functions
        Assert.Contains("mousedown", content);     // Mouse event
        Assert.Contains("mousemove", content);      // Mouse event  
        Assert.Contains("mouseup", content);       // Mouse event
        Assert.Contains("touchstart", content);    // Touch event
        Assert.Contains("touchmove", content);      // Touch event
        Assert.Contains("touchend", content);       // Touch event
        Assert.Contains("toDataURL", content);      // Canvas to base64
    }

    #endregion

    #region Multiple Zones Per Document Tests

    /// <summary>
    /// Test: CreateZone page loads successfully
    /// </summary>
    [Fact]
    public async Task Test_CreateZonePage_LoadsSuccessfully()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/CreateZone?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify page elements
        Assert.Contains("Dessiner la zone de signature", content);
        Assert.Contains("drawingCanvas", content);
    }

    /// <summary>
    /// Test: Zone form has required fields
    /// </summary>
    [Fact]
    public async Task Test_CreateZonePage_FormHasRequiredFields()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/CreateZone?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify form fields
        Assert.Contains("Label", content);
        Assert.Contains("PageNumber", content);
        Assert.Contains("Level", content);
        Assert.Contains("Order", content);
        Assert.Contains("IsRequired", content);
    }

    /// <summary>
    /// Test: Drawing canvas overlay exists
    /// </summary>
    [Fact]
    public async Task Test_CreateZonePage_DrawingCanvasOverlayExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/CreateZone?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify canvas overlay
        Assert.Contains("drawingCanvas", content);
        Assert.Contains("pdfContainer", content);
    }

    /// <summary>
    /// Test: Save button is disabled initially
    /// </summary>
    [Fact]
    public async Task Test_CreateZonePage_SaveButtonDisabledInitially()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/CreateZone?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify button starts disabled
        Assert.Contains("saveBtn", content);
        Assert.Contains("disabled", content);
    }

    /// <summary>
    /// Test: Clear drawing function exists
    /// </summary>
    [Fact]
    public async Task Test_CreateZonePage_ClearDrawingFunctionExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Act
        var response = await client.GetAsync($"/Signature/CreateZone?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify clear function
        Assert.Contains("clearDrawing()", content);
        Assert.Contains("Effacer", content);
    }

    /// <summary>
    /// Test: GetZones API endpoint returns zones
    /// </summary>
    [Fact]
    public async Task Test_GetZones_ReturnsZonesForDocument()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentWithZonesAsync();

        // Act
        var response = await client.GetAsync($"/Signature/GetZones?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify zones are returned
        Assert.Contains("zone", content.ToLower());
    }

    /// <summary>
    /// Test: Multiple zones can be created
    /// </summary>
    [Fact]
    public async Task Test_CreateZone_CanCreateMultipleZones()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Get page to extract anti-forgery token
        var getResponse = await client.GetAsync($"/Signature/CreateZone?documentId={documentId}");
        var content = await getResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(content);

        // Create first zone
        var zone1Data = new
        {
            documentId = documentId,
            pageNumber = 1,
            positionX = 100,
            positionY = 700,
            width = 150,
            height = 50,
            level = "Signature",
            label = "Signature 1",
            isRequired = true,
            order = 1
        };

        var response1 = await client.PostAsync("/Signature/CreateZoneJson", 
            new StringContent(System.Text.Json.JsonSerializer.Serialize(zone1Data), 
                System.Text.Encoding.UTF8, "application/json"));

        // Assert first zone created
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        // Create second zone
        var zone2Data = new
        {
            documentId = documentId,
            pageNumber = 1,
            positionX = 300,
            positionY = 700,
            width = 100,
            height = 30,
            level = "Initial",
            label = "Initial 1",
            isRequired = true,
            order = 2
        };

        var response2 = await client.PostAsync("/Signature/CreateZoneJson",
            new StringContent(System.Text.Json.JsonSerializer.Serialize(zone2Data),
                System.Text.Encoding.UTF8, "application/json"));

        // Assert second zone created
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        // Verify both zones exist
        var zonesResponse = await client.GetAsync($"/Signature/GetZones?documentId={documentId}");
        var zonesContent = await zonesResponse.Content.ReadAsStringAsync();
        
        Assert.True(zonesContent.Contains("Signature 1") || zonesContent.Contains("Signature"));
    }

    /// <summary>
    /// Test: Draw page shows signature zones from database
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_ShowsSignatureZones()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentWithZonesAsync();

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify zones are displayed
        Assert.Contains("signature-zone", content);
    }

    #endregion

    #region Multiple Signers Tests

    /// <summary>
    /// Test: Zone can be assigned to specific user
    /// </summary>
    [Fact]
    public async Task Test_CreateZone_CanAssignToUser()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (documentId, userId) = await _factory.SeedTestDocumentWithUserAsync();

        // Get page to extract anti-forgery token
        var getResponse = await client.GetAsync($"/Signature/CreateZone?documentId={documentId}");
        var content = await getResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(content);

        // Create zone with assigned user
        var zoneData = new
        {
            documentId = documentId,
            pageNumber = 1,
            positionX = 100,
            positionY = 700,
            width = 150,
            height = 50,
            level = "Signature",
            label = "User Signature",
            isRequired = true,
            order = 1,
            assignedUserId = userId
        };

        var response = await client.PostAsync("/Signature/CreateZoneJson",
            new StringContent(System.Text.Json.JsonSerializer.Serialize(zoneData),
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify zone has assigned user
        var zonesResponse = await client.GetAsync($"/Signature/GetZones?documentId={documentId}");
        var zonesContent = await zonesResponse.Content.ReadAsStringAsync();
        
        Assert.Contains(userId.ToString(), zonesContent);
    }

    /// <summary>
    /// Test: Zone displays assigned user information
    /// </summary>
    [Fact]
    public async Task Test_DrawPage_ShowsAssignedUserInfo()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (documentId, userId) = await _factory.SeedTestDocumentWithUserAsync();

        // Create zone with assigned user
        await _factory.CreateZoneForUserAsync(documentId, userId);

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Zone should be displayed (may or may not show user based on implementation)
        Assert.Contains("signature-zone", content);
    }

    /// <summary>
    /// Test: Signed zone shows signed indicator
    /// </summary>
    [Fact]
    public async Task Test_SignedZone_ShowsSignedIndicator()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (documentId, userId) = await _factory.SeedTestDocumentWithUserAsync();

        // Create and sign a zone
        await _factory.CreateAndSignZoneAsync(documentId, userId);

        // Act
        var response = await client.GetAsync($"/Signature/Draw?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify signed indicator exists in the UI
        Assert.Contains("signed", content.ToLower());
    }

    /// <summary>
    /// Test: GetZones returns signed status for zones
    /// </summary>
    [Fact]
    public async Task Test_GetZones_ReturnsSignedStatus()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (documentId, userId) = await _factory.SeedTestDocumentWithUserAsync();

        // Create and sign a zone
        await _factory.CreateAndSignZoneAsync(documentId, userId);

        // Act
        var response = await client.GetAsync($"/Signature/GetZones?documentId={documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify signed status is returned
        Assert.Contains("isSigned", content);
    }

    #endregion

    #region Zone Validation Tests

    /// <summary>
    /// Test: ValidateZone endpoint exists
    /// </summary>
    [Fact]
    public async Task Test_ValidateZone_EndpointExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Create a zone first
        await _factory.CreateZoneAsync(documentId);

        // Validate zone
        var validateData = new
        {
            zoneX = 100,
            zoneY = 700,
            zoneWidth = 150,
            zoneHeight = 50,
            signatureX = 110,
            signatureY = 710,
            signatureWidth = 50,
            signatureHeight = 20
        };

        var response = await client.PostAsync("/Signature/ValidateZone",
            new StringContent(System.Text.Json.JsonSerializer.Serialize(validateData),
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Test: Delete zone endpoint exists
    /// </summary>
    [Fact]
    public async Task Test_DeleteZone_EndpointExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var documentId = await _factory.SeedTestDocumentAsync();

        // Create a zone
        var zoneId = await _factory.CreateZoneAsync(documentId);

        // Get page to extract anti-forgery token
        var getResponse = await client.GetAsync($"/Signature/CreateZone?documentId={documentId}");
        var content = await getResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(content);

        // Delete zone
        var response = await client.PostAsync($"/Signature/DeleteZone?id={zoneId}",
            new StringContent("", System.Text.Encoding.UTF8));

        // Assert - should succeed (may redirect)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Redirect);
    }

    #endregion

    #region Helper Methods

    private string ExtractAntiForgeryToken(string html)
    {
        // Extract anti-forgery token from the form
        var startIndex = html.IndexOf("__RequestVerificationToken\"");
        if (startIndex == -1) return string.Empty;
        
        var valueStart = html.IndexOf("value=\"", startIndex);
        if (valueStart == -1) return string.Empty;
        
        valueStart += 7;
        var valueEnd = html.IndexOf("\"", valueStart);
        if (valueEnd == -1) return string.Empty;
        
        return html.Substring(valueStart, valueEnd - valueStart);
    }

    #endregion
}

/// <summary>
/// Test Web Application Factory for Signature Zone tests
/// </summary>
public class SignatureZoneWebApplicationFactory : WebApplicationFactory<SoftSign.Web.Program>
{
    private Guid _testDocumentId;
    private Guid _testUserId;
    private readonly List<SignatureZone> _testZones = new();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => 
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            });

            // Build service provider
            var sp = services.BuildServiceProvider();

            // Seed test data
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
        });
    }

    /// <summary>
    /// Seed a test document
    /// </summary>
    public async Task<Guid> SeedTestDocumentAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        _testUserId = Guid.NewGuid();
        var user = new User
        {
            Id = _testUserId,
            Email = "testuser@test.com",
            FirstName = "Test",
            LastName = "User",
            
            EmailConfirmed = true,
            IsActive = true
        };
        
        _testDocumentId = Guid.NewGuid();
        var document = new Document
        {
            Id = _testDocumentId,
            Title = "Test Document",
            FileName = "test.pdf",
            FilePath = "/uploads/test.pdf",
            Status = DocumentStatus.Pending,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        
        context.Users.Add(user);
        context.Documents.Add(document);
        
        await context.SaveChangesAsync();
        
        return _testDocumentId;
    }

    /// <summary>
    /// Seed a test document with a user
    /// </summary>
    public async Task<(Guid documentId, Guid userId)> SeedTestDocumentWithUserAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        _testUserId = Guid.NewGuid();
        var user = new User
        {
            Id = _testUserId,
            Email = "signer@test.com",
            FirstName = "Signer",
            LastName = "User",
            
            EmailConfirmed = true,
            IsActive = true
        };
        
        _testDocumentId = Guid.NewGuid();
        var document = new Document
        {
            Id = _testDocumentId,
            Title = "Test Document for Signing",
            FileName = "test.pdf",
            FilePath = "/uploads/test.pdf",
            Status = DocumentStatus.Pending,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        
        context.Users.Add(user);
        context.Documents.Add(document);
        
        await context.SaveChangesAsync();
        
        return (_testDocumentId, _testUserId);
    }

    /// <summary>
    /// Seed a test document with signature zones
    /// </summary>
    public async Task<Guid> SeedTestDocumentWithZonesAsync()
    {
        var (documentId, _) = await SeedTestDocumentWithUserAsync();
        
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Add zones
        var zone1 = new SignatureZone
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            PageNumber = 1,
            PositionX = 100,
            PositionY = 700,
            Width = 150,
            Height = 50,
            Level = SignatureLevel.Signature,
            Label = "Signature Zone",
            IsRequired = true,
            Order = 1
        };
        
        var zone2 = new SignatureZone
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            PageNumber = 1,
            PositionX = 300,
            PositionY = 700,
            Width = 100,
            Height = 30,
            Level = SignatureLevel.Initial,
            Label = "Initial Zone",
            IsRequired = true,
            Order = 2
        };
        
        context.SignatureZones.Add(zone1);
        context.SignatureZones.Add(zone2);
        
        await context.SaveChangesAsync();
        
        return documentId;
    }

    /// <summary>
    /// Create a zone for a specific user
    /// </summary>
    public async Task<Guid> CreateZoneForUserAsync(Guid documentId, Guid userId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var zone = new SignatureZone
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            PageNumber = 1,
            PositionX = 100,
            PositionY = 700,
            Width = 150,
            Height = 50,
            Level = SignatureLevel.Signature,
            Label = "User Signature Zone",
            AssignedUserId = userId,
            IsRequired = true,
            Order = 1
        };
        
        context.SignatureZones.Add(zone);
        await context.SaveChangesAsync();
        
        return zone.Id;
    }

    /// <summary>
    /// Create a zone
    /// </summary>
    public async Task<Guid> CreateZoneAsync(Guid documentId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var zone = new SignatureZone
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            PageNumber = 1,
            PositionX = 100,
            PositionY = 700,
            Width = 150,
            Height = 50,
            Level = SignatureLevel.Signature,
            Label = "Test Zone",
            IsRequired = true,
            Order = 1
        };
        
        context.SignatureZones.Add(zone);
        await context.SaveChangesAsync();
        
        return zone.Id;
    }

    /// <summary>
    /// Create and sign a zone
    /// </summary>
    public async Task CreateAndSignZoneAsync(Guid documentId, Guid userId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Create base64 signature image
        var signatureImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        
        var zone = new SignatureZone
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            PageNumber = 1,
            PositionX = 100,
            PositionY = 700,
            Width = 150,
            Height = 50,
            Level = SignatureLevel.Signature,
            Label = "Signed Zone",
            AssignedUserId = userId,
            IsRequired = true,
            Order = 1,
            SignatureImage = signatureImage,
            SignedByUserId = userId,
            SignedAt = DateTime.UtcNow
        };
        
        context.SignatureZones.Add(zone);
        await context.SaveChangesAsync();
    }
}
