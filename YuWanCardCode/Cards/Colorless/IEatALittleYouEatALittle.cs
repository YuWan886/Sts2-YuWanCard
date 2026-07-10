using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Persistence;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class IEatALittleYouEatALittle : YuWanCardModel
{
    private const int BaseCombatUses = 4;
    private const int UpgradeCombatUses = 4;

    private static readonly SavedAttachedState<CardModel, int> CombatUseCountState =
        new("IEatALittleYouEatALittleCombatUseCount", defaultValueFactory: () => 0);

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override string? CustomPortraitPath => "res://YuWanCard/images/card_portraits/i_eat_a_little_you_eat_a_little.png";

    public IEatALittleYouEatALittle() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithVar("Uses", BaseCombatUses, UpgradeCombatUses);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState is not { } combatState)
        {
            return;
        }

        await IncreaseRandomBuff();

        int useCount = CombatUseCountState[this] + 1;
        CombatUseCountState[this] = useCount;
        SyncDisplayedUses();
        if (useCount >= GetConfiguredCombatUses())
        {
            return;
        }

        if (cardPlay.IsLastInSeries)
        {
            await GiveToRandomTeammate(combatState);
        }
    }

    protected override (PileType, CardPilePosition) GetResultPileTypeAndPositionForCardPlay()
    {
        return (PileType.None, CardPilePosition.Bottom);
    }

    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        SyncDisplayedUses();
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        SyncDisplayedUses();
    }

    private int GetConfiguredCombatUses()
    {
        return BaseCombatUses + (IsUpgraded ? UpgradeCombatUses : 0);
    }

    private int GetRemainingUses()
    {
        return Math.Max(0, GetConfiguredCombatUses() - CombatUseCountState[this]);
    }

    private void SyncDisplayedUses()
    {
        DynamicVars["Uses"].BaseValue = GetRemainingUses();
    }

    private async Task IncreaseRandomBuff()
    {
        if (Owner?.Creature == null)
        {
            return;
        }

        PowerModel? randomBuff = DeterministicRandomUtils.PickDeterministicBuffPower(
            Owner.Creature.Powers.Where(IsEligibleBuff),
            Owner.RunState.Rng.CombatCardSelection);
        if (randomBuff?.Id == null)
        {
            return;
        }

        PowerModel? canonical = ModelDb.GetByIdOrNull<PowerModel>(randomBuff.Id);
        if (canonical == null)
        {
            return;
        }

        try
        {
            PowerModel? mutable = canonical.ToMutable();
            if (mutable == null)
            {
                return;
            }

            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), mutable, Owner.Creature, 1, Owner.Creature, this);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[IEatALittleYouEatALittle] skip {randomBuff.Id}：{ex.Message}");
        }
    }

    private static bool IsEligibleBuff(PowerModel power)
    {
        return power.IsVisible
               && power.Type == PowerType.Buff
               && power.Amount > 0
               && power.StackType != PowerStackType.None
               && PowerSafetyUtils.IsSafePower(power);
    }

    private async Task GiveToRandomTeammate(ICombatState combatState)
    {
        if (Owner == null)
        {
            return;
        }

        Player originalOwner = Owner;
        List<Player> teammates = combatState.GetTeammatesOf(originalOwner.Creature)
            .Where(creature => creature.IsAlive && creature.IsPlayer)
            .Select(creature => creature.Player)
            .OfType<Player>()
            .Where(player => player != originalOwner)
            .ToList();
        if (teammates.Count == 0)
        {
            return;
        }

        Player? targetPlayer = originalOwner.RunState.Rng.CombatTargets.NextItem(teammates);
        if (targetPlayer == null)
        {
            return;
        }

        CardModel? transferredCard = CardCopyHelper.CreateCombatCopy(this, targetPlayer);
        if (transferredCard == null)
        {
            return;
        }

        CombatUseCountState[transferredCard] = CombatUseCountState[this];
        if (transferredCard is IEatALittleYouEatALittle transferredTypedCard)
        {
            transferredTypedCard.SyncDisplayedUses();
        }

        await CardPileCmd.AddGeneratedCardToCombat(transferredCard, PileType.Hand, originalOwner, CardPilePosition.Random);
    }
}
