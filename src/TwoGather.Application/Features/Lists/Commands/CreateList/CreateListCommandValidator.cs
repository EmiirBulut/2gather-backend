using FluentValidation;

namespace TwoGather.Application.Features.Lists.Commands.CreateList;

public class CreateListCommandValidator : AbstractValidator<CreateListCommand>
{
    public CreateListCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("List name is required.")
            .MaximumLength(200).WithMessage("List name must not exceed 200 characters.");
    }
}
