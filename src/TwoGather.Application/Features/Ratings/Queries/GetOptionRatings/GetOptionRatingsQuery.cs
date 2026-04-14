using MediatR;
using TwoGather.Application.Features.Ratings.DTOs;

namespace TwoGather.Application.Features.Ratings.Queries.GetOptionRatings;

public record GetOptionRatingsQuery(Guid OptionId) : IRequest<OptionRatingDto>;
