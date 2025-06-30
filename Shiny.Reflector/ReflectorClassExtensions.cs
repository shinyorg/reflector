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


    // /// <summary>
    // /// Converts the properties of the reflector class to a dictionary.
    // /// </summary>
    // /// <param name="this"></param>
    // /// <param name="deepFallbackCanUseReflection">If true, will use reflection to get properties that are not directly accessible.</param>
    // /// <returns></returns>
    // public static IDictionary<string, object> ToDictionary(
    //     this IReflectorClass @this,
    //     bool deepFallbackCanUseReflection = false
    // )
    // {
    //     var dict = new Dictionary<string, object>();
    //     foreach (var prop in @this.Properties)
    //     {
    //         var value = @this[prop.Name];
    //         if (value != null)
    //         {
    //             dict.Add(prop.Name, ConvertValueToDictionary(value, prop.Type, deepFallbackCanUseReflection));
    //         }
    //     }
    //
    //     return dict;
    // }
    //
    // static object ConvertValueToDictionary(object value, Type propertyType, bool deepFallbackCanUseReflection)
    // {
    //     // Use a stack to handle deep object graphs without recursion
    //     var stack = new Stack<(object obj, Type type, Action<object> setter)>();
    //     object result = null;
    //     
    //     stack.Push((value, propertyType, converted => result = converted));
    //     
    //     while (stack.Count > 0)
    //     {
    //         var (currentObj, currentType, setter) = stack.Pop();
    //         
    //         if (currentObj == null)
    //         {
    //             setter(null);
    //         }
    //         // Check if simple type (string, number, date/time types) - add directly
    //         else if (IsSimpleType(currentType))
    //         {
    //             setter(currentObj);
    //         }
    //         // Handle collections (arrays, lists, etc.)
    //         else if (currentObj is System.Collections.IEnumerable enumerable && currentType != typeof(string))
    //         {
    //             var list = new List<object>();
    //             setter(list);
    //             
    //             // var elementType = GetElementType(currentType);
    //             foreach (var item in enumerable)
    //             {
    //                 if (item != null)
    //                 {
    //                     if (IsSimpleType(item.GetType()))
    //                     {
    //                         list.Add(item);
    //                     }
    //                     else
    //                     {
    //                         // For complex items in collections, we need to process them
    //                         var itemReflector = item.GetReflector(deepFallbackCanUseReflection);
    //                         if (itemReflector != null)
    //                         {
    //                             list.Add(itemReflector.ToDictionary(deepFallbackCanUseReflection));
    //                         }
    //                         else
    //                         {
    //                             list.Add(item);
    //                         }
    //                     }
    //                 }
    //                 else
    //                 {
    //                     list.Add(null);
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             // Handle complex objects
    //             var reflector = currentObj.GetReflector(deepFallbackCanUseReflection);
    //             if (reflector != null)
    //             {
    //                 var nestedDict = new Dictionary<string, object>();
    //                 setter(nestedDict);
    //
    //                 // Process all properties of the complex object
    //                 foreach (var prop in reflector.Properties)
    //                 {
    //                     var propValue = reflector[prop.Name];
    //                     if (propValue != null)
    //                     {
    //                         if (IsSimpleType(prop.Type))
    //                         {
    //                             nestedDict.Add(prop.Name, propValue);
    //                         }
    //                         else
    //                         {
    //                             // Push complex properties onto the stack for processing
    //                             stack.Push((propValue, prop.Type, converted => nestedDict.Add(prop.Name, converted)));
    //                         }
    //                     }
    //                 }
    //             }
    //             else
    //             {
    //                 // If we can't get a reflector, add the object as-is
    //                 setter(currentObj);
    //             }
    //         }
    //     }
    //     
    //     return result;
    // }
    //
    // static Type GetElementType(Type collectionType)
    // {
    //     if (collectionType.IsArray)
    //         return collectionType.GetElementType();
    //     
    //     // Handle generic collections
    //     if (collectionType.IsGenericType)
    //     {
    //         var genericArgs = collectionType.GetGenericArguments();
    //         if (genericArgs.Length > 0)
    //             return genericArgs[0];
    //     }
    //     
    //     // Fallback to object type
    //     return typeof(object);
    // }
    //
    //
    // static bool IsSimpleType(Type type)
    // {
    //     // Handle nullable types
    //     var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
    //     
    //     return underlyingType.IsPrimitive ||
    //            underlyingType == typeof(string) ||
    //            underlyingType == typeof(int) ||
    //            underlyingType == typeof(byte) ||
    //            underlyingType == typeof(short) ||
    //            underlyingType == typeof(double) ||
    //            underlyingType == typeof(float) ||
    //            underlyingType == typeof(decimal) ||
    //            underlyingType == typeof(DateOnly) ||
    //            underlyingType == typeof(TimeOnly) ||
    //            underlyingType == typeof(DateTime) ||
    //            underlyingType == typeof(DateTimeOffset) ||
    //            underlyingType == typeof(TimeSpan) ||
    //            underlyingType == typeof(Guid) ||
    //            underlyingType.IsEnum;
    // }
    //
    //
    // /// <summary>
    // /// Sets the properties of the reflector class from a dictionary.
    // /// </summary>
    // /// <param name="this"></param>
    // /// <param name="dictionary"></param>
    // /// <param name="deepFallbackCanUseReflection">If true, will use reflection to set properties that are not directly accessible.</param>
    // /// <exception cref="ArgumentNullException"></exception>
    // public static void SetObjectFromDictionary(
    //     this IReflectorClass @this, 
    //     IDictionary<string, object> dictionary,
    //     bool deepFallbackCanUseReflection = false
    // )
    // {
    //     if (dictionary == null)
    //         throw new ArgumentNullException(nameof(dictionary), "Dictionary cannot be null");
    //
    //     foreach (var kvp in dictionary)
    //     {
    //         var prop = @this.TryGetPropertyInfo(kvp.Key);
    //         if (prop is { HasSetter: true })
    //         {
    //             var convertedValue = ConvertValueFromDictionary(kvp.Value, prop.Type, deepFallbackCanUseReflection);
    //             if (convertedValue != null && prop.Type.IsInstanceOfType(convertedValue))
    //             {
    //                 @this[kvp.Key] = convertedValue;
    //             }
    //             else if (convertedValue == null && !prop.Type.IsValueType)
    //             {
    //                 @this[kvp.Key] = null;
    //             }
    //         }
    //     }
    // }
    //
    // static object? ConvertValueFromDictionary(object value, Type targetType, bool deepFallbackCanUseReflection)
    // {
    //     if (value == null)
    //         return null;
    //
    //     // If the value is already of the correct type, return it directly
    //     if (targetType.IsInstanceOfType(value))
    //         return value;
    //
    //     // Handle nullable types
    //     var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
    //
    //     // Handle simple type conversions
    //     if (IsSimpleType(underlyingType))
    //     {
    //         try
    //         {
    //             return Convert.ChangeType(value, underlyingType);
    //         }
    //         catch
    //         {
    //             return null;
    //         }
    //     }
    //
    //     // Handle collections
    //     if (value is System.Collections.IList sourceList && 
    //         (targetType.IsArray || (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))))
    //     {
    //         var elementType = GetElementType(targetType);
    //         var targetList = new List<object>();
    //
    //         foreach (var item in sourceList)
    //         {
    //             var convertedItem = ConvertValueFromDictionary(item, elementType, deepFallbackCanUseReflection);
    //             targetList.Add(convertedItem);
    //         }
    //
    //         // Convert to array if needed
    //         if (targetType.IsArray)
    //         {
    //             var array = Array.CreateInstance(elementType, targetList.Count);
    //             for (int i = 0; i < targetList.Count; i++)
    //             {
    //                 array.SetValue(targetList[i], i);
    //             }
    //             return array;
    //         }
    //
    //         // Return as List<T>
    //         var listType = typeof(List<>).MakeGenericType(elementType);
    //         var list = Activator.CreateInstance(listType);
    //         var addMethod = listType.GetMethod("Add");
    //         foreach (var item in targetList)
    //         {
    //             addMethod?.Invoke(list, new[] { item });
    //         }
    //         return list;
    //     }
    //
    //     // Handle complex objects (dictionaries)
    //     if (value is IDictionary<string, object> sourceDict)
    //     {
    //         // Try to create an instance of the target type
    //         try
    //         {
    //             var instance = Activator.CreateInstance(targetType);
    //             if (instance != null)
    //             {
    //                 var reflector = instance.GetReflector(deepFallbackCanUseReflection);
    //                 if (reflector != null)
    //                 {
    //                     reflector.SetObjectFromDictionary(sourceDict, deepFallbackCanUseReflection);
    //                     return instance;
    //                 }
    //             }
    //         }
    //         catch
    //         {
    //             // If we can't create an instance or get a reflector, return the original value
    //         }
    //     }
    //
    //     // Fallback: return the original value
    //     return value;
    // }
}