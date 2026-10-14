using System.ComponentModel;

namespace custom_router.validation.api;

public record RouteParameterValidation(
    string Name,
    string TypeName,
    Type? ParameterType,
    TypeConverter? Converter,
    bool IsDuplicate);