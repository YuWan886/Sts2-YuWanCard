using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Core.RightClick;

public static class YuWanRightClickRegistry
{
    private const int InterfaceBindingPriority = int.MinValue;
    private static readonly object Gate = new();
    private static readonly YuWanRightClickBindingId InterfaceBindingId = new($"{MainFile.ModId}:model_interface");
    private static readonly List<RegisteredRightClickBinding> Bindings = [];
    private static long _nextBindingSequence;

    public static IDisposable Register<TModel>(
        string localStem,
        Func<YuWanRightClickExecutionContext, Task> execute,
        int priority = 0,
        Func<YuWanRightClickContext, bool>? canHandleLocal = null,
        Func<YuWanRightClickExecutionContext, bool>? canExecute = null)
        where TModel : AbstractModel
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
        ArgumentNullException.ThrowIfNull(execute);

        var id = new YuWanRightClickBindingId($"{MainFile.ModId}:{localStem.Trim()}");
        var binding = new RegisteredRightClickBinding(
            id,
            typeof(TModel),
            canHandleLocal,
            canExecute,
            execute,
            priority,
            Interlocked.Increment(ref _nextBindingSequence));

        lock (Gate)
        {
            if (Bindings.Any(existing => existing.Id == id))
            {
                throw new InvalidOperationException($"Right-click binding is already registered: {id}");
            }

            Bindings.Add(binding);
            SortBindings();
        }

        return binding;
    }

    public static bool TryDispatch(YuWanRightClickContext context)
    {
        List<YuWanRightClickBindingId> bindingIds = CollectBindingIds(context);
        if (bindingIds.Count == 0)
        {
            return false;
        }

        if (!TryCreatePayload(context, bindingIds, out YuWanRightClickManagedPayload payload))
        {
            MainFile.Logger.Warn(
                $"RightClick: failed to build managed payload for {context.Model.Id} ({context.Model.GetType().FullName}).");
            return false;
        }

        return YuWanRightClickManagedActions.Request(RunManager.Instance, payload);
    }

    internal static async Task ExecuteManagedPayload(
        YuWanRightClickManagedPayload payload,
        GameActionPlayerChoiceContext? playerChoiceContext,
        GameAction? action)
    {
        if (!TryGetPlayer(payload.OwnerNetId, out Player player))
        {
            return;
        }

        if (!TryResolveModel(player, payload.Kind, payload.ModelToken, out AbstractModel model))
        {
            return;
        }

        await ExecuteBindings(player, model, payload.Trigger, payload.BindingIds, playerChoiceContext, action);
    }

    private static async Task ExecuteBindings(
        Player player,
        AbstractModel model,
        YuWanRightClickTrigger trigger,
        IReadOnlyList<YuWanRightClickBindingId> bindingIds,
        GameActionPlayerChoiceContext? playerChoiceContext,
        GameAction? action)
    {
        var context = new YuWanRightClickExecutionContext(player, model, trigger, playerChoiceContext, action);
        var executed = false;

        foreach (YuWanRightClickBindingId bindingId in bindingIds)
        {
            try
            {
                if (await TryExecuteBinding(bindingId, model, context))
                {
                    executed = true;
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn(
                    $"RightClick: binding execution failed. binding={bindingId} model={model.Id} type={model.GetType().FullName} error={ex}");
            }
        }

        if (executed)
        {
            model.InvokeExecutionFinished();
        }
    }

    private static async Task<bool> TryExecuteBinding(
        YuWanRightClickBindingId bindingId,
        AbstractModel model,
        YuWanRightClickExecutionContext context)
    {
        if (bindingId == InterfaceBindingId)
        {
            if (model is not IYuWanRightClickableModel rightClickable)
            {
                return false;
            }

            if (!TryCanExecute(rightClickable, context))
            {
                return false;
            }

            await rightClickable.OnRightClick(context);
            return true;
        }

        RegisteredRightClickBinding? binding = TryGetBinding(bindingId);
        if (binding == null || !binding.ModelType.IsInstanceOfType(model))
        {
            return false;
        }

        if (!TryCanExecute(binding, context))
        {
            return false;
        }

        await binding.Execute(context);
        return true;
    }

    private static List<YuWanRightClickBindingId> CollectBindingIds(YuWanRightClickContext context)
    {
        RegisteredRightClickBinding[] bindings = GetBindingsSnapshot();
        var ids = (from binding in bindings
            where binding.ModelType.IsInstanceOfType(context.Model)
            where TryCanHandleLocal(binding, context)
            select binding.Id).ToList();

        if (context.Model is IYuWanRightClickableModel rightClickable
            && TryCanHandleLocal(rightClickable, context))
        {
            InsertBuiltInBinding(ids, bindings, InterfaceBindingId, InterfaceBindingPriority);
        }

        return ids;
    }

    private static void InsertBuiltInBinding(
        List<YuWanRightClickBindingId> ids,
        IReadOnlyList<RegisteredRightClickBinding> bindings,
        YuWanRightClickBindingId id,
        int priority)
    {
        int insertIndex = ids
            .Select(bindingId => bindings.FirstOrDefault(candidate => candidate.Id == bindingId))
            .TakeWhile(binding => binding != null && binding.Priority > priority)
            .Count();

        ids.Insert(insertIndex, id);
    }

    private static bool TryCreatePayload(
        YuWanRightClickContext context,
        IReadOnlyList<YuWanRightClickBindingId> bindingIds,
        out YuWanRightClickManagedPayload payload)
    {
        payload = default;

        if (!TryGetModelKind(context.Model, context.Player, out YuWanRightClickModelKind kind))
        {
            return false;
        }

        EnsureIdentity(context.Model);
        if (!MultiplayerModelIdentityRegistry.TryGetToken(context.Model, out MultiplayerModelIdentityToken token))
        {
            return false;
        }

        payload = new YuWanRightClickManagedPayload(
            context.Player.NetId,
            kind,
            token,
            context.Trigger,
            [.. bindingIds]);
        return true;
    }

    private static void EnsureIdentity(AbstractModel model)
    {
        switch (model)
        {
            case CardModel card:
                MultiplayerModelIdentityRegistry.RegisterCardTree(card);
                break;
            case RelicModel relic:
                MultiplayerModelIdentityRegistry.EnsureRegistered(relic);
                break;
            case PowerModel power:
                MultiplayerModelIdentityRegistry.EnsureRegistered(power);
                break;
            case PotionModel potion:
                MultiplayerModelIdentityRegistry.EnsureRegistered(potion);
                break;
        }
    }

    private static bool TryGetModelKind(AbstractModel model, Player player, out YuWanRightClickModelKind kind)
    {
        kind = default;
        switch (model)
        {
            case CardModel card when card.Owner == player:
                kind = YuWanRightClickModelKind.Card;
                return true;
            case RelicModel relic when relic.Owner == player:
                kind = YuWanRightClickModelKind.Relic;
                return true;
            case PowerModel power when IsPowerReachableForPlayer(power, player):
                kind = YuWanRightClickModelKind.Power;
                return true;
            case PotionModel potion when potion.Owner == player:
                kind = YuWanRightClickModelKind.Potion;
                return true;
            default:
                return false;
        }
    }

    private static bool IsPowerReachableForPlayer(PowerModel power, Player player)
    {
        return power.Owner.Player == player
               || power.Owner.PetOwner == player
               || power.Owner.IsEnemy;
    }

    private static bool TryResolveModel(
        Player player,
        YuWanRightClickModelKind kind,
        MultiplayerModelIdentityToken token,
        out AbstractModel model)
    {
        model = null!;
        if (!MultiplayerModelIdentityRegistry.TryResolve(token, out AbstractModel resolved))
        {
            return false;
        }

        switch (kind)
        {
            case YuWanRightClickModelKind.Card:
                if (resolved is not CardModel card || card.Owner != player || card.Pile?.Type != PileType.Hand)
                {
                    return false;
                }

                model = card;
                return true;

            case YuWanRightClickModelKind.Relic:
                if (resolved is not RelicModel relic || relic.Owner != player)
                {
                    return false;
                }

                model = relic;
                return true;

            case YuWanRightClickModelKind.Power:
                if (resolved is not PowerModel power || !IsPowerReachableForPlayer(power, player))
                {
                    return false;
                }

                model = power;
                return true;

            case YuWanRightClickModelKind.Potion:
                if (resolved is not PotionModel potion || potion.Owner != player)
                {
                    return false;
                }

                model = potion;
                return true;

            default:
                return false;
        }
    }

    private static bool TryGetPlayer(ulong ownerNetId, out Player player)
    {
        player = RunManager.Instance
            .DebugOnlyGetState()
            ?.Players
            .FirstOrDefault(candidate => candidate.NetId == ownerNetId)!;
        return player != null;
    }

    private static bool TryCanHandleLocal(RegisteredRightClickBinding binding, YuWanRightClickContext context)
    {
        if (binding.CanHandleLocal == null)
        {
            return true;
        }

        try
        {
            return binding.CanHandleLocal(context);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"RightClick: binding preflight failed. binding={binding.Id} model={context.Model.Id} type={context.Model.GetType().FullName} error={ex}");
            return false;
        }
    }

    private static bool TryCanHandleLocal(IYuWanRightClickableModel rightClickable, YuWanRightClickContext context)
    {
        try
        {
            return rightClickable.CanHandleRightClickLocal(context);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"RightClick: interface preflight failed. model={context.Model.Id} type={context.Model.GetType().FullName} error={ex}");
            return false;
        }
    }

    private static bool TryCanExecute(RegisteredRightClickBinding binding, YuWanRightClickExecutionContext context)
    {
        if (binding.CanExecute == null)
        {
            return true;
        }

        try
        {
            return binding.CanExecute(context);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"RightClick: binding execute guard failed. binding={binding.Id} model={context.Model.Id} type={context.Model.GetType().FullName} error={ex}");
            return false;
        }
    }

    private static bool TryCanExecute(IYuWanRightClickableModel rightClickable, YuWanRightClickExecutionContext context)
    {
        try
        {
            return rightClickable.CanExecuteRightClick(context);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"RightClick: interface execute guard failed. model={context.Model.Id} type={context.Model.GetType().FullName} error={ex}");
            return false;
        }
    }

    private static RegisteredRightClickBinding? TryGetBinding(YuWanRightClickBindingId bindingId)
    {
        lock (Gate)
        {
            return Bindings.FirstOrDefault(binding => binding.Id == bindingId);
        }
    }

    private static RegisteredRightClickBinding[] GetBindingsSnapshot()
    {
        lock (Gate)
        {
            return [.. Bindings];
        }
    }

    private static void SortBindings()
    {
        Bindings.Sort((left, right) =>
        {
            int priority = right.Priority.CompareTo(left.Priority);
            return priority != 0 ? priority : left.Sequence.CompareTo(right.Sequence);
        });
    }

    private sealed class RegisteredRightClickBinding(
        YuWanRightClickBindingId id,
        Type modelType,
        Func<YuWanRightClickContext, bool>? canHandleLocal,
        Func<YuWanRightClickExecutionContext, bool>? canExecute,
        Func<YuWanRightClickExecutionContext, Task> execute,
        int priority,
        long sequence) : IDisposable
    {
        private bool _disposed;

        public YuWanRightClickBindingId Id { get; } = id;
        public Type ModelType { get; } = modelType;
        public Func<YuWanRightClickContext, bool>? CanHandleLocal { get; } = canHandleLocal;
        public Func<YuWanRightClickExecutionContext, bool>? CanExecute { get; } = canExecute;
        public Func<YuWanRightClickExecutionContext, Task> Execute { get; } = execute;
        public int Priority { get; } = priority;
        public long Sequence { get; } = sequence;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            lock (Gate)
            {
                Bindings.Remove(this);
            }
        }
    }
}
