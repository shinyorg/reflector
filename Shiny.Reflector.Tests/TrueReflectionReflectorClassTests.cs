using Shiny.Reflector.Infrastructure;
using Shouldly;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Shiny.Reflector.Tests;

public class TrueReflectionReflectorClassTests
{
    [Fact]
    public void Records_Properties()
    {
        var @this = new TestRecord("123")
        {
            Value = 12.34
        };
        var reflector = new TrueReflectionReflectorClass(@this);
        reflector.GetValue<string>("Id").ShouldBe("123");

        var prop = reflector.TryGetPropertyInfo("Id");
        prop.ShouldNotBeNull();
        prop.HasSetter.ShouldBeFalse("should not be true");

        prop = reflector.TryGetPropertyInfo("Value");
        prop.ShouldNotBeNull();
        prop.HasSetter.ShouldBeTrue("should be true");
    }


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

    [Fact]
    public void GetAttributes_NoAttributes()
    {
        var @this = new MySampleTestClass();
        var reflector = new TrueReflectionReflectorClass(@this);

        reflector.Attributes.ShouldBeEmpty();
    }

    [Fact]
    public void GetAttributes_SingleAttributeWithoutArguments()
    {
        var @this = new ObsoleteClass();
        var reflector = new TrueReflectionReflectorClass(@this);

        reflector.Attributes.ShouldHaveSingleItem();
        var attr = reflector.Attributes[0];
        attr.Type.ShouldBe(typeof(ObsoleteAttribute));
        attr.Arguments.ShouldBeEmpty();
    }

    [Fact]
    public void GetAttributes_SingleAttributeWithConstructorArguments()
    {
        var @this = new ObsoleteWithMessageClass();
        var reflector = new TrueReflectionReflectorClass(@this);

        reflector.Attributes.ShouldHaveSingleItem();
        var attr = reflector.Attributes[0];
        attr.Type.ShouldBe(typeof(ObsoleteAttribute));
        attr.Arguments.Length.ShouldBe(1);

        var arg = attr.Arguments[0];
        arg.Type.ShouldBe(typeof(string));
        arg.Name.ShouldBe("message");
        arg.Value.ShouldBe("This class is obsolete");
        arg.IsOptional.ShouldBeTrue(); // ObsoleteAttribute message parameter has default value
    }

    [Fact]
    public void GetAttributes_MultipleAttributes()
    {
        var @this = new MultipleAttributesClass();
        var reflector = new TrueReflectionReflectorClass(@this);

        reflector.Attributes.Length.ShouldBe(2);

        // Should have both ObsoleteAttribute and DescriptionAttribute
        var obsoleteAttr = reflector.Attributes.FirstOrDefault(a => a.Type == typeof(ObsoleteAttribute));
        obsoleteAttr.ShouldNotBeNull();

        var descriptionAttr = reflector.Attributes.FirstOrDefault(a => a.Type == typeof(DescriptionAttribute));
        descriptionAttr.ShouldNotBeNull();
    }

    [Fact]
    public void GetAttributes_AttributeWithNamedArguments()
    {
        var @this = new DisplayAttributeClass();
        var reflector = new TrueReflectionReflectorClass(@this);

        reflector.Attributes.ShouldHaveSingleItem();
        var attr = reflector.Attributes[0];
        attr.Type.ShouldBe(typeof(DisplayAttribute));

        // Should have arguments for Name and Description properties
        var nameArg = attr.Arguments.FirstOrDefault(a => a.Name == "Name");
        nameArg.ShouldNotBeNull();
        nameArg.Value.ShouldBe("Test Display Name");
        nameArg.IsOptional.ShouldBeTrue(); // Named properties are always optional

        var descArg = attr.Arguments.FirstOrDefault(a => a.Name == "Description");
        descArg.ShouldNotBeNull();
        descArg.Value.ShouldBe("Test Description");
        descArg.IsOptional.ShouldBeTrue();
    }

    [Fact]
    public void GetAttributes_CustomAttributeWithConstructorAndNamedArguments()
    {
        var @this = new CustomAttributeClass();
        var reflector = new TrueReflectionReflectorClass(@this);

        reflector.Attributes.ShouldHaveSingleItem();
        var attr = reflector.Attributes[0];
        attr.Type.ShouldBe(typeof(CustomTestAttribute));

        // Constructor argument
        var ctorArg = attr.Arguments.FirstOrDefault(a => a.Name == "id");
        ctorArg.ShouldNotBeNull();
        ctorArg.Value.ShouldBe(42);
        ctorArg.Type.ShouldBe(typeof(int));
        ctorArg.IsOptional.ShouldBeFalse();

        // Named property argument
        var nameArg = attr.Arguments.FirstOrDefault(a => a.Name == "Name");
        nameArg.ShouldNotBeNull();
        nameArg.Value.ShouldBe("Custom Name");
        nameArg.Type.ShouldBe(typeof(string));
        nameArg.IsOptional.ShouldBeTrue();
    }

    [Fact]
    public void GetAttributes_AttributeWithNullValues()
    {
        var @this = new NullValueAttributeClass();
        var reflector = new TrueReflectionReflectorClass(@this);

        reflector.Attributes.ShouldHaveSingleItem();
        var attr = reflector.Attributes[0];
        attr.Type.ShouldBe(typeof(CustomTestAttribute));

        var nameArg = attr.Arguments.FirstOrDefault(a => a.Name == "Name");
        nameArg.ShouldNotBeNull();
        nameArg.Value.ShouldBeNull();
    }

    [Fact]
    public void GetAttributes_AttributeWithDifferentValueTypes()
    {
        var @this = new DifferentTypesAttributeClass();
        var reflector = new TrueReflectionReflectorClass(@this);

        reflector.Attributes.ShouldHaveSingleItem();
        var attr = reflector.Attributes[0];
        attr.Type.ShouldBe(typeof(ComplexTestAttribute));

        // Check different argument types
        var intArg = attr.Arguments.FirstOrDefault(a => a.Name == "IntValue");
        intArg?.Value.ShouldBe(123);
        intArg?.Type.ShouldBe(typeof(int));

        var boolArg = attr.Arguments.FirstOrDefault(a => a.Name == "BoolValue");
        boolArg?.Value.ShouldBe(true);
        boolArg?.Type.ShouldBe(typeof(bool));

        var doubleArg = attr.Arguments.FirstOrDefault(a => a.Name == "DoubleValue");
        doubleArg?.Value.ShouldBe(45.67);
        doubleArg?.Type.ShouldBe(typeof(double));
    }
}

// Test classes with various attribute scenarios
[Obsolete]
file class ObsoleteClass
{
}

[Obsolete("This class is obsolete")]
file class ObsoleteWithMessageClass
{
}

[Obsolete("Multiple attributes test")]
[Description("Test description")]
file class MultipleAttributesClass
{
}

[Display(Name = "Test Display Name", Description = "Test Description")]
file class DisplayAttributeClass
{
}

[CustomTest(42, Name = "Custom Name")]
file class CustomAttributeClass
{
}

[CustomTest(1, Name = null)]
file class NullValueAttributeClass
{
}

[ComplexTest(IntValue = 123, BoolValue = true, DoubleValue = 45.67)]
file class DifferentTypesAttributeClass
{
}

// Custom test attributes
file class CustomTestAttribute : Attribute
{
    public CustomTestAttribute(int id)
    {
        Id = id;
    }

    public int Id { get; }
    public string? Name { get; set; }
}

file class ComplexTestAttribute : Attribute
{
    public int IntValue { get; set; }
    public bool BoolValue { get; set; }
    public double DoubleValue { get; set; }
    public string? StringValue { get; set; }
}

file partial record TestRecord(string Id)
{
    public double Value { get; set; }
}

file class MySampleTestClass
{
    public string? Name { get; set; }
    public int Age { get; set; }
}