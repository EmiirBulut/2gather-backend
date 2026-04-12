using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Lists.Commands.DeleteList;

public class DeleteListCommandHandler : IRequestHandler<DeleteListCommand>
{
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteListCommandHandler> _logger;

    public DeleteListCommandHandler(
        IListRepository listRepository,
        ICurrentUserService currentUserService,
        ILogger<DeleteListCommandHandler> logger)
    {
        _listRepository = listRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(DeleteListCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);

        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        await _listRepository.DeleteAsync(list, cancellationToken);
        await _listRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("List {ListId} deleted by user {UserId}", request.ListId, _currentUserService.UserId);
    }
}
