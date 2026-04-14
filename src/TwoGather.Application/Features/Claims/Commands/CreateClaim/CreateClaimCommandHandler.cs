using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Claims.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Claims.Commands.CreateClaim;

public class CreateClaimCommandHandler : IRequestHandler<CreateClaimCommand, ClaimDto>
{
    private readonly IOptionClaimRepository _claimRepository;
    private readonly IOptionRepository _optionRepository;
    private readonly IListRepository _listRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public CreateClaimCommandHandler(
        IOptionClaimRepository claimRepository,
        IOptionRepository optionRepository,
        IListRepository listRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _claimRepository = claimRepository;
        _optionRepository = optionRepository;
        _listRepository = listRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<ClaimDto> Handle(CreateClaimCommand request, CancellationToken cancellationToken)
    {
        var option = await _optionRepository.GetByIdWithItemAsync(request.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), request.OptionId);

        var member = await _listRepository.GetMemberAsync(option.Item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        if (!option.IsFinal)
            throw new DomainException("Yalnızca nihai kararı verilmiş seçeneklere talip olunabilir.");

        if (option.Item.Status == ItemStatus.Purchased)
            throw new DomainException("Satın alınan ürüne yeni talip eklenemez.");

        var approvedTotal = await _claimRepository.GetApprovedPercentageTotalAsync(request.OptionId, cancellationToken);
        if (approvedTotal + request.Percentage > 100)
            throw new DomainException($"Talep edilen yüzde kapasiteyi aşıyor. Kalan: {100 - approvedTotal}%");

        var claim = new OptionClaim
        {
            Id = Guid.NewGuid(),
            OptionId = request.OptionId,
            UserId = _currentUserService.UserId,
            Percentage = request.Percentage,
            Status = ClaimStatus.Pending,
            CreatedAt = _dateTimeService.UtcNow
        };

        await _claimRepository.AddAsync(claim, cancellationToken);
        await _claimRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
        var dto = new ClaimDto(claim.Id, claim.UserId, user?.DisplayName ?? string.Empty, claim.Percentage, claim.Status, claim.CreatedAt);

        await _notificationService.ClaimCreatedAsync(option.Item.ListId, request.OptionId, dto, cancellationToken);

        var owner = await _listRepository.GetOwnerAsync(option.Item.ListId, cancellationToken);
        if (owner is not null)
            await _notificationService.ClaimPendingNotificationAsync(option.Item.ListId, owner.UserId, dto, cancellationToken);

        return dto;
    }
}
