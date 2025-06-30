using Shouldly;

namespace Shiny.Reflector.Tests;


public class ReflectorClassExtensionsTests
{
    [Fact]
    public void Records_TrySetValue()
    {
        var record = new TestRecord("1234")
        {
            Value = 11
        };
        var reflector = record.GetReflector()!;
        reflector
            .TrySetValue(
                nameof(TestRecord.Id),
                "NewId"
            )
            .ShouldBeFalse();
        
        record.Id.ShouldBe("1234");
        
        reflector
            .TrySetValue(
                nameof(TestRecord.Value),
                22
            )
            .ShouldBeTrue();
        
        record.Value.ShouldBe(22);
    }
    
    
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


    // [Fact]
    // public Task GetDictionary_SetDictionary()
    // {
    //     var graph = new ReflectorClassExtensionsTestClass
    //     {
    //         IntValue = 11,
    //         StringValue = "Test String",
    //         AnotherValue = new(),
    //         DateValue = DateTimeOffset.UtcNow,
    //         Third = new() 
    //         {
    //             Name = "Item 1",
    //             Items = [
    //                 new FourthClass { Value = 1 },
    //                 new FourthClass { Value = 2 }
    //             ]
    //         }
    //     };
    //     var reflector = graph.GetReflector()!;
    //     var dict = reflector.ToDictionary(true);
    //     
    //     var newGraph = new ReflectorClassExtensionsTestClass();
    //     var newReflector = newGraph.GetReflector()!;
    //     newReflector.SetObjectFromDictionary(dict);
    //
    //     return Verify(newGraph)
    //         .IgnoreMembersWithType(typeof(IReflectorClass));
    // }
}

[Reflector]
public partial record TestRecord(string Id)
{
    public int Value { get; set; }
}

[Reflector]
public partial class ReflectorClassExtensionsTestClass
{
    public int IntValue { get; set; }
    public string StringValue { get; set; }
    public AnotherTestClass AnotherValue { get; set; }

    public DateTimeOffset DateValue { get; set; } = DateTimeOffset.Now;
    
    public ThirdClass Third { get; set; }
}

public class AnotherTestClass;

public class InheritedTestClass : AnotherTestClass;

[Reflector]
public partial class ThirdClass
{
    public string Name { get; set; }
    public FourthClass[] Items { get; set; }
}

public partial class FourthClass
{
    public int Value { get; set; }
}