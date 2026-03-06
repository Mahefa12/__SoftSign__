using Microsoft.Extensions.Logging;
using SoftSign.Application.Interfaces;

namespace SoftSign.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(string basePath, ILogger<FileStorageService> logger)
    {
        _basePath = basePath;
        _logger = logger;
        _logger.LogInformation("FileStorageService initialized with basePath: {BasePath}", basePath);
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder = "documents")
    {
        var folderPath = Path.Combine(_basePath, folder);
        
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(folderPath, uniqueFileName);

        using var outputStream = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream);
        
        return Path.Combine(folder, uniqueFileName);
    }

    public Task<Stream?> GetFileAsync(string filePath)
    {
        // Normalize path separators for Windows/Unix compatibility
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, normalizedPath);
        
        _logger.LogDebug("GetFileAsync: Looking for file at {FullPath}", fullPath);
        
        // Check primary path first (most common case)
        if (File.Exists(fullPath))
        {
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream?>(fileStream);
        }

        // Only try alternative paths if primary path doesn't exist
        // These paths are kept for backward compatibility
        string[] altPaths;
        try
        {
            altPaths = new[]
            {
                Path.Combine(_basePath, "wwwroot", normalizedPath),
                Path.Combine(Directory.GetParent(_basePath)?.FullName ?? _basePath, "wwwroot", "uploads", normalizedPath)
            };
        }
        catch
        {
            // If parent directory access fails, just return null
            return Task.FromResult<Stream?>(null);
        }
        
        foreach (var altPath in altPaths)
        {
            var absoluteAltPath = Path.GetFullPath(altPath);
            if (File.Exists(absoluteAltPath))
            {
                _logger.LogDebug("File found at alternative path: {AltPath}", absoluteAltPath);
                var altStream = new FileStream(absoluteAltPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return Task.FromResult<Stream?>(altStream);
            }
        }
        
        _logger.LogWarning("File not found: {FullPath}", fullPath);
        return Task.FromResult<Stream?>(null);
    }

    public Task DeleteFileAsync(string filePath)
    {
        // Normalize path separators for Windows/Unix compatibility
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, normalizedPath);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        // Normalize path separators for Windows/Unix compatibility
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, normalizedPath);
        
        return Task.FromResult(File.Exists(fullPath));
    }

    public string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
