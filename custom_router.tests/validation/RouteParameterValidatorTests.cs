using System.ComponentModel;
using NUnit.Framework;
using custom_router.logic.tests;
using custom_router.validation.api;
using custom_router.validation.impl;

namespace custom_router.validation.tests;

public class RouteParameterValidatorTests
{
    [TestCase("int", typeof(int))]
    [TestCase("INT", typeof(int))]
    [TestCase("Int32", typeof(int))]
    [TestCase("system.int32", typeof(int))]
    [TestCase("bool", typeof(bool))]
    [TestCase("double", typeof(double))]
    [TestCase("float", typeof(float))]
    [TestCase("decimal", typeof(decimal))]
    [TestCase("long", typeof(long))]
    [TestCase("short", typeof(short))]
    [TestCase("byte", typeof(byte))]
    [TestCase("CHAR", typeof(char))]
    [TestCase("UINT", typeof(uint))]
    [TestCase("USHORT", typeof(ushort))]
    [TestCase("ULONG", typeof(ulong))]
    [TestCase("SBYTE", typeof(sbyte))]
    [TestCase("guid", typeof(Guid))]
    [TestCase("SYSTEM.GUID", typeof(Guid))]
    [TestCase("DayOfWeek", typeof(DayOfWeek))]
    [TestCase("routevalue", typeof(RouteValue))]
    [TestCase("CUSTOM_ROUTER.LOGIC.TESTS.ROUTEVALUE", typeof(RouteValue))]
    public void Validate_MatchingType_ReturnsTrue(string typeName, Type parameterType)
    {
        // Arrange
        RouteParameterValidator validator = Create();
        TypeConverter converter = TypeDescriptor.GetConverter(parameterType);
        // Act
        bool result = validator.Validate(new RouteParameterValidation("value", typeName, parameterType, converter, false));
        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("string")]
    [TestCase("STRING")]
    [TestCase("system.string")]
    public void Validate_StringWithoutConverter_ReturnsTrue(string typeName)
    {
        // Arrange
        RouteParameterValidator validator = Create();
        // Act
        bool result = validator.Validate(new RouteParameterValidation("value", typeName, typeof(string), null, false));
        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("", "int", typeof(int), false)]
    [TestCase("missing", "int", null, false)]
    [TestCase("value", "int", typeof(int), true)]
    [TestCase("value", "", typeof(int), false)]
    [TestCase("value", "unknown", typeof(int), false)]
    [TestCase("value", "string", typeof(int), false)]
    [TestCase("value", "int", typeof(DayOfWeek), false)]
    public void Validate_InvalidFacts_ReturnsFalse(
        string name, string typeName, Type? parameterType, bool isDuplicate)
    {
        // Arrange
        RouteParameterValidator validator = Create();
        TypeConverter? converter = parameterType == null ? null : TypeDescriptor.GetConverter(parameterType);
        // Act
        bool result = validator.Validate(new RouteParameterValidation(name, typeName, parameterType, converter, isDuplicate));
        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Validate_UnsupportedConverter_ReturnsFalse(bool hasConverter)
    {
        // Arrange
        RouteParameterValidator validator = Create();
        TypeConverter? converter = hasConverter ? TypeDescriptor.GetConverter(typeof(object)) : null;
        // Act
        bool result = validator.Validate(new RouteParameterValidation("value", "object", typeof(object), converter, false));
        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Validate_NullValue_ReturnsFalse()
    {
        // Arrange
        RouteParameterValidator validator = Create();
        // Act
        bool result = validator.Validate(null!);
        // Assert
        Assert.That(result, Is.False);
    }

    private static RouteParameterValidator Create() => new();
}
