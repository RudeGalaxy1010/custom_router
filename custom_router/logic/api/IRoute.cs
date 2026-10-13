namespace custom_router.logic.api;

public interface IRoute
{
    bool IsParameterized { get; }
    bool TryProcess(string route);
}
