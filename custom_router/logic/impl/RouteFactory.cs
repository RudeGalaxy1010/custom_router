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

    public IRoute Create(string template, Action onNext) =>
        new Route(template, _routeTokenizer, _parameterTokenizer, onNext);
}
