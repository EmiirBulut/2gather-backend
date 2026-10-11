using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Users.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUserService.UserId);

        var email = request.Email.Trim().ToLowerInvariant();
        if (email != user.Email)
        {
            // Email is the login identifier, so changing it requires the current password
            if (string.IsNullOrEmpty(request.CurrentPassword))
                throw new DomainException("E-posta adresini değiştirmek için mevcut şifrenizi girin.");
            if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw new DomainException("Mevcut şifre hatalı.");

            var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existing is not null)
                throw new DomainException("Bu e-posta adresi zaten kullanılıyor.");

            user.Email = email;
        }

        user.DisplayName = request.DisplayName.Trim();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated their profile", user.Id);
        return new UserProfileDto(user.Id, user.Email, user.DisplayName);
    }
}
