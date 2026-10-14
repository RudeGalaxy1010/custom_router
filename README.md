# CustomRouter

Маршрутизатор строковых путей на C# / .NET 9

## Использование

```csharp
using custom_router.logic.impl;
using custom_router.validation.impl;

var router = new Router(
    new RegexValidator(@"\s|//"),
    new RouteFactory(
        new SimpleTokenizer('/', '\\'),
        new SimpleTokenizer(':'),
        new RouteParameterValidator()));

using var page = router.Register("page", () => Task.CompletedTask);
using var users = router.Register("users/{a:int}/{b:int}", async (int b, int a) =>
{
    await Task.Delay(10);
    Console.WriteLine($"a={a}, b={b}");
});

await router.ProcessAsync("users/10/20"); // a=10, b=20
```

## Правила

- Обработчик возвращает `Task` или `Task<T>`; результат `T` игнорируется. `void`, `ValueTask` и multicast не поддерживаются.
- Параметры `{name:type}` связываются с аргументами по имени с учётом регистра. Типы в lambda указываются явно.
- Имена типов, полные имена и C#-алиасы сравниваются без учёта регистра. Поддерживаются типы со строковым `TypeConverter`; преобразование использует invariant culture.
- Пустой путь, пробелы и `//` запрещены. Завершающий `/` незначим, начальный значим. Дубликаты запрещены; `Dispose` удаляет регистрацию.
- Приоритет: статические маршруты, затем большее число нетекстовых параметров, затем порядок регистрации.
- Ошибки формата и переполнения допускают поиск следующего маршрута. Остальные ошибки пробрасываются; отсутствие маршрута даёт `KeyNotFoundException`.
- Параллельные вызовы допустимы без изменения регистраций. `Register` и `Dispose` разрешены только после завершения всех запросов, не из обработчиков. Общее состояние обработчиков и конвертеров защищается их реализацией.

## Ошибки
- `ArgumentException`: некорректный путь, шаблон, тип конвертера или контракт обработчика.
- `ArgumentNullException`: отсутствующий обработчик или зависимость конструктора.
- `InvalidOperationException`: повторная регистрация шаблона.
- `KeyNotFoundException`: подходящий маршрут не найден.