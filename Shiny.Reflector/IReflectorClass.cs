namespace Shiny.Reflector;


public interface IReflectorClass
{
    object ReflectedObject { get; }
    PropertyGeneratedInfo[] Properties { get; }

    T? GetValue<T>(string key);
    void SetValue<T>(string key, T? value);

    object? this[string key] { get; set; }
}

//void CallMethod(string methodName, params object?[]? parameters);
// object? CallMethod
//Task CallAsyncMethod
// Task<T> CallAsyncMethod<T>(string methodName, params object?[]? parameters);

// public record MethodGeneratedInfo(
//     string Name,
//     Type ReturnType,
//     ParameterGeneratedInfo[] Parameters
// );
//
// public record ParameterGeneratedInfo(
//     string Name,
//     Type Type,
//     bool IsOptional = false,
//     object? DefaultValue = null
// );
