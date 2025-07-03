using System.Reflection;

namespace Shiny.Reflector.Infrastructure;


public class TrueReflectionReflectorClass : IReflectorClass
{
    readonly ReflectedPropertyGeneratedInfo[] properties;
    
    
    public TrueReflectionReflectorClass(object obj)
    {
        this.ReflectedObject = obj;
        
        this.properties = obj
            .GetType()
            .GetProperties()
            .Where(x => x.GetMethod != null)
            .Select(x => new ReflectedPropertyGeneratedInfo(x))
            .ToArray();
        
        //obj.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
        //    .Where(m => m.IsSpecialName && (m.Name.StartsWith("get_") || m.Name.StartsWith("set_")))
        //    .ToList();
    }
    
    
    public object ReflectedObject { get; }
    public PropertyGeneratedInfo[] Properties => this.properties.Cast<PropertyGeneratedInfo>().ToArray();
    
    public T? GetValue<T>(string key)
        => this[key] is T value ? value : default;

    public void SetValue<T>(string key, T? value)
        => this[key] = value;

    public object? this[string key]
    {
        get
        {
            var prop = this.properties.FirstOrDefault(x => x.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase));
            if (prop == null)
                throw new ArgumentException($"Property '{key}' not found on {this.ReflectedObject.GetType().Name}");
            
            var value = prop.Property.GetValue(this.ReflectedObject);
            return value;
        }
        set
        {
            var prop = this.properties.FirstOrDefault(x => x.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase));
            if (prop == null)
                throw new ArgumentException($"Property '{key}' not found on {this.ReflectedObject.GetType().Name}");
            
            prop.Property.SetValue(this.ReflectedObject, value);
        }
    }
}

public record ReflectedPropertyGeneratedInfo(
    PropertyInfo Property
) : PropertyGeneratedInfo(
    Property.Name, 
    Property.PropertyType, 
    Property.SetMethod != null && !Property
        .SetMethod
        .ReturnParameter
        .GetRequiredCustomModifiers()
        .Any(m => m.Name == "IsExternalInit")
);
