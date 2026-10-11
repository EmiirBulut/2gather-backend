using MediatR;

namespace TwoGather.Application.Features.Members.Commands.ProcessInviteEmailEvent;

/// <summary>A delivery event reported by the email provider (e.g. Resend "email.delivered").</summary>
public record ProcessInviteEmailEventCommand(string EventType, string ProviderMessageId, DateTime OccurredAt) : IRequest;
