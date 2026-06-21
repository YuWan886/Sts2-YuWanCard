using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Integrations.Hextech.RelicPools;

namespace YuWanCard.Hextech.Relics;

[Pool(typeof(HextechPigRunePool))]
public abstract class HextechPigForgeBase : YuWanRelicModel
{
    private const string HextechForgeIconBasePath = "res://HextechRunes/images/relics";

    public sealed override RelicRarity Rarity => RelicRarity.None;

    protected override string IconBasePath => $"{HextechForgeIconBasePath}/{GetForgeIconStem()}";

    public sealed override string? CustomRarityLabelKey => "YUWANCARD-HEXTECH_RUNE_RARITY.label";

    public abstract HextechForgeRarity HextechRarity { get; }

    /// <summary>
    /// Mirror of HextechRunes.HextechForgeBase stacking behavior. Pig forges cannot inherit
    /// HextechForgeBase (it lives in the optional HextechRunes assembly), so we replicate the
    /// stacking members here. Duplicate-obtain merging is handled by HextechRuntimeCompat's
    /// RelicCmd.Obtain prefix, since Hextech's own stacking hook only recognizes its own
    /// HextechForgeBase instances.
    /// </summary>
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int SavedStackCount
    {
        get => StackCount;
        set
        {
            int target = Math.Max(1, value);
            while (StackCount < target)
            {
                IncrementStackCount();
            }

            InvokeDisplayAmountChanged();
        }
    }

    public override bool IsStackable => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => !IsCanonical ? StackCount : 0;

    protected int StackAmount => Math.Max(1, StackCount);

    protected decimal StackMultiplier => StackAmount;

    protected decimal Stacked(decimal value)
    {
        return value * StackMultiplier;
    }

    protected decimal StackedMultiplier(decimal value)
    {
        if (StackAmount <= 1)
        {
            return value;
        }

        return (decimal)Math.Pow((double)value, StackAmount);
    }

    public void AddForgeStack(bool flash = true)
    {
        IncrementStackCount();
        InvokeDisplayAmountChanged();
        if (flash)
        {
            Flash();
        }
    }

    public virtual bool IsAvailableForPlayer(Player player)
    {
        return player.Character.Id == ModelDb.GetId<Characters.Pig>();
    }

    protected HextechPigForgeBase() : base(true)
    {
        // SavedStackCount is declared on this abstract base; the registration scanner only
        // registers concrete model types, so register the runtime type here (idempotent).
        SavedPropertyRegistration.RegisterType(GetType());
    }

    private string GetForgeIconStem()
    {
        return HextechRarity switch
        {
            HextechForgeRarity.Silver => "silverForge",
            HextechForgeRarity.Gold => "goldForge",
            HextechForgeRarity.Prismatic => "prismaticForge",
            _ => "silverForge"
        };
    }
}
