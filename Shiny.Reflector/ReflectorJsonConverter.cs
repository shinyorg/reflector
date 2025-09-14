using System.Text.Json;
using System.Text.Json.Serialization;
using Shiny.Reflector.Infrastructure;

namespace Shiny.Reflector;

/// <summary>
/// A JsonConverter that uses the Shiny.Reflector system for high-performance serialization
/// without traditional reflection overhead.
/// </summary>
public class ReflectorJsonConverter<T> : JsonConverter<T> where T : class, new()
{
    private readonly bool _useSourceGeneratedReflector;
    private readonly bool _fallbackToTrueReflection;

    /// <summary>
    /// Initializes a new instance of the ReflectorJsonConverter.
    /// </summary>
    /// <param name="useSourceGeneratedReflector">Whether to prefer source-generated reflectors when available</param>
    /// <param name="fallbackToTrueReflection">Whether to fallback to true reflection if no reflector is available</param>
    public ReflectorJsonConverter(bool useSourceGeneratedReflector = true, bool fallbackToTrueReflection = true)
    {
        _useSourceGeneratedReflector = useSourceGeneratedReflector;
        _fallbackToTrueReflection = fallbackToTrueReflection;
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject token, got {reader.TokenType}");

        var instance = new T();
        var reflector = GetReflector(instance);

        if (reflector == null)
            throw new JsonException($"No reflector available for type {typeof(T).Name}. Enable fallbackToTrueReflection or add [Reflector] attribute.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return instance;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");

            var propertyName = reader.GetString();
            if (string.IsNullOrEmpty(propertyName))
                continue;

            reader.Read(); // Move to property value

            var propertyInfo = reflector.TryGetPropertyInfo(propertyName);
            if (propertyInfo == null)
            {
                // Skip unknown properties
                reader.Skip();
                continue;
            }

            if (!propertyInfo.HasSetter)
            {
                // Skip read-only properties
                reader.Skip();
                continue;
            }

            try
            {
                var value = JsonSerializer.Deserialize(ref reader, propertyInfo.Type, options);
                reflector.SetValue(propertyName, value);
            }
            catch (Exception ex)
            {
                throw new JsonException($"Failed to deserialize property '{propertyName}' of type {propertyInfo.Type.Name}", ex);
            }
        }

        throw new JsonException("Unexpected end of JSON input");
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var reflector = GetReflector(value);
        if (reflector == null)
            throw new JsonException($"No reflector available for type {typeof(T).Name}. Enable fallbackToTrueReflection or add [Reflector] attribute.");

        writer.WriteStartObject();

        foreach (var property in reflector.Properties)
        {
            try
            {
                var propertyValue = reflector[property.Name];
                
                // Handle naming policy if specified
                var propertyName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
                
                writer.WritePropertyName(propertyName);
                JsonSerializer.Serialize(writer, propertyValue, property.Type, options);
            }
            catch (Exception ex)
            {
                throw new JsonException($"Failed to serialize property '{property.Name}' of type {property.Type.Name}", ex);
            }
        }

        writer.WriteEndObject();
    }

    private IReflectorClass? GetReflector(T instance)
    {
        if (_useSourceGeneratedReflector)
        {
            var reflector = instance.GetReflector(_fallbackToTrueReflection);
            if (reflector != null)
                return reflector;
        }

        if (_fallbackToTrueReflection)
            return new TrueReflectionReflectorClass(instance);

        return null;
    }
}

/// <summary>
/// Non-generic version of ReflectorJsonConverter for easier registration with JsonSerializerOptions.
/// </summary>
public class ReflectorJsonConverter : JsonConverterFactory
{
    private readonly bool _useSourceGeneratedReflector;
    private readonly bool _fallbackToTrueReflection;

    /// <summary>
    /// Initializes a new instance of the ReflectorJsonConverter factory.
    /// </summary>
    /// <param name="useSourceGeneratedReflector">Whether to prefer source-generated reflectors when available</param>
    /// <param name="fallbackToTrueReflection">Whether to fallback to true reflection if no reflector is available</param>
    public ReflectorJsonConverter(bool useSourceGeneratedReflector = true, bool fallbackToTrueReflection = true)
    {
        _useSourceGeneratedReflector = useSourceGeneratedReflector;
        _fallbackToTrueReflection = fallbackToTrueReflection;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        // Only handle reference types that have a parameterless constructor
        if (!typeToConvert.IsClass || typeToConvert.IsAbstract)
            return false;

        if (typeToConvert == typeof(string) || typeToConvert.IsPrimitive)
            return false;

        // Check if type has a parameterless constructor
        var constructor = typeToConvert.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            return false;

        // If we're not using fallback, check if the type has a reflector
        if (!_fallbackToTrueReflection)
        {
            try
            {
                var instance = Activator.CreateInstance(typeToConvert);
                if (instance == null)
                    return false;

                var reflector = instance.GetReflector(fallbackToTrueReflection: false);
                return reflector != null;
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(ReflectorJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType, _useSourceGeneratedReflector, _fallbackToTrueReflection);
    }
}