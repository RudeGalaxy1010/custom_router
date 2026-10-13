namespace custom_router.logic.api;

public interface IRouteFactory
{
    IRoute Create(string template, Action onNext);
}
