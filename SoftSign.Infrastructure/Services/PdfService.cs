using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;

namespace SoftSign.Infrastructure.Services;

public class PdfService : IPdfService
{
    public PdfService()
    {
        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ConvertToPdfAsync(Stream fileStream, string contentType)
    {
        // For now, return a placeholder PDF
        // In a production environment, you would use libraries like:
        // - Spire.Doc for Word documents
        // - ClosedXML for Excel documents
        // - PdfSharpCore for existing PDF manipulation
        
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(c => ComposeHeader(c));
                page.Content().Element(c => ComposeContent(c, contentType));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Document Conversion")
                    .FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                column.Item().Text("Converted from original format")
                    .FontSize(10).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeContent(IContainer container, string contentType)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Item().Text($"Source Content Type: {contentType}");
            column.Item().PaddingTop(10).Text(
                "This is a placeholder PDF document. " +
                "In a production environment, this would contain the actual converted content " +
                "from the uploaded document (Word, Excel, PowerPoint, etc.).");
            column.Item().PaddingTop(20).Text(
                "The document conversion feature requires additional libraries for full functionality: " +
                "- Spire.Doc for Word documents " +
                "- ClosedXML for Excel documents " +
                "- PdfSharpCore for PDF manipulation");
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    }

    /// <summary>
    /// Applies signatures from SignatureZone entities to a PDF document.
    /// Uses the SignatureImage stored in each zone.
    /// </summary>
    public async Task<byte[]> ApplySignaturesFromZonesAsync(byte[] pdfBytes, IEnumerable<SignatureZone> zones)
    {
        try
        {
            Console.WriteLine($"=== ApplySignaturesFromZonesAsync DEBUG ===");
            
            var zoneList = zones.Where(z => z.IsSigned && !string.IsNullOrEmpty(z.SignatureImage)).ToList();
            Console.WriteLine($"Found {zoneList.Count} signed zones to apply");
            
            if (zoneList.Count == 0)
            {
                Console.WriteLine("No signed zones found, returning original PDF");
                return pdfBytes;
            }

            using var inputStream = new MemoryStream(pdfBytes);
            using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);
            
            Console.WriteLine($"PDF has {document.PageCount} pages");
            
            foreach (var zone in zoneList)
            {
                Console.WriteLine($"Applying signature to zone: Page={zone.PageNumber}, X={zone.PositionX}, Y={zone.PositionY}, W={zone.Width}, H={zone.Height}");
                
                // Ensure page number is valid
                var pageNumber = zone.PageNumber;
                if (pageNumber < 1) pageNumber = 1;
                if (pageNumber > document.PageCount) pageNumber = document.PageCount;
                
                var page = document.Pages[pageNumber - 1];
                var pageWidth = page.Width.Point;
                var pageHeight = page.Height.Point;
                
                // Zone coordinates are stored as percentages (0-1 range)
                // Convert to PDF pixels
                var percentX = zone.PositionX;
                var percentY = zone.PositionY;
                var percentWidth = zone.Width;
                var percentHeight = zone.Height;
                
                Console.WriteLine($"=== PDF COORDINATE DEBUG ===");
                Console.WriteLine($"Page dimensions: {pageWidth} x {pageHeight} points");
                Console.WriteLine($"Zone percentages: X={percentX}, Y={percentY}, W={percentWidth}, H={percentHeight}");
                
                // Convert percentages to pixels
                var x = percentX * pageWidth;
                var y = percentY * pageHeight;
                var width = percentWidth * pageWidth;
                var height = percentHeight * pageHeight;
                
                Console.WriteLine($"Zone pixels (before Y flip): X={x}, Y={y}, W={width}, H={height}");
                
                // RULE 5: Only flip Y when stamping PDF
                // PDF uses bottom-left origin, but browser uses top-left
                // Convert from browser coordinates (top-left) to PDF coordinates (bottom-left)
                var pdfY = pageHeight - y - height;
                
                Console.WriteLine($"Zone coordinates after Y flip (PDF bottom-left): X={x}, Y={pdfY}, W={width}, H={height}");
                Console.WriteLine($"===========================");
                
                // Validate Y position - ensure signature stays within page bounds
                // In PDF coordinates (bottom-left origin), Y + height should not exceed page height
                if (pdfY + height > pageHeight)
                {
                    Console.WriteLine($"WARNING: Y position {pdfY} + height {height} exceeds page height {pageHeight}");
                    pdfY = pageHeight - height;
                    Console.WriteLine($"Adjusted Y to: {pdfY}");
                }
                
                // Also ensure Y is not negative
                if (pdfY < 0)
                {
                    Console.WriteLine($"WARNING: Y position {pdfY} is negative, setting to 0");
                    pdfY = 0;
                }
                
                // Also validate X position
                if (x < 0)
                {
                    Console.WriteLine($"WARNING: X position {x} is negative, setting to 0");
                    x = 0;
                }
                if (x + width > pageWidth)
                {
                    Console.WriteLine($"WARNING: X position {x} + width {width} exceeds page width {pageWidth}");
                    x = pageWidth - width;
                }
                
                Console.WriteLine($"Final placement: X={x}, Y={pdfY}, W={width}, H={height}");
                Console.WriteLine($"===========================");
                
                // Decode and apply signature image
                var signatureImageBase64 = zone.SignatureImage!;
                var signatureImageBytes = Convert.FromBase64String(signatureImageBase64.Replace("data:image/png;base64,", ""));
                
                using var image = XImage.FromStream(() => new MemoryStream(signatureImageBytes));
                using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                
                gfx.DrawImage(image, x, pdfY, width, height);
                Console.WriteLine($"Signature applied to zone {zone.Id}");
            }
            
            using var outputStream = new MemoryStream();
            document.Save(outputStream);
            Console.WriteLine("All signatures applied successfully");
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== ERROR in ApplySignaturesFromZonesAsync ===");
            Console.WriteLine($"Error: {ex.Message}");
            return pdfBytes;
        }
    }

    public async Task<byte[]> ApplySignatureOverlayAsync(byte[] pdfBytes, string signatureImageBase64, int pageNumber, double x, double y, double width, double height)
    {
        try
        {
            Console.WriteLine($"=== PDF SERVICE DEBUG ===");
            Console.WriteLine($"ApplySignatureOverlayAsync: Page={pageNumber}, X={x}, Y={y}, Width={width}, Height={height}");
            
            // Decode base64 image
            var signatureImageBytes = Convert.FromBase64String(signatureImageBase64.Replace("data:image/png;base64,", ""));
            Console.WriteLine($"Signature image size: {signatureImageBytes.Length} bytes");
            
            using var inputStream = new MemoryStream(pdfBytes);
            using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);
            
            Console.WriteLine($"PDF has {document.PageCount} pages");
            
            // Ensure page number is valid
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > document.PageCount) pageNumber = document.PageCount;
            
            var page = document.Pages[pageNumber - 1];
            var pageWidth = page.Width.Point;
            var pageHeight = page.Height.Point;
            Console.WriteLine($"Page size: Width={pageWidth}, Height={pageHeight}");
            
            // Input coordinates are in percentages (0-1 range)
            // Convert to PDF pixels
            var pixelX = x * pageWidth;
            var pixelY = y * pageHeight;
            var pixelWidth = width * pageWidth;
            var pixelHeight = height * pageHeight;
            
            Console.WriteLine($"Input percentages: X={x}, Y={y}, W={width}, H={height}");
            Console.WriteLine($"Converted to pixels: X={pixelX}, Y={pixelY}, W={pixelWidth}, H={pixelHeight}");
            
            // RULE 5: Only flip Y when stamping PDF
            // PDF uses bottom-left origin, but browser uses top-left
            var pdfY = pageHeight - pixelY - pixelHeight;
            Console.WriteLine($"Y after flip (PDF bottom-left): {pdfY}");
            
            // Validate Y position - for PDF bottom-left coords, Y + height should NOT exceed page height
            if (pdfY + pixelHeight > pageHeight)
            {
                Console.WriteLine($"WARNING: Y position {pdfY} + height {pixelHeight} = {pdfY + pixelHeight} exceeds page height {pageHeight}");
                Console.WriteLine($"Adjusting Y to fit on page...");
                pdfY = pageHeight - pixelHeight;
                Console.WriteLine($"New Y position: {pdfY}");
            }
            
            // Validate coordinates are within page bounds
            if (pixelX < 0 || pdfY < 0 || pixelX + pixelWidth > pageWidth || pdfY + pixelHeight > pageHeight)
            {
                Console.WriteLine($"WARNING: Coordinates may be out of bounds! Page: {pageWidth}x{pageHeight}");
            }
            
            // Create XImage from the signature bytes
            using var imageStream = new MemoryStream(signatureImageBytes);
            using var image = XImage.FromStream(() => new MemoryStream(signatureImageBytes));
            Console.WriteLine($"Image loaded successfully");
            
            // Calculate position - PdfSharp uses points
            using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            
            // Draw the image on the page
            Console.WriteLine($"Drawing image at: X={pixelX}, Y={pdfY}, W={pixelWidth}, H={pixelHeight}");
            gfx.DrawImage(image, pixelX, pdfY, pixelWidth, pixelHeight);
            Console.WriteLine($"Image drawn successfully");
            
            using var outputStream = new MemoryStream();
            document.Save(outputStream);
            Console.WriteLine("Signature applied successfully - returning signed PDF");
            Console.WriteLine($"Output PDF size: {outputStream.Length} bytes");
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            // Log the error but return original PDF to not block the workflow
            Console.WriteLine($"=== ERROR in ApplySignatureOverlayAsync ===");
            Console.WriteLine($"Error applying signature overlay: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return pdfBytes;
        }
    }

    public Task<int> GetPageCountAsync(byte[] pdfBytes)
    {
        try
        {
            using var ms = new MemoryStream(pdfBytes);
            using var document = PdfReader.Open(ms, PdfDocumentOpenMode.InformationOnly);
            return Task.FromResult(document.PageCount);
        }
        catch
        {
            return Task.FromResult(1);
        }
    }

    public Task<(double Width, double Height)> GetPageSizeAsync(byte[] pdfBytes, int pageNumber = 1)
    {
        try
        {
            using var ms = new MemoryStream(pdfBytes);
            using var document = PdfReader.Open(ms, PdfDocumentOpenMode.InformationOnly);
            
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > document.PageCount) pageNumber = document.PageCount;
            
            var page = document.Pages[pageNumber - 1];
            return Task.FromResult((page.Width.Point, page.Height.Point));
        }
        catch
        {
            // Default to A4 size
            return Task.FromResult((595.28, 841.89));
        }
    }

    public async Task<byte[]> MergePdfsAsync(IEnumerable<byte[]> pdfFiles)
    {
        // In production, use PdfSharpCore to merge PDFs
        // using (var result = new PdfDocument())
        // {
        //     foreach (var pdfBytes in pdfFiles)
        //     {
        //         using (var ms = new MemoryStream(pdfBytes))
        //         using (var doc = PdfReader.Open(ms, PdfDocumentOpenMode.Import))
        //         {
        //             for (int i = 0; i < doc.PageCount; i++)
        //             {
        //                 result.AddPage(doc.Pages[i]);
        //             }
        //         }
        //     }
        //     return result.Save();
        // }
        
        // Placeholder - return first PDF
        return pdfFiles.FirstOrDefault() ?? Array.Empty<byte>();
    }
}
