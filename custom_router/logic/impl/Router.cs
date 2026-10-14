using custom_router.logic.api;
using custom_router.validation.api;

namespace custom_router.logic.impl;

public class Router : IRouter
{
    private readonly IValidator<string> _pathValidator;
    private readonly IRouteFactory _routeFactory;
    private readonly List<(string Template, IRoute Route)> _routes = [];

    public Router(IValidator<string> pathValidator, IRouteFactory routeFactory)
    {
        _pathValidator = pathValidator ?? throw new ArgumentNullException(nameof(pathValidator));
        _routeFactory = routeFactory ?? throw new ArgumentNullException(nameof(routeFactory));
    }

    public IDisposable Register(string template, Delegate handler)
    {
        ValidatePath(template);
        ArgumentNullException.ThrowIfNull(handler);

        string cleanTemplate = PathNormalization.Normalize(template);

        if (_routes.Exists(registration => registration.Template == cleanTemplate))
        {
            throw new InvalidOperationException($"Route already registered for: {template}");
        }

        IRoute route = _routeFactory.Create(template, handler);

        int insertionIndex = _routes.FindIndex(registration =>
        {
            IRoute existing = registration.Route;

            if (route.IsParameterized != existing.IsParameterized)
            {
                return !route.IsParameterized;
            }

            return route.Priority > existing.Priority;
        });

        _routes.Insert(insertionIndex < 0 ? _routes.Count : insertionIndex, (cleanTemplate, route));

        return new DisposeActionHandle(() =>
        {
            _routes.RemoveAll(registration =>
                registration.Template == cleanTemplate
                && ReferenceEquals(registration.Route, route));
        });
    }

    public async Task ProcessAsync(string path)
    {
        ValidatePath(path);

        foreach ((string _, IRoute route) in _routes)
        {
            if (!route.TryMatch(path, out object?[] parameters))
            {
                continue;
            }

            await route.ProcessAsync(parameters);
            return;
        }

        throw new KeyNotFoundException($"Route not found: {path}");
    }

    private void ValidatePath(string path)
    {
        if (!_pathValidator.Validate(path))
        {
            throw new ArgumentException("path is invalid", nameof(path));
        }
    }
}