namespace custom_router.logic.impl;

public class DisposeActionHandle(Action disposeAction) : IDisposable
{
    private Action? _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));

    public void Dispose()
    {
        Action? disposeAction = _disposeAction;
        _disposeAction = null;
        disposeAction?.Invoke();
    }
}
