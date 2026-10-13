using custom_router.logic.api;

namespace custom_router.logic.impl;

public class Route : IRoute
{
    private readonly ITokenizer _routeTokenizer;
    private readonly ITokenizer _paramsTokenizer;
    private readonly Action _onNext;

    public Route(ITokenizer routeTokenizer, ITokenizer paramsTokenizer, Action onNext)
    {
        _routeTokenizer = routeTokenizer ?? throw new ArgumentNullException(nameof(routeTokenizer));
        _paramsTokenizer = paramsTokenizer ?? throw new ArgumentNullException(nameof(paramsTokenizer));
        _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
    }

    public void Process(string route)
    {
        // TODO:
        // Tokenize
        // Parse parameters

        _onNext.Invoke();
    }
}