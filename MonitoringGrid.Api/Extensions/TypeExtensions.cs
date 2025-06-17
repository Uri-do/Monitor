namespace MonitoringGrid.Api.Extensions;

/// <summary>
/// Extension methods for Type operations
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Determines if a type is nullable
    /// </summary>
    public static bool IsNullableType(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    /// <summary>
    /// Gets the underlying type if nullable, otherwise returns the type itself
    /// </summary>
    public static Type GetUnderlyingType(this Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <summary>
    /// Determines if a type is a simple type (primitive, string, DateTime, etc.)
    /// </summary>
    public static bool IsSimpleType(this Type type)
    {
        var underlyingType = type.GetUnderlyingType();
        
        return underlyingType.IsPrimitive ||
               underlyingType == typeof(string) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateTimeOffset) ||
               underlyingType == typeof(TimeSpan) ||
               underlyingType == typeof(Guid) ||
               underlyingType == typeof(decimal) ||
               underlyingType.IsEnum;
    }

    /// <summary>
    /// Determines if a type is a collection type
    /// </summary>
    public static bool IsCollectionType(this Type type)
    {
        return type != typeof(string) && 
               (type.IsArray || 
                type.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }

    /// <summary>
    /// Gets the element type of a collection
    /// </summary>
    public static Type? GetCollectionElementType(this Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments().FirstOrDefault();
    }
}
