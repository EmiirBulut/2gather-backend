using FluentValidation;

namespace TwoGather.Application.Features.Lists.Commands.RenameList;

public class RenameListCommandValidator : AbstractValidator<RenameListCommand>
{
    public RenameListCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Liste adı zorunludur.")
            .MaximumLength(200).WithMessage("Liste adı en fazla 200 karakter olabilir.");
    }
}
