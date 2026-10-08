using ConnectHub.BLL.Interfaces.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _baseStoragePath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _logger = logger;
        var customPath = configuration["FileStorage:BasePath"];
        _baseStoragePath = !string.IsNullOrWhiteSpace(customPath)
            ? customPath
            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName, string folder)
    {
        if (stream == null || stream.Length == 0)
            throw new ArgumentException("Stream cannot be null or empty.", nameof(stream));

        var targetDirectory = Path.Combine(_baseStoragePath, folder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var fullPath = Path.Combine(targetDirectory, fileName);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
        await stream.CopyToAsync(fileStream);

        var relativePath = $"{folder.TrimEnd('/')}/{fileName}".Replace('\\', '/');
        _logger.LogInformation("File saved successfully to relative path: {RelativePath}", relativePath);

        return relativePath;
    }

    public Task DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.CompletedTask;

        var fullPath = Path.Combine(_baseStoragePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted successfully at relative path: {RelativePath}", relativePath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> GetFileStreamAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.FromResult<Stream?>(null);

        var fullPath = Path.Combine(_baseStoragePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found at physical path: {FullPath}", fullPath);
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }
}
