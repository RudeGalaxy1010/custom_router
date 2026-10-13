using custom_router.di.api;

namespace custom_router.logic.api;

public interface IRouter : IService
{
    IDisposable Register(string path, Action onNext);
    IDisposable Register<T>(string path, Action<T> onNext);
    IDisposable Register<T1, T2>(string path, Action<T1, T2> onNext);
    void Process(string value);
}
