using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Common.Helpers;

public static class ListAuthorizationHelper
{
    public static void RequireRole(ListMember? member, params MemberRole[] allowed)
    {
        if (member is null || !allowed.Contains(member.Role))
            throw new ForbiddenException("You do not have permission to perform this action.");
    }
}
