using TwoGather.Domain.Enums;

namespace TwoGather.Application.Common.Interfaces;

public interface IEmailService
{
    /// <summary>Sends the invite email and returns the provider's message id. Throws when sending fails.</summary>
    Task<string> SendInviteAsync(string toEmail, string listName, string inviterName, string inviteToken, MemberRole role, CancellationToken cancellationToken = default);
}
