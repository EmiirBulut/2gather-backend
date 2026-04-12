using FluentValidation;

namespace TwoGather.Application.Features.Categories.Commands.CreateCustomCategory;

public class CreateCustomCategoryCommandValidator : AbstractValidator<CreateCustomCategoryCommand>
{
    public CreateCustomCategoryCommandValidator()
    {
        RuleFor(x => x.ListId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RoomLabel).NotEmpty().MaximumLength(100);
    }
}
