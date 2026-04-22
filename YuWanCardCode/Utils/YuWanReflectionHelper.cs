using System.Reflection;

namespace YuWanCard.Utils;

public static class YuWanReflectionHelper
{
    public static T? GetPrivateField<T>(object instance, string fieldName)
    {
        try
        {
            var field = instance.GetType().GetField(fieldName, 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
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
            var field = instance.GetType().GetField(fieldName, 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
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
            var method = instance.GetType().GetMethod(methodName, 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
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
            var method = instance.GetType().GetMethod(methodName, 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
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
            var method = parameterTypes == null 
                ? type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                : type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
            
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
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            
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
            var property = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (property == null)
            {
                MainFile.Logger.Warn($"[ReflectionHelper] Property '{propertyName}' not found in {type.Name}");
            }
            
            return property;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[ReflectionHelper] Failed to get property '{propertyName}': {ex.Message}");
            return null;
        }
    }
}
