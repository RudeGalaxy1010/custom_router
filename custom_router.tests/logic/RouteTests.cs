using System.ComponentModel;
using NUnit.Framework;
using custom_router.logic.impl;
using custom_router.validation.impl;

namespace custom_router.logic.tests;

public class RouteTests
{
    [Test]
    public void Constructor_NullParameterValidator_ThrowsArgumentNullException()
    {
        // Arrange
        Func<Task> handler = () => Task.CompletedTask;
        // Act
        TestDelegate action = () => _ = new Route(
            new SimpleTokenizer('/'), new SimpleTokenizer(':'), null!, "users", handler);
        // Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action)!;
        Assert.That(exception.ParamName, Is.EqualTo("parameterValidator"));
    }

    [Test]
    public void Constructor_NullTemplate_ThrowsArgumentNullException()
    {
        // Arrange
        Func<Task> handler = () => Task.CompletedTask;

        // Act
        TestDelegate action = () => _ = new Route(
            new SimpleTokenizer('/'),
            new SimpleTokenizer(':'),
            new RouteParameterValidator(),
            null!,
            handler);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Test]
    public void TryMatch_EqualStaticRoute_DoesNotInvokeHandler()
    {
        // Arrange
        int calls = 0;
        Route route = Create("main/page/", () => { calls++; return Task.CompletedTask; });

        // Act
        bool result = route.TryMatch("main/page/", out object?[] parameters);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(parameters, Is.Empty);
        Assert.That(calls, Is.EqualTo(0));
    }

    [TestCase("{id:int}")]
    [TestCase("{value:int}")]
    [TestCase("{value:object}")]
    public void Constructor_InvalidParameter_ThrowsArgumentException(string token)
    {
        // Arrange
        string template = $"users/{token}";
        Func<object, Task> handler = value => Task.CompletedTask;
        // Act
        TestDelegate action = () => _ = new Route(new SimpleTokenizer('/'), new SimpleTokenizer(':'),
            new RouteParameterValidator(), template, handler);
        // Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(action)!;
        Assert.That(exception.Message, Does.StartWith($"Invalid route parameter: {token}"));
        Assert.That(exception.ParamName, Is.EqualTo("template"));
    }

    [Test]
    public void TryMatch_DifferentStaticRoute_DoesNotInvokeHandler()
    {
        // Arrange
        int calls = 0;
        Route route = Create("main/page/", () => { calls++; return Task.CompletedTask; });

        // Act
        bool result = route.TryMatch("main/home/", out _);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public async Task InvokeAsync_MatchingRoute_InvokesHandler()
    {
        // Arrange
        int calls = 0;
        Route route = Create("main/page/", () => { calls++; return Task.CompletedTask; });

        // Act
        route.TryMatch("main/page/", out object?[] parameters);
        await route.ProcessAsync(parameters);

        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void TryMatch_ParameterizedRoute_ConvertsWithoutInvocation()
    {
        // Arrange
        int calls = 0;
        Func<int, Task> handler = id => { calls++; return Task.CompletedTask; };
        Route route = new(new SimpleTokenizer('/'), new SimpleTokenizer(':'), new RouteParameterValidator(),
            "users/{id:int}", handler);

        // Act
        bool result = route.TryMatch("users/42", out object?[] parameters);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(parameters, Is.EqualTo(new object?[] { 42 }));
        Assert.That(calls, Is.Zero);
    }

    [TestCase("main/page", "main/page/")]
    [TestCase("main/page/", "main/page")]
    [TestCase("/", "/")]
    public void TryMatch_CanonicalPath_Matches(string template, string path)
    {
        // Arrange
        Route route = Create(template);
        // Act
        bool result = route.TryMatch(path, out _);
        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("/", "")]
    [TestCase("main/page", "main/page//")]
    public void TryMatch_ExtraEmptySegment_DoesNotMatch(string template, string path)
    {
        // Arrange
        Route route = Create(template);
        // Act
        bool result = route.TryMatch(path, out _);
        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryMatch_AdditionalAliases_ConvertsArguments()
    {
        // Arrange
        Func<char, uint, ushort, ulong, sbyte, Task> handler =
            (char letter, uint number, ushort count, ulong total, sbyte delta) => Task.CompletedTask;
        Route route = new(new SimpleTokenizer('/'), new SimpleTokenizer(':'), new RouteParameterValidator(),
            "{letter:CHAR}/{number:UINT}/{count:USHORT}/{total:ULONG}/{delta:SBYTE}", handler);
        // Act
        bool result = route.TryMatch("a/42/7/100/-1", out object?[] parameters);
        // Assert
        Assert.That(result, Is.True);
        Assert.That(parameters, Is.EqualTo(new object?[] { 'a', 42u, (ushort)7, 100ul, (sbyte)-1 }));
    }

    [Test]
    public void TryMatch_RepeatedConversion_ReusesPreparedConverter()
    {
        // Arrange
        RouteValueConverter converter = (RouteValueConverter)TypeDescriptor.GetConverter(typeof(RouteValue));
        int checks = converter.CapabilityChecks;
        int conversions = converter.ConversionCalls;
        Func<RouteValue, Task> handler = value => Task.CompletedTask;
        Route route = new(new SimpleTokenizer('/'), new SimpleTokenizer(':'), new RouteParameterValidator(),
            "{value:RouteValue}", handler);
        TypeDescriptor.Refresh(typeof(RouteValue));
        // Act
        bool first = route.TryMatch("first", out object?[] firstParameters);
        bool second = route.TryMatch("second", out object?[] secondParameters);
        // Assert
        Assert.That(first && second, Is.True);
        Assert.That(firstParameters[0], Is.EqualTo(new RouteValue("first")));
        Assert.That(secondParameters[0], Is.EqualTo(new RouteValue("second")));
        Assert.That(converter.CapabilityChecks, Is.EqualTo(checks + 1));
        Assert.That(converter.ConversionCalls, Is.EqualTo(conversions + 2));
    }

    private static Route Create(string template = "main/page/", Func<Task>? handler = null) =>
        new(new SimpleTokenizer('/'), new SimpleTokenizer(':'), new RouteParameterValidator(),
            template, handler ?? (() => Task.CompletedTask));
}
