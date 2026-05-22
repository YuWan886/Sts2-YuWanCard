using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Globalization;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfForgePersists : WhatIfRelicModel
{
    private const decimal BaseBladeDamage = 10m;

    [SavedProperty]
    public string YuWanCard_StoredForge { get; set; } = "0";

    public override bool ShowCounter => true;

    public override int DisplayAmount => decimal.ToInt32(decimal.Truncate(GetStoredForgeAmount()));

    public WhatIfForgePersists() : base(true)
    {
    }

    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();

        decimal storedForge = GetStoredForgeAmount();
        if (Owner?.PlayerCombatState == null || storedForge <= 0)
        {
            return;
        }

        await ForgeCmd.Forge(storedForge, Owner, this);
    }

    public override Task AfterForge(decimal amount, Player forger, AbstractModel? source)
    {
        if (forger == Owner)
        {
            SetStoredForgeAmount(GetCurrentForgeAmount());
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        SetStoredForgeAmount(GetCurrentForgeAmount());
        return Task.CompletedTask;
    }

    private decimal GetCurrentForgeAmount()
    {
        if (Owner?.PlayerCombatState == null)
        {
            return GetStoredForgeAmount();
        }

        return Owner.PlayerCombatState.AllCards
            .OfType<SovereignBlade>()
            .Where(card => !card.IsDupe)
            .Sum(card => Math.Max(0m, card.DynamicVars.Damage.BaseValue - BaseBladeDamage));
    }

    private decimal GetStoredForgeAmount()
    {
        return decimal.TryParse(
            YuWanCard_StoredForge,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal storedForge)
            ? storedForge
            : 0m;
    }

    private void SetStoredForgeAmount(decimal amount)
    {
        string newValue = Math.Max(0m, amount).ToString(CultureInfo.InvariantCulture);
        if (YuWanCard_StoredForge == newValue)
        {
            return;
        }

        YuWanCard_StoredForge = newValue;
        InvokeDisplayAmountChanged();
    }
}
