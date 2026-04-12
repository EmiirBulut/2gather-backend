using FluentValidation;
using MediatR;
using TwoGather.Application.Common.Behaviors;

namespace TwoGather.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private record TestCommand(string Name) : IRequest<string>;

    private class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Name).MaximumLength(5).WithMessage("Name too long.");
        }
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsNext()
    {
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var nextCalled = false;

        var result = await behavior.Handle(
            new TestCommand("Hi"),
            _ => { nextCalled = true; return Task.FromResult("ok"); },
            CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ThrowsValidationException()
    {
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new TestCommand(""),
                _ => Task.FromResult("ok"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MultipleViolations_ThrowsWithAllErrors()
    {
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new TestCommand("TooLongName"),
                _ => Task.FromResult("ok"),
                CancellationToken.None));

        Assert.Contains(ex.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, string>(new List<IValidator<TestCommand>>());
        var nextCalled = false;

        await behavior.Handle(
            new TestCommand(""),
            _ => { nextCalled = true; return Task.FromResult("ok"); },
            CancellationToken.None);

        Assert.True(nextCalled);
    }
}
