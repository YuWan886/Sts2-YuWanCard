using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Utils;

public class GoldModificationGuard
{
    private decimal _pendingModification;
    private bool _isApplyingModification;
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
    }

    public bool ShouldGainGold(decimal amount, Player player)
    {
        var owner = _getOwner();
        if (owner == null || player != owner)
        {
            return true;
        }

        if (_isApplyingModification)
        {
            return true;
        }

        _pendingModification = _calculateModification(amount);
        return true;
    }

    public async Task AfterGoldGained(Player player)
    {
        var owner = _getOwner();
        if (owner == null || player != owner)
        {
            return;
        }

        if (_isApplyingModification)
        {
            return;
        }

        if (_pendingModification <= 0m)
        {
            return;
        }

        var modification = _pendingModification;
        _pendingModification = 0m;
        _isApplyingModification = true;
        try
        {
            await _modifyGoldAction(modification);
        }
        finally
        {
            _isApplyingModification = false;
        }
    }

    public void Reset()
    {
        _pendingModification = 0m;
        _isApplyingModification = false;
    }
}
