namespace Shiny.Reflector;


public interface IReflectorClass
{
    object ReflectedObject { get; }
    PropertyGeneratedInfo[] Properties { get; }
        
    T? GetValue<T>(string key);
    void SetValue<T>(string key, T? value);
        
    object? GetValue(string key);
    void SetValue(string key, object? value);
}