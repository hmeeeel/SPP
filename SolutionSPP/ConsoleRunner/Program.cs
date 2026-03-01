using FrameworkTesting.Testing;
using System.Reflection;

try
{
    var runner = new TestRunner();

    string? assemblyPath = args.FirstOrDefault(a => !a.StartsWith("--"));
    Assembly testAssembly;

    if (assemblyPath is not null)
    {
        Console.WriteLine($"Загрузка сборки: {assemblyPath}");
        testAssembly = Assembly.LoadFrom(assemblyPath);
    }
    else
    {
        testAssembly = Assembly.Load("AppTest");
    }

    var results = await runner.RunAllAsync(testAssembly);

    int failed = results.Sum(r => r.Failed + r.Errors);
    return failed == 0 ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Критическая ошибка тест-раннера: {ex.Message}");
    return 2;
}