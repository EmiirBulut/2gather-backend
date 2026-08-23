using MediatR;
using TwoGather.Application.Features.Lists.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Lists.Commands.CreateList;

public record CreateListCommand(string Name, ListTemplate Template) : IRequest<ListDto>;
