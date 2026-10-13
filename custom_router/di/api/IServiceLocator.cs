namespace custom_router.di.api;

public interface IServiceLocator
{
    T Resolve<T>() where T : IService;
    void BindInterface<TService>(TService service) where TService : IService;
}