using NUnit.Framework;
using custom_router.logic.impl;

namespace custom_router.tests.logic;

public class TokenizerTests
{
    [Test]
    public void Tokenize_ConfiguredSeparators_SplitsInput()
    {
        // Arrange
        SimpleTokenizer tokenizer = new('/', '\\');

        // Act
        string[] tokens = tokenizer.Tokenize("/foo/bar\\baz/");

        // Assert
        Assert.That(tokens, Is.EqualTo(["", "foo", "bar", "baz", ""]));
    }

    [Test]
    public void Tokenize_RepeatedSeparator_PreservesEmptySegment()
    {
        // Arrange
        SimpleTokenizer tokenizer = new('/');

        // Act
        string[] tokens = tokenizer.Tokenize("foo//bar");

        // Assert
        Assert.That(tokens, Is.EqualTo(["foo", "", "bar"]));
    }
}
