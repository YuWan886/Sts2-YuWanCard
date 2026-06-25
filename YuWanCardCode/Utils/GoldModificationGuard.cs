using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Utils;

/// <summary>
/// Helper for relics/powers that need to modify gold gain amounts or trigger side effects.
/// Uses <c>ModifyGoldGained</c> / <c>AfterModifyingGoldGained</c> (beta API — replaces removed ShouldGainGold).
/// </summary>
public class GoldModificationGuard
{
    private bool _isApplyingModification;
    private readonly object _bindingToken;
    private readonly Func<Player?> _getOwner;
    private readonly Func<decimal, decimal> _calculateDelta;
    private readonly Func<Player, decimal, Task>? _onModified;

    public GoldModificationGuard(
        object bindingToken,
        Func<Player?> getOwner,
        Func<decimal, decimal> calculateDelta,
        Func<Player, decimal, Task>? onModified = null)
    {
        _bindingToken = bindingToken ?? throw new ArgumentNullException(nameof(bindingToken));
        _getOwner = getOwner ?? throw new ArgumentNullException(nameof(getOwner));
        _calculateDelta = calculateDelta ?? throw new ArgumentNullException(nameof(calculateDelta));
        _onModified = onModified;
    }

    public bool IsBoundTo(object bindingToken)
    {
        return ReferenceEquals(_bindingToken, bindingToken);
    }

    public decimal ModifyGoldGained(Player player, decimal amount)
    {
        var owner = _getOwner();
        if (owner == null || player != owner || _isApplyingModification)
            return amount;
        return amount + _calculateDelta(amount);
    }

    public async Task AfterModifyingGoldGained(Player player, decimal amount)
    {
        if (_onModified == null)
            return;

        var owner = _getOwner();
        if (owner == null || player != owner || _isApplyingModification)
            return;

        decimal delta = _calculateDelta(amount);
        _isApplyingModification = true;
        try
        {
            await _onModified(owner, delta);
        }
        finally
        {
            _isApplyingModification = false;
        }
    }

    public void Reset()
    {
        _isApplyingModification = false;
    }
}
