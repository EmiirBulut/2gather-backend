using System.Security.Cryptography;
using System.Text;

namespace TwoGather.Application.Common.Webhooks;

/// <summary>
/// Verifies Svix-signed webhooks (used by Resend): the signature is
/// base64(HMAC-SHA256(secret, "{id}.{timestamp}.{body}")), sent as space-separated "v1,&lt;sig&gt;" entries.
/// </summary>
public static class SvixSignatureVerifier
{
    private const string SecretPrefix = "whsec_";
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromMinutes(5);

    public static bool IsValid(
        string secret,
        string? messageId,
        string? timestamp,
        string? signatureHeader,
        string body,
        DateTimeOffset now,
        TimeSpan? tolerance = null)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(messageId) ||
            string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        if (!long.TryParse(timestamp, out var unixSeconds))
            return false;

        var sentAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        if ((now - sentAt).Duration() > (tolerance ?? DefaultTolerance))
            return false;

        byte[] key;
        try
        {
            key = Convert.FromBase64String(secret.StartsWith(SecretPrefix, StringComparison.Ordinal)
                ? secret[SecretPrefix.Length..]
                : secret);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{messageId}.{timestamp}.{body}"));

        foreach (var entry in signatureHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(',', 2);
            if (parts.Length != 2 || parts[0] != "v1")
                continue;

            byte[] provided;
            try
            {
                provided = Convert.FromBase64String(parts[1]);
            }
            catch (FormatException)
            {
                continue;
            }

            if (CryptographicOperations.FixedTimeEquals(expected, provided))
                return true;
        }

        return false;
    }
}
