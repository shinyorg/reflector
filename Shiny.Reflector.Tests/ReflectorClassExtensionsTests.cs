using Shouldly;

namespace Shiny.Reflector.Tests;

public class ReflectorClassExtensionsTests
{

    [Fact]
    public void TryGetValue_WrongTypeReturnsFalse()
    {
        var cls = new ReflectorClassExtensionsTestClass
        {
            IntValue = 42,
            StringValue = "Hello",
            AnotherValue = new AnotherTestClass()
        };
        var reflector = cls.GetReflector();
        reflector.ShouldNotBeNull("no reflector class found");
        reflector
            .TryGetValue<string>(
                nameof(ReflectorClassExtensionsTestClass.IntValue), 
                out _
            )
            .ShouldBeFalse("should have returned false");
    }
    
    
    [Fact]
    public void TryGetValue_Success()
    {
        var cls = new ReflectorClassExtensionsTestClass
        {
            IntValue = 42,
            StringValue = "Hello",
            AnotherValue = new AnotherTestClass()
        };
        var reflector = cls.GetReflector();
        reflector.ShouldNotBeNull("no reflector class found");
        reflector
            .TryGetValue<string>(nameof(ReflectorClassExtensionsTestClass.StringValue), out var value)
            .ShouldBeTrue("should have returned false");
        
        value.ShouldBe("Hello");
    }
    
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
    
    
    [Fact]
    public void TrySetValue_InheritedType()
    {
        var cls = new ReflectorClassExtensionsTestClass
        {
            IntValue = 42,
            StringValue = "Hello",
            AnotherValue = new AnotherTestClass()
        };
        var reflector = cls.GetReflector();
        reflector.ShouldNotBeNull("no reflector class found");
        reflector
            .TrySetValue(
                nameof(ReflectorClassExtensionsTestClass.AnotherValue),
                new InheritedTestClass()
            )
            .ShouldBeTrue();

        (cls.AnotherValue as InheritedTestClass).ShouldNotBeNull("Class did not cast");
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

public class InheritedTestClass : AnotherTestClass;

