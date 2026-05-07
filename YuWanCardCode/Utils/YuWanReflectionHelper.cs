using System.Reflection;

namespace YuWanCard.Utils;

public static class YuWanReflectionHelper
{
    private const BindingFlags MethodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags FieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags PropertyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

    private static MethodInfo? FindMethodInHierarchy(Type type, string methodName)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            var method = t.GetMethod(methodName, MethodFlags);
            if (method != null)
                return method;
        }
        return null;
    }

    private static FieldInfo? FindFieldInHierarchy(Type type, string fieldName)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            var field = t.GetField(fieldName, FieldFlags);
            if (field != null)
                return field;
        }
        return null;
    }

    private static PropertyInfo? FindPropertyInHierarchy(Type type, string propertyName)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            var prop = t.GetProperty(propertyName, PropertyFlags);
            if (prop != null)
                return prop;
        }
        return null;
    }

    public static T? GetPrivateField<T>(object instance, string fieldName)
    {
        try
        {
            var field = FindFieldInHierarchy(instance.GetType(), fieldName);
            if (field == null)
            {
                MainFile.Logger.Warn($"[ReflectionHelper] Field '{fieldName}' not found in {instance.GetType().Name}");
                return default;
            }
            return (T?)field.GetValue(instance);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[ReflectionHelper] Failed to get field '{fieldName}': {ex.Message}");
            return default;
        }
    }

    public static bool SetPrivateField(object instance, string fieldName, object? value)
    {
        try
        {
            var field = FindFieldInHierarchy(instance.GetType(), fieldName);
            if (field == null)
            {
                MainFile.Logger.Warn($"[ReflectionHelper] Field '{fieldName}' not found in {instance.GetType().Name}");
                return false;
            }
            field.SetValue(instance, value);
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[ReflectionHelper] Failed to set field '{fieldName}': {ex.Message}");
            return false;
        }
    }

    public static T? CallPrivateMethod<T>(object instance, string methodName, params object[] parameters)
    {
        try
        {
            var method = FindMethodInHierarchy(instance.GetType(), methodName);
            if (method == null)
            {
                MainFile.Logger.Warn($"[ReflectionHelper] Method '{methodName}' not found in {instance.GetType().Name}");
                return default;
            }
            return (T?)method.Invoke(instance, parameters);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[ReflectionHelper] Failed to call method '{methodName}': {ex.Message}");
            return default;
        }
    }

    public static bool CallPrivateMethod(object instance, string methodName, params object[] parameters)
    {
        try
        {
            var method = FindMethodInHierarchy(instance.GetType(), methodName);
            if (method == null)
            {
                MainFile.Logger.Warn($"[ReflectionHelper] Method '{methodName}' not found in {instance.GetType().Name}");
                return false;
            }
            method.Invoke(instance, parameters);
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[ReflectionHelper] Failed to call method '{methodName}': {ex.Message}");
            return false;
        }
    }

    public static MethodInfo? GetPrivateMethod(Type type, string methodName, Type[]? parameterTypes = null)
    {
        try
        {
            MethodInfo? method = null;
            for (var t = type; t != null; t = t.BaseType)
            {
                method = parameterTypes == null
                    ? t.GetMethod(methodName, MethodFlags)
                    : t.GetMethod(methodName, MethodFlags, null, parameterTypes, null);
                if (method != null)
                    break;
            }
            if (method == null)
            {
                MainFile.Logger.Warn($"[ReflectionHelper] Method '{methodName}' not found in {type.Name}");
            }
            return method;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[ReflectionHelper] Failed to get method '{methodName}': {ex.Message}");
            return null;
        }
    }

    public static FieldInfo? GetPrivateField(Type type, string fieldName)
    {
        try
        {
            var field = FindFieldInHierarchy(type, fieldName);
            if (field == null)
            {
                MainFile.Logger.Warn($"[ReflectionHelper] Field '{fieldName}' not found in {type.Name}");
            }
            return field;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[ReflectionHelper] Failed to get field '{fieldName}': {ex.Message}");
            return null;
        }
    }

    public static PropertyInfo? GetPrivateProperty(Type type, string propertyName)
    {
        try
        {
            var prop = FindPropertyInHierarchy(type, propertyName);
            if (prop == null)
            {
                MainFile.Logger.Warn($"[ReflectionHelper] Property '{propertyName}' not found in {type.Name}");
            }
            return prop;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[ReflectionHelper] Failed to get property '{propertyName}': {ex.Message}");
            return null;
        }
    }
}
