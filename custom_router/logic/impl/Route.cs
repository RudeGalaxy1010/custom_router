using custom_router.logic.api;

namespace custom_router.logic.impl;

public class Route : IRoute
{
    private readonly string[] _templateTokens;
    private readonly ITokenizer _routeTokenizer;
    private readonly ITokenizer _paramsTokenizer;
    private readonly Action _onNext;

    public Route(
        string template,
        ITokenizer routeTokenizer,
        ITokenizer paramsTokenizer,
        Action onNext)
    {
        ArgumentException.ThrowIfNullOrEmpty(template);

        _templateTokens = routeTokenizer?.Tokenize(template)
            ?? throw new ArgumentNullException(nameof(routeTokenizer));
        _routeTokenizer = routeTokenizer ?? throw new ArgumentNullException(nameof(routeTokenizer));
        _paramsTokenizer = paramsTokenizer ?? throw new ArgumentNullException(nameof(paramsTokenizer));
        _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
    }

    public bool Matches(string route)
    {
        string[] routeTokens = _routeTokenizer.Tokenize(route);

        return _templateTokens.SequenceEqual(routeTokens);
    }

    public void Process()
    {
        _onNext.Invoke();
    }
}
