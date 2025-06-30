using Shiny.Reflector.Infrastructure;

namespace Shiny.Reflector;


public static class ReflectorClassExtensions
{
    /// <summary>
    /// If the object is an <see cref="IReflectorClass"/>, this will return it.
    /// </summary>
    /// <param name="this">Your object</param>
    /// <param name="fallbackToTrueReflection">Use true reflection if necessary</param>
    /// <returns>A reflector isn't if one is found to exist on the class</returns>
    public static IReflectorClass? GetReflector(this object @this, bool fallbackToTrueReflection = false)
    {
        if (@this is IHasReflectorClass reflector)
            return reflector.Reflector;

        if (fallbackToTrueReflection)
            // Fallback to TrueReflectionReflectorClass if the object is a class
            return new TrueReflectionReflectorClass(@this);

        return null;
    }
    
    /// <summary>
    /// Tries to get the property information for a given property name.
    /// </summary>
    /// <param name="this"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static PropertyGeneratedInfo? TryGetPropertyInfo(this IReflectorClass @this, string propertyName) =>
        @this
            .Properties
            .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Checks if the class has a property with the specified name.
    /// </summary>
    /// <param name="this"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static bool HasProperty(this IReflectorClass @this, string propertyName)
    {
        if (String.IsNullOrWhiteSpace(propertyName))
            return false;
        
        return @this
            .Properties
            .Any(p => p.Name.Equals(
                propertyName, 
                StringComparison.OrdinalIgnoreCase
            ));
    }

    /// <summary>
    /// Tries to get the value of a property.
    /// </summary>
    /// <param name="this"></param>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TryGetValue(this IReflectorClass @this, string propertyName, out object? value)
    {
        if (@this.HasProperty(propertyName))
        {
            value = @this[propertyName];
            return true;
        }
        value = null;
        return false;
    }

    
    /// <summary>
    /// Tries to set the value of a property.
    /// </summary>
    /// <param name="this"></param>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TrySetValue(this IReflectorClass @this, string propertyName, object value)
    {
        var prop = @this.TryGetPropertyInfo(propertyName);
        
        if (prop != null && prop.Type.IsInstanceOfType(value))
        {
            @this[propertyName] = value;
            return true;
        }
        return false;
    }
    
    
    /// <summary>
    /// Tries to get the value of a property as a specific type.
    /// </summary>
    /// <param name="this"></param>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TryGetValue<T>(this IReflectorClass @this, string propertyName, out T value)
    {
        var result = false;
        value = default;
        
        if (@this.HasProperty(propertyName))
        {
            var objValue = @this[propertyName];
            if (objValue is T tValue)
            {
                value = tValue;
                result = true;
            }
        }
        
        return result;
    }

    
    /// <summary>
    /// Tries to set the value of a property as a specific type.
    /// </summary>
    /// <param name="this"></param>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TrySetValue<T>(this IReflectorClass @this, string propertyName, T value)
    {
        var result = false;
        var property = @this.TryGetPropertyInfo(propertyName);
        if (property is { HasSetter: true } && typeof(T).IsAssignableTo(property.Type))
        {
            @this[propertyName] = value;
            result = true;
        }
        return result;
    }


    /// <summary>
    /// Converts the properties of the reflector class to a dictionary.
    /// </summary>
    /// <param name="this"></param>
    /// <returns></returns>
    public static IDictionary<string, object> ToDictionary(this IReflectorClass @this)
    {
        var dict = new Dictionary<string, object>();
        foreach (var prop in @this.Properties)
        {
            var value = @this[prop.Name];
            if (value != null)
                dict.Add(prop.Name, value);
        }

        return dict;
    }


    /// <summary>
    /// Sets the properties of the reflector class from a dictionary.
    /// </summary>
    /// <param name="this"></param>
    /// <param name="dictionary"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void SetObjectFromDictionary(this IReflectorClass @this, IDictionary<string, object> dictionary)
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary), "Dictionary cannot be null");

        foreach (var kvp in dictionary)
        {
            var prop = @this.TryGetPropertyInfo(kvp.Key);
            if (prop != null && prop.Type.IsInstanceOfType(kvp.Value))
                @this[kvp.Key] = kvp.Value;
        }
    }
}