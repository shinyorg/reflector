using Samples;

var myClass = new MySampleClass
{
    Name = "Hello World",
    Age = null
};
var reflector = myClass.GetReflector();
foreach (var prop in reflector.Properties) 
{
    Console.WriteLine($"{prop.Name} ({prop.Type.Name})");
}

var name = reflector.GetValue<string>("Name");
Console.WriteLine("Reflector Name: " + name);

var age = reflector.GetValue<int?>("age");
Console.WriteLine("Reflector Value: " + age);

reflector.SetValue("Age", 99);
age = reflector.GetValue<int?>("age");
Console.WriteLine("Reflector Value: " + age);

Console.ReadLine();
