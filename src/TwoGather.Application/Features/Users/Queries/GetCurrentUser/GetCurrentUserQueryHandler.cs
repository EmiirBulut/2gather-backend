using MediatR;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Users.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(IUserRepository userRepository, ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUserService.UserId);

        return new UserProfileDto(user.Id, user.Email, user.DisplayName);
    }
}
