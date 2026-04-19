using MediatR;
using TwoGather.Application.Features.Notifications.DTOs;

namespace TwoGather.Application.Features.Notifications.Queries.GetNotificationCount;

public record GetNotificationCountQuery(Guid ListId) : IRequest<NotificationCountDto>;
