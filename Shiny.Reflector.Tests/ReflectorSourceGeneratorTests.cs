using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Reflection;

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
    public Task GeneratesReflectorExtensionsWithShinyReflectorExtensionsNamespace()
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

        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorExtensionsNamespace"] = "MyApp.Extensions"
        };

        return Verify(Generate(source, properties));
    }

    [Fact]
    public Task GeneratesReflectorExtensionsWithRootNamespaceFallback()
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

        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "MyApp"
        };

        return Verify(Generate(source, properties));
    }

    [Fact]
    public Task GeneratesReflectorExtensionsWithGlobalNamespaceFallback()
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

        // No MSBuild properties provided, should fallback to "global"
        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorExtensionsWithShinyNamespacePriority()
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

        // Both properties provided - ShinyReflectorExtensionsNamespace should take priority
        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorExtensionsNamespace"] = "MyApp.Reflection",
            ["build_property.RootNamespace"] = "MyApp"
        };

        return Verify(Generate(source, properties));
    }

    [Fact]
    public Task GeneratesReflectorExtensionsWithMultipleClassesInCustomNamespace()
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

        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorExtensionsNamespace"] = "Common.Extensions"
        };

        return Verify(Generate(source, properties));
    }

    GeneratorDriverRunResult Generate(string source, Dictionary<string, string>? analyzerConfigOptions = null)
    {
        // Parse the source code
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create references including the Shiny.Reflector assembly
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.Private.CoreLib
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location), // System.Runtime
            MetadataReference.CreateFromFile(typeof(ReflectorAttribute).Assembly.Location), // Shiny.Reflector
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location), // System.Collections
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), // System.Linq
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location), // System.Runtime explicitly
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location) // netstandard
        };

        // Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // Check for compilation errors
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.ToString()));
            throw new InvalidOperationException($"Compilation errors:\n{errorMessages}");
        }

        // Create and run the generator
        var generator = new ReflectorSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        // Add analyzer config options if provided
        if (analyzerConfigOptions != null)
        {
            var optionsProvider = new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions);
            driver = (CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(optionsProvider);
        }
        
        // Run the generators
        var genResult = driver.RunGenerators(compilation);
        var runResult = genResult.GetRunResult();

        return runResult;
    }
}

// Test helper class to simulate MSBuild properties
public class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly TestAnalyzerConfigOptions _globalOptions;

    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
    {
        _globalOptions = new TestAnalyzerConfigOptions(globalOptions);
    }

    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _globalOptions;
}

public class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _options;

    public TestAnalyzerConfigOptions(Dictionary<string, string> options)
    {
        _options = options;
    }

    public override bool TryGetValue(string key, out string value)
    {
        return _options.TryGetValue(key, out value!);
    }
}
