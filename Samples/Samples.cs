using Shiny.Reflector;

namespace Samples;

[Reflector]
public partial class MySampleClass
{
    public string Name { get; set; }
    public int? Age { get; set; }
    
    public AnotherSampleClass SampleClass { get; set; }
}

[Reflector]
public partial record AnotherSampleClass(int Id)
{
    public string Value { get; set; }
}