using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Lists.DTOs;
using TwoGather.Domain.Enums;
using ListEntity = TwoGather.Domain.Entities.List;
using ListMemberEntity = TwoGather.Domain.Entities.ListMember;

namespace TwoGather.Application.Features.Lists.Commands.CreateList;

public class CreateListCommandHandler : IRequestHandler<CreateListCommand, ListDto>
{
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<CreateListCommandHandler> _logger;

    public CreateListCommandHandler(
        IListRepository listRepository,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        ILogger<CreateListCommandHandler> logger)
    {
        _listRepository = listRepository;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task<ListDto> Handle(CreateListCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeService.UtcNow;

        var list = new ListEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            OwnerId = _currentUserService.UserId,
            CreatedAt = now
        };

        var ownerMember = new ListMemberEntity
        {
            Id = Guid.NewGuid(),
            ListId = list.Id,
            UserId = _currentUserService.UserId,
            Role = MemberRole.Owner,
            JoinedAt = now
        };

        await _listRepository.AddAsync(list, cancellationToken);
        await _listRepository.AddMemberAsync(ownerMember, cancellationToken);
        await _listRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("List {ListId} created by user {UserId}", list.Id, _currentUserService.UserId);

        return new ListDto(list.Id, list.Name, list.OwnerId, list.CreatedAt, 1);
    }
}
