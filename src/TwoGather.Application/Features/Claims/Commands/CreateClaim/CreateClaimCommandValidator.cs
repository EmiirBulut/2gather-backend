using FluentValidation;

namespace TwoGather.Application.Features.Claims.Commands.CreateClaim;

public class CreateClaimCommandValidator : AbstractValidator<CreateClaimCommand>
{
    public CreateClaimCommandValidator()
    {
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.Percentage).Must(p => p == 25 || p == 50 || p == 75 || p == 100)
            .WithMessage("Percentage must be 25, 50, 75, or 100.");
    }
}
