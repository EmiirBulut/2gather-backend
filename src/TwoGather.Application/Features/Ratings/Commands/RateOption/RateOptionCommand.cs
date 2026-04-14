using MediatR;
using TwoGather.Application.Features.Ratings.DTOs;

namespace TwoGather.Application.Features.Ratings.Commands.RateOption;

public record RateOptionCommand(Guid OptionId, int Score) : IRequest<OptionRatingDto>;
