using NUnit.Framework;
using custom_router.logic.api;
using custom_router.logic.impl;
using custom_router.validation.api;
using custom_router.validation.impl;

namespace custom_router.tests.logic;

public class RouterTests
{
    [Test]
    public void Constructor_NullPathValidator_ThrowsArgumentNullException()
    {
        // Arrange
        IRouteFactory routeFactory = CreateRouteFactory();

        // Act
        TestDelegate action = () => _ = new Router(null!, routeFactory);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Test]
    public void Constructor_NullRouteFactory_ThrowsArgumentNullException()
    {
        // Arrange
        IValidator<string> pathValidator = CreatePathValidator();

        // Act
        TestDelegate action = () => _ = new Router(pathValidator, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Test]
    public void Process_RegisteredRoute_InvokesHandler()
    {
        // Arrange
        IRouter router = CreateRouter();
        int calls = 0;

        // Act
        using (router.Register("main/page/", () => calls++))
        {
            router.Process("main/page/");
        }

        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Process_RouteWithDifferentSegmentCount_ThrowsKeyNotFoundException()
    {
        // Arrange
        IRouter router = CreateRouter();
        using IDisposable handle = router.Register("main/page/", () => { });

        // Act
        TestDelegate action = () => router.Process("main/page/details/");

        // Assert
        Assert.Throws<KeyNotFoundException>(action);
    }

    [Test]
    public void Process_RouteWithDifferentStaticSegment_ThrowsKeyNotFoundException()
    {
        // Arrange
        IRouter router = CreateRouter();
        using IDisposable handle = router.Register("main/page/", () => { });

        // Act
        TestDelegate action = () => router.Process("main/home/");

        // Assert
        Assert.Throws<KeyNotFoundException>(action);
    }

    [Test]
    public void Process_UnknownRoute_ThrowsKeyNotFoundException()
    {
        // Arrange
        IRouter router = CreateRouter();

        using (router.Register("main/page/", () => { }))
        {
            // Act
            KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(
                () => router.Process("other/page/"))!;

            // Assert
            Assert.That(exception.Message, Does.Contain("other/page/"));
        }
    }

    [Test]
    public void Register_DuplicateRoute_ThrowsInvalidOperationException()
    {
        // Arrange
        IRouter router = CreateRouter();
        using IDisposable handle = router.Register("main/page/", () => { });

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => router.Register("main/page/", () => { }))!;

        // Assert
        Assert.That(exception.Message, Does.Contain("main/page/"));
    }

    [Test]
    public void Process_DisposedRoute_ThrowsKeyNotFoundException()
    {
        // Arrange
        IRouter router = CreateRouter();
        IDisposable handle = router.Register("main/page/", () => { });

        // Act
        handle.Dispose();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => router.Process("main/page/"));
    }

    [Test]
    public void Dispose_OldHandleAfterReplacement_PreservesNewRoute()
    {
        // Arrange
        IRouter router = CreateRouter();
        IDisposable first = router.Register("main/page/", () => { });
        first.Dispose();
        IDisposable second = router.Register("main/page/", () => { });

        // Act
        first.Dispose();

        // Assert
        Assert.DoesNotThrow(() => router.Process("main/page/"));
        second.Dispose();
    }

    [Test]
    public void Register_InvalidPath_ThrowsArgumentException()
    {
        // Arrange
        IRouter router = CreateRouter();

        // Act
        TestDelegate doubleSeparator = () => router.Register("main//page/", () => { });
        TestDelegate whitespace = () => router.Register("main page/", () => { });

        // Assert
        Assert.Throws<ArgumentException>(doubleSeparator);
        Assert.Throws<ArgumentException>(whitespace);
    }

    [Test]
    public void Register_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        IRouter router = CreateRouter();

        // Act
        TestDelegate action = () => router.Register("main/page/", null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Test]
    public void Register_IntParameter_ConvertsAndPassesValue()
    {
        // Arrange
        IRouter router = CreateRouter();
        int actual = 0;

        // Act
        using (router.Register<int>("users/{id:int}/", id => actual = id))
        {
            router.Process("users/42/");
        }

        // Assert
        Assert.That(actual, Is.EqualTo(42));
    }

    [Test]
    public void Register_MultipleParameters_PassesValuesInTemplateOrder()
    {
        // Arrange
        IRouter router = CreateRouter();
        int actualId = 0;
        string actualName = string.Empty;

        // Act
        using (router.Register<int, string>(
                   "users/{id:int}/{name:string}/",
                   (id, name) =>
                   {
                       actualId = id;
                       actualName = name;
                   }))
        {
            router.Process("users/42/alice/");
        }

        // Assert
        Assert.That(actualId, Is.EqualTo(42));
        Assert.That(actualName, Is.EqualTo("alice"));
    }

    [Test]
    public void Register_InvalidParameterValue_DoesNotInvokeHandler()
    {
        // Arrange
        IRouter router = CreateRouter();
        int calls = 0;
        using IDisposable handle = router.Register<int>("users/{id:int}/", _ => calls++);

        // Act
        TestDelegate action = () => router.Process("users/not-an-int/");

        // Assert
        Assert.Throws<KeyNotFoundException>(action);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void Register_MismatchedTemplateParameterType_ThrowsArgumentException()
    {
        // Arrange
        IRouter router = CreateRouter();

        // Act
        TestDelegate action = () => router.Register<int>("users/{id:string}/", _ => { });

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    private static IRouter CreateRouter(
        IValidator<string>? pathValidator = null,
        IRouteFactory? routeFactory = null) => new Router(
        pathValidator ?? CreatePathValidator(),
        routeFactory ?? CreateRouteFactory());

    private static IValidator<string> CreatePathValidator() => new RegexValidator(@"\s|//");

    private static IRouteFactory CreateRouteFactory() => new RouteFactory(
        new SimpleTokenizer('/', '\\'),
        new SimpleTokenizer(':'));
}
