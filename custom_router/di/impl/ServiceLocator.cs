using custom_router.di.api;

namespace custom_router.di.impl;

public class ServiceLocator : IServiceLocator
{
    private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();

    public T Resolve<T>() where T : IService
    {
        if (!_services.TryGetValue(typeof(T), out IService? service))
        {
            throw new KeyNotFoundException($"Service is not bound: {typeof(T)}");
        }

        return (T)service;
    }

    public void BindInterface<TService>(TService service) where TService : IService
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (!_services.TryAdd(typeof(TService), service))
        {
            throw new InvalidOperationException($"Service is already bound: {typeof(TService)}");
        }
    }
}
