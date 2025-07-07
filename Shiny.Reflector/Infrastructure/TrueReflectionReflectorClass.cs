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

    AttributeInfo[]? attributes;
    public AttributeInfo[] Attributes => this.attributes ??= ReflectedObject
        .GetType()
        .GetCustomAttributes()
        .Select(attr => new AttributeInfo(
            attr.GetType(),
            GetAttributeArguments(attr)
        ))
        .ToArray();

    static AttributeArgumentInfo[] GetAttributeArguments(Attribute attr)
    {
        var arguments = new List<AttributeArgumentInfo>();
        var attrType = attr.GetType();
        
        // Get constructor arguments
        var constructors = attrType.GetConstructors();
        var constructor = constructors.FirstOrDefault(c => c.GetParameters().Length > 0);
        
        if (constructor != null)
        {
            var parameters = constructor.GetParameters();
            foreach (var param in parameters)
            {
                try
                {
                    // Try to get the value using reflection on the attribute's properties
                    var property = attrType.GetProperty(param.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    var field = attrType.GetField(param.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    
                    object? value = null;
                    if (property != null && property.CanRead)
                    {
                        value = property.GetValue(attr);
                    }
                    else if (field != null)
                    {
                        value = field.GetValue(attr);
                    }
                    
                    arguments.Add(new AttributeArgumentInfo(
                        param.ParameterType,
                        param.Name ?? string.Empty,
                        value,
                        param.HasDefaultValue,
                        param.HasDefaultValue ? param.DefaultValue : null
                    ));
                }
                catch
                {
                    // If we can't get the value, add it as optional with null value
                    arguments.Add(new AttributeArgumentInfo(
                        param.ParameterType,
                        param.Name ?? string.Empty,
                        null,
                        param.HasDefaultValue,
                        param.HasDefaultValue ? param.DefaultValue : null
                    ));
                }
            }
        }
        
        // Get named property arguments (properties that aren't constructor parameters)
        var properties = attrType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);
            
        foreach (var property in properties)
        {
            // Skip if this property was already added as a constructor parameter
            if (arguments.Any(a => string.Equals(a.Name, property.Name, StringComparison.OrdinalIgnoreCase)))
                continue;
                
            try
            {
                var value = property.GetValue(attr);
                arguments.Add(new AttributeArgumentInfo(
                    property.PropertyType,
                    property.Name,
                    value,
                    true, // Named properties are always optional
                    null
                ));
            }
            catch
            {
                // If we can't get the value, skip this property
            }
        }
        
        return arguments.ToArray();
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
