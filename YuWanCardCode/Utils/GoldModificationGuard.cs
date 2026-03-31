using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Utils;

public class GoldModificationGuard
{
    private readonly RecursiveCallGuard<decimal> _guard;
    private readonly Func<Player?> _getOwner;
    private readonly Func<decimal, decimal> _calculateModification;
    private readonly Func<decimal, Task> _modifyGoldAction;

    public GoldModificationGuard(
        Func<Player?> getOwner,
        Func<decimal, decimal> calculateModification,
        Func<decimal, Task> modifyGoldAction)
    {
        _getOwner = getOwner ?? throw new ArgumentNullException(nameof(getOwner));
        _calculateModification = calculateModification ?? throw new ArgumentNullException(nameof(calculateModification));
        _modifyGoldAction = modifyGoldAction ?? throw new ArgumentNullException(nameof(modifyGoldAction));

        _guard = new RecursiveCallGuard<decimal>(
            action: ExecuteModification,
            shouldProcess: value => value > 0m
        );
    }

    public bool ShouldGainGold(decimal amount, Player player)
    {
        var owner = _getOwner();
        if (owner == null || player != owner)
        {
            return true;
        }

        if (_guard.TryExecute(_calculateModification(amount)))
        {
            return true;
        }

        return true;
    }

    public async Task AfterGoldGained(Player player)
    {
        var owner = _getOwner();
        if (owner != null && player == owner)
        {
            await _guard.ExecutePendingAsync();
        }
    }

    private async Task ExecuteModification(decimal amount)
    {
        await _modifyGoldAction(amount);
    }

    public void Reset()
    {
        _guard.Reset();
    }
}
