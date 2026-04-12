using FluentValidation;

namespace TwoGather.Application.Features.Members.Commands.AcceptInvite;

public class AcceptInviteCommandValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
