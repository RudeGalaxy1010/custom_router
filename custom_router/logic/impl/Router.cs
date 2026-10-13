using custom_router.logic.api;
using custom_router.validation.api;

namespace custom_router.logic.impl;

public class Router : IRouter
{
    private readonly IValidator<string> _pathValidator;
    private readonly IRouteFactory _routeFactory;
    private readonly Dictionary<string, IRoute> _routes = new Dictionary<string, IRoute>();

    public Router(IValidator<string> pathValidator, IRouteFactory routeFactory)
    {
        _pathValidator = pathValidator ?? throw new ArgumentNullException(nameof(pathValidator));
        _routeFactory = routeFactory ?? throw new ArgumentNullException(nameof(routeFactory));
    }

    public IDisposable Register(string path, Action onNext)
    {
        if (!_pathValidator.Validate(path))
        {
            throw new ArgumentException("path is invalid", nameof(path));
        }

        if (onNext == null)
        {
            throw new ArgumentNullException(nameof(onNext));
        }

        IRoute route = _routeFactory.Create(path, onNext);

        return _routes.TryAdd(path, route)
            ? new DisposeActionHandle(() => _routes.Remove(path))
            : throw new InvalidOperationException($"Route already registered for: {path}");
    }

    public void Process(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("value cannot be null or empty", nameof(value));
        }

        IRoute? route = _routes.Values.FirstOrDefault(current => current.Matches(value));

        if (route is not null)
        {
            route.Process();
        }
        else
        {
            throw new KeyNotFoundException($"Route not found: {value}");
        }
    }
}
