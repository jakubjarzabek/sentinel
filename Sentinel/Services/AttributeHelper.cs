namespace Sentinel.Services;

public static class AttributeHelper
{
    public static bool HasAttribute<T>(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var attributes = type.GetCustomAttributes(typeof(T), true);

        return attributes.Any();
    }

    public static bool HasAttribute<T>(this object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.GetType().HasAttribute<T>();
    }
}