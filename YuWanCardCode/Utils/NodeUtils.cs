using Godot;

namespace YuWanCard.Utils;

public static class NodeUtils
{
    /// <summary>
    /// 安全获取指定类型的子节点，如果获取失败则记录警告日志。
    /// </summary>
    /// <typeparam name="T">要获取的节点类型，必须继承自 Node。</typeparam>
    /// <param name="node">要搜索的父节点。</param>
    /// <param name="path">节点路径，如果为 null 则使用 FindChild 方法。</param>
    /// <returns>找到的节点实例，如果未找到则返回 null。</returns>
    public static T? GetNodeSafe<T>(this Node node, string? path = null) where T : Node
    {
        if (node == null)
        {
            MainFile.Logger.Warn($"NodeUtils: Cannot get node, parent node is null");
            return null;
        }

        T? result;
        if (string.IsNullOrEmpty(path))
        {
            result = node.FindChild("", true, true) as T;
        }
        else
        {
            result = node.GetNodeOrNull<T>(path);
        }

        if (result == null)
        {
            var typeName = typeof(T).Name;
            var pathInfo = path ?? "(FindChild)";
            MainFile.Logger.Warn($"NodeUtils: Failed to get node of type {typeName} at path '{pathInfo}'");
        }

        return result;
    }

    /// <summary>
    /// 尝试获取指定类型的节点并执行操作，如果节点不存在则不执行操作。
    /// </summary>
    /// <typeparam name="T">要获取的节点类型，必须继承自 Node。</typeparam>
    /// <param name="node">要搜索的父节点。</param>
    /// <param name="path">节点路径，如果为 null 则使用 FindChild 方法。</param>
    /// <param name="action">要执行的操作。</param>
    /// <returns>如果节点存在且操作执行成功则返回 true，否则返回 false。</returns>
    public static bool TryExecuteOnNode<T>(this Node node, string? path, Action<T> action) where T : Node
    {
        var targetNode = node.GetNodeSafe<T>(path);
        if (targetNode == null)
        {
            return false;
        }

        try
        {
            action(targetNode);
            return true;
        }
        catch (Exception ex)
        {
            var typeName = typeof(T).Name;
            var pathInfo = path ?? "(FindChild)";
            MainFile.Logger.Error($"NodeUtils: Error executing action on node of type {typeName} at path '{pathInfo}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 尝试获取指定类型的节点并执行异步操作，如果节点不存在则不执行操作。
    /// </summary>
    /// <typeparam name="T">要获取的节点类型，必须继承自 Node。</typeparam>
    /// <param name="node">要搜索的父节点。</param>
    /// <param name="path">节点路径，如果为 null 则使用 FindChild 方法。</param>
    /// <param name="action">要执行的异步操作。</param>
    /// <returns>如果节点存在且操作执行成功则返回 true，否则返回 false。</returns>
    public static async Task<bool> TryExecuteOnNodeAsync<T>(this Node node, string? path, Func<T, Task> action) where T : Node
    {
        var targetNode = node.GetNodeSafe<T>(path);
        if (targetNode == null)
        {
            return false;
        }

        try
        {
            await action(targetNode);
            return true;
        }
        catch (Exception ex)
        {
            var typeName = typeof(T).Name;
            var pathInfo = path ?? "(FindChild)";
            MainFile.Logger.Error($"NodeUtils: Error executing async action on node of type {typeName} at path '{pathInfo}': {ex.Message}");
            return false;
        }
    }
}
