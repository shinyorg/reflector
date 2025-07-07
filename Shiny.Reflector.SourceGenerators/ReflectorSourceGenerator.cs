using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text;

namespace Shiny.Reflector.SourceGenerators;

// Data structures for code generation
public class ClassInfo
{
    public string ClassName { get; }
    public string Namespace { get; }
    public string FullTypeName { get; }
    public List<PropertyInfo> Properties { get; }
    public List<AttributeInfo> Attributes { get; }

    public ClassInfo(string className, string namespaceName, string fullTypeName, List<PropertyInfo> properties, List<AttributeInfo> attributes)
    {
        ClassName = className;
        Namespace = namespaceName;
        FullTypeName = fullTypeName;
        Properties = properties;
        Attributes = attributes;
    }
}

public class PropertyInfo
{
    public string Name { get; }
    public string TypeName { get; }
    public string TypeForTypeOf { get; }
    public bool HasSetter { get; }

    public PropertyInfo(string name, string typeName, string typeForTypeOf, bool hasSetter)
    {
        Name = name;
        TypeName = typeName;
        TypeForTypeOf = typeForTypeOf;
        HasSetter = hasSetter;
    }
}

public class AttributeInfo
{
    public string TypeName { get; }
    public string TypeForTypeOf { get; }
    public List<AttributeArgumentInfo> Arguments { get; }

    public AttributeInfo(string typeName, string typeForTypeOf, List<AttributeArgumentInfo> arguments)
    {
        TypeName = typeName;
        TypeForTypeOf = typeForTypeOf;
        Arguments = arguments;
    }
}

public class AttributeArgumentInfo
{
    public string TypeName { get; }
    public string TypeForTypeOf { get; }
    public string Name { get; }
    public string? ValueExpression { get; }
    public bool IsOptional { get; }
    public string? DefaultValueExpression { get; }

    public AttributeArgumentInfo(string typeName, string typeForTypeOf, string name, string? valueExpression, bool isOptional, string? defaultValueExpression)
    {
        TypeName = typeName;
        TypeForTypeOf = typeForTypeOf;
        Name = name;
        ValueExpression = valueExpression;
        IsOptional = isOptional;
        DefaultValueExpression = defaultValueExpression;
    }
}

[Generator(LanguageNames.CSharp)]
public class ReflectorSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all partial classes marked with ReflectorAttribute
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m is not null);

        // Combine with compilation and analyzer config options to get type information and MSBuild properties
        var compilationAndClassesAndOptions = context.CompilationProvider
            .Combine(classDeclarations.Collect())
            .Combine(context.AnalyzerConfigOptionsProvider);

        // Generate the source code
        context.RegisterSourceOutput(
            compilationAndClassesAndOptions, 
            static (spc, source) => Execute(
                source.Left.Left, 
                source.Left.Right, 
                source.Right, 
                spc
            )
        );
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => (node is ClassDeclarationSyntax cls && 
            cls.AttributeLists.Count > 0 && 
            cls.Modifiers.Any(SyntaxKind.PartialKeyword)) ||
           (node is RecordDeclarationSyntax rec && 
            rec.AttributeLists.Count > 0 && 
            rec.Modifiers.Any(SyntaxKind.PartialKeyword));

    static TypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in typeDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attributeSyntax);
                if (!(symbolInfo.Symbol is IMethodSymbol attributeSymbol))
                    continue;

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == "Shiny.Reflector.ReflectorAttribute")
                {
                    return typeDeclarationSyntax;
                }
            }
        }

        return null;
    }

    static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax?> typeDeclarations, AnalyzerConfigOptionsProvider optionsProvider, SourceProductionContext context)
    {
        // Get the accessor modifier from MSBuild properties
        var accessorModifier = GetAccessorModifier(optionsProvider);
        
        // Collect all classes for generating a single ReflectorExtensions
        var allClasses = new List<ClassInfo>();

        if (!typeDeclarations.IsDefaultOrEmpty)
        {
            var distinctTypes = typeDeclarations.Where(x => x is not null).Distinct();

            foreach (var typeDeclaration in distinctTypes)
            {
                var semanticModel = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
                var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;

                if (typeSymbol != null)
                {
                    var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "global";

                    var properties = GetProperties(typeSymbol);
                    var attributes = GetAttributes(typeSymbol);
                    var classInfo = new ClassInfo(
                        typeSymbol.Name, 
                        namespaceName, 
                        typeSymbol.ToDisplayString(),
                        properties,
                        attributes
                    );
                    allClasses.Add(classInfo);

                    // Generate reflector class for this type
                    GenerateReflectorClass(context, classInfo, accessorModifier);
                    
                    // Generate partial type with Reflector property
                    GeneratePartialTypeWithReflectorProperty(context, classInfo, accessorModifier, typeSymbol.IsRecord);
                }
            }
        }
    }

    static string GetAccessorModifier(AnalyzerConfigOptionsProvider optionsProvider)
    {
        // Try to get global options first
        var globalOptions = optionsProvider.GlobalOptions;

        // Check for ShinyReflectorUseInternalAccessors
        if (globalOptions.TryGetValue("build_property.ShinyReflectorUseInternalAccessors", out var useInternal) && 
            string.Equals(useInternal, "true", StringComparison.OrdinalIgnoreCase))
        {
            return "internal";
        }

        // Default to public if not specified or false
        return "public";
    }

    static List<PropertyInfo> GetProperties(INamedTypeSymbol typeSymbol)
    {
        var properties = new List<PropertyInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public } property)
            {
                // For records, primary constructor parameters are readonly by default
                // We can identify them by checking if they are auto-implemented properties
                // that don't have an explicit setter or if the setter is init-only
                bool hasSetter = false;
                
                if (typeSymbol.IsRecord)
                {
                    // For records, only allow setting if there's an explicit setter that's not init-only
                    hasSetter = property.SetMethod is
                    {
                        DeclaredAccessibility: Accessibility.Public, 
                        IsInitOnly: false
                    };
                }
                else
                {
                    // For classes, use the original logic
                    hasSetter = property.SetMethod is { DeclaredAccessibility: Accessibility.Public };
                }
                
                var typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // Remove nullable annotations for typeof() expressions
                var typeForTypeOf = typeName.Replace("?", "");
                properties.Add(new PropertyInfo(property.Name, typeName, typeForTypeOf, hasSetter));
            }
        }

        return properties;
    }

    static List<AttributeInfo> GetAttributes(INamedTypeSymbol typeSymbol)
    {
        var attributes = new List<AttributeInfo>();

        foreach (var attributeData in typeSymbol.GetAttributes())
        {
            var typeName = attributeData.AttributeClass?.ToDisplayString() ?? "";
            var typeForTypeOf = typeName.Replace("?", "");
            var arguments = new List<AttributeArgumentInfo>();

            // Handle constructor arguments
            if (attributeData.ConstructorArguments.Length > 0)
            {
                var constructor = attributeData.AttributeConstructor;
                if (constructor != null)
                {
                    for (int i = 0; i < attributeData.ConstructorArguments.Length; i++)
                    {
                        var arg = attributeData.ConstructorArguments[i];
                        var param = constructor.Parameters[i];
                        
                        var argumentTypeName = arg.Type?.ToDisplayString() ?? param.Type.ToDisplayString();
                        var argumentTypeForTypeOf = argumentTypeName.Replace("?", "");
                        var argumentName = param.Name;
                        string? valueExpression = GetValueExpression(arg.Value);
                        bool isOptional = param.HasExplicitDefaultValue;
                        string? defaultValueExpression = isOptional ? GetValueExpression(param.ExplicitDefaultValue) : null;

                        arguments.Add(new AttributeArgumentInfo(
                            argumentTypeName, 
                            argumentTypeForTypeOf, 
                            argumentName, 
                            valueExpression, 
                            isOptional, 
                            defaultValueExpression
                        ));
                    }
                }
            }

            // Handle named arguments
            foreach (var namedArgument in attributeData.NamedArguments)
            {
                var argumentTypeName = namedArgument.Value.Type?.ToDisplayString() ?? "object";
                var argumentTypeForTypeOf = argumentTypeName.Replace("?", "");
                var argumentName = namedArgument.Key;
                string? valueExpression = GetValueExpression(namedArgument.Value.Value);
                bool isOptional = true; // Named arguments are always optional
                string? defaultValueExpression = null;

                arguments.Add(new AttributeArgumentInfo(
                    argumentTypeName, 
                    argumentTypeForTypeOf, 
                    argumentName, 
                    valueExpression, 
                    isOptional, 
                    defaultValueExpression
                ));
            }

            attributes.Add(new AttributeInfo(typeName, typeForTypeOf, arguments));
        }

        return attributes;
    }

    static string? GetValueExpression(object? value)
    {
        return value switch
        {
            null => "null",
            bool boolValue => boolValue ? "true" : "false",
            byte byteValue => byteValue.ToString(),
            sbyte sbyteValue => sbyteValue.ToString(),
            short shortValue => shortValue.ToString(),
            ushort ushortValue => ushortValue.ToString(),
            int intValue => intValue.ToString(),
            uint uintValue => $"{uintValue}u",
            long longValue => $"{longValue}L",
            ulong ulongValue => $"{ulongValue}UL",
            float floatValue => $"{floatValue}f",
            double doubleValue => doubleValue.ToString(),
            decimal decimalValue => $"{decimalValue}m",
            char charValue => $"'{charValue}'",
            string stringValue => $"\"{stringValue.Replace("\"", "\\\"")}\"",
            _ => value?.ToString() ?? "null"
        };
    }

    static void GenerateReflectorClass(SourceProductionContext context, ClassInfo classInfo, string accessorModifier)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"// <auto-generated />");
        sb.AppendLine($"#nullable enable");
        sb.AppendLine();

        if (classInfo.Namespace != "global")
        {
            sb.AppendLine($"namespace {classInfo.Namespace}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"{accessorModifier} class {classInfo.ClassName}Reflector : global::Shiny.Reflector.IReflectorClass");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly global::{classInfo.FullTypeName} _reflectedObject;");
        sb.AppendLine();
        sb.AppendLine($"    {accessorModifier} {classInfo.ClassName}Reflector(global::{classInfo.FullTypeName} reflectedObject)");
        sb.AppendLine("    {");
        sb.AppendLine("        _reflectedObject = reflectedObject;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public object ReflectedObject => _reflectedObject;");
        sb.AppendLine();

        // Generate Properties array
        sb.AppendLine("    public global::Shiny.Reflector.PropertyGeneratedInfo[] Properties => new global::Shiny.Reflector.PropertyGeneratedInfo[]");
        sb.AppendLine("    {");
        for (int i = 0; i < classInfo.Properties.Count; i++)
        {
            var prop = classInfo.Properties[i];
            var comma = i < classInfo.Properties.Count - 1 ? "," : "";
            sb.AppendLine($"        new global::Shiny.Reflector.PropertyGeneratedInfo(\"{prop.Name}\", typeof({prop.TypeForTypeOf}), {prop.HasSetter.ToString().ToLower()}){comma}");
        }
        sb.AppendLine("    };");
        sb.AppendLine();

        // Generate GetValue<T> method
        sb.AppendLine("    public T? GetValue<T>(string key)");
        sb.AppendLine("    {");
        sb.AppendLine("        var value = this[key];");
        sb.AppendLine("        if (value == null)");
        sb.AppendLine("            return default(T);");
        sb.AppendLine();
        sb.AppendLine("        return (T)value;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate indexer
        var settableProps = classInfo.Properties.Where(p => p.HasSetter).ToList();
        sb.AppendLine("    public object? this[string key]");
        sb.AppendLine("    {");
        sb.AppendLine("        get");
        sb.AppendLine("        {");
        sb.AppendLine("            switch (key?.ToLower())");
        sb.AppendLine("            {");
        foreach (var prop in classInfo.Properties)
        {
            sb.AppendLine($"                case \"{prop.Name.ToLower()}\":");
            sb.AppendLine($"                    return _reflectedObject.{prop.Name};");
        }
        sb.AppendLine("                default:");
        sb.AppendLine($"                    throw new global::System.InvalidOperationException($\"Cannot get value for key '{{key}}' in {classInfo.ClassName}Reflector\");");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        set");
        sb.AppendLine("        {");
        if (settableProps.Count > 0)
        {
            sb.AppendLine("            switch (key?.ToLower())");
            sb.AppendLine("            {");
            foreach (var prop in settableProps)
            {
                sb.AppendLine($"                case \"{prop.Name.ToLower()}\":");
                // Special handling for nullable value types
                if (prop.TypeName.Contains("?") && !prop.TypeName.StartsWith("string"))
                {
                    var underlyingType = prop.TypeName.Replace("?", "");
                    sb.AppendLine($"                    if (value != null && value is not {underlyingType})");
                    sb.AppendLine($"                        throw new global::System.InvalidOperationException($\"Cannot set value for key '{{key}}' in {classInfo.ClassName}Reflector. Expected a {prop.TypeName} value.\");");
                    sb.AppendLine();
                    sb.AppendLine($"                    _reflectedObject.{prop.Name} = ({prop.TypeName})value;");
                }
                else
                {
                    sb.AppendLine($"                    if (value is not null and not {prop.TypeName})");
                    sb.AppendLine($"                        throw new global::System.InvalidOperationException($\"Cannot set value for key '{{key}}' in {classInfo.ClassName}Reflector. Expected a {prop.TypeName} value.\");");
                    sb.AppendLine();
                    sb.AppendLine($"                    _reflectedObject.{prop.Name} = ({prop.TypeName})value;");
                }
                sb.AppendLine("                    break;");
                sb.AppendLine();
            }
            sb.AppendLine("                default:");
            sb.AppendLine($"                    throw new global::System.InvalidOperationException($\"Cannot set value for key '{{key}}' in {classInfo.ClassName}Reflector\");");
            sb.AppendLine("            }");
        }
        else
        {
            sb.AppendLine($"            throw new global::System.InvalidOperationException($\"Cannot set value for key '{{key}}' in {classInfo.ClassName}Reflector\");");
        }
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate SetValue<T> method
        sb.AppendLine("    public void SetValue<T>(string key, T? value)");
        sb.AppendLine("    {");
        sb.AppendLine("        this[key] = value;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate Attributes property
        sb.AppendLine("    public global::Shiny.Reflector.AttributeInfo[] Attributes => new global::Shiny.Reflector.AttributeInfo[]");
        sb.AppendLine("    {");
        for (int i = 0; i < classInfo.Attributes.Count; i++)
        {
            var attr = classInfo.Attributes[i];
            var comma = i < classInfo.Attributes.Count - 1 ? "," : "";
            
            sb.AppendLine($"        new global::Shiny.Reflector.AttributeInfo(");
            sb.AppendLine($"            typeof({attr.TypeForTypeOf}),");
            sb.AppendLine($"            new global::Shiny.Reflector.AttributeArgumentInfo[]");
            sb.AppendLine($"            {{");
            
            for (int j = 0; j < attr.Arguments.Count; j++)
            {
                var arg = attr.Arguments[j];
                var argComma = j < attr.Arguments.Count - 1 ? "," : "";
                
                sb.AppendLine($"                new global::Shiny.Reflector.AttributeArgumentInfo(");
                sb.AppendLine($"                    typeof({arg.TypeForTypeOf}),");
                sb.AppendLine($"                    \"{arg.Name}\",");
                sb.AppendLine($"                    {arg.ValueExpression ?? "null"},");
                sb.AppendLine($"                    {arg.IsOptional.ToString().ToLower()},");
                sb.AppendLine($"                    {arg.DefaultValueExpression ?? "null"}");
                sb.AppendLine($"                ){argComma}");
            }
            
            sb.AppendLine($"            }}");
            sb.AppendLine($"        ){comma}");
        }
        sb.AppendLine("    };");
        sb.AppendLine("}");

        if (classInfo.Namespace != "global")
        {
            sb.AppendLine("}");
        }

        context.AddSource($"{classInfo.ClassName}Reflector.g.cs", sb.ToString());
    }

    static void GeneratePartialTypeWithReflectorProperty(SourceProductionContext context, ClassInfo classInfo, string accessorModifier, bool isRecord)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"// <auto-generated />");
        sb.AppendLine($"#nullable enable");
        sb.AppendLine();

        if (classInfo.Namespace != "global")
        {
            sb.AppendLine($"namespace {classInfo.Namespace}");
            sb.AppendLine("{");
        }

        var typeKeyword = isRecord ? "record" : "class";
        sb.AppendLine($"{accessorModifier} partial {typeKeyword} {classInfo.ClassName} : global::Shiny.Reflector.IHasReflectorClass");
        sb.AppendLine("{");
        sb.AppendLine("     private global::Shiny.Reflector.IReflectorClass? _reflector;");
        sb.AppendLine($"    {accessorModifier} global::Shiny.Reflector.IReflectorClass Reflector => _reflector ??= new {classInfo.ClassName}Reflector(this);");
        sb.AppendLine("}");

        if (classInfo.Namespace != "global")
        {
            sb.AppendLine("}");
        }

        context.AddSource($"{classInfo.ClassName}_ReflectorProperty.g.cs", sb.ToString());
    }
}
