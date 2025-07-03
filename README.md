# Shiny Reflector

Reflection is awesome and super powerful, but also very slow and non-AOT compliant. Using source generators, we look to solve some of those pain points.
This library gives you the power of reflection... without the actual reflection!

## Features
- List Properties as well as what have getters and setters
- Read/Write Values using string keys and object values
- Easy property indexer for loose typing access (ie. myreflector["MyProperty"] = 123)
- Fallback to reflection when a `reflector` is not available
- Generate build variables of your choice into a static class for easy access
- Works with the [MVVM Community Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) source generation

## Usage

### Internal Class "Reflection"

Using the following attribute and marking your class as partial
```csharp
[Shiny.Reflector.ReflectorAttribute]
public partial class MyClass
{
    public int MyProperty { get; set; }
    public string Message { get; set;}
}
```

> [!NOTE]
> Works on records as well, but you must use the `partial` keyword and attribute just like a class.

Just that attribute allows you to do this:

```csharp
var myClass = new MySampleClass
{
    Name = "Hello World",
    Age = null
};

// NOTE: the use of GetReflector - a reflector class is generated for each class marked with the ReflectorAttribute
var reflector = myClass.GetReflector();
foreach (var prop in reflector.Properties) 
{
    var objValue = reflector[prop.Name];
    Console.WriteLine($"Property: {prop.Name} ({prop.Type}) - Current Value: {objValue}");
}

// generics for type casting
var name = reflector.GetValue<string>("Name");
Console.WriteLine("Reflector Name: " + name);

// indexers for loose typing
Console.WriteLine("Reflector Value: " + reflector["age"]);

// set with generics
reflector.SetValue("Age", 99);

// or just an object on the indexer
reflector["name"] = "Something Else";
Console.WriteLine("Reflector Name Value: " + reflector["NaMe"]);
Console.WriteLine("Reflector Age Value: " + reflector["NaMe"]);
```

### Assembly Info Generation

You can also generate assembly information for your project by adding the following attribute to your `AssemblyInfo.cs` file:

In your project file, you can add the following property to enable the generation of assembly info:
```xml
<Project>
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <MyCustomVar>Hello World</MyCustomVar>
    </PropertyGroup>
    
    <ItemGroup>
        <ReflectorItem Include="MyReflectorItem" 
                       Value="This is a sample value" 
                       Visible="false" />
        
        <!--capture a build variable that we don't look for-->
        <ReflectorItem Include="PropertyGroupMyCustomVar" 
                       Value="$(MyCustomVar)" 
                       Visible="false" />
    </ItemGroup>
</Project>
```

And a new AssemblyInfo static class with constants will be generated for you (NOTE: it grabbed defaulted variables like `Company`, `Version`, etc. from the project file):

```csharp
public static class AssemblyInfo
{
    public const string Company = "Samples";
    public const string Version = "1.0.0";
    public const string TargetFramework = "net9.0";
    public const string TargetFrameworkVersion = "v9.0";
    public const string Platform = "AnyCPU";
    public const string MyReflectorItem = "This is a sample value";
    public const string PropertyGroupMyCustomVar = "Hello World";
}

```

and now you can do easy things like this:

```csharp
Console.WriteLine("Target Framework: " + AssemblyInfo.TargetFramework);
Console.WriteLine("My Custom Var: " + AssemblyInfo.PropertyGroupMyCustomVar);
```

## Using Reflector with MVVM Community Toolkit

If you are using the Community Toolkit MVVM and more specifically, the source generation that it uses. You need to do the following Reflector detects your properties.

In your csproj file, add the following:
```xml
<PropertyGroup>
    <LangVersion>preview</LangVersion>
</PropertyGroup>
```

Now, in your `ObservableObject` class, you can use the `Reflector` attribute.  Note properties use the newer C# partial properties keyword.
```csharp
[Shiny.Reflector.ReflectorAttribute]
public partial class MyObservableObject : ObservableObject
{
    [ObservableProperty]
    public partial string MyProperty { get; set; }
}
```

