using MediatR;
using TwoGather.Application.Features.Users.DTOs;

namespace TwoGather.Application.Features.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserProfileDto>;
