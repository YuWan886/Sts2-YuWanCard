using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.RightClick;

/// <summary>
/// 在模型上实现此接口即可接收 YuWan Core 的右键事件。
/// </summary>
public interface IYuWanRightClickableModel
{
    /// <summary>
    /// 仅用于本地快速过滤；这里只应依赖稳定的本地 UI 事实。
    /// </summary>
    bool CanHandleRightClickLocal(YuWanRightClickContext context)
    {
        return true;
    }

    /// <summary>
    /// 执行期判定；在本地执行或远端同步落地前调用。
    /// </summary>
    bool CanExecuteRightClick(YuWanRightClickExecutionContext context)
    {
        return true;
    }

    /// <summary>
    /// 右键动作实际执行入口。
    /// </summary>
    Task OnRightClick(YuWanRightClickExecutionContext context);
}

public interface IYuWanRightClickableCard : IYuWanRightClickableModel;

public interface IYuWanRightClickableRelic : IYuWanRightClickableModel;

public interface IYuWanRightClickablePower : IYuWanRightClickableModel;

public interface IYuWanRightClickablePotion : IYuWanRightClickableModel;

public readonly record struct YuWanRightClickBindingId(string Id)
{
    public override string ToString()
    {
        return Id;
    }
}

public readonly record struct YuWanRightClickTrigger(bool IsController = false, string? Metadata = null);

public readonly record struct YuWanRightClickContext(
    MegaCrit.Sts2.Core.Entities.Players.Player Player,
    AbstractModel Model,
    YuWanRightClickTrigger Trigger);

public readonly record struct YuWanRightClickExecutionContext(
    MegaCrit.Sts2.Core.Entities.Players.Player Player,
    AbstractModel Model,
    YuWanRightClickTrigger Trigger,
    MegaCrit.Sts2.Core.GameActions.Multiplayer.GameActionPlayerChoiceContext? PlayerChoiceContext,
    MegaCrit.Sts2.Core.GameActions.GameAction? Action);

public enum YuWanRightClickModelKind
{
    Card = 0,
    Relic = 1,
    Power = 2,
    Potion = 3
}

public static class YuWanRightClick
{
    public static IDisposable Register<TModel>(
        string localStem,
        Func<YuWanRightClickExecutionContext, Task> execute,
        int priority = 0,
        Func<YuWanRightClickContext, bool>? canHandleLocal = null,
        Func<YuWanRightClickExecutionContext, bool>? canExecute = null)
        where TModel : AbstractModel
    {
        return YuWanRightClickRegistry.Register<TModel>(
            localStem,
            execute,
            priority,
            canHandleLocal,
            canExecute);
    }
}
