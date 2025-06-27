using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shiny.Reflector.Tests;


public class ReflectorSourceGeneratorTests
{
    [Fact]
    public Task GeneratesReflectorForSimpleClass()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public class TestClass
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForClassWithReadOnlyProperties()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public class TestClass
                {
                    public Guid Id { get; }
                    public string Name { get; set; }
                    public DateTime CreatedAt { get; }
                }
            }
            """;

        var result = Generate(source);
        return Verify(result);
    }

    [Fact]
    public Task GeneratesReflectorForClassWithNullableProperties()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public class TestClass
                {
                    public string? Name { get; set; }
                    public int? Age { get; set; }
                    public DateTime? BirthDate { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForClassWithComplexTypes()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public class TestClass
                {
                    public string Name { get; set; }
                    public Address Address { get; set; }
                    public List<string> Tags { get; set; }
                }

                public class Address
                {
                    public string Street { get; set; }
                    public string City { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForMultipleClassesInSameNamespace()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public class FirstClass
                {
                    public string Name { get; set; }
                }

                [Reflector]
                public class SecondClass
                {
                    public int Value { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForClassesInDifferentNamespaces()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace FirstNamespace
            {
                [Reflector]
                public class FirstClass
                {
                    public string Name { get; set; }
                }
            }

            namespace SecondNamespace
            {
                [Reflector]
                public class SecondClass
                {
                    public int Value { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForClassWithMixedPropertyTypes()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public class TestClass
                {
                    public Guid Id { get; }
                    public string? Name { get; set; }
                    public int? Number { get; set; }
                    public DateTimeOffset Date { get; set; }
                    public bool IsActive { get; set; }
                    public decimal Price { get; set; }
                    public AnotherClass? Related { get; set; }
                }

                public class AnotherClass
                {
                    public int Id { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task DoesNotGenerateReflectorForClassWithoutAttribute()
    {
        var source = """
            using System;

            namespace TestNamespace
            {
                public class TestClass
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForClassWithOnlyReadOnlyProperties()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public class TestClass
                {
                    public Guid Id { get; }
                    public string Name { get; }
                    public DateTime CreatedAt { get; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForClassWithGenericTypes()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public class TestClass
                {
                    public List<string> Names { get; set; }
                    public Dictionary<string, int> Values { get; set; }
                    public IEnumerable<DateTime> Dates { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForEmptyClass()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public class EmptyClass
                {
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task HandlesInvalidAttributeUsage()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                public class TestClass
                {
                    [Reflector] // Invalid - attribute should be on class, not property
                    public string Name { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }
    
    
    
    GeneratorDriverRunResult Generate(string source)
    {
        // Parse the source code
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create references including the Shiny.Reflector assembly
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ReflectorAttribute).Assembly.Location),
        };

        // Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: references
        );

        var generator = new ReflectorSourceGenerator();
        var run = CSharpGeneratorDriver
            .Create([generator])
            .RunGenerators(compilation);

        // Create driver and run the generator
        return run.GetRunResult();
    }
}
