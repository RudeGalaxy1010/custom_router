namespace custom_router.logic.api;

public interface IRoute
{
    bool IsParameterized { get; }
    int Priority { get; }
    bool TryMatch(string route, out object?[] parameters);
    Task ProcessAsync(object?[] parameters);
}
