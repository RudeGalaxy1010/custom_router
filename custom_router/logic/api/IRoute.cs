namespace custom_router.logic.api;

public interface IRoute
{
    bool IsParameterized { get; }
    bool IsAsync { get; }
    int Specificity { get; }
    bool TryProcess(string route);
    Task<bool> TryProcessAsync(string route);
}
