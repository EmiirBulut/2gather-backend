using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Auth.DTOs;
using TwoGather.Domain.Exceptions;
using RefreshTokenEntity = TwoGather.Domain.Entities.RefreshToken;

namespace TwoGather.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IDateTimeService dateTimeService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new DomainException("Invalid email or password.");

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresAt = _dateTimeService.UtcNow.AddDays(7),
            CreatedAt = _dateTimeService.UtcNow,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return new AuthResponseDto(accessToken, rawRefreshToken, user.Id, user.Email, user.DisplayName);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
