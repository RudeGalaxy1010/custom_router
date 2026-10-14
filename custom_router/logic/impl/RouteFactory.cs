using custom_router.logic.api;
using custom_router.validation.api;

namespace custom_router.logic.impl;

public class RouteFactory : IRouteFactory
{
    private readonly ITokenizer _routeTokenizer;
    private readonly ITokenizer _parameterTokenizer;
    private readonly IValidator<RouteParameterValidation> _parameterValidator;

    public RouteFactory(ITokenizer routeTokenizer, ITokenizer parameterTokenizer,
        IValidator<RouteParameterValidation> parameterValidator)
    {
        _routeTokenizer = routeTokenizer ?? throw new ArgumentNullException(nameof(routeTokenizer));
        _parameterTokenizer = parameterTokenizer ?? throw new ArgumentNullException(nameof(parameterTokenizer));
        _parameterValidator = parameterValidator ?? throw new ArgumentNullException(nameof(parameterValidator));
    }

    public IRoute Create(string template, Delegate onNext) =>
        new Route(_routeTokenizer, _parameterTokenizer, _parameterValidator, template, onNext);
}