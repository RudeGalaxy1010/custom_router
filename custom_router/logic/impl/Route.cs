using System.Linq.Expressions;
using System.Reflection;
using System.ComponentModel;
using custom_router.logic.api;
using custom_router.validation.api;

namespace custom_router.logic.impl;

public class Route : IRoute
{
    private readonly string[] _templateTokens;
    private readonly ITokenizer _routeTokenizer;
    private readonly TypeConverter?[] _parameterConverters;
    private readonly int[] _templateParameterIndexes;
    private readonly Func<object?[], Task> _onNextAsync;

    public bool IsParameterized => _parameterConverters.Length > 0;
    public int Priority { get; private set; }

    public Route(
        ITokenizer routeTokenizer,
        ITokenizer paramsTokenizer,
        IValidator<RouteParameterValidation> parameterValidator,
        string template,
        Delegate onNext)
    {
        ArgumentNullException.ThrowIfNull(routeTokenizer);
        ArgumentNullException.ThrowIfNull(paramsTokenizer);
        ArgumentNullException.ThrowIfNull(parameterValidator);
        ArgumentException.ThrowIfNullOrEmpty(template);
        ArgumentNullException.ThrowIfNull(onNext);

        _routeTokenizer = routeTokenizer;
        _templateTokens = TokenizePath(template);

        string[] parameterNames = onNext.Method.GetParameters()
            .Select(parameter => parameter.Name ?? string.Empty)
            .ToArray();

        MethodInfo invoke = onNext.GetType().GetMethod("Invoke") ??
                            throw new ArgumentException("Handlers must expose an Invoke method.", nameof(onNext));
        Type[] parameterTypes = invoke.GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        _parameterConverters = new TypeConverter?[parameterTypes.Length];
        _templateParameterIndexes = Enumerable.Repeat(-1, _templateTokens.Length).ToArray();

        if (!typeof(Task).IsAssignableFrom(invoke.ReturnType))
        {
            throw new ArgumentException("Handlers must return Task or a subclass of Task.", nameof(onNext));
        }

        if (onNext.GetInvocationList().Length > 1)
        {
            throw new ArgumentException("Asynchronous multicast handlers are not supported.", nameof(onNext));
        }

        PrepareTemplate(paramsTokenizer, parameterValidator, template, parameterNames, parameterTypes);
        _onNextAsync = CreateInvoker(onNext, parameterTypes);
    }

    public Task ProcessAsync(object?[] parameters) =>
        _onNextAsync(parameters);

    public bool TryMatch(string route, out object?[] parameters)
    {
        string[] routeTokens = TokenizePath(route);

        if (routeTokens.Length != _templateTokens.Length)
        {
            parameters = [];
            return false;
        }

        parameters = new object?[_parameterConverters.Length];

        for (int i = 0; i < _templateTokens.Length; i++)
        {
            int parameterIndex = _templateParameterIndexes[i];

            if (parameterIndex < 0)
            {
                if (_templateTokens[i] != routeTokens[i])
                {
                    return false;
                }

                continue;
            }

            if (!TryConvert(routeTokens[i], _parameterConverters[parameterIndex], out object? value))
            {
                return false;
            }

            parameters[parameterIndex] = value;
        }

        return true;
    }

    private void PrepareTemplate(ITokenizer paramsTokenizer,
        IValidator<RouteParameterValidation> parameterValidator,
        string template,
        string[] parameterNames,
        Type[] parameterTypes)
    {
        HashSet<string> templateParameterNames = [];

        for (int index = 0; index < _templateTokens.Length; index++)
        {
            string token = _templateTokens[index];

            if (!IsParameter(token))
            {
                continue;
            }

            string[] parts = paramsTokenizer.Tokenize(token[1..^1]);

            if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]))
            {
                throw new ArgumentException($"Invalid route parameter: {token} in template: {template}",
                    nameof(template));
            }

            string parameterName = parts[0];
            int parameterIndex = Array.IndexOf(parameterNames, parameterName);

            bool isDuplicate = !templateParameterNames.Add(parameterName);
            Type? parameterType = parameterIndex < 0 ? null : parameterTypes[parameterIndex];
            TypeConverter? converter = null;

            if (parameterType != null && parameterType != typeof(string))
            {
                converter = TypeDescriptor.GetConverter(parameterType);
            }

            RouteParameterValidation validation = new(parameterName, parts[1], parameterType, converter, isDuplicate);

            if (!parameterValidator.Validate(validation))
            {
                throw new ArgumentException($"Invalid route parameter: {token} in template: {template}",
                    nameof(template));
            }

            _templateParameterIndexes[index] = parameterIndex;

            if (parameterType == typeof(string))
            {
                continue;
            }

            _parameterConverters[parameterIndex] = converter;
            Priority++;
        }

        if (_templateParameterIndexes.Count(index => index >= 0) != parameterTypes.Length)
        {
            throw new ArgumentException($"Template parameters do not match handler parameters: {template}",
                nameof(template));
        }
    }

    private Func<object?[], Task> CreateInvoker(Delegate handler, Type[] parameterTypes)
    {
        ParameterExpression values = Expression.Parameter(typeof(object?[]), "values");
        Expression[] arguments = parameterTypes.Select((type, index) =>
            Expression.Convert(Expression.ArrayIndex(values, Expression.Constant(index)), type)).ToArray<Expression>();
        Expression call = Expression.Invoke(Expression.Constant(handler), arguments);
        return Expression.Lambda<Func<object?[], Task>>(call, values).Compile();
    }

    private static bool IsParameter(string token) =>
        token.Length > 1 && token[0] == '{' && token[^1] == '}';

    private static bool TryConvert(string value, TypeConverter? converter, out object? result)
    {
        if (converter == null)
        {
            result = value;
            return true;
        }

        try
        {
            result = converter.ConvertFromInvariantString(value);
            return result != null;
        }
        // Числовые конвертеры могут оборачивать ошибки значения в ArgumentException.
        catch (Exception exception) when (
            exception is FormatException or OverflowException ||
            exception is ArgumentException { InnerException: FormatException or OverflowException }) { }

        result = null;
        return false;
    }

    private string[] TokenizePath(string path)
    {
        string[] tokens = _routeTokenizer.Tokenize(PathNormalization.Normalize(path));
        return path.EndsWith('\\') && tokens.Length > 1 && tokens[^1].Length == 0
            ? tokens[..^1]
            : tokens;
    }
}