using MediatR;
using TwoGather.Application.Features.Auth.DTOs;

namespace TwoGather.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResponseDto>;
