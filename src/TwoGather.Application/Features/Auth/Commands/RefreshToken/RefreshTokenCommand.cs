using MediatR;
using TwoGather.Application.Features.Auth.DTOs;

namespace TwoGather.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<AuthResponseDto>;
