using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Interfaces;

namespace TwoGather.Infrastructure.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendInviteEmailAsync(string toEmail, string listName, string inviteToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Invite sent to {Email} for list '{ListName}'. Token: {Token}",
            toEmail, listName, inviteToken);

        return Task.CompletedTask;
    }
}
