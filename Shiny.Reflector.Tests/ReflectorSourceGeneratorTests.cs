using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Reflection;
using Shiny.Reflector.SourceGenerators;

namespace Shiny.Reflector.Tests;

public class ReflectorSourceGeneratorTests
{
    [Fact]
    public Task GeneratesReflectorForRecords()
    {
        var source = """
             using System;
             using Shiny.Reflector;

             namespace TestNamespace
             {
                 [Reflector]
                 public partial record TestClass(string Name, int Age)
                 {
                     public double Value { get; set; }
                 }
             }
             """;

        return Verify(Generate(source));
    }
    
    
    [Fact]
    public Task GeneratesReflectorForPartialClass()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class TestClass
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForPartialClassWithReadOnlyProperties()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class TestClass
                {
                    public Guid Id { get; }
                    public string Name { get; set; }
                    public DateTime CreatedAt { get; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForPartialClassWithNullableProperties()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class TestClass
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
    public Task GeneratesReflectorForPartialClassWithComplexTypes()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class TestClass
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
    public Task DoesNotGenerateReflectorForNonPartialClass()
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
    public Task DoesNotGenerateReflectorForClassWithoutAttribute()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                public partial class TestClass
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorForPartialClassWithMixedPropertyTypes()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class TestClass
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
    public Task GeneratesReflectorForPartialClassWithGenericTypes()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class TestClass
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
    public Task GeneratesReflectorForEmptyPartialClass()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class EmptyClass
                {
                }
            }
            """;

        return Verify(Generate(source));
    }

    [Fact]
    public Task GeneratesReflectorWithInternalAccessors()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class TestClass
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorUseInternalAccessors"] = "true"
        };

        return Verify(Generate(source, properties));
    }


    [Fact]
    public Task GeneratesReflectorWithInternalAccessorsEvenWithoutReflectorClasses()
    {
        var source = """
            using System;

            namespace TestNamespace
            {
                public class RegularClass
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorUseInternalAccessors"] = "true",
            ["build_property.RootNamespace"] = "MyApp"
        };

        return Verify(Generate(source, properties));
    }

    [Fact]
    public Task GeneratesReflectorWithPublicAccessorsWhenPropertyIsFalse()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class TestClass
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorUseInternalAccessors"] = "false"
        };

        return Verify(Generate(source, properties));
    }

    [Fact]
    public Task GeneratesReflectorWithPublicAccessorsWhenPropertyIsEmpty()
    {
        var source = """
            using System;
            using Shiny.Reflector;

            namespace TestNamespace
            {
                [Reflector]
                public partial class TestClass
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorUseInternalAccessors"] = ""
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
