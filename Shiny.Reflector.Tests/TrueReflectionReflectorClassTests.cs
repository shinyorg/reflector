using Shiny.Reflector.Infrastructure;
using Shouldly;
using Xunit.Abstractions;

namespace Shiny.Reflector.Tests;


public class TrueReflectionReflectorClassTests(ITestOutputHelper output)
{
    [Fact]
    public void GetValue_Presets()
    {
        var @this = new MySampleTestClass
        {
            Name = "Test Name",
            Age = 91
        };
        var reflector = new TrueReflectionReflectorClass(@this);
        reflector.GetValue<int>("Age").ShouldBe(91);
        reflector.GetValue<string>("Name").ShouldBe("Test Name");
    }


    [Fact]
    public void SetValue_Success()
    {
        var @this = new MySampleTestClass();
        var reflector = new TrueReflectionReflectorClass(@this);
        reflector.SetValue("Age", 22);
        
        @this.Age.ShouldBe(22);
    }


    [Fact]
    public Task GetProperties()
    {
        var @this = new MySampleTestClass();
        var reflector = new TrueReflectionReflectorClass(@this);
        return Verify(reflector.Properties
            .Select(x => new PropertyGeneratedInfo(x.Name, x.Type, x.HasSetter)));
    }

    [Fact]
    public void SetValue_Null()
    {
        var @this = new MySampleTestClass
        {
            Name = "Test Name",
            Age = 91
        };
        var reflector = new TrueReflectionReflectorClass(@this);
        reflector["Name"] = null;
        @this.Name.ShouldBeNull();
    }
}

file class MySampleTestClass
{
    public string? Name { get; set; }
    public int Age { get; set; }
}