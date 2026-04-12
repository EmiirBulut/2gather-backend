using MediatR;
using TwoGather.Application.Features.Categories.DTOs;

namespace TwoGather.Application.Features.Categories.Queries.GetCategories;

public record GetCategoriesQuery(Guid ListId) : IRequest<IReadOnlyList<CategoryDto>>;
