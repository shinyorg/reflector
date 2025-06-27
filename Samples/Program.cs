using Samples;

var myClass = new MySampleClass
{
    Name = "Hello World",
    Age = null
};
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

Console.ReadLine();
