namespace custom_router.logic.api;

public interface IRoute
{
    bool TryProcess(string route);
}
