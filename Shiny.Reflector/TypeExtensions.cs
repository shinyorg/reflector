namespace Shiny.Reflector;

public static class TypeExtensions
{
    public static bool IsSimpleType(this Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType.IsPrimitive ||
               underlyingType == typeof(string) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(byte) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(DateOnly) ||
               underlyingType == typeof(TimeOnly) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateTimeOffset) ||
               underlyingType == typeof(TimeSpan) ||
               underlyingType == typeof(Guid) ||
               underlyingType.IsEnum;
    }
}