using custom_router.logic.api;

namespace custom_router.logic.impl;

public class RouteFactory : IRouteFactory
{
    private readonly ITokenizer _routeTokenizer;
    private readonly ITokenizer _parameterTokenizer;

    public RouteFactory(ITokenizer routeTokenizer, ITokenizer parameterTokenizer)
    {
        _routeTokenizer = routeTokenizer ?? throw new ArgumentNullException(nameof(routeTokenizer));
        _parameterTokenizer = parameterTokenizer ?? throw new ArgumentNullException(nameof(parameterTokenizer));
    }

    public IRoute Create(Action onNext) =>
        new Route(_routeTokenizer, _parameterTokenizer, onNext);
}