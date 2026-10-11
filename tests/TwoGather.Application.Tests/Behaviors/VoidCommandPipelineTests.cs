using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TwoGather.Application.Features.Items.Commands.MarkItemPurchased;

namespace TwoGather.Application.Tests.Behaviors;

public class VoidCommandPipelineTests
{
    // Regression: the pipeline used to skip validators for void IRequest commands
    // because the behaviors were constrained to IRequest<TResponse>.
    [Fact]
    public async Task Send_InvalidVoidCommand_IsRejectedByValidationPipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ValidationException>(() =>
            mediator.Send(new MarkItemPurchasedCommand(Guid.NewGuid(), Array.Empty<Guid>())));
    }
}
