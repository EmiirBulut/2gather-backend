using FluentValidation;

namespace TwoGather.Application.Features.Ratings.Commands.RateOption;

public class RateOptionCommandValidator : AbstractValidator<RateOptionCommand>
{
    public RateOptionCommandValidator()
    {
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(1, 5);
    }
}
