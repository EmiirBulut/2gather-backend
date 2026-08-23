using MediatR;
using TwoGather.Application.Features.Categories.DTOs;

namespace TwoGather.Application.Features.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand(Guid CategoryId, string Name, string RoomLabel) : IRequest<CategoryDto>;
