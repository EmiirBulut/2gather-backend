namespace TwoGather.Infrastructure.Services;

internal static class ImageKeys
{
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    public static string Create(string keyPrefix, string contentType)
    {
        var extension = Extensions.GetValueOrDefault(contentType, ".bin");
        return $"{keyPrefix.Trim('/')}/{Guid.NewGuid():N}{extension}";
    }

    /// <summary>Returns the object key when <paramref name="url"/> lives under <paramref name="baseUrl"/>.</summary>
    public static string? FromUrl(string url, string baseUrl)
    {
        var prefix = baseUrl.TrimEnd('/') + "/";
        return url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? url[prefix.Length..] : null;
    }
}
