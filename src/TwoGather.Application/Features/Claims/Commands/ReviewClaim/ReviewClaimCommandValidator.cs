using FluentValidation;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Claims.Commands.ReviewClaim;

public class ReviewClaimCommandValidator : AbstractValidator<ReviewClaimCommand>
{
    public ReviewClaimCommandValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.Decision).Must(d => d == ClaimStatus.Approved || d == ClaimStatus.Rejected)
            .WithMessage("Decision must be Approved or Rejected.");
    }
}
