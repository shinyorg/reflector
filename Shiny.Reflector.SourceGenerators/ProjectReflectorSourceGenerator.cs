using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text;
using System.Collections.Immutable;
using System.IO;

namespace Shiny.Reflector.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class ProjectReflectorSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get MSBuild properties and additional files
        var msbuildData = context.AnalyzerConfigOptionsProvider
            .Combine(context.AdditionalTextsProvider.Collect())
            .Select((combined, _) => GetMsBuildData(combined.Left, combined.Right));

        // Register the source output
        context.RegisterSourceOutput(msbuildData, (spc, data) =>
        {
            if (data.ShouldGenerate)
            {
                var source = GenerateAssemblyInfoClass(data);
                spc.AddSource($"{data.ClassName}.g.cs", source);
            }
        });
    }

    private static MsBuildData GetMsBuildData(AnalyzerConfigOptionsProvider provider)
    {
        var globalOptions = provider.GlobalOptions;

        // Check if generator should run
        var shouldGenerate = true;
        if (globalOptions.TryGetValue("build_property.ShinyReflectorGenerateAssemblyInfo", out var generateValue))
        {
            shouldGenerate = string.IsNullOrEmpty(generateValue) || 
                           bool.TryParse(generateValue, out var boolValue) && boolValue;
        }

        if (!shouldGenerate)
        {
            return new MsBuildData { ShouldGenerate = false };
        }

        // Get namespace
        var namespaceName = string.Empty;
        if (globalOptions.TryGetValue("build_property.ShinyReflectorAssemblyInfoNamespace", out var customNamespace))
        {
            namespaceName = customNamespace;
        }
        else if (globalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace))
        {
            namespaceName = rootNamespace;
        }

        // Get class name
        var className = "AssemblyInfo";
        if (globalOptions.TryGetValue("build_property.ShinyReflectorAssemblyInfoClassName", out var customClassName) && 
            !string.IsNullOrEmpty(customClassName))
        {
            className = customClassName;
        }

        // Collect all MSBuild properties
        var properties = new Dictionary<string, string>();

        // We need to use a known set of properties or rely on the fact that MSBuild properties
        // are passed through the analyzer config options with the "build_property." prefix
        // Since we can't iterate over AnalyzerConfigOptions directly, we'll collect common properties
        var commonProperties = new[]
        {
            "Company",
            "Title",
            "Description",
            "Version",
            "ApplicationTitle",
            "ApplicationId",
            "ApplicationVersion",
            "ApplicationDisplayVersion",
            "AssemblyCompany",
            "AssemblyProduct",
            "AssemblyCopyright",
            "AssemblyVersion",
            "AssemblyFileVersion",
            "AssemblyInformationalVersion",
            "TargetFramework",
            "TargetFrameworkVersion",
            "Platform"
        };

        foreach (var propertyName in commonProperties)
        {
            if (globalOptions.TryGetValue($"build_property.{propertyName}", out var propertyValue) && 
                !string.IsNullOrEmpty(propertyValue))
            {
                // Skip properties we've already handled
                if (propertyName != "ShinyReflectorGenerateAssemblyInfo" && 
                    propertyName != "ShinyReflectorAssemblyInfoNamespace" &&
                    propertyName != "ShinyReflectorAssemblyInfoClassName")
                {
                    properties[propertyName] = propertyValue;
                }
            }
        }

        return new MsBuildData
        {
            ShouldGenerate = true,
            Namespace = namespaceName,
            ClassName = className,
            Properties = properties,
            ReflectorItems = new Dictionary<string, string>() // Will be populated from AdditionalFiles
        };
    }

    private static MsBuildData GetMsBuildData(AnalyzerConfigOptionsProvider provider, ImmutableArray<AdditionalText> additionalFiles)
    {
        var data = GetMsBuildData(provider);
        
        if (!data.ShouldGenerate)
            return data;

        // Collect ReflectorItems from AdditionalFiles
        var reflectorItems = new Dictionary<string, string>();
        
        foreach (var file in additionalFiles)
        {
            var options = provider.GetOptions(file);
            if (options.TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var sourceItemGroup) &&
                sourceItemGroup == "ReflectorItem")
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Path);
                if (options.TryGetValue("build_metadata.AdditionalFiles.Value", out var value) && 
                    !string.IsNullOrEmpty(fileName))
                {
                    reflectorItems[fileName] = value;
                }
            }
        }

        data.ReflectorItems = reflectorItems;
        return data;
    }

    static string GenerateAssemblyInfoClass(MsBuildData data)
    {
        var sb = new StringBuilder();

        // Add namespace if specified
        if (!string.IsNullOrEmpty(data.Namespace))
        {
            sb.AppendLine($"namespace {data.Namespace};");
            sb.AppendLine();
        }

        // Generate class
        sb.AppendLine($"public static class {data.ClassName}");
        sb.AppendLine("{");

        // Add MSBuild properties as constants
        foreach (var kvp in data.Properties)
        {
            var constantName = SanitizeIdentifier(kvp.Key);
            var escapedValue = EscapeStringLiteral(kvp.Value);
            sb.AppendLine($"    public const string {constantName} = \"{escapedValue}\";");
        }

        // Add ReflectorItem constants
        foreach (var kvp in data.ReflectorItems)
        {
            var constantName = SanitizeIdentifier(kvp.Key);
            var escapedValue = EscapeStringLiteral(kvp.Value);
            sb.AppendLine($"    public const string {constantName} = \"{escapedValue}\";");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    static string SanitizeIdentifier(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "Unknown";

        var sb = new StringBuilder();
        
        // First character must be letter or underscore
        var firstChar = input[0];
        if (char.IsLetter(firstChar) || firstChar == '_')
        {
            sb.Append(firstChar);
        }
        else
        {
            sb.Append('_');
        }

        // Subsequent characters can be letters, digits, or underscores
        for (int i = 1; i < input.Length; i++)
        {
            var ch = input[i];
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append('_');
            }
        }

        var result = sb.ToString();
        
        // Ensure it's not a C# keyword
        if (IsCSharpKeyword(result))
        {
            result = "@" + result;
        }

        return result;
    }

    private static string EscapeStringLiteral(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
    }

    private static bool IsCSharpKeyword(string identifier)
    {
        var keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while"
        };

        return keywords.Contains(identifier);
    }

    private class MsBuildData
    {
        public bool ShouldGenerate { get; set; }
        public string Namespace { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public Dictionary<string, string> Properties { get; set; } = new();
        public Dictionary<string, string> ReflectorItems { get; set; } = new();
    }
}
