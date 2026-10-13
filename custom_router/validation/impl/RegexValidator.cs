using System.Text.RegularExpressions;
using custom_router.validation.api;

namespace custom_router.validation.impl;

public class RegexValidator(string forbiddenPattern) : IValidator<string>
{
    private readonly Regex _regex = new Regex(forbiddenPattern);

    public bool Validate(string value) =>
        !string.IsNullOrEmpty(value) && !_regex.IsMatch(value);
}
