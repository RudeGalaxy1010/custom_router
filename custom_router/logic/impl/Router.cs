using custom_router.logic.api;
using custom_router.validation.api;

namespace custom_router.logic.impl;

public class Router : IRouter
{
    private readonly IValidator<string> _pathValidator;
    private readonly IRouteFactory _routeFactory;
    private readonly Dictionary<string, IRoute> _routes = new Dictionary<string, IRoute>();
    private readonly Lock _routesLock = new Lock();

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

        return AddRoute(path, route);
    }

    public IDisposable Register<T>(string path, Action<T> onNext)
    {
        ValidateRegistration(path, onNext);
        return AddRoute(path, _routeFactory.Create(path, onNext));
    }

    public IDisposable Register<T1, T2>(string path, Action<T1, T2> onNext)
    {
        ValidateRegistration(path, onNext);
        return AddRoute(path, _routeFactory.Create(path, onNext));
    }

    public IDisposable RegisterAsync(string path, Func<Task> onNext)
    {
        ValidateRegistration(path, onNext);
        return AddRoute(path, _routeFactory.Create(path, onNext));
    }

    public IDisposable RegisterAsync<T>(string path, Func<T, Task> onNext)
    {
        ValidateRegistration(path, onNext);
        return AddRoute(path, _routeFactory.Create(path, onNext));
    }

    public IDisposable RegisterAsync<T1, T2>(string path, Func<T1, T2, Task> onNext)
    {
        ValidateRegistration(path, onNext);
        return AddRoute(path, _routeFactory.Create(path, onNext));
    }

    private IDisposable AddRoute(string path, IRoute route)
    {
        lock (_routesLock)
        {
            if (!_routes.TryAdd(path, route))
            {
                throw new InvalidOperationException($"Route already registered for: {path}");
            }
        }

        return new DisposeActionHandle(() => RemoveRoute(path, route));
    }

    public void Process(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("value cannot be null or empty", nameof(value));
        }

        if (!_pathValidator.Validate(value))
        {
            throw new ArgumentException("value is invalid", nameof(value));
        }

        IRoute[] routes;
        lock (_routesLock)
        {
            routes = _routes.Values
                .OrderBy(route => route.IsParameterized)
                .ThenByDescending(route => route.Specificity)
                .ToArray();
        }

        foreach (IRoute route in routes)
        {
            if (route.TryProcess(value))
            {
                if (route.IsAsync)
                {
                    throw new InvalidOperationException("Use ProcessAsync for asynchronous routes.");
                }

                return;
            }
        }

        throw new KeyNotFoundException($"Route not found: {value}");
    }

    public async Task ProcessAsync(string value)
    {
        ValidatePath(value);

        IRoute[] routes;
        lock (_routesLock)
        {
            routes = _routes.Values
                .OrderBy(route => route.IsParameterized)
                .ThenByDescending(route => route.Specificity)
                .ToArray();
        }

        foreach (IRoute route in routes)
        {
            if (await route.TryProcessAsync(value))
            {
                return;
            }
        }

        throw new KeyNotFoundException($"Route not found: {value}");
    }

    private void RemoveRoute(string path, IRoute route)
    {
        lock (_routesLock)
        {
            if (_routes.TryGetValue(path, out IRoute? current) && ReferenceEquals(current, route))
            {
                _routes.Remove(path);
            }
        }
    }

    private void ValidateRegistration(string path, Delegate onNext)
    {
        if (!_pathValidator.Validate(path))
        {
            throw new ArgumentException("path is invalid", nameof(path));
        }

        ArgumentNullException.ThrowIfNull(onNext);
    }

    private void ValidatePath(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("value cannot be null or empty", nameof(value));
        }

        if (!_pathValidator.Validate(value))
        {
            throw new ArgumentException("value is invalid", nameof(value));
        }
    }
}
