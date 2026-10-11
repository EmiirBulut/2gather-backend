using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TwoGather.Application.Common.Webhooks;
using TwoGather.Application.Features.Members.Commands.ProcessInviteEmailEvent;
using TwoGather.Infrastructure.Settings;

namespace TwoGather.Api.Controllers;

[ApiController]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ResendOptions _resendOptions;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IMediator mediator, IOptions<ResendOptions> resendOptions, ILogger<WebhooksController> logger)
    {
        _mediator = mediator;
        _resendOptions = resendOptions.Value;
        _logger = logger;
    }

    // Resend delivery events (Svix-signed). Configure in Resend → Webhooks with Resend__WebhookSecret.
    [HttpPost("api/webhooks/resend")]
    public async Task<IActionResult> Resend(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_resendOptions.WebhookSecret))
            return NotFound();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var isValid = SvixSignatureVerifier.IsValid(
            _resendOptions.WebhookSecret,
            Request.Headers["svix-id"],
            Request.Headers["svix-timestamp"],
            Request.Headers["svix-signature"],
            body,
            DateTimeOffset.UtcNow);
        if (!isValid)
        {
            _logger.LogWarning("Rejected Resend webhook with an invalid signature");
            return Unauthorized();
        }

        var payload = JsonSerializer.Deserialize<ResendWebhookPayload>(body, JsonOptions);
        var emailId = payload?.Data?.EmailId;
        if (payload?.Type is null || string.IsNullOrWhiteSpace(emailId))
            return Ok();

        await _mediator.Send(
            new ProcessInviteEmailEventCommand(payload.Type, emailId, (payload.CreatedAt ?? DateTimeOffset.UtcNow).UtcDateTime),
            cancellationToken);
        return Ok();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record ResendWebhookPayload(
        string? Type,
        [property: System.Text.Json.Serialization.JsonPropertyName("created_at")] DateTimeOffset? CreatedAt,
        ResendWebhookData? Data);

    private sealed record ResendWebhookData(
        [property: System.Text.Json.Serialization.JsonPropertyName("email_id")] string? EmailId);
}
