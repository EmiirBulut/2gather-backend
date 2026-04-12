using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Categories.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Categories.Commands.CreateCustomCategory;

public class CreateCustomCategoryCommandHandler : IRequestHandler<CreateCustomCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateCustomCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IListRepository listRepository,
        ICurrentUserService currentUserService)
    {
        _categoryRepository = categoryRepository;
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CategoryDto> Handle(CreateCustomCategoryCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            ListId = request.ListId,
            Name = request.Name,
            RoomLabel = request.RoomLabel,
            IsSystem = false
        };

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.RoomLabel, category.IsSystem, category.ListId);
    }
}
