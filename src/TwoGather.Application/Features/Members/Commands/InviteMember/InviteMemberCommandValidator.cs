using FluentValidation;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Members.Commands.InviteMember;

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.ListId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role)
            .Must(r => r == MemberRole.Editor || r == MemberRole.Viewer)
            .WithMessage("Invited role must be Editor or Viewer.");
    }
}
