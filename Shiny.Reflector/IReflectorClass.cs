namespace Shiny.Reflector;

// TODO: events
// TODO: attributes
public interface IReflectorClass
{
    object ReflectedObject { get; }
    PropertyGeneratedInfo[] Properties { get; }

    T? GetValue<T>(string key);
    void SetValue<T>(string key, T? value);

    object? this[string key] { get; set; }
    
    // AttributeInfo[] Attributes { get; }
    // object? CallMethod(string methodName, params object?[]? parameters);
    // Task<object?> CallMethodAsync(string methodName, params object?[]? parameters);
}


// public record AttributeInfo(
//     Type Type
//     // TODO: properties/values
// );
// TODO: generics
/*
public record MethodGeneratedInfo(
     string Name,
     Type ReturnType,
     bool IsAsync,
     //Attribute[] Attributes,
     ParameterGeneratedInfo[] Parameters
);

public record ParameterGeneratedInfo(
     string Name,
     Type Type,
     //Attribute[] Attributes,
     bool IsOptional = false,
     object? DefaultValue = null
);
*/
