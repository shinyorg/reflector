using System.Text.Json;
using Shouldly;
using Xunit.Abstractions;

namespace Shiny.Reflector.Tests;


public class ReflectorJsonConverterTests(ITestOutputHelper output)
{
    [Fact]
    public void Serialize_SimpleObject_WithTrueReflection()
    {
        var obj = new TestPersonClass
        {
            Name = "John Doe",
            Age = 30,
            IsActive = true
        };

        var options = new JsonSerializerOptions
        {
            Converters = { new ReflectorJsonConverter<TestPersonClass>(fallbackToTrueReflection: true) }
        };

        var json = JsonSerializer.Serialize(obj, options);
        
        output.WriteLine($"Serialized JSON: {json}");
        json.ShouldContain("\"Name\":\"John Doe\"");
        json.ShouldContain("\"Age\":30");
        json.ShouldContain("\"IsActive\":true");
    }

    
    [Fact]
    public void Deserialize_SimpleObject_WithTrueReflection()
    {
        var json = """{"Name":"Jane Smith","Age":25,"IsActive":false}""";

        var options = new JsonSerializerOptions
        {
            Converters = { new ReflectorJsonConverter<TestPersonClass>(fallbackToTrueReflection: true) }
        };

        var obj = JsonSerializer.Deserialize<TestPersonClass>(json, options);

        obj.ShouldNotBeNull();
        obj.Name.ShouldBe("Jane Smith");
        obj.Age.ShouldBe(25);
        obj.IsActive.ShouldBe(false);
    }

    [Fact]
    public void Serialize_ComplexObject_WithNestedProperties()
    {
        var obj = new TestComplexClass
        {
            Id = 123,
            Name = "Test Complex",
            CreatedDate = new DateTime(2025, 1, 1),
            Tags = new List<string> { "tag1", "tag2" },
            Metadata = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            }
        };

        var options = new JsonSerializerOptions
        {
            Converters = { new ReflectorJsonConverter<TestComplexClass>(fallbackToTrueReflection: true) }
        };

        var json = JsonSerializer.Serialize(obj, options);
        
        output.WriteLine($"Complex JSON: {json}");
        json.ShouldContain("\"Id\":123");
        json.ShouldContain("\"Name\":\"Test Complex\"");
        json.ShouldContain("\"Tags\":[\"tag1\",\"tag2\"]");
    }

    [Fact]
    public void Deserialize_ComplexObject_WithNestedProperties()
    {
        var json = """
        {
            "Id": 456,
            "Name": "Deserialized Complex",
            "CreatedDate": "2025-02-01T00:00:00",
            "Tags": ["deserialize", "test"],
            "Metadata": {"testKey": "testValue"}
        }
        """;

        var options = new JsonSerializerOptions
        {
            Converters = { new ReflectorJsonConverter<TestComplexClass>(fallbackToTrueReflection: true) }
        };

        var obj = JsonSerializer.Deserialize<TestComplexClass>(json, options);

        obj.ShouldNotBeNull();
        obj.Id.ShouldBe(456);
        obj.Name.ShouldBe("Deserialized Complex");
        obj.CreatedDate.ShouldBe(new DateTime(2025, 2, 1));
        obj.Tags.ShouldNotBeNull();
        obj.Tags.Count.ShouldBe(2);
        obj.Tags[0].ShouldBe("deserialize");
        obj.Tags[1].ShouldBe("test");
    }

    [Fact]
    public void Serialize_WithPropertyNamingPolicy()
    {
        var obj = new TestPersonClass
        {
            Name = "Snake Case Test",
            Age = 40,
            IsActive = true
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new ReflectorJsonConverter<TestPersonClass>(fallbackToTrueReflection: true) }
        };

        var json = JsonSerializer.Serialize(obj, options);
        
        output.WriteLine($"Snake case JSON: {json}");
        json.ShouldContain("\"name\":\"Snake Case Test\"");
        json.ShouldContain("\"age\":40");
        json.ShouldContain("\"is_active\":true");
    }
    

    [Fact]
    public void Deserialize_IgnoresUnknownProperties()
    {
        var json = """{"Name":"Test","Age":30,"UnknownProperty":"ShouldBeIgnored"}""";

        var options = new JsonSerializerOptions
        {
            Converters = { new ReflectorJsonConverter<TestPersonClass>(fallbackToTrueReflection: true) }
        };

        var obj = JsonSerializer.Deserialize<TestPersonClass>(json, options);

        obj.ShouldNotBeNull();
        obj.Name.ShouldBe("Test");
        obj.Age.ShouldBe(30);
    }
    

    [Fact]
    public void Serialize_NullObject_ReturnsNull()
    {
        TestPersonClass? obj = null;

        var options = new JsonSerializerOptions
        {
            Converters = { new ReflectorJsonConverter<TestPersonClass>(fallbackToTrueReflection: true) }
        };

        var json = JsonSerializer.Serialize(obj, options);
        
        json.ShouldBe("null");
    }

    
    [Fact]
    public void JsonConverterFactory_CreateConverter_ReturnsCorrectType()
    {
        var factory = new ReflectorJsonConverter(fallbackToTrueReflection: true);
        var options = new JsonSerializerOptions();

        var converter = factory.CreateConverter(typeof(TestPersonClass), options);
        
        converter.ShouldNotBeNull();
        converter.ShouldBeOfType<ReflectorJsonConverter<TestPersonClass>>();
    }

    
    [Fact]
    public void Deserialize_InvalidJson_ThrowsJsonException()
    {
        var json = """{"Name":"Test","Age":"NotANumber"}""";

        var options = new JsonSerializerOptions
        {
            Converters = { new ReflectorJsonConverter<TestPersonClass>(fallbackToTrueReflection: true) }
        };

        Should.Throw<JsonException>(() =>
        {
            JsonSerializer.Deserialize<TestPersonClass>(json, options);
        });
    }

    
    [Fact]
    public void Deserialize_NonObjectJson_ThrowsJsonException()
    {
        var json = """["not","an","object"]""";

        var options = new JsonSerializerOptions
        {
            Converters = { new ReflectorJsonConverter<TestPersonClass>(fallbackToTrueReflection: true) }
        };

        Should.Throw<JsonException>(() =>
        {
            JsonSerializer.Deserialize<TestPersonClass>(json, options);
        });
    }

    
    [Fact]
    public void RoundTrip_Serialization_MaintainsDataIntegrity()
    {
        var original = new TestComplexClass
        {
            Id = 999,
            Name = "Round Trip Test",
            CreatedDate = DateTime.Now,
            Tags = new List<string> { "round", "trip", "test" },
            Metadata = new Dictionary<string, object>
            {
                { "number", 42 },
                { "text", "hello world" },
                { "boolean", true }
            }
        };

        var options = new JsonSerializerOptions
        {
            Converters = { new ReflectorJsonConverter<TestComplexClass>(fallbackToTrueReflection: true) }
        };

        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<TestComplexClass>(json, options);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(original.Id);
        deserialized.Name.ShouldBe(original.Name);
        deserialized.CreatedDate.ShouldBe(original.CreatedDate, TimeSpan.FromSeconds(1)); // Allow small time difference
        deserialized.Tags.ShouldNotBeNull();
        deserialized.Tags.Count.ShouldBe(original.Tags.Count);
        
        for (int i = 0; i < original.Tags.Count; i++)
        {
            deserialized.Tags[i].ShouldBe(original.Tags[i]);
        }
    }
}


[Reflector]
public partial class TestPersonClass
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

[Reflector]
public partial class TestComplexClass
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

// file class TestReadOnlyClass
// {
//     public string? Name { get; set; }
//     public int Age { get; set; }
//     public string ReadOnlyProperty { get; } = "Default";
// }
//
// file abstract class TestAbstractClass
// {
//     public string? Name { get; set; }
// }
