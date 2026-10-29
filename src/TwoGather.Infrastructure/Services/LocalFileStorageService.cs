using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Interfaces;

namespace TwoGather.Infrastructure.Services;

/// <summary>
/// Development fallback used when R2 is not configured: writes images to wwwroot/uploads
/// and serves them from the API itself. Not suitable for Render (its disk is ephemeral).
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    public const string UploadsFolder = "uploads";
    public static readonly string UploadsRelativePath = Path.Combine("wwwroot", UploadsFolder);

    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LocalFileStorageService> logger)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string UploadsRoot => Path.Combine(_environment.ContentRootPath, UploadsRelativePath);

    private string PublicBaseUrl
    {
        get
        {
            var request = _httpContextAccessor.HttpContext?.Request
                ?? throw new InvalidOperationException("Local image storage requires an HTTP request context.");
            return $"{request.Scheme}://{request.Host}/{UploadsFolder}";
        }
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string contentType, string keyPrefix, CancellationToken ct)
    {
        var key = ImageKeys.Create(keyPrefix, contentType);
        var path = Path.Combine(UploadsRoot, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = File.Create(path);
        await imageStream.CopyToAsync(file, ct);

        _logger.LogInformation("Stored image {Key} on local disk", key);
        return $"{PublicBaseUrl}/{key}";
    }

    public Task DeleteImageAsync(string imageUrl, CancellationToken ct)
    {
        var key = ImageKeys.FromUrl(imageUrl, PublicBaseUrl);
        if (key is null) return Task.CompletedTask;

        var root = Path.GetFullPath(UploadsRoot);
        var path = Path.GetFullPath(Path.Combine(root, key));
        if (path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}
