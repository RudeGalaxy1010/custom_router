using custom_router.di.api;

namespace custom_router.logic.api;

public interface IRouter : IService
{
    IDisposable Register(string path, Action onNext);
    void Process(string value);
}