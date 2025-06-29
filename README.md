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

### Put ReflectionExtensions (the thing that let's you get the reflector) in a separate assembly

```
<PropertyGroup>
    <ShinyReflectorNamespace>MyNamespace</ShinyReflectorNamespace>
    
    OR
    
    <RootNamespace>MyNamespace</RootNamespace>
    
    OTHERWISE
    global namespace (no namespace)
</PropertyGroup>
```

### TODO
* Ensure GetReflector calls don't get mixed up since they can be generated in different assemblies
  * Could add it directly to a partial class, the problem is that will interfere with libraries like CTMVVM or does it?
  * I don't want dependency injection involved here
  * Shiny wants to access GetReflector, so it needs to be public and I need to ensure it gets generated in the same assembly OR does pickup
    the extension from another assembly