using BenchmarkDotNet.Attributes;
using Shiny.Reflector.Infrastructure;

namespace Shiny.Reflector.Benchmarks;


[MemoryDiagnoser]
public class ReflectorBenchmarks
{
    static readonly TestClass testClass = new()
    {
        Id = "123",
        Created = DateTimeOffset.UtcNow
    };
    
    [Benchmark]
    public void DotNetReflection()
    {
        var cls = new TrueReflectionReflectorClass(testClass);
        var len = cls.Properties.Length; // force load
    }


    [Benchmark]
    public void ShinyReflector()
    {
        var reflector = testClass.GetReflector(); // get source generated reflector
        var len = reflector!.Properties.Length; // already loaded, but wanted equal to dotnet reflection
    }
}