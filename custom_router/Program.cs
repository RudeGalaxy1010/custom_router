using custom_router.di.api;
using custom_router.di.impl;
using custom_router.logic.api;
using custom_router.logic.impl;
using custom_router.validation.api;
using custom_router.validation.impl;

namespace custom_router;

public class Program
{
    public async static Task Main(string[] args)
    {
        IServiceLocator serviceLocator = CreateServices();
        IRouter router = serviceLocator.Resolve<IRouter>();

        using var handle = router.Register("main/page/",
            () =>
            {
                Console.WriteLine("Main page");
                return Task.CompletedTask;
            });

        using var handle1 = router.Register("main/page/{a:int}",
            (int a) =>
            {
                Console.WriteLine($"Main page {a}");
                return Task.CompletedTask;
            });

        using var handle2 = router.Register(
            "users/{a:int}/{b:int}",
            (int b, int a) =>
            {
                Console.WriteLine($"a = {a}, b = {b}");
                return Task.CompletedTask;
            });

        using var handle3 = router.Register(
            "async/users/{id:int}",
            async (int id) =>
            {
                await Task.Delay(10);
                Console.WriteLine($"Async user {id}");
            });

        await router.ProcessAsync("main/page/");
        await router.ProcessAsync("main/page/1");
        await router.ProcessAsync("users/10/20");
        await router.ProcessAsync("async/users/42");
    }

    private static IServiceLocator CreateServices()
    {
        IServiceLocator serviceLocator = new ServiceLocator();

        ITokenizer routeTokenizer = new SimpleTokenizer('/', '\\');
        ITokenizer paramsTokenizer = new SimpleTokenizer(':');
        IValidator<RouteParameterValidation> parameterValidator = new RouteParameterValidator();
        IRouteFactory routeFactory = new RouteFactory(routeTokenizer, paramsTokenizer, parameterValidator);
        IValidator<string> pathValidator = new RegexValidator(@"\s|//");
        IRouter router = new Router(pathValidator, routeFactory);

        serviceLocator.BindInterface(router);

        return serviceLocator;
    }
}
