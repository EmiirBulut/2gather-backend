namespace TwoGather.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>Stores the image under <paramref name="keyPrefix"/> and returns its public URL.</summary>
    Task<string> UploadImageAsync(Stream imageStream, string contentType, string keyPrefix, CancellationToken ct);

    /// <summary>Deletes an image previously returned by <see cref="UploadImageAsync"/>. Unknown URLs are ignored.</summary>
    Task DeleteImageAsync(string imageUrl, CancellationToken ct);
}
