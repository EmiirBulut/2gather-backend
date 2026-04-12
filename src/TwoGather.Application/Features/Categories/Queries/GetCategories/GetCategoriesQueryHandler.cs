using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Categories.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Categories.Queries.GetCategories;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCategoriesQueryHandler(
        ICategoryRepository categoryRepository,
        IListRepository listRepository,
        ICurrentUserService currentUserService)
    {
        _categoryRepository = categoryRepository;
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor, MemberRole.Viewer);

        var categories = await _categoryRepository.GetByListIdAsync(request.ListId, cancellationToken);

        return categories.Select(c => new CategoryDto(c.Id, c.Name, c.RoomLabel, c.IsSystem, c.ListId)).ToList();
    }
}
