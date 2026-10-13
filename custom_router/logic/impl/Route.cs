using System.Linq.Expressions;
using System.Reflection;
using System.ComponentModel;
using custom_router.logic.api;

namespace custom_router.logic.impl;

public class Route : IRoute
{
    private readonly string[] _templateTokens;
    private readonly ITokenizer _routeTokenizer;
    private readonly ITokenizer _paramsTokenizer;
    private readonly Type[] _parameterTypes;
    private readonly string[] _parameterNames;
    private readonly int[] _templateParameterIndexes;
    private readonly Action<object?[]> _onNext;
    private readonly Func<object?[], Task>? _onNextAsync;

    public bool IsParameterized => _parameterTypes.Length > 0;
    public bool IsAsync { get; }
    public int Specificity { get; private set; }

    public Route(
        string template,
        ITokenizer routeTokenizer,
        ITokenizer paramsTokenizer,
        Delegate onNext)
    {
        ArgumentException.ThrowIfNullOrEmpty(template);
        ArgumentNullException.ThrowIfNull(onNext);

        _routeTokenizer = routeTokenizer ?? throw new ArgumentNullException(nameof(routeTokenizer));
        _paramsTokenizer = paramsTokenizer ?? throw new ArgumentNullException(nameof(paramsTokenizer));
        _templateTokens = TokenizePath(template);
        ParameterInfo[] parameters = onNext.Method.GetParameters();
        _parameterTypes = parameters.Select(parameter => parameter.ParameterType).ToArray();
        _parameterNames = parameters.Select(parameter => parameter.Name ?? string.Empty).ToArray();
        _templateParameterIndexes = Enumerable.Repeat(-1, _templateTokens.Length).ToArray();
        IsAsync = onNext.Method.ReturnType == typeof(Task);
        ValidateTemplate();
        _onNext = IsAsync ? static _ => { } : CreateInvoker(onNext);
        _onNextAsync = IsAsync ? CreateAsyncInvoker(onNext) : null;
    }

    public bool TryProcess(string route)
    {
        string[] routeTokens = TokenizePath(route);
        if (routeTokens.Length != _templateTokens.Length)
        {
            return false;
        }

        object?[] parameters = new object?[_parameterTypes.Length];
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

            int parameterIndex = _templateParameterIndexes[index];
            if (!TryConvert(routeTokens[index], _parameterTypes[parameterIndex], out object? value))
            {
                return false;
            }

            parameters[parameterIndex] = value;
        }

        _onNext(parameters);
        return true;
    }

    public async Task<bool> TryProcessAsync(string route)
    {
        if (!TryGetParameters(route, out object?[] parameters))
        {
            return false;
        }

        if (IsAsync)
        {
            await _onNextAsync!(parameters);
        }
        else
        {
            _onNext(parameters);
        }

        return true;
    }

    private void ValidateTemplate()
    {
        int parameterCount = _templateTokens.Count(IsParameter);
        if (parameterCount != _parameterTypes.Length)
        {
            throw new ArgumentException("Template parameters do not match handler parameters.", nameof(_templateTokens));
        }

        HashSet<string> templateParameterNames = [];
        for (int index = 0; index < _templateTokens.Length; index++)
        {
            string token = _templateTokens[index];
            if (!IsParameter(token))
            {
                continue;
            }

            string[] parameterTokens = _paramsTokenizer.Tokenize(token[1..^1]);
            string parameterName = parameterTokens.ElementAtOrDefault(0) ?? string.Empty;
            string parameterTypeName = parameterTokens.ElementAtOrDefault(1) ?? string.Empty;
            int parameterIndex = Array.IndexOf(_parameterNames, parameterName);
            if (parameterTokens.Length != 2 || string.IsNullOrEmpty(parameterTokens[0]) ||
                !templateParameterNames.Add(parameterName) || parameterIndex < 0 ||
                !IsTypeMatch(parameterTypeName, _parameterTypes[parameterIndex]))
            {
                throw new ArgumentException($"Invalid route parameter: {token}", nameof(_templateTokens));
            }

            _templateParameterIndexes[index] = parameterIndex;
            if (_parameterTypes[parameterIndex] != typeof(string))
            {
                Specificity++;
            }
        }

        if (_templateParameterIndexes.Count(index => index >= 0) != _parameterTypes.Length)
        {
            throw new ArgumentException("Template parameters do not match handler parameters.", nameof(_templateTokens));
        }
    }

    private Action<object?[]> CreateInvoker(Delegate handler)
    {
        ParameterExpression values = Expression.Parameter(typeof(object?[]), "values");
        Expression[] arguments = _parameterTypes.Select((type, index) =>
            Expression.Convert(Expression.ArrayIndex(values, Expression.Constant(index)), type)).ToArray<Expression>();
        Expression call = Expression.Invoke(Expression.Constant(handler), arguments);
        return Expression.Lambda<Action<object?[]>>(call, values).Compile();
    }

    private Func<object?[], Task> CreateAsyncInvoker(Delegate handler)
    {
        ParameterExpression values = Expression.Parameter(typeof(object?[]), "values");
        Expression[] arguments = _parameterTypes.Select((type, index) =>
            Expression.Convert(Expression.ArrayIndex(values, Expression.Constant(index)), type)).ToArray<Expression>();
        Expression call = Expression.Invoke(Expression.Constant(handler), arguments);
        return Expression.Lambda<Func<object?[], Task>>(call, values).Compile();
    }

    private static bool IsParameter(string token) =>
        token.Length > 1 && token[0] == '{' && token[^1] == '}';

    private static bool IsTypeMatch(string typeName, Type parameterType) =>
        typeName == parameterType.Name ||
        typeName == parameterType.FullName ||
        parameterType == typeof(int) && typeName == "int" ||
        parameterType == typeof(string) && typeName == "string" ||
        parameterType == typeof(bool) && typeName == "bool" ||
        parameterType == typeof(double) && typeName == "double" ||
        parameterType == typeof(float) && typeName == "float" ||
        parameterType == typeof(decimal) && typeName == "decimal" ||
        parameterType == typeof(long) && typeName == "long" ||
        parameterType == typeof(short) && typeName == "short" ||
        parameterType == typeof(byte) && typeName == "byte" ||
        parameterType == typeof(Guid) && typeName == "Guid";

    private static bool TryConvert(string value, Type type, out object? result)
    {
        if (type == typeof(string))
        {
            result = value;
            return true;
        }

        TypeConverter converter = TypeDescriptor.GetConverter(type);
        if (converter.CanConvertFrom(typeof(string)))
        {
            try
            {
                result = converter.ConvertFromInvariantString(value);
                return result is not null;
            }
            catch (Exception) when (converter is not null)
            {
                // Такое значение означает, что маршрут не подошёл.
            }
        }

        result = null;
        return false;
    }

    private string[] TokenizePath(string path)
    {
        string[] tokens = _routeTokenizer.Tokenize(path);
        return tokens.Length > 1 && tokens[^1].Length == 0
            ? tokens[..^1]
            : tokens;
    }

    private bool TryGetParameters(string? route, out object?[] parameters)
    {
        if (!IsAsync || route == null)
        {
            parameters = [];
            return false;
        }

        string[] routeTokens = TokenizePath(route);
        if (routeTokens.Length != _templateTokens.Length)
        {
            parameters = [];
            return false;
        }

        parameters = new object?[_parameterTypes.Length];
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

            int parameterIndex = _templateParameterIndexes[index];
            if (!TryConvert(routeTokens[index], _parameterTypes[parameterIndex], out object? value))
            {
                return false;
            }

            parameters[parameterIndex] = value;
        }

        return true;
    }
}
