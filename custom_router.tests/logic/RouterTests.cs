using NUnit.Framework;
using custom_router.logic.api;
using custom_router.logic.impl;
using custom_router.validation.api;
using custom_router.validation.impl;

namespace custom_router.logic.tests;

public class RouterTests
{
    [Test]
    public void Constructor_NullPathValidator_ThrowsArgumentNullException()
    {
        // Arrange
        IRouteFactory factory = CreateRouteFactory();
        // Act
        TestDelegate action = () => _ = new Router(null!, factory);
        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Test]
    public void Constructor_NullRouteFactory_ThrowsArgumentNullException()
    {
        // Arrange
        IValidator<string> validator = CreatePathValidator();
        // Act
        TestDelegate action = () => _ = new Router(validator, null!);
        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [TestCase("main/page", "main/page/")]
    [TestCase("main/page/", "main/page")]
    [TestCase("/", "/")]
    public async Task ProcessAsync_StaticRoute_InvokesHandler(string template, string path)
    {
        // Arrange
        IRouter router = Create();
        int calls = 0;
        using IDisposable handle = router.Register(template, () => { calls++; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync(path);
        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    [TestCase("main/page/details/")]
    [TestCase("main/home/")]
    [TestCase("other/page/")]
    public void ProcessAsync_UnknownRoute_ThrowsKeyNotFoundException(string path)
    {
        // Arrange
        IRouter router = Create();
        using IDisposable handle = router.Register("main/page/", () => Task.CompletedTask);
        // Act
        // Assert
        KeyNotFoundException exception = Assert.ThrowsAsync<KeyNotFoundException>(() => router.ProcessAsync(path))!;
        Assert.That(exception.Message, Does.Contain(path));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("main//page/")]
    [TestCase("main page/")]
    public void Register_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Arrange
        IRouter router = Create();
        // Act
        TestDelegate action = () => router.Register(path!, () => Task.CompletedTask);
        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("users//")]
    [TestCase("users/a b")]
    public void ProcessAsync_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Arrange
        IRouter router = Create();
        // Act
        // Assert
        Assert.ThrowsAsync<ArgumentException>(() => router.ProcessAsync(path!));
    }

    [Test]
    public void Register_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        IRouter router = Create();
        // Act
        TestDelegate action = () => router.Register("main/page", null!);
        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [TestCase("users/10", "users/10")]
    [TestCase("users/10", "users/10/")]
    [TestCase("users/10/", "users/10")]
    [TestCase("users/{id:int}", "users/{id:int}/")]
    [TestCase("users/{id:int}/", "users/{id:int}")]
    [TestCase("/", "/")]
    public void Register_CanonicalDuplicate_ThrowsInvalidOperationException(string first, string second)
    {
        // Arrange
        IRouter router = Create();
        Delegate handler = first.Contains('{')
            ? (Func<int, Task>)((int id) => Task.CompletedTask)
            : (Func<Task>)(() => Task.CompletedTask);
        using IDisposable handle = router.Register(first, handler);
        // Act
        TestDelegate action = () => router.Register(second, handler);
        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Test]
    public void ProcessAsync_DisposedRoute_ThrowsKeyNotFoundException()
    {
        // Arrange
        IRouter router = Create();
        IDisposable handle = router.Register("main/page", () => Task.CompletedTask);
        // Act
        handle.Dispose();
        // Assert
        Assert.ThrowsAsync<KeyNotFoundException>(() => router.ProcessAsync("main/page"));
    }

    [Test]
    public async Task Dispose_OldHandleAfterReplacement_PreservesNewRoute()
    {
        // Arrange
        IRouter router = Create();
        IDisposable first = router.Register("main/page/", () => Task.CompletedTask);
        first.Dispose();
        int calls = 0;
        using IDisposable second = router.Register("main/page", () => { calls++; return Task.CompletedTask; });
        // Act
        first.Dispose();
        await router.ProcessAsync("main/page/");
        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    [TestCase("users/{id:int}", "users/42")]
    [TestCase("users/{id:Int32}/", "users/42/")]
    [TestCase("users/{id:System.Int32}", "users/42")]
    [TestCase("users/{id:INT}", "users/42")]
    [TestCase("users/{id:system.int32}/", "users/42")]
    [TestCase("users/{id:int}", "users/42/")]
    public async Task ProcessAsync_IntegerParameter_PassesConvertedValue(string template, string path)
    {
        // Arrange
        IRouter router = Create();
        int actual = 0;
        using IDisposable handle = router.Register(template, (int id) => { actual = id; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync(path);
        // Assert
        Assert.That(actual, Is.EqualTo(42));
    }

    [Test]
    public async Task ProcessAsync_ReversedTypedParameters_BindsByName()
    {
        // Arrange
        IRouter router = Create();
        (int, string, int) actual = default;
        using IDisposable handle = router.Register("users/{id:int}/{name:string}/{count:int}",
            (int count, string name, int id) => { actual = (id, name, count); return Task.CompletedTask; });
        // Act
        await router.ProcessAsync("users/42/alice/7");
        // Assert
        Assert.That(actual, Is.EqualTo((42, "alice", 7)));
    }

    [Test]
    public async Task ProcessAsync_ReversedIntegerParameters_BindsByName()
    {
        // Arrange
        IRouter router = Create();
        (int, int) actual = default;
        using IDisposable handle = router.Register("users/{a:int}/{b:int}",
            (int b, int a) => { actual = (a, b); return Task.CompletedTask; });
        // Act
        await router.ProcessAsync("users/10/20");
        // Assert
        Assert.That(actual, Is.EqualTo((10, 20)));
    }

    [TestCase("Guid")]
    [TestCase("guid")]
    [TestCase("System.Guid")]
    [TestCase("SYSTEM.GUID")]
    public async Task ProcessAsync_GuidParameter_PassesValue(string typeName)
    {
        // Arrange
        IRouter router = Create();
        Guid expected = Guid.NewGuid();
        Guid actual = Guid.Empty;
        using IDisposable handle = router.Register($"users/{{id:{typeName}}}", (Guid id) => { actual = id; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync($"users/{expected}");
        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_DoubleParameter_ConvertsInvariantValue()
    {
        // Arrange
        IRouter router = Create();
        double actual = 0;
        using IDisposable handle = router.Register("values/{value:double}", (double value) => { actual = value; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync("values/1.5");
        // Assert
        Assert.That(actual, Is.EqualTo(1.5));
    }

    [TestCase("users/{id:string}")]
    [TestCase("users/{userId:int}")]
    [TestCase("users/{id:int}/{id:int}")]
    [TestCase("users/{id:int:extra}")]
    [TestCase("users")]
    [TestCase("users/{id:int}/{other:int}")]
    [TestCase("users/{ID:int}")]
    [TestCase("users/{id}")]
    [TestCase("users/{:int}")]
    [TestCase("users/{id:}")]
    [TestCase("users/{}")]
    [TestCase("users/{id:int")]
    [TestCase("users/id:int}")]
    public void Register_InvalidTemplateBinding_ThrowsArgumentException(string template)
    {
        // Arrange
        IRouter router = Create();
        // Act
        TestDelegate action = () => router.Register(template, (int id) => Task.CompletedTask);
        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [TestCase("Object")]
    [TestCase("OBJECT")]
    public void Register_UnsupportedConversion_ThrowsArgumentException(string typeName)
    {
        // Arrange
        IRouter router = Create();
        // Act
        TestDelegate action = () => router.Register($"users/{{id:{typeName}}}", (object id) => Task.CompletedTask);
        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [TestCase("not-an-int")]
    [TestCase("999999999999999999999999")]
    public void ProcessAsync_InvalidInteger_DoesNotInvokeHandler(string value)
    {
        // Arrange
        IRouter router = Create();
        int calls = 0;
        using IDisposable handle = router.Register("users/{id:int}", (int id) => { calls++; return Task.CompletedTask; });
        // Act
        // Assert
        Assert.ThrowsAsync<KeyNotFoundException>(() => router.ProcessAsync($"users/{value}"));
        Assert.That(calls, Is.Zero);
    }

    [TestCase("not-an-int")]
    [TestCase("999999999999999999999999")]
    public async Task ProcessAsync_InvalidInteger_InvokesStringFallback(string value)
    {
        // Arrange
        IRouter router = Create();
        int calls = 0;
        string actual = string.Empty;
        using IDisposable typed = router.Register("users/{id:int}", (int id) => { calls++; return Task.CompletedTask; });
        using IDisposable fallback = router.Register("users/{id:string}", (string id) => { actual = id; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync($"users/{value}");
        // Assert
        Assert.That(calls, Is.Zero);
        Assert.That(actual, Is.EqualTo(value));
    }

    [Test]
    public async Task ProcessAsync_StaticRoute_OverridesParameterizedRoute()
    {
        // Arrange
        IRouter router = Create();
        string actual = string.Empty;
        using IDisposable typed = router.Register("users/{id:int}", (int id) => { actual = "typed"; return Task.CompletedTask; });
        using IDisposable plain = router.Register("users/10", () => { actual = "static"; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync("users/10");
        // Assert
        Assert.That(actual, Is.EqualTo("static"));
    }

    [Test]
    public async Task ProcessAsync_HigherSpecificity_OverridesStringRoute()
    {
        // Arrange
        IRouter router = Create();
        string actual = string.Empty;
        using IDisposable fallback = router.Register("users/{id:string}/{count:string}",
            (string id, string count) => { actual = "string"; return Task.CompletedTask; });
        using IDisposable mixed = router.Register("users/{id:int}/{count:string}",
            (int id, string count) => { actual = "mixed"; return Task.CompletedTask; });
        using IDisposable typed = router.Register("users/{id:int}/{count:int}",
            (int id, int count) => { actual = "typed"; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync("users/10/20");
        // Assert
        Assert.That(actual, Is.EqualTo("typed"));
    }

    [Test]
    public async Task ProcessAsync_EqualPriority_PreservesRegistrationOrder()
    {
        // Arrange
        IRouter router = Create();
        string actual = string.Empty;
        using IDisposable first = router.Register("users/{id:int}", (int id) => { actual = "first"; return Task.CompletedTask; });
        using IDisposable second = router.Register("users/{number:long}", (long number) => { actual = "second"; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync("users/10");
        // Assert
        Assert.That(actual, Is.EqualTo("first"));
    }

    [Test]
    public async Task ProcessAsync_ContravariantHandler_UsesDelegateParameterType()
    {
        // Arrange
        IRouter router = Create();
        object? actual = null;
        Task Handler(object id) { actual = id; return Task.CompletedTask; }
        Func<string, Task> handler = Handler;
        using IDisposable handle = router.Register("users/{id:string}", handler);
        // Act
        await router.ProcessAsync("users/alice");
        // Assert
        Assert.That(actual, Is.EqualTo("alice"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ProcessAsync_TaskResult_AwaitsDelegateContract(bool covariant)
    {
        // Arrange
        IRouter router = Create();
        TaskCompletionSource<int> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> Handler() => release.Task;
        Delegate handler = covariant ? (Func<Task>)Handler : (Func<Task<int>>)Handler;
        using IDisposable handle = router.Register("users/10", handler);
        // Act
        Task processing = router.ProcessAsync("users/10");
        try
        {
            // Assert
            Assert.That(processing.IsCompleted, Is.False);
            release.TrySetResult(42);
            await processing.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(processing.IsCompletedSuccessfully, Is.True);
        }
        finally
        {
            release.TrySetResult(42);
            await processing.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Test]
    public async Task ProcessAsync_AsyncHandler_AwaitsCompletion()
    {
        // Arrange
        IRouter router = Create();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        using IDisposable handle = router.Register("users/10", async () =>
        {
            entered.SetResult();
            await release.Task;
            calls++;
        });
        // Act
        Task processing = router.ProcessAsync("users/10");
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            // Assert
            Assert.That(processing.IsCompleted, Is.False);
            Assert.That(calls, Is.Zero);
            release.TrySetResult();
            await processing.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(calls, Is.EqualTo(1));
        }
        finally
        {
            release.TrySetResult();
            await processing.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ProcessAsync_HandlerThrows_PropagatesException(bool synchronousThrow)
    {
        // Arrange
        IRouter router = Create();
        using IDisposable handle = router.Register("users/10", () => synchronousThrow
            ? throw new InvalidOperationException("handler failed")
            : Task.FromException(new InvalidOperationException("handler failed")));
        // Act
        // Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => router.ProcessAsync("users/10"));
    }

    [Test]
    public void Register_MulticastHandler_RejectsWithoutInvocation()
    {
        // Arrange
        IRouter router = Create();
        int calls = 0;
        Task Handler() { calls++; return Task.CompletedTask; }
        Func<Task> handler = Handler;
        handler += Handler;
        // Act
        TestDelegate action = () => router.Register("users/10", handler);
        // Assert
        Assert.Throws<ArgumentException>(action);
        Assert.That(calls, Is.Zero);
        Assert.ThrowsAsync<KeyNotFoundException>(() => router.ProcessAsync("users/10"));
    }

    [Test]
    public void Register_NonTaskContracts_RejectsWithoutInvocation()
    {
        // Arrange
        IRouter router = Create();
        int calls = 0;
        Action plain = () => calls++;
        Action asynchronous = async () => { await Task.Yield(); calls++; };
        Func<int> value = () => ++calls;
        Task TaskHandler() { calls++; return Task.CompletedTask; }
        Func<object> erasedTask = TaskHandler;
        Func<ValueTask> valueTask = () => { calls++; return ValueTask.CompletedTask; };
        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => router.Register("plain", plain));
        Assert.Throws<ArgumentException>(() => router.Register("async-void", asynchronous));
        Assert.Throws<ArgumentException>(() => router.Register("value", value));
        Assert.Throws<ArgumentException>(() => router.Register("erased", erasedTask));
        Assert.Throws<ArgumentException>(() => router.Register("value-task", valueTask));
        Assert.That(calls, Is.Zero);
    }

    [Test]
    public async Task ProcessAsync_CustomDelegate_BindsTargetNames()
    {
        // Arrange
        IRouter router = Create();
        int actual = 0;
        Task<int> Handler(int id) { actual = id; return Task.FromResult(id); }
        RouteHandler handler = Handler;
        using IDisposable handle = router.Register("users/{id:int}", handler);
        // Act
        await router.ProcessAsync("users/42");
        // Assert
        Assert.That(actual, Is.EqualTo(42));
    }

    [TestCase("RouteValue")]
    [TestCase("custom_router.logic.tests.RouteValue")]
    [TestCase("routevalue")]
    [TestCase("CUSTOM_ROUTER.LOGIC.TESTS.ROUTEVALUE")]
    public async Task ProcessAsync_CustomConverter_UsesTypeName(string typeName)
    {
        // Arrange
        IRouter router = Create();
        RouteValue? actual = null;
        using IDisposable handle = router.Register($"values/{{value:{typeName}}}",
            (RouteValue value) => { actual = value; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync("values/hello");
        // Assert
        Assert.That(actual?.Value, Is.EqualTo("hello"));
    }

    [TestCase("format")]
    [TestCase("overflow")]
    [TestCase("wrapped-format")]
    [TestCase("wrapped-overflow")]
    public async Task ProcessAsync_ExpectedConverterFailure_UsesFallback(string value)
    {
        // Arrange
        IRouter router = Create();
        string actual = string.Empty;
        using IDisposable typed = router.Register("values/{value:RouteValue}", (RouteValue value) => Task.CompletedTask);
        using IDisposable fallback = router.Register("values/{value:string}",
            (string value) => { actual = value; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync($"values/{value}");
        // Assert
        Assert.That(actual, Is.EqualTo(value));
    }

    [Test]
    public void ProcessAsync_UnexpectedConverterFailure_PropagatesException()
    {
        // Arrange
        IRouter router = Create();
        using IDisposable handle = router.Register("values/{value:RouteValue}", (RouteValue value) => Task.CompletedTask);
        // Act
        // Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => router.ProcessAsync("values/bug"));
    }

    [Test]
    public void ProcessAsync_UnsupportedConverterFailure_PropagatesException()
    {
        // Arrange
        IRouter router = Create();
        using IDisposable typed = router.Register("values/{value:RouteValue}", (RouteValue value) => Task.CompletedTask);
        using IDisposable fallback = router.Register("values/{value:string}", (string value) => Task.CompletedTask);
        // Act
        // Assert
        Assert.ThrowsAsync<NotSupportedException>(() => router.ProcessAsync("values/unsupported"));
    }

    [Test]
    public void ProcessAsync_WrongConverterResult_PropagatesException()
    {
        // Arrange
        IRouter router = Create();
        using IDisposable handle = router.Register("values/{value:RouteValue}", (RouteValue value) => Task.CompletedTask);
        // Act
        // Assert
        Assert.ThrowsAsync<InvalidCastException>(() => router.ProcessAsync("values/wrong-type"));
    }

    [TestCase("main\\page", "main/page")]
    [TestCase("main/page", "main\\page")]
    [TestCase("main/page", "main/page\\")]
    [TestCase("main/page\\", "main/page")]
    public async Task ProcessAsync_BackslashSeparator_PreservesMatching(string template, string path)
    {
        // Arrange
        IRouter router = Create();
        int calls = 0;
        using IDisposable handle = router.Register(template, () => { calls++; return Task.CompletedTask; });
        // Act
        await router.ProcessAsync(path);
        // Assert
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public async Task ProcessAsync_ConcurrentReaders_IsolatesParameters()
    {
        // Arrange
        IRouter router = Create();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readers = 0;
        int[] actual = new int[100];
        using IDisposable handle = router.Register("users/{id:int}", async (int id) =>
        {
            if (Interlocked.Increment(ref readers) == actual.Length)
            {
                entered.SetResult();
            }

            await release.Task;
            actual[id] = id + 1;
        });
        // Act
        Task[] requests = Enumerable.Range(0, actual.Length)
            .Select(id => Task.Run(() => router.ProcessAsync($"users/{id}"))).ToArray();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            release.TrySetResult();
            await Task.WhenAll(requests).WaitAsync(TimeSpan.FromSeconds(5));
            // Assert
            Assert.That(actual, Is.EqualTo(Enumerable.Range(1, actual.Length)));
        }
        finally
        {
            release.TrySetResult();
            await Task.WhenAll(requests).WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static IRouter Create(IValidator<string>? pathValidator = null, IRouteFactory? routeFactory = null) =>
        new Router(pathValidator ?? CreatePathValidator(), routeFactory ?? CreateRouteFactory());

    private static IValidator<string> CreatePathValidator() => new RegexValidator(@"\s|//");

    private static IRouteFactory CreateRouteFactory() => new RouteFactory(new SimpleTokenizer('/', '\\'),
        new SimpleTokenizer(':'), new RouteParameterValidator());
}
