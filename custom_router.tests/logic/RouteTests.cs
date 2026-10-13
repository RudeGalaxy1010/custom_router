using NUnit.Framework;
using custom_router.logic.impl;

namespace custom_router.tests.logic;

public class RouteTests
{
    [Test]
    public void Constructor_NullTemplate_ThrowsArgumentNullException()
    {
        // Arrange
        Action handler = () => { };

        // Act
        TestDelegate action = () => _ = new Route(
            null!,
            new SimpleTokenizer('/'),
            new SimpleTokenizer(':'),
            handler);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Test]
    public void TryProcess_EqualStaticRoute_InvokesHandler()
    {
        // Arrange
        int calls = 0;
        Route route = Create("main/page/", () => calls++);

        // Act
        bool result = route.TryProcess("main/page/");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void TryProcess_DifferentStaticRoute_DoesNotInvokeHandler()
    {
        // Arrange
        int calls = 0;
        Route route = Create("main/page/", () => calls++);

        // Act
        bool result = route.TryProcess("main/home/");

        // Assert
        Assert.That(result, Is.False);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void TryProcess_MatchingRoute_InvokesHandler()
    {
        // Arrange
        int calls = 0;
        Route route = Create("main/page/", () => calls++);

        // Act
        route.TryProcess("main/page/");

        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    private static Route Create(string template = "main/page/", Action? handler = null) =>
        new(template, new SimpleTokenizer('/'), new SimpleTokenizer(':'), handler ?? (() => { }));
}
