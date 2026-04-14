using FluentValidation;

namespace TwoGather.Application.Features.Options.Commands.CreateOption;

public class CreateOptionCommandValidator : AbstractValidator<CreateOptionCommand>
{
    public CreateOptionCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price.HasValue);
        RuleFor(x => x.Currency).MaximumLength(10).When(x => x.Currency is not null);
        RuleFor(x => x.Link).MaximumLength(2000).When(x => x.Link is not null);
        RuleFor(x => x.Brand).MaximumLength(100).When(x => x.Brand is not null);
        RuleFor(x => x.Model).MaximumLength(100).When(x => x.Model is not null);
        RuleFor(x => x.Color).MaximumLength(50).When(x => x.Color is not null);
    }
}
