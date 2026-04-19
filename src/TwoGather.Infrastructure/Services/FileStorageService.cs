using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Interfaces;

namespace TwoGather.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(ILogger<FileStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadImageAsync(Stream imageStream, string fileName, CancellationToken ct)
    {
        _logger.LogInformation("FileStorageService stub: UploadImageAsync called for {FileName}", fileName);
        return Task.FromResult($"https://placeholder.2gather.app/images/{fileName}");
    }
}
