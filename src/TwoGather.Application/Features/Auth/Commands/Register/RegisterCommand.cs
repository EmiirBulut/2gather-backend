using MediatR;
using TwoGather.Application.Features.Auth.DTOs;

namespace TwoGather.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string DisplayName
) : IRequest<AuthResponseDto>;
