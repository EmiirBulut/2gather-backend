namespace TwoGather.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadImageAsync(Stream imageStream, string fileName, CancellationToken ct);
}
