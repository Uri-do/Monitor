namespace EnterpriseApp.Worker.Services;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to storage
    /// </summary>
    Task<string> SaveFileAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a file to storage with metadata
    /// </summary>
    Task<string> SaveFileAsync(string fileName, byte[] content, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a file from storage
    /// </summary>
    Task<byte[]> GetFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata
    /// </summary>
    Task<FileMetadata?> GetFileMetadataAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a directory
    /// </summary>
    Task<IEnumerable<string>> ListFilesAsync(string? directoryPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a download URL for a file (for cloud storage)
    /// </summary>
    Task<string?> GetDownloadUrlAsync(string filePath, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old files based on retention policy
    /// </summary>
    Task<int> CleanupOldFilesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
}

/// <summary>
/// File metadata information
/// </summary>
public class FileMetadata
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Content type
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Local file storage implementation
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the LocalFileStorageService
    /// </summary>
    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _basePath = _configuration.GetValue<string>("FileStorage:LocalPath") ?? Path.Combine(Path.GetTempPath(), "EnterpriseApp", "Files");
        
        // Ensure base directory exists
        Directory.CreateDirectory(_basePath);
    }

    /// <summary>
    /// Saves a file to local storage
    /// </summary>
    public async Task<string> SaveFileAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        return await SaveFileAsync(fileName, content, null, cancellationToken);
    }

    /// <summary>
    /// Saves a file to local storage with metadata
    /// </summary>
    public async Task<string> SaveFileAsync(string fileName, byte[] content, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create subdirectory based on date
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var directoryPath = Path.Combine(_basePath, dateFolder);
            Directory.CreateDirectory(directoryPath);

            // Generate unique file name if file already exists
            var filePath = Path.Combine(directoryPath, fileName);
            var counter = 1;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            while (File.Exists(filePath))
            {
                var newFileName = $"{fileNameWithoutExtension}_{counter}{extension}";
                filePath = Path.Combine(directoryPath, newFileName);
                counter++;
            }

            // Save file
            await File.WriteAllBytesAsync(filePath, content, cancellationToken);

            // Save metadata if provided
            if (metadata != null && metadata.Any())
            {
                var metadataPath = filePath + ".metadata";
                var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
                await File.WriteAllTextAsync(metadataPath, metadataJson, cancellationToken);
            }

            var relativePath = Path.GetRelativePath(_basePath, filePath);
            _logger.LogInformation("File saved successfully: {FilePath}", relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Gets a file from local storage
    /// </summary>
    public async Task<byte[]> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from local storage
    /// </summary>
    public Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                
                // Also delete metadata file if it exists
                var metadataPath = fullPath + ".metadata";
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }

                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Checks if a file exists in local storage
    /// </summary>
    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <summary>
    /// Gets file metadata from local storage
    /// </summary>
    public async Task<FileMetadata?> GetFileMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                return null;
            }

            var fileInfo = new FileInfo(fullPath);
            var metadata = new FileMetadata
            {
                FileName = fileInfo.Name,
                FilePath = filePath,
                Size = fileInfo.Length,
                CreatedDate = fileInfo.CreationTimeUtc,
                ModifiedDate = fileInfo.LastWriteTimeUtc,
                ContentType = GetContentType(fileInfo.Extension)
            };

            // Load custom metadata if available
            var metadataPath = fullPath + ".metadata";
            if (File.Exists(metadataPath))
            {
                var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
                var customMetadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
                if (customMetadata != null)
                {
                    metadata.Metadata = customMetadata;
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file metadata: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Lists files in a directory
    /// </summary>
    public Task<IEnumerable<string>> ListFilesAsync(string? directoryPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchPath = string.IsNullOrEmpty(directoryPath) ? _basePath : Path.Combine(_basePath, directoryPath);
            
            if (!Directory.Exists(searchPath))
            {
                return Task.FromResult(Enumerable.Empty<string>());
            }

            var files = Directory.GetFiles(searchPath, "*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".metadata"))
                .Select(f => Path.GetRelativePath(_basePath, f))
                .ToList();

            return Task.FromResult<IEnumerable<string>>(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files in directory: {DirectoryPath}", directoryPath);
            return Task.FromResult(Enumerable.Empty<string>());
        }
    }

    /// <summary>
    /// Gets a download URL for a file (returns local file path)
    /// </summary>
    public Task<string?> GetDownloadUrlAsync(string filePath, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        return Task.FromResult<string?>(File.Exists(fullPath) ? fullPath : null);
    }

    /// <summary>
    /// Cleans up old files based on retention policy
    /// </summary>
    public Task<int> CleanupOldFilesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - maxAge;
            var deletedCount = 0;

            var files = Directory.GetFiles(_basePath, "*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".metadata"));

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < cutoffDate)
                    {
                        File.Delete(file);
                        
                        // Also delete metadata file if it exists
                        var metadataPath = file + ".metadata";
                        if (File.Exists(metadataPath))
                        {
                            File.Delete(metadataPath);
                        }

                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old file: {FilePath}", file);
                }
            }

            _logger.LogInformation("Cleaned up {Count} old files", deletedCount);
            return Task.FromResult(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old files");
            return Task.FromResult(0);
        }
    }

    /// <summary>
    /// Gets content type based on file extension
    /// </summary>
    private static string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".pdf" => "application/pdf",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}

/// <summary>
/// Extension methods for file storage service registration
/// </summary>
public static class FileStorageServiceExtensions
{
    /// <summary>
    /// Adds file storage services
    /// </summary>
    public static IServiceCollection AddFileStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("FileStorage:Provider", "Local");

        switch (provider.ToLower())
        {
            case "local":
                services.AddSingleton<IFileStorageService, LocalFileStorageService>();
                break;
            case "azure":
                // services.AddSingleton<IFileStorageService, AzureFileStorageService>();
                throw new NotImplementedException("Azure file storage not implemented in template");
            case "aws":
                // services.AddSingleton<IFileStorageService, AwsFileStorageService>();
                throw new NotImplementedException("AWS file storage not implemented in template");
            default:
                services.AddSingleton<IFileStorageService, LocalFileStorageService>();
                break;
        }

        return services;
    }
}
