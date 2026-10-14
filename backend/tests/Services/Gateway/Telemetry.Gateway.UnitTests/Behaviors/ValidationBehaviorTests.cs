using FluentValidation;
using MediatR;
using Telemetry.Gateway.Application.Behaviors;
using Telemetry.Gateway.Application.Common;

namespace Telemetry.Gateway.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);

        var response = await behavior.Handle(new TestRequest("Kitchen", 1), _ => Task.FromResult("handled"), CancellationToken);

        response.ShouldBe("handled");
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsWithErrorsPerProperty_WithoutCallingNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("handled");
        };

        var exception = await Should.ThrowAsync<RequestValidationException>(() =>
            behavior.Handle(new TestRequest(string.Empty, 0), next, CancellationToken));

        nextCalled.ShouldBeFalse();
        exception.Errors.Keys.ShouldBe([nameof(TestRequest.Name), nameof(TestRequest.Count)], ignoreOrder: true);
    }

    [Fact]
    public async Task Handle_WithoutValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);

        (await behavior.Handle(new TestRequest(string.Empty, 0), _ => Task.FromResult("handled"), CancellationToken))
            .ShouldBe("handled");
    }

    public sealed record TestRequest(string Name, int Count) : IRequest<string>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(request => request.Name).NotEmpty();
            RuleFor(request => request.Count).GreaterThan(0);
        }
    }
}
