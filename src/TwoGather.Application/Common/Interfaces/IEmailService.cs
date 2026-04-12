namespace TwoGather.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendInviteEmailAsync(string toEmail, string listName, string inviteToken, CancellationToken cancellationToken = default);
}
