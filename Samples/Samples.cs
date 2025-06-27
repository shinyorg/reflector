using Shiny.Reflector;

namespace Samples;

[Reflector]
public class MySampleClass
{
    public string Name { get; set; }
    public int? Age { get; set; }
    
    public AnotherSampleClass SampleClass { get; set; }
}

public class AnotherSampleClass
{
    public string Value { get; set; }
}