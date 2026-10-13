using custom_router.di.api;
using custom_router.di.impl;
using custom_router.logic.api;
using custom_router.logic.impl;
using custom_router.validation.api;
using custom_router.validation.impl;

namespace custom_router;

public class Program
{
    public static void Main(string[] args)
    {
        IServiceLocator serviceLocator = CreateServices();

        IRouter router = serviceLocator.Resolve<IRouter>();
        using var handle = router.Register("main/page/",
            () => Console.WriteLine("Main page"));

        using var handle1 = router.Register<int>("main/page/{a:int}",
            a => Console.WriteLine($"Main page {a}"));

        using var handle2 = router.Register<int, int>(
            "users/{a:int}/{b:int}",
            (b, a) => Console.WriteLine($"a = {a}, b = {b}"));

        router.Process("main/page/");
        router.Process("main/page/1");
        router.Process("users/10/20");
    }

    private static IServiceLocator CreateServices()
    {
        IServiceLocator serviceLocator = new ServiceLocator();

        ITokenizer routeTokenizer = new SimpleTokenizer('/', '\\');
        ITokenizer paramsTokenizer = new SimpleTokenizer(':');
        IRouteFactory routeFactory = new RouteFactory(routeTokenizer, paramsTokenizer);
        IValidator<string> pathValidator = new RegexValidator(@"\s|//");
        IRouter router = new Router(pathValidator, routeFactory);

        serviceLocator.BindInterface(router);

        return serviceLocator;
    }
}
