using Samples;
using Shiny.Reflector;

var myClass = new MySampleClass
{
    Name = "Hello World",
    Age = null
};
var reflector = myClass.GetReflector();
if (reflector == null)
{
    Console.WriteLine("No reflector found");
    return;
}
foreach (var prop in reflector.Properties) 
{
    var objValue = reflector[prop.Name];
    Console.WriteLine($"Property: {prop.Name} ({prop.Type}) - Current Value: {objValue}");
}

Console.WriteLine("Has Rando Property: " + reflector.HasProperty("asdfasdfasdf"));
Console.WriteLine("Try Get Rando Property: " + reflector.TryGetValue("asdfasdfasdf", out var randoValue));
Console.WriteLine("Try Set Rando Property: " + reflector.TrySetValue("asdfasdfasdf", "Hello World"));

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

Console.ReadLine();
