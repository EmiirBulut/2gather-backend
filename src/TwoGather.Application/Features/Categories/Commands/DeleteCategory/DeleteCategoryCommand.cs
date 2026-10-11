using MediatR;

namespace TwoGather.Application.Features.Categories.Commands.DeleteCategory;

/// <summary>Deletes a list category together with every item in it (owner only).</summary>
public record DeleteCategoryCommand(Guid CategoryId) : IRequest;
