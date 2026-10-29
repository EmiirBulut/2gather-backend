using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Infrastructure.Settings;

namespace TwoGather.Infrastructure.Services;

/// <summary>Stores images in a Cloudflare R2 bucket through its S3-compatible API.</summary>
public sealed class R2FileStorageService : IFileStorageService, IDisposable
{
    private readonly R2Options _options;
    private readonly ILogger<R2FileStorageService> _logger;
    private readonly AmazonS3Client _client;

    public R2FileStorageService(IOptions<R2Options> options, ILogger<R2FileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
            AuthenticationRegion = "auto",
            ForcePathStyle = true,
            // R2 does not support the SDK's default streaming checksums
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey), config);
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string contentType, string keyPrefix, CancellationToken ct)
    {
        var key = ImageKeys.Create(keyPrefix, contentType);

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = imageStream,
            ContentType = contentType,
            DisablePayloadSigning = true,
            Headers = { CacheControl = "public, max-age=31536000, immutable" },
        }, ct);

        _logger.LogInformation("Uploaded image {Key} to R2", key);
        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    public async Task DeleteImageAsync(string imageUrl, CancellationToken ct)
    {
        var key = ImageKeys.FromUrl(imageUrl, _options.PublicBaseUrl);
        if (key is null) return;

        await _client.DeleteObjectAsync(_options.BucketName, key, ct);
        _logger.LogInformation("Deleted image {Key} from R2", key);
    }

    public void Dispose() => _client.Dispose();
}
