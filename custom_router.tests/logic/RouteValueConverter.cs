using System.ComponentModel;
using System.Globalization;

namespace custom_router.logic.tests;

public sealed class RouteValueConverter : TypeConverter
{
    public int CapabilityChecks { get; private set; }
    public int ConversionCalls { get; private set; }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        CapabilityChecks++;
        return sourceType == typeof(string);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        ConversionCalls++;
        return value switch
        {
            "format" => throw new FormatException(),
            "overflow" => throw new OverflowException(),
            "unsupported" => throw new NotSupportedException(),
            "wrapped-format" => throw new ArgumentException("Invalid value", new FormatException()),
            "wrapped-overflow" => throw new ArgumentException("Invalid value", new OverflowException()),
            "bug" => throw new InvalidOperationException(),
            "wrong-type" => 42,
            string text => new RouteValue(text),
            _ => base.ConvertFrom(context, culture, value)
        };
    }
}
