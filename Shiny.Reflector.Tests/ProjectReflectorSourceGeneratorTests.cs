using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Shiny.Reflector.SourceGenerators;
using System.Collections.Immutable;


namespace Shiny.Reflector.Tests;


public class ProjectReflectorSourceGeneratorTests
{
    [Fact]
    public Task GeneratesAssemblyInfoWithDefaultSettings()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.Company"] = "Test Company",
            ["build_property.Title"] = "Test Application",
            ["build_property.Version"] = "1.0.0"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithCustomNamespace()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorAssemblyInfoNamespace"] = "Custom.Namespace",
            ["build_property.Company"] = "Test Company",
            ["build_property.Version"] = "2.0.0"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithCustomClassName()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.ShinyReflectorAssemblyInfoClassName"] = "ProjectInfo",
            ["build_property.Company"] = "Test Company",
            ["build_property.ApplicationTitle"] = "My App"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithAllCommonProperties()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "MyApplication",
            ["build_property.Company"] = "Acme Corp",
            ["build_property.Title"] = "Acme Application",
            ["build_property.Description"] = "A test application for unit testing",
            ["build_property.Version"] = "1.2.3",
            ["build_property.ApplicationTitle"] = "Acme App",
            ["build_property.ApplicationId"] = "com.acme.app",
            ["build_property.ApplicationVersion"] = "1.2.3.4",
            ["build_property.ApplicationDisplayVersion"] = "1.2.3-beta",
            ["build_property.AssemblyCompany"] = "Acme Corporation",
            ["build_property.AssemblyProduct"] = "Acme Product Suite",
            ["build_property.AssemblyCopyright"] = "Copyright © 2025 Acme Corp",
            ["build_property.AssemblyVersion"] = "1.2.3.0",
            ["build_property.AssemblyFileVersion"] = "1.2.3.0",
            ["build_property.AssemblyInformationalVersion"] = "1.2.3-beta+abc123",
            ["build_property.TargetFramework"] = "net9.0",
            ["build_property.TargetFrameworkVersion"] = "v9.0",
            ["build_property.Platform"] = "AnyCPU"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task DoesNotGenerateWhenExplicitlyDisabled()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorGenerateAssemblyInfo"] = "false",
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.Company"] = "Test Company"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesWhenExplicitlyEnabled()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorGenerateAssemblyInfo"] = "true",
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.Company"] = "Test Company"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesWhenGeneratePropertyIsEmpty()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.ShinyReflectorGenerateAssemblyInfo"] = "",
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.Company"] = "Test Company"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithoutNamespace()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.Company"] = "Test Company",
            ["build_property.Version"] = "1.0.0"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithSpecialCharactersInProperties()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.Company"] = "Test \"Company\" & Co.",
            ["build_property.Description"] = "Line 1\nLine 2\tTabbed\rCarriage Return",
            ["build_property.AssemblyCopyright"] = "Copyright © 2025 Test\\Company"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithCSharpKeywordAsPropertyName()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.class"] = "KeywordValue",
            ["build_property.namespace"] = "NamespaceValue",
            ["build_property.string"] = "StringValue"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithNumericAndSpecialCharacterPropertyNames()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.123Property"] = "NumericStart",
            ["build_property.Property-With-Dashes"] = "DashValue",
            ["build_property.Property.With.Dots"] = "DotValue",
            ["build_property.Property With Spaces"] = "SpaceValue"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithEmptyPropertyValues()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.Company"] = "",
            ["build_property.Title"] = "Test App",
            ["build_property.Description"] = ""
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task IgnoresNonBuildProperties()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "TestApp",
            ["build_property.Company"] = "Test Company",
            ["some_other_property"] = "Should be ignored",
            ["analyzer_property.SomeValue"] = "Also ignored"
        };

        return Verify(Generate(properties));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithReflectorItems()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "TestApp"
        };
        var reflectorItems = new Dictionary<string, string>
        {
            ["ReflectorKey1"] = "ReflectorValue1",
            ["ReflectorKey2"] = "ReflectorValue2"
        };
        return Verify(Generate(properties, reflectorItems));
    }

    [Fact]
    public Task GeneratesAssemblyInfoWithReflectorItemsSpecialCharacters()
    {
        var properties = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "TestApp"
        };
        var reflectorItems = new Dictionary<string, string>
        {
            ["Reflector-Item"] = "Value-With-Dash",
            ["Reflector Item"] = "Value With Space",
            ["Reflector.Item"] = "Value.With.Dot",
            ["class"] = "KeywordValue"
        };
        return Verify(Generate(properties, reflectorItems));
    }

    GeneratorDriverRunResult Generate(Dictionary<string, string> analyzerConfigOptions, Dictionary<string, string>? reflectorItems = null)
    {
        // Create a minimal source file (the generator doesn't need actual source code)
        var source = """
            namespace TestNamespace
            {
                public class TestClass
                {
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create basic references
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.Private.CoreLib
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location) // System.Runtime
        };

        // Create AdditionalFiles for ReflectorItems if provided
        var additionalFiles = new List<AdditionalText>();
        if (reflectorItems != null)
        {
            foreach (var kvp in reflectorItems)
            {
                var additionalFile = new TestAdditionalFile(kvp.Key, kvp.Value);
                additionalFiles.Add(additionalFile);
            }
        }

        // Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // Create and run the generator
        var generator = new ProjectReflectorSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        
        // Add additional files first
        if (additionalFiles.Count > 0)
        {
            driver = driver.AddAdditionalTexts(additionalFiles.ToImmutableArray());
        }
        
        // Add analyzer config options with support for ReflectorItems metadata
        var optionsProvider = new ProjectTestAnalyzerConfigOptionsProvider(analyzerConfigOptions, additionalFiles);
        driver = driver.WithUpdatedAnalyzerConfigOptions(optionsProvider);
        
        // Run the generators
        var genResult = driver.RunGenerators(compilation);
        var runResult = genResult.GetRunResult();

        return runResult;
    }

    private class TestAdditionalFile : AdditionalText
    {
        private readonly string path;
        public readonly string Value;

        public TestAdditionalFile(string fileName, string value)
        {
            this.path = fileName;
            this.Value = value;
        }

        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From("");
        }
    }

    private class ProjectTestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestAnalyzerConfigOptions globalOptions;
        private readonly Dictionary<string, TestAnalyzerConfigOptions> fileOptions;

        public ProjectTestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions, List<AdditionalText>? additionalFiles = null)
        {
            this.globalOptions = new TestAnalyzerConfigOptions(globalOptions);
            this.fileOptions = new Dictionary<string, TestAnalyzerConfigOptions>();

            // Create file-specific options for ReflectorItems
            if (additionalFiles != null)
            {
                foreach (var file in additionalFiles)
                {
                    if (file is TestAdditionalFile testFile)
                    {
                        var fileSpecificOptions = new Dictionary<string, string>
                        {
                            ["build_metadata.AdditionalFiles.SourceItemGroup"] = "ReflectorItem",
                            ["build_metadata.AdditionalFiles.Value"] = testFile.Value
                        };
                        this.fileOptions[file.Path] = new TestAnalyzerConfigOptions(fileSpecificOptions);
                    }
                }
            }
        }

        public override AnalyzerConfigOptions GlobalOptions => globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => globalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return fileOptions.TryGetValue(textFile.Path, out var options) ? options : globalOptions;
        }
    }

    private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> options;

        public TestAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            this.options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return options.TryGetValue(key, out value!);
        }
    }
}
