using NUnit.Framework;
using custom_router.di.api;
using custom_router.di.impl;

namespace custom_router.tests.di;

public class ServiceLocatorTests
{
    [Test]
    public void Resolve_BoundService_ReturnsService()
    {
        // Arrange
        IServiceLocator locator = new ServiceLocator();
        TestService service = new();

        locator.BindInterface<IService>(service);

        // Act
        IService resolved = locator.Resolve<IService>();

        // Assert
        Assert.That(resolved, Is.SameAs(service));
    }

    [Test]
    public void Resolve_UnboundService_ThrowsKeyNotFoundException()
    {
        // Arrange
        IServiceLocator locator = new ServiceLocator();

        // Act
        KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(
            () => locator.Resolve<IService>())!;

        // Assert
        Assert.That(exception.Message, Does.Contain(nameof(IService)));
    }

    [Test]
    public void Bind_SameServiceTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        IServiceLocator locator = new ServiceLocator();
        locator.BindInterface<IService>(new TestService());

        // Act
        TestDelegate action = () => locator.BindInterface<IService>(new TestService());

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Test]
    public void Bind_NullService_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceLocator locator = new ServiceLocator();

        // Act
        TestDelegate action = () => locator.BindInterface<IService>(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    private sealed class TestService : IService;
}
