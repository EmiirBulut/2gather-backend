using MediatR;

namespace TwoGather.Application.Features.Items.Commands.UploadItemImage;

public record UploadItemImageCommand(Guid ItemId, Stream ImageStream, string FileName) : IRequest<string>;
