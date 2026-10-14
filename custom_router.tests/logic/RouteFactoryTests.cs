using NUnit.Framework;
using custom_router.logic.api;
using custom_router.logic.impl;
using custom_router.validation.api;
using custom_router.validation.impl;

namespace custom_router.logic.tests;

public class RouteFactoryTests
{
    [Test]
    public void Constructor_NullParameterValidator_ThrowsArgumentNullException()
    {
        // Arrange
        ITokenizer routeTokenizer = new SimpleTokenizer('/');
        ITokenizer parameterTokenizer = new SimpleTokenizer(':');
        // Act
        TestDelegate action = () => _ = new RouteFactory(routeTokenizer, parameterTokenizer, null!);
        // Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action)!;
        Assert.That(exception.ParamName, Is.EqualTo("parameterValidator"));
    }

    [Test]
    public void Create_SharedValidator_IsolatesDuplicateNames()
    {
        // Arrange
        RouteFactory factory = Create();
        Func<int, Task> handler = id => Task.CompletedTask;
        factory.Create("users/{id:int}", handler);
        // Act
        IRoute route = factory.Create("posts/{id:int}", handler);
        bool result = route.TryMatch("posts/42", out object?[] parameters);
        // Assert
        Assert.That(result, Is.True);
        Assert.That(parameters, Is.EqualTo(new object?[] { 42 }));
    }

    private static RouteFactory Create(ITokenizer? routeTokenizer = null, ITokenizer? parameterTokenizer = null,
        IValidator<RouteParameterValidation>? parameterValidator = null) =>
        new(routeTokenizer ?? new SimpleTokenizer('/'), parameterTokenizer ?? new SimpleTokenizer(':'),
            parameterValidator ?? new RouteParameterValidator());
}
