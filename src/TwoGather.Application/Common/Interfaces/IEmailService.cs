using TwoGather.Domain.Enums;

namespace TwoGather.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendInviteAsync(string toEmail, string listName, string inviterName, string inviteToken, MemberRole role, CancellationToken cancellationToken = default);
}
