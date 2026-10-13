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
    public void Matches_EqualStaticRoute_ReturnsTrue()
    {
        // Arrange
        Route route = Create("main/page/");

        // Act
        bool result = route.Matches("main/page/");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Matches_DifferentStaticRoute_ReturnsFalse()
    {
        // Arrange
        Route route = Create("main/page/");

        // Act
        bool result = route.Matches("main/home/");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Process_MatchingRoute_InvokesHandler()
    {
        // Arrange
        int calls = 0;
        Route route = Create("main/page/", () => calls++);

        // Act
        route.Process();

        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    private static Route Create(string template = "main/page/", Action? handler = null) =>
        new(template, new SimpleTokenizer('/'), new SimpleTokenizer(':'), handler ?? (() => { }));
}
