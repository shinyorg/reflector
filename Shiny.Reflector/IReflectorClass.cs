namespace Shiny.Reflector;


public interface IReflectorClass
{
    object ReflectedObject { get; }
    PropertyGeneratedInfo[] Properties { get; }
        
    T? GetValue<T>(string key);
    void SetValue<T>(string key, T? value);
        
    object? this[string key] { get; set; }
}