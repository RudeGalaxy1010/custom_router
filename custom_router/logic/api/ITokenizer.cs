using custom_router.di.api;

namespace custom_router.logic.api;

public interface ITokenizer : IService
{
    string[] Tokenize(string input);
}