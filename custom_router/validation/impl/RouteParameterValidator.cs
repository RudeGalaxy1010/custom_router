using custom_router.validation.api;

namespace custom_router.validation.impl;

public class RouteParameterValidator : IValidator<RouteParameterValidation>
{
    public bool Validate(RouteParameterValidation value)
    {
        if (value == null || string.IsNullOrEmpty(value.Name) || value.IsDuplicate || value.ParameterType == null ||
            !IsTypeMatch(value.TypeName, value.ParameterType))
        {
            return false;
        }

        return value.ParameterType == typeof(string) ||
               (value.Converter != null && value.Converter.CanConvertFrom(typeof(string)));
    }

    private static bool IsTypeMatch(string typeName, Type parameterType)
    {
        string? alias = Type.GetTypeCode(parameterType) switch
        {
            TypeCode.Int32 => "int",
            TypeCode.String => "string",
            TypeCode.Boolean => "bool",
            TypeCode.Double => "double",
            TypeCode.Single => "float",
            TypeCode.Decimal => "decimal",
            TypeCode.Int64 => "long",
            TypeCode.Int16 => "short",
            TypeCode.Byte => "byte",
            TypeCode.Char => "char",
            TypeCode.UInt32 => "uint",
            TypeCode.UInt16 => "ushort",
            TypeCode.UInt64 => "ulong",
            TypeCode.SByte => "sbyte",
            _ when parameterType == typeof(object) => "object",
            _ => null
        };

        if (parameterType.IsEnum)
        {
            alias = null;
        }

        return string.Equals(typeName, parameterType.Name, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(typeName, parameterType.FullName, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(typeName, alias, StringComparison.OrdinalIgnoreCase);
    }
}
