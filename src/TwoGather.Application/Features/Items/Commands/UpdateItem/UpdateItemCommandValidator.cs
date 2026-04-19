using FluentValidation;

namespace TwoGather.Application.Features.Items.Commands.UpdateItem;

public class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ImageUrl).MaximumLength(2000).Must(url => url == null || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("ImageUrl must be a valid URL.").When(x => x.ImageUrl != null);
        RuleFor(x => x.PlanningNote).MaximumLength(1000);
    }
}
