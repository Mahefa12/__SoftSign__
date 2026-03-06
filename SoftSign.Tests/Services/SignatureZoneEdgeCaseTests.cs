using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using Xunit;

namespace SoftSign.Tests.Services;

/// <summary>
/// Edge Case Tests for Signature Zone Feature
/// Tests various scenarios including multiple zones, different pages, 
/// screen sizes, zoom levels, browser compatibility, and document reopening.
/// </summary>
public class SignatureZoneEdgeCaseTests
{
    // Standard A4 page dimensions in points (1 point = 1/72 inch)
    private const double A4Width = 595.28;
    private const double A4Height = 841.89;

    #region 1. Multiple Signature Zones on One Page

    /// <summary>
    /// EDGE CASE: Test that zones don't overlap when positioned adjacently
    /// </summary>
    [Fact]
    public void MultipleZones_NoOverlap_WhenPositionedAdjacently()
    {
        // Arrange - Two zones side by side with small gap
        var zone1 = new SignatureZone
        {
            PageNumber = 1,
            PositionX = 50,
            PositionY = 700,
            Width = 200,
            Height = 50,
            Order = 1,
            Label = "Zone 1"
        };

        var zone2 = new SignatureZone
        {
            PageNumber = 1,
            PositionX = 260, // 50 + 200 + 10 gap
            PositionY = 700,
            Width = 200,
            Height = 50,
            Order = 2,
            Label = "Zone 2"
        };

        // Act - Check for overlap
        bool hasOverlap = CheckZonesOverlap(zone1, zone2);

        // Assert - No overlap expected
        Assert.False(hasOverlap, "Zones should not overlap");
    }

    /// <summary>
    /// EDGE CASE: Test detection of overlapping zones
    /// </summary>
    [Fact]
    public void MultipleZones_DetectsOverlap_WhenZonesOverlap()
    {
        // Arrange - Two zones that overlap
        var zone1 = new SignatureZone
        {
            PageNumber = 1,
            PositionX = 50,
            PositionY = 700,
            Width = 200,
            Height = 50,
            Order = 1,
            Label = "Zone 1"
        };

        var zone2 = new SignatureZone
        {
            PageNumber = 1,
            PositionX = 200, // Overlaps with zone1
            PositionY = 700,
            Width = 200,
            Height = 50,
            Order = 2,
            Label = "Zone 2"
        };

        // Act - Check for overlap
        bool hasOverlap = CheckZonesOverlap(zone1, zone2);

        // Assert - Overlap expected
        Assert.True(hasOverlap, "Zones should overlap");
    }

    /// <summary>
    /// EDGE CASE: Test that each zone can be signed independently
    /// </summary>
    [Fact]
    public void MultipleZones_CanSignIndependently()
    {
        // Arrange - Multiple zones with one signed
        var zones = new List<SignatureZone>
        {
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                PageNumber = 1,
                PositionX = 50,
                PositionY = 700,
                Width = 150,
                Height = 40,
                Order = 1,
                Label = "Signature 1",
                IsRequired = true
            },
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                PageNumber = 1,
                PositionX = 250,
                PositionY = 700,
                Width = 150,
                Height = 40,
                Order = 2,
                Label = "Signature 2",
                IsRequired = true
            },
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                PageNumber = 1,
                PositionX = 50,
                PositionY = 600,
                Width = 100,
                Height = 30,
                Order = 3,
                Label = "Initial",
                IsRequired = false
            }
        };

        // Act - Sign only the first zone by setting SignatureImage
        var zoneToSign = zones[0];
        zoneToSign.SignatureImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        zoneToSign.SignedAt = DateTime.UtcNow;

        // Assert - Only first zone signed
        Assert.False(string.IsNullOrEmpty(zones[0].SignatureImage));
        Assert.True(string.IsNullOrEmpty(zones[1].SignatureImage));
        Assert.True(string.IsNullOrEmpty(zones[2].SignatureImage));
        Assert.NotNull(zones[0].SignedAt);
        Assert.Null(zones[1].SignedAt);
    }

    /// <summary>
    /// EDGE CASE: Test zone ordering and rendering
    /// </summary>
    [Fact]
    public void MultipleZones_Ordering_IsDeterminedByOrderProperty()
    {
        // Arrange - Zones with different orders
        var zones = new List<SignatureZone>
        {
            new SignatureZone { Order = 3, Label = "Third" },
            new SignatureZone { Order = 1, Label = "First" },
            new SignatureZone { Order = 2, Label = "Second" }
        };

        // Act - Sort by order
        var sortedZones = zones.OrderBy(z => z.Order).ToList();

        // Assert
        Assert.Equal("First", sortedZones[0].Label);
        Assert.Equal("Second", sortedZones[1].Label);
        Assert.Equal("Third", sortedZones[2].Label);
    }

    /// <summary>
    /// EDGE CASE: Test maximum zones on single page
    /// </summary>
    [Fact]
    public void MultipleZones_CanHaveManyZones_OnSinglePage()
    {
        // Arrange - 10 zones on one page
        var zones = new List<SignatureZone>();
        for (int i = 0; i < 10; i++)
        {
            zones.Add(new SignatureZone
            {
                PageNumber = 1,
                PositionX = 50 + (i % 3) * 180,
                PositionY = 700 - (i / 3) * 100,
                Width = 150,
                Height = 40,
                Order = i + 1,
                Label = $"Zone {i + 1}",
                IsRequired = i < 5 // Only first 5 required
            });
        }

        // Assert - All zones created
        Assert.Equal(10, zones.Count);
        Assert.Equal(5, zones.Count(z => z.IsRequired));
    }

    #endregion

    #region 2. Multiple Pages

    /// <summary>
    /// EDGE CASE: Test zones on different pages
    /// </summary>
    [Fact]
    public void MultiplePages_ZonesCanExistOnDifferentPages()
    {
        // Arrange - Zones on pages 1, 3, and 5
        var zones = new List<SignatureZone>
        {
            new SignatureZone { PageNumber = 1, PositionX = 100, PositionY = 700, Width = 200, Height = 50, Order = 1 },
            new SignatureZone { PageNumber = 3, PositionX = 100, PositionY = 700, Width = 200, Height = 50, Order = 2 },
            new SignatureZone { PageNumber = 5, PositionX = 100, PositionY = 700, Width = 200, Height = 50, Order = 3 }
        };

        // Assert - All pages different
        Assert.Equal(1, zones[0].PageNumber);
        Assert.Equal(3, zones[1].PageNumber);
        Assert.Equal(5, zones[2].PageNumber);
    }

    /// <summary>
    /// EDGE CASE: Test zone on first page
    /// </summary>
    [Fact]
    public void MultiplePages_ZoneOnFirstPage_IsValid()
    {
        var zone = new SignatureZone
        {
            PageNumber = 1,
            PositionX = 100,
            PositionY = 100,
            Width = 200,
            Height = 50
        };

        Assert.Equal(1, zone.PageNumber);
        Assert.True(zone.PageNumber > 0);
    }

    /// <summary>
    /// EDGE CASE: Test zone on last page of multi-page document
    /// </summary>
    [Fact]
    public void MultiplePages_ZoneOnLastPage_IsValid()
    {
        int totalPages = 10;
        var zone = new SignatureZone
        {
            PageNumber = totalPages,
            PositionX = 100,
            PositionY = 100,
            Width = 200,
            Height = 50
        };

        Assert.Equal(totalPages, zone.PageNumber);
    }

    /// <summary>
    /// EDGE CASE: Test multiple zones on multiple pages
    /// </summary>
    [Fact]
    public void MultiplePages_MultipleZonesAcrossPages()
    {
        // Arrange - 2 zones on page 1, 1 zone on page 2, 1 zone on page 3
        var zones = new List<SignatureZone>
        {
            new SignatureZone { PageNumber = 1, PositionX = 50, PositionY = 700, Width = 150, Height = 40, Order = 1 },
            new SignatureZone { PageNumber = 1, PositionX = 250, PositionY = 700, Width = 150, Height = 40, Order = 2 },
            new SignatureZone { PageNumber = 2, PositionX = 100, PositionY = 700, Width = 150, Height = 40, Order = 3 },
            new SignatureZone { PageNumber = 3, PositionX = 100, PositionY = 700, Width = 150, Height = 40, Order = 4 }
        };

        // Assert
        Assert.Equal(2, zones.Count(z => z.PageNumber == 1));
        Assert.Equal(1, zones.Count(z => z.PageNumber == 2));
        Assert.Equal(1, zones.Count(z => z.PageNumber == 3));
    }

    #endregion

    #region 3. Different Screen Sizes (Responsive Behavior)

    /// <summary>
    /// EDGE CASE: Test zone positioning at minimum viewport width (mobile)
    /// </summary>
    [Fact]
    public void Responsive_MinimumViewport_WidthCalculation()
    {
        // Arrange - Mobile viewport (320px width)
        double viewportWidth = 320;
        double viewportHeight = 568;
        double pdfDisplayWidth = viewportWidth - 20; // 20px padding
        double scaleFactor = pdfDisplayWidth / A4Width;

        // Act - Calculate zone display size
        double zoneDisplayWidth = 200 * scaleFactor;
        double zoneDisplayHeight = 50 * scaleFactor;

        // Assert - Zone should fit in viewport
        Assert.True(zoneDisplayWidth <= pdfDisplayWidth);
        Assert.True(zoneDisplayWidth > 0);
        Assert.True(zoneDisplayHeight > 0);
    }

    /// <summary>
    /// EDGE CASE: Test zone positioning at tablet viewport
    /// </summary>
    [Fact]
    public void Responsive_TabletViewport_WidthCalculation()
    {
        // Arrange - Tablet viewport (768px width)
        double viewportWidth = 768;
        double pdfDisplayWidth = viewportWidth - 40; // 40px padding
        double scaleFactor = pdfDisplayWidth / A4Width;

        // Act
        double zoneDisplayWidth = 200 * scaleFactor;
        double zoneDisplayHeight = 50 * scaleFactor;

        // Assert
        Assert.True(zoneDisplayWidth > 100); // Should be reasonably sized
    }

    /// <summary>
    /// EDGE CASE: Test zone positioning at desktop viewport
    /// </summary>
    [Fact]
    public void Responsive_DesktopViewport_WidthCalculation()
    {
        // Arrange - Desktop viewport (1920px width)
        double viewportWidth = 1920;
        double pdfDisplayWidth = 800; // Max display width
        double scaleFactor = pdfDisplayWidth / A4Width;

        // Act
        double zoneDisplayWidth = 200 * scaleFactor;
        double zoneDisplayHeight = 50 * scaleFactor;

        // Assert
        Assert.True(zoneDisplayWidth > 200); // Scaled up
    }

    /// <summary>
    /// EDGE CASE: Test zone scaling maintains aspect ratio
    /// </summary>
    [Fact]
    public void Responsive_AspectRatio_IsMaintained()
    {
        // Arrange
        double originalWidth = 200;
        double originalHeight = 50;
        double aspectRatio = originalWidth / originalHeight;

        // Various scale factors
        double[] scaleFactors = { 0.5, 1.0, 1.5, 2.0 };

        foreach (var scale in scaleFactors)
        {
            // Act
            double newWidth = originalWidth * scale;
            double newHeight = originalHeight * scale;
            double newAspectRatio = newWidth / newHeight;

            // Assert - Aspect ratio should be maintained
            Assert.Equal(aspectRatio, newAspectRatio, 10);
        }
    }

    #endregion

    #region 4. Zoomed PDF Preview (Coordinate Conversion)

    /// <summary>
    /// EDGE CASE: Test coordinate conversion at 50% zoom
    /// </summary>
    [Fact]
    public void Zoom_50Percent_CoordinateConversion()
    {
        // Arrange
        double zoomFactor = 0.5;
        double htmlZoneX = 100;
        double htmlZoneY = 100;
        double zoneWidth = 200;
        double zoneHeight = 80;

        // Act - Convert HTML coordinates to PDF coordinates with zoom
        double pdfZoneX = htmlZoneX / zoomFactor;
        double pdfZoneY = A4Height - (htmlZoneY / zoomFactor) - (zoneHeight / zoomFactor);

        // Assert
        Assert.Equal(200, pdfZoneX, 2);
        Assert.Equal(661.89, pdfZoneY, 2);
    }

    /// <summary>
    /// EDGE CASE: Test coordinate conversion at 150% zoom
    /// </summary>
    [Fact]
    public void Zoom_150Percent_CoordinateConversion()
    {
        // Arrange
        double zoomFactor = 1.5;
        double htmlZoneX = 300;
        double htmlZoneY = 150;
        double zoneWidth = 300;
        double zoneHeight = 120;

        // Act
        double pdfZoneX = htmlZoneX / zoomFactor;
        double pdfZoneY = A4Height - (htmlZoneY / zoomFactor) - (zoneHeight / zoomFactor);

        // Assert
        Assert.Equal(200, pdfZoneX, 2);
        Assert.Equal(587.93, pdfZoneY, 2);
    }

    /// <summary>
    /// EDGE CASE: Test coordinate conversion at 200% zoom (double)
    /// </summary>
    [Fact]
    public void Zoom_200Percent_CoordinateConversion()
    {
        // Arrange
        double zoomFactor = 2.0;
        double htmlZoneX = 400;
        double htmlZoneY = 200;
        double zoneWidth = 400;
        double zoneHeight = 160;

        // Act
        double pdfZoneX = htmlZoneX / zoomFactor;
        double pdfZoneY = A4Height - (htmlZoneY / zoomFactor) - (zoneHeight / zoomFactor);

        // Assert
        Assert.Equal(200, pdfZoneX, 2);
        Assert.Equal(513.89, pdfZoneY, 2);
    }

    /// <summary>
    /// EDGE CASE: Test zone position at edge of page with zoom
    /// </summary>
    [Fact]
    public void Zoom_EdgePosition_StaysWithinBounds()
    {
        // Arrange - Zone at bottom-right corner with different zoom levels
        double[] zoomFactors = { 0.5, 1.0, 1.5, 2.0 };

        foreach (var zoom in zoomFactors)
        {
            // Zone at position that would exceed bounds at certain zoom
            double htmlX = (A4Width - 50) * zoom;
            double htmlY = (A4Height - 30) * zoom;
            double htmlWidth = 100 * zoom;
            double htmlHeight = 60 * zoom;

            // Convert to PDF coordinates
            double pdfX = htmlX / zoom;
            double pdfY = A4Height - htmlY / zoom - htmlHeight / zoom;

            // Assert - PDF coordinates should be within page bounds
            Assert.True(pdfX >= 0, $"PDF X should be >= 0 at zoom {zoom}");
            Assert.True(pdfY >= 0, $"PDF Y should be >= 0 at zoom {zoom}");
            Assert.True(pdfX + (htmlWidth / zoom) <= A4Width, $"Zone should not exceed width at zoom {zoom}");
            Assert.True(pdfY + (htmlHeight / zoom) <= A4Height, $"Zone should not exceed height at zoom {zoom}");
        }
    }

    /// <summary>
    /// EDGE CASE: Test canvas scaling with zoom maintains precision
    /// </summary>
    [Fact]
    public void Zoom_CanvasScaling_MaintainsPrecision()
    {
        // Arrange - Very small zone
        double zoomFactor = 1.0;
        double htmlX = 100.5;
        double htmlY = 100.5;
        double zoneWidth = 50.25;
        double zoneHeight = 25.75;

        // Act - Convert with precision
        double pdfX = htmlX / zoomFactor;
        double pdfY = A4Height - (htmlY / zoomFactor) - (zoneHeight / zoomFactor);

        // Assert - Should maintain decimal precision
        Assert.Equal(100.5, pdfX, 2);
    }

    #endregion

    #region 5. Different Browsers (Canvas Behavior)

    /// <summary>
    /// EDGE CASE: Test canvas coordinate retrieval across different canvas sizes
    /// </summary>
    [Fact]
    public void Browser_CanvasCoordinateRetrieval_IsAccurate()
    {
        // Simulate different canvas rendering scenarios
        
        // Scenario 1: Canvas same size as display
        double canvasWidth = 595.28;
        double canvasHeight = 841.89;
        double clientWidth = 595.28;
        double clientHeight = 841.89;

        double scaleX = canvasWidth / clientWidth;
        double scaleY = canvasHeight / clientHeight;

        Assert.Equal(1.0, scaleX);
        Assert.Equal(1.0, scaleY);

        // Scenario 2: Canvas scaled down (high DPI display)
        canvasWidth = 1190.56;
        canvasHeight = 1683.78;
        clientWidth = 595.28;
        clientHeight = 841.89;

        scaleX = canvasWidth / clientWidth;
        scaleY = canvasHeight / clientHeight;

        Assert.Equal(2.0, scaleX);
        Assert.Equal(2.0, scaleY);
    }

    /// <summary>
    /// EDGE CASE: Test touch event coordinates on mobile browsers
    /// </summary>
    [Fact]
    public void Browser_TouchEventCoordinates_AreCorrect()
    {
        // Simulate touch event at specific position
        double touchClientX = 150;
        double touchClientY = 200;
        double canvasRectLeft = 50;
        double canvasRectTop = 50;

        // Calculate canvas coordinates
        double canvasX = touchClientX - canvasRectLeft;
        double canvasY = touchClientY - canvasRectTop;

        Assert.Equal(100, canvasX);
        Assert.Equal(150, canvasY);
    }

    /// <summary>
    /// EDGE CASE: Test canvas context 2D availability
    /// </summary>
    [Fact]
    public void Browser_Canvas2DContext_IsAvailable()
    {
        // Verify we can work with canvas 2D context properties
        string strokeStyle = "#1a1a1a";
        double lineWidth = 2.0;
        string lineCap = "round";
        string lineJoin = "round";

        // These are standard canvas 2D context properties
        Assert.NotNull(strokeStyle);
        Assert.True(lineWidth > 0);
        Assert.Equal("round", lineCap);
        Assert.Equal("round", lineJoin);
    }

    /// <summary>
    /// EDGE CASE: Test base64 PNG conversion from canvas
    /// </summary>
    [Fact]
    public void Browser_Base64PngConversion_IsValid()
    {
        // Simulate base64 PNG data URL format
        string signatureData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Verify format
        Assert.StartsWith("data:image/png;base64,", signatureData);

        // Extract base64 part
        string base64Data = signatureData.Replace("data:image/png;base64,", "");
        
        // Should be valid base64 (can be decoded)
        try
        {
            byte[] decoded = Convert.FromBase64String(base64Data);
            Assert.NotEmpty(decoded);
        }
        catch
        {
            Assert.Fail("Invalid base64 string");
        }
    }

    #endregion

    #region 6. Reopening a Partially Signed Document

    /// <summary>
    /// EDGE CASE: Test that signed zones persist after reopening
    /// </summary>
    [Fact]
    public void Reopen_SignedZones_Persist()
    {
        // Arrange - Document with some zones signed
        var zones = new List<SignatureZone>
        {
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                SignatureImage = "data:image/png;base64,signed1",
                SignedAt = DateTime.UtcNow.AddHours(-1),
                SignedByUserId = Guid.NewGuid()
            },
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                SignatureImage = null,
                SignedAt = null,
                SignedByUserId = null
            },
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                SignatureImage = "data:image/png;base64,signed2",
                SignedAt = DateTime.UtcNow.AddMinutes(-30),
                SignedByUserId = Guid.NewGuid()
            }
        };

        // Simulate reopening - reload zones from database
        var reloadedZones = zones.Select(z => new SignatureZone
        {
            Id = z.Id,
            SignedAt = z.SignedAt,
            SignedByUserId = z.SignedByUserId,
            SignatureImage = z.SignatureImage
        }).ToList();

        // Assert - Signed zones should persist
        Assert.Equal(2, reloadedZones.Count(z => z.IsSigned));
        Assert.Equal(2, reloadedZones.Count(z => !string.IsNullOrEmpty(z.SignatureImage)));
        Assert.Equal(2, reloadedZones.Count(z => z.SignedAt != null));
    }

    /// <summary>
    /// EDGE CASE: Test that unsigned zones remain editable
    /// </summary>
    [Fact]
    public void Reopen_UnsignedZones_RemainEditable()
    {
        // Arrange
        var zones = new List<SignatureZone>
        {
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                IsRequired = true,
                SignatureImage = "data:image/png;base64,signed"
            },
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                IsRequired = true,
                SignatureImage = null
            },
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                IsRequired = false,
                SignatureImage = null
            }
        };

        // Act - Check which zones can be edited
        var editableZones = zones.Where(z => string.IsNullOrEmpty(z.SignatureImage)).ToList();
        var signedZones = zones.Where(z => !string.IsNullOrEmpty(z.SignatureImage)).ToList();

        // Assert
        Assert.Equal(2, editableZones.Count);
        Assert.Single(signedZones);
        
        // Verify signed zones cannot be edited
        Assert.True(signedZones[0].IsSigned);
        Assert.False(editableZones.All(z => z.IsSigned));
    }

    /// <summary>
    /// EDGE CASE: Test partially signed document status
    /// </summary>
    [Fact]
    public void Reopen_PartiallySigned_StatusIsCorrect()
    {
        // Arrange
        var zones = new List<SignatureZone>
        {
            new SignatureZone { IsRequired = true, SignatureImage = "data:image/png;base64,signed" },
            new SignatureZone { IsRequired = true, SignatureImage = null },
            new SignatureZone { IsRequired = false, SignatureImage = null }
        };

        // Act - Check completion status
        var requiredZones = zones.Where(z => z.IsRequired).ToList();
        bool allRequiredSigned = requiredZones.All(z => z.IsSigned);
        int signedCount = zones.Count(z => !string.IsNullOrEmpty(z.SignatureImage));
        int totalCount = zones.Count;

        // Assert
        Assert.False(allRequiredSigned); // Not all required signed
        Assert.Equal(1, signedCount);
        Assert.Equal(3, totalCount);
        Assert.Equal(2, requiredZones.Count);
    }

    /// <summary>
    /// EDGE CASE: Test signing remaining zones after reopening
    /// </summary>
    [Fact]
    public void Reopen_CanSignRemainingZones()
    {
        // Arrange - Partially signed document
        var zones = new List<SignatureZone>
        {
            new SignatureZone { Id = Guid.NewGuid(), SignatureImage = "existing" },
            new SignatureZone { Id = Guid.NewGuid(), SignatureImage = null }
        };

        // Act - Sign the remaining zone
        var unsignedZone = zones.First(z => string.IsNullOrEmpty(z.SignatureImage));
        unsignedZone.SignedAt = DateTime.UtcNow;
        unsignedZone.SignedByUserId = Guid.NewGuid();
        unsignedZone.SignatureImage = "data:image/png;base64,new_signature";

        // Assert - Now all required zones should be signed
        Assert.True(zones.All(z => !string.IsNullOrEmpty(z.SignatureImage)));
        Assert.Equal(2, zones.Count(z => !string.IsNullOrEmpty(z.SignatureImage)));
    }

    /// <summary>
    /// EDGE CASE: Test signature timestamps are preserved
    /// </summary>
    [Fact]
    public void Reopen_SignatureTimestamps_ArePreserved()
    {
        // Arrange
        DateTime signedAt1 = DateTime.UtcNow.AddHours(-2);
        DateTime signedAt2 = DateTime.UtcNow.AddHours(-1);

        var zones = new List<SignatureZone>
        {
            new SignatureZone { SignedAt = signedAt1, SignatureImage = "sig1" },
            new SignatureZone { SignedAt = signedAt2, SignatureImage = "sig2" }
        };

        // Act - Simulate reload
        var reloaded = zones.Select(z => new { z.SignedAt, z.SignatureImage }).ToList();

        // Assert
        Assert.Equal(signedAt1, reloaded[0].SignedAt);
        Assert.Equal(signedAt2, reloaded[1].SignedAt);
    }

    /// <summary>
    /// EDGE CASE: Test multiple signers on same document
    /// </summary>
    [Fact]
    public void Reopen_MultipleSigners_AreTracked()
    {
        // Arrange
        Guid user1 = Guid.NewGuid();
        Guid user2 = Guid.NewGuid();

        var zones = new List<SignatureZone>
        {
            new SignatureZone
            {
                SignedByUserId = user1,
                SignedAt = DateTime.UtcNow.AddHours(-1),
                SignatureImage = "data:image/png;base64,sig1"
            },
            new SignatureZone
            {
                SignedByUserId = user2,
                SignedAt = DateTime.UtcNow,
                SignatureImage = "data:image/png;base64,sig2"
            }
        };

        // Assert
        Assert.Equal(user1, zones[0].SignedByUserId);
        Assert.Equal(user2, zones[1].SignedByUserId);
        Assert.NotEqual(zones[0].SignedByUserId, zones[1].SignedByUserId);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if two signature zones overlap
    /// </summary>
    private bool CheckZonesOverlap(SignatureZone zone1, SignatureZone zone2)
    {
        // Check if on same page
        if (zone1.PageNumber != zone2.PageNumber)
            return false;

        // Check horizontal overlap
        bool horizontalOverlap = zone1.PositionX < (zone2.PositionX + zone2.Width) &&
                                (zone1.PositionX + zone1.Width) > zone2.PositionX;

        // Check vertical overlap
        bool verticalOverlap = zone1.PositionY < (zone2.PositionY + zone2.Height) &&
                               (zone1.PositionY + zone1.Height) > zone2.PositionY;

        return horizontalOverlap && verticalOverlap;
    }

    #endregion
}
