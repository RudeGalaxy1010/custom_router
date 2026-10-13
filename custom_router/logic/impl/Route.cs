using System.Globalization;
using System.Linq.Expressions;
using custom_router.logic.api;

namespace custom_router.logic.impl;

public class Route : IRoute
{
    private readonly string[] _templateTokens;
    private readonly ITokenizer _routeTokenizer;
    private readonly ITokenizer _paramsTokenizer;
    private readonly Type[] _parameterTypes;
    private readonly Action<object?[]> _onNext;

    public Route(
        string template,
        ITokenizer routeTokenizer,
        ITokenizer paramsTokenizer,
        Delegate onNext)
    {
        ArgumentException.ThrowIfNullOrEmpty(template);

        _templateTokens = routeTokenizer?.Tokenize(template)
            ?? throw new ArgumentNullException(nameof(routeTokenizer));
        _routeTokenizer = routeTokenizer ?? throw new ArgumentNullException(nameof(routeTokenizer));
        _paramsTokenizer = paramsTokenizer ?? throw new ArgumentNullException(nameof(paramsTokenizer));
        _parameterTypes = onNext?.Method.GetParameters().Select(parameter => parameter.ParameterType).ToArray()
            ?? throw new ArgumentNullException(nameof(onNext));
        ValidateTemplate();
        _onNext = CreateInvoker(onNext);
    }

    public bool TryProcess(string route)
    {
        string[] routeTokens = _routeTokenizer.Tokenize(route);
        if (routeTokens.Length != _templateTokens.Length)
        {
            return false;
        }

        object?[] parameters = new object?[_parameterTypes.Length];
        int parameterIndex = 0;

        for (int index = 0; index < _templateTokens.Length; index++)
        {
            string templateToken = _templateTokens[index];
            if (!IsParameter(templateToken))
            {
                if (templateToken != routeTokens[index])
                {
                    return false;
                }

                continue;
            }

            if (!TryConvert(routeTokens[index], _parameterTypes[parameterIndex], out object? value))
            {
                return false;
            }

            parameters[parameterIndex++] = value;
        }

        _onNext(parameters);
        return true;
    }

    private void ValidateTemplate()
    {
        int parameterCount = _templateTokens.Count(IsParameter);
        if (parameterCount != _parameterTypes.Length)
        {
            throw new ArgumentException("Template parameters do not match handler parameters.", nameof(_templateTokens));
        }

        foreach ((string token, Type parameterType) in _templateTokens
                     .Where(IsParameter)
                     .Zip(_parameterTypes))
        {
            string[] parameterTokens = _paramsTokenizer.Tokenize(token[1..^1]);
            if (parameterTokens.Length != 2 || string.IsNullOrEmpty(parameterTokens[0]) ||
                !IsSupportedType(parameterTokens[1]) ||
                GetType(parameterTokens[1]) != parameterType)
            {
                throw new ArgumentException($"Invalid route parameter: {token}", nameof(_templateTokens));
            }
        }
    }

    private Action<object?[]> CreateInvoker(Delegate handler)
    {
        ParameterExpression values = Expression.Parameter(typeof(object?[]), "values");
        Expression[] arguments = _parameterTypes.Select((type, index) =>
            Expression.Convert(Expression.ArrayIndex(values, Expression.Constant(index)), type)).ToArray();
        Expression call = Expression.Invoke(Expression.Constant(handler), arguments);
        return Expression.Lambda<Action<object?[]>>(call, values).Compile();
    }

    private static bool IsParameter(string token) =>
        token.Length > 1 && token[0] == '{' && token[^1] == '}';

    private static bool IsSupportedType(string type) => GetType(type) is not null;

    private static Type? GetType(string type) => type switch
    {
        "int" => typeof(int),
        "string" => typeof(string),
        "Guid" => typeof(Guid),
        _ => null,
    };

    private static bool TryConvert(string value, Type type, out object? result)
    {
        if (type == typeof(string))
        {
            result = value;
            return true;
        }

        if (type == typeof(int) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
        {
            result = intValue;
            return true;
        }

        if (type == typeof(Guid) && Guid.TryParse(value, out Guid guidValue))
        {
            result = guidValue;
            return true;
        }

        result = null;
        return false;
    }
}
