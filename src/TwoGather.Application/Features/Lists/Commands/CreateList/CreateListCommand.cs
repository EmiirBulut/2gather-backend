using MediatR;
using TwoGather.Application.Features.Lists.DTOs;

namespace TwoGather.Application.Features.Lists.Commands.CreateList;

public record CreateListCommand(string Name) : IRequest<ListDto>;
