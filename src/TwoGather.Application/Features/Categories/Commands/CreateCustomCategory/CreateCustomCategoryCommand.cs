using MediatR;
using TwoGather.Application.Features.Categories.DTOs;

namespace TwoGather.Application.Features.Categories.Commands.CreateCustomCategory;

public record CreateCustomCategoryCommand(Guid ListId, string Name, string RoomLabel) : IRequest<CategoryDto>;
