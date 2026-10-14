using System.ComponentModel;

namespace custom_router.logic.tests;

[TypeConverter(typeof(RouteValueConverter))]
public sealed record RouteValue(string Value);
