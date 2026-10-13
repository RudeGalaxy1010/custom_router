using custom_router.logic.api;

namespace custom_router.logic.impl;

public class SimpleTokenizer(params char[] separators) : ITokenizer
{
    public string[] Tokenize(string input) => input
        .Split(separators);
}