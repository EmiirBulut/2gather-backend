using FluentValidation;

namespace TwoGather.Application.Features.Options.Commands.UpdateOption;

public class UpdateOptionCommandValidator : AbstractValidator<UpdateOptionCommand>
{
    public UpdateOptionCommandValidator()
    {
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price.HasValue);
        RuleFor(x => x.Currency).MaximumLength(10).When(x => x.Currency is not null);
        RuleFor(x => x.Link).MaximumLength(2000).When(x => x.Link is not null);
    }
}
