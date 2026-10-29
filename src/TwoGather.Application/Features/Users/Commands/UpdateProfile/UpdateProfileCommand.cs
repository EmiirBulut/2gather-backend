using MediatR;
using TwoGather.Application.Features.Users.DTOs;

namespace TwoGather.Application.Features.Users.Commands.UpdateProfile;

/// <summary>CurrentPassword is required only when the email changes.</summary>
public record UpdateProfileCommand(string DisplayName, string Email, string? CurrentPassword) : IRequest<UserProfileDto>;
