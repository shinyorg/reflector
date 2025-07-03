# Shiny Reflector

Reflection without the actual... reflection.  AOT compliant!

## Usage

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

## Setup
1. Install the NuGet package <a href="https://www.nuget.org/packages/Shiny.Reflector" target="_blank"><img src="https://img.shields.io/nuget/v/Shiny.Reflector?style=for-the-badge" /></a>
2. Add the following [Reflector] attribute to your class and make sure it is marked as partial:
    ```csharp
    [Shiny.Reflector.ReflectorAttribute]
    public partial class MyClass { ... }
    ```
3. You can now use `GetReflector()` (available on all objects) to get a reflector for that class (falls back to reflection if the reflector is not available):
    ```csharp
    var reflector = myClass.GetReflector();
    ```

## Using Reflector with the MVVM Community Toolkit

If you are using the [Community Toolkit MVVM](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) and more specifically, the source generation that it uses. You need to do the following Reflector detects your properties.

In your `csproj` file, add the following:
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

