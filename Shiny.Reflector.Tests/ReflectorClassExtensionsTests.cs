using Shouldly;

namespace Shiny.Reflector.Tests;

public class ReflectorClassExtensionsTests
{
    [Fact]
    public void TrySetValue_ReturnsFalseWhenTypeIsWrong()
    {
        var cls = new ReflectorClassExtensionsTestClass
        {
            IntValue = 42,
            StringValue = "Hello",
            AnotherValue = new AnotherTestClass()
        };
        var reflector = cls.GetReflector();
        reflector.ShouldNotBeNull("no reflector class found");
        reflector.TrySetValue("IntValue", "test").ShouldBeFalse("should have returned false");
    }
}

[Reflector]
public partial class ReflectorClassExtensionsTestClass
{
    public int IntValue { get; set; }
    public string StringValue { get; set; }
    public AnotherTestClass AnotherValue { get; set; }
}

public class AnotherTestClass;

