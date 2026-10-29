using FluentValidation;

namespace TwoGather.Application.Features.Items.Commands.MarkItemPurchased;

public class MarkItemPurchasedCommandValidator : AbstractValidator<MarkItemPurchasedCommand>
{
    public MarkItemPurchasedCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.PurchaserUserIds)
            .NotEmpty().WithMessage("En az bir satın alan kişi seçilmelidir.");
        RuleForEach(x => x.PurchaserUserIds).NotEmpty();
        RuleFor(x => x.PurchasedOptionId).NotEqual(Guid.Empty).When(x => x.PurchasedOptionId.HasValue);
    }
}
