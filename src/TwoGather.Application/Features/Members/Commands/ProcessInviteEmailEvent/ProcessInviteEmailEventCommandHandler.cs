using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Members.Commands.ProcessInviteEmailEvent;

public class ProcessInviteEmailEventCommandHandler : IRequestHandler<ProcessInviteEmailEventCommand>
{
    private readonly IListInviteRepository _inviteRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProcessInviteEmailEventCommandHandler> _logger;

    public ProcessInviteEmailEventCommandHandler(
        IListInviteRepository inviteRepository,
        INotificationService notificationService,
        ILogger<ProcessInviteEmailEventCommandHandler> logger)
    {
        _inviteRepository = inviteRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(ProcessInviteEmailEventCommand request, CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByProviderMessageIdAsync(request.ProviderMessageId, cancellationToken);
        if (invite is null)
        {
            // Not an invite email, or an event for an email that has since been re-sent
            _logger.LogInformation("Ignoring {EventType} for unknown message {MessageId}", request.EventType, request.ProviderMessageId);
            return;
        }

        var nextStatus = NextStatus(invite.EmailStatus, request.EventType);
        if (nextStatus is null || nextStatus == invite.EmailStatus)
            return;

        invite.EmailStatus = nextStatus.Value;
        if (nextStatus == InviteEmailStatus.Delivered)
            invite.DeliveredAt = request.OccurredAt;

        await _inviteRepository.UpdateAsync(invite, cancellationToken);
        await _inviteRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invite {InviteId} email status changed to {Status}", invite.Id, nextStatus);
        await _notificationService.InviteStatusChangedAsync(invite.ListId, invite.Id, cancellationToken);
    }

    // Events can arrive out of order, so a later-stage status is never downgraded.
    public static InviteEmailStatus? NextStatus(InviteEmailStatus current, string eventType) => eventType switch
    {
        "email.sent" when current is InviteEmailStatus.Pending or InviteEmailStatus.Failed => InviteEmailStatus.Sent,
        "email.delivered" when current is InviteEmailStatus.Pending or InviteEmailStatus.Sent or InviteEmailStatus.Failed
            => InviteEmailStatus.Delivered,
        "email.bounced" when current is not InviteEmailStatus.Complained => InviteEmailStatus.Bounced,
        "email.complained" => InviteEmailStatus.Complained,
        "email.failed" when current is InviteEmailStatus.Pending or InviteEmailStatus.Sent => InviteEmailStatus.Failed,
        _ => null
    };
}
