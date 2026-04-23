using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Members.Commands.CancelInvite;
using TwoGather.Application.Features.Members.Commands.InviteMember;
using TwoGather.Application.Features.Members.Commands.RemoveMember;
using TwoGather.Application.Features.Members.Commands.ResendInvite;
using TwoGather.Application.Features.Members.Commands.UpdateMemberRole;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Application.Features.Members.Queries.GetMembers;
using TwoGather.Application.Features.Members.Queries.GetPendingInvites;
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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> GetMembers(
        Guid listId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMembersQuery(listId), cancellationToken);
        return Ok(result);
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
    [HttpGet("invites")]
    public async Task<ActionResult<IReadOnlyList<PendingInviteDto>>> GetPendingInvites(
        Guid listId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingInvitesQuery(listId), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("invites/{inviteId:guid}")]
    public async Task<IActionResult> CancelInvite(
        Guid listId,
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelInviteCommand(listId, inviteId), cancellationToken);
        return NoContent();
    }

    [HttpPost("invites/{inviteId:guid}/resend")]
    public async Task<IActionResult> ResendInvite(
        Guid listId,
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResendInviteCommand(listId, inviteId), cancellationToken);
        return NoContent();
    }
}

public record InviteMemberRequest(
    string Email,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] MemberRole Role
);

public record UpdateMemberRoleRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] MemberRole Role
);
