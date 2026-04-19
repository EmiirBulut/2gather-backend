using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Claims.DTOs;
using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Items.Queries.GetItemDetail;

public class GetItemDetailQueryHandler : IRequestHandler<GetItemDetailQuery, ItemDetailDto>
{
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly IOptionRepository _optionRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetItemDetailQueryHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        IOptionRepository optionRepository,
        ICurrentUserService currentUserService)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _optionRepository = optionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ItemDetailDto> Handle(GetItemDetailQuery request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdWithCategoryAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), request.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor, MemberRole.Viewer);

        var options = await _optionRepository.GetByItemIdWithRatingsAndClaimsAsync(request.ItemId, _currentUserService.UserId, cancellationToken);

        return new ItemDetailDto
        {
            Id = item.Id,
            Name = item.Name,
            ImageUrl = item.ImageUrl,
            PlanningNote = item.PlanningNote,
            Status = item.Status,
            PurchasedAt = item.PurchasedAt,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? string.Empty,
            RoomLabel = item.Category?.RoomLabel ?? string.Empty,
            Options = options.Select(x => new ItemOptionDetailDto
            {
                Id = x.option.Id,
                ItemId = x.option.ItemId,
                Title = x.option.Title,
                Price = x.option.Price,
                Currency = x.option.Currency,
                Link = x.option.Link,
                Notes = x.option.Notes,
                Brand = x.option.Brand,
                Model = x.option.Model,
                Color = x.option.Color,
                IsSelected = x.option.IsSelected,
                IsFinal = x.option.IsFinal,
                FinalizedAt = x.option.FinalizedAt,
                CreatedAt = x.option.CreatedAt,
                AverageRating = x.averageRating,
                TotalRatings = x.totalRatings,
                CurrentUserScore = x.currentUserScore,
                ApprovedClaimsTotal = x.approvedClaimsTotal,
                RemainingClaimPercentage = 100 - x.approvedClaimsTotal,
                Claims = x.claims.Select(c => new ClaimDto(c.Id, c.UserId, c.User.DisplayName, c.Percentage, c.Status, c.CreatedAt)).ToList()
            }).ToList()
        };
    }
}
