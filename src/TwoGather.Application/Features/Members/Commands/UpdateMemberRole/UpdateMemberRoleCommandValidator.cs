using FluentValidation;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Members.Commands.UpdateMemberRole;

public class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
{
    public UpdateMemberRoleCommandValidator()
    {
        RuleFor(x => x.ListId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => r == MemberRole.Editor || r == MemberRole.Viewer)
            .WithMessage("Role must be Editor or Viewer.");
    }
}
