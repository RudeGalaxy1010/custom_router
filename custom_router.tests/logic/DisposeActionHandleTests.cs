using NUnit.Framework;
using custom_router.logic.impl;

namespace custom_router.tests.logic;

public class DisposeActionHandleTests
{
    [Test]
    public void Constructor_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        Action? disposeAction = null;

        // Act
        TestDelegate action = () => _ = new DisposeActionHandle(disposeAction!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Test]
    public void Dispose_CalledTwice_InvokesActionOnce()
    {
        // Arrange
        int calls = 0;
        IDisposable handle = Create(() => calls++);

        // Act
        handle.Dispose();
        handle.Dispose();

        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_ActionReentersDispose_InvokesActionOnce()
    {
        // Arrange
        DisposeActionHandle? handle = null;
        int calls = 0;
        handle = Create(() =>
        {
            calls++;
            handle!.Dispose();
        });

        // Act
        handle.Dispose();

        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_ActionThrows_CanBeCalledOnlyOnce()
    {
        // Arrange
        int calls = 0;
        IDisposable handle = Create(() =>
        {
            calls++;
            throw new InvalidOperationException();
        });

        // Act
        Assert.Throws<InvalidOperationException>(() => handle.Dispose());
        handle.Dispose();

        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    private static DisposeActionHandle Create(Action? disposeAction = null) =>
        new(disposeAction ?? (() => { }));
}
