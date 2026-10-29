using System.Security.Cryptography;
using System.Text;
using TwoGather.Application.Common.Webhooks;

namespace TwoGather.Application.Tests.Common;

public class SvixSignatureVerifierTests
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("test-signing-key-0123456789");
    private static readonly string Secret = "whsec_" + Convert.ToBase64String(Key);
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
    private const string Body = """{"type":"email.delivered","data":{"email_id":"abc"}}""";
    private const string MessageId = "msg_2abc";

    private static string Sign(string id, string timestamp, string body, byte[]? key = null)
        => "v1," + Convert.ToBase64String(HMACSHA256.HashData(key ?? Key, Encoding.UTF8.GetBytes($"{id}.{timestamp}.{body}")));

    private static string Timestamp(DateTimeOffset at) => at.ToUnixTimeSeconds().ToString();

    [Fact]
    public void IsValid_CorrectSignature_ReturnsTrue()
    {
        var ts = Timestamp(Now);
        Assert.True(SvixSignatureVerifier.IsValid(Secret, MessageId, ts, Sign(MessageId, ts, Body), Body, Now));
    }

    [Fact]
    public void IsValid_OneOfSeveralSignaturesMatches_ReturnsTrue()
    {
        var ts = Timestamp(Now);
        var header = $"v1,{Convert.ToBase64String(new byte[32])} {Sign(MessageId, ts, Body)}";
        Assert.True(SvixSignatureVerifier.IsValid(Secret, MessageId, ts, header, Body, Now));
    }

    [Fact]
    public void IsValid_TamperedBody_ReturnsFalse()
    {
        var ts = Timestamp(Now);
        Assert.False(SvixSignatureVerifier.IsValid(Secret, MessageId, ts, Sign(MessageId, ts, Body), Body + " ", Now));
    }

    [Fact]
    public void IsValid_WrongSecret_ReturnsFalse()
    {
        var ts = Timestamp(Now);
        var header = Sign(MessageId, ts, Body, Encoding.UTF8.GetBytes("another-key"));
        Assert.False(SvixSignatureVerifier.IsValid(Secret, MessageId, ts, header, Body, Now));
    }

    [Fact]
    public void IsValid_TimestampTooOld_ReturnsFalse()
    {
        var ts = Timestamp(Now.AddMinutes(-10));
        Assert.False(SvixSignatureVerifier.IsValid(Secret, MessageId, ts, Sign(MessageId, ts, Body), Body, Now));
    }

    [Theory]
    [InlineData(null, "1", "v1,x")]
    [InlineData("id", null, "v1,x")]
    [InlineData("id", "1", null)]
    [InlineData("id", "not-a-number", "v1,x")]
    public void IsValid_MissingOrMalformedHeaders_ReturnsFalse(string? id, string? ts, string? header)
    {
        Assert.False(SvixSignatureVerifier.IsValid(Secret, id, ts, header, Body, Now));
    }
}
