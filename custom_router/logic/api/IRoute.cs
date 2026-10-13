namespace custom_router.logic.api;

public interface IRoute
{
    bool Matches(string route);
    void Process();
}
