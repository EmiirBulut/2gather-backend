using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;

namespace TwoGather.Infrastructure.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendInviteAsync(string toEmail, string listName, string inviterName, string inviteToken, MemberRole role, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Invite sent to {Email} for list '{ListName}' by {Inviter} ({Role}). Token: {Token}",
            toEmail, listName, inviterName, role, inviteToken);

        return Task.CompletedTask;
    }
}
