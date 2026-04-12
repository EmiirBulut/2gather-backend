using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Members.Commands.InviteMember;
using TwoGather.Application.Features.Members.Commands.RemoveMember;
using TwoGather.Application.Features.Members.Commands.UpdateMemberRole;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Api.Controllers;

[ApiController]
[Route("api/lists/{listId:guid}/members")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly IMediator _mediator;

    public MembersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("invite")]
    public async Task<ActionResult<InviteDto>> InviteMember(
        Guid listId,
        [FromBody] InviteMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new InviteMemberCommand(listId, request.Email, request.Role), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid listId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveMemberCommand(listId, userId), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{userId:guid}/role")]
    public async Task<ActionResult<MemberDto>> UpdateMemberRole(
        Guid listId,
        Guid userId,
        [FromBody] UpdateMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateMemberRoleCommand(listId, userId, request.Role), cancellationToken);
        return Ok(result);
    }
}

public record InviteMemberRequest(string Email, MemberRole Role);
public record UpdateMemberRoleRequest(MemberRole Role);
