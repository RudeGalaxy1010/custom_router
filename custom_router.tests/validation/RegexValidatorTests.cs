using NUnit.Framework;
using custom_router.validation.impl;

namespace custom_router.tests.validation;

public class RegexValidatorTests
{
    private readonly RegexValidator _validator = new(@"\s|//");

    [TestCase("foo/bar")]
    [TestCase("/foo/{id:int}")]
    public void Validate_ForbiddenPatternAbsent_ReturnsTrue(string value)
    {
        // Arrange

        // Act
        bool result = _validator.Validate(value);

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("foo//bar")]
    [TestCase("foo bar")]
    [TestCase("foo\tbar")]
    public void Validate_ForbiddenPatternPresent_ReturnsFalse(string value)
    {
        // Arrange

        // Act
        bool result = _validator.Validate(value);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Validate_NullOrEmptyValue_ReturnsFalse()
    {
        // Arrange

        // Act
        bool nullResult = _validator.Validate(null!);
        bool emptyResult = _validator.Validate(string.Empty);

        // Assert
        Assert.That(nullResult, Is.False);
        Assert.That(emptyResult, Is.False);
    }
}
