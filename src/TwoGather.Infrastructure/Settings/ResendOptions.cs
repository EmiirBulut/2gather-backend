namespace TwoGather.Infrastructure.Settings;

public class ResendOptions
{
    public const string SectionName = "Resend";
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string AppBaseUrl { get; set; } = string.Empty;
    /// <summary>Signing secret of the Resend webhook endpoint (whsec_...).</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
