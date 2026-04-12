using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigBully : YuWanCardModel
{
    private const int BaseDamage = 9;
    private const int TeammateDamageBonus = 3;
    private const int TeammateDamageBonusUpgraded = 4;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public PigBully() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithVars(new PigBullyDamageVar(BaseDamage, TeammateDamageBonus, TeammateDamageBonusUpgraded));
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            int teammateCount = GetTeammateCount();
            int damageBonus = IsUpgraded ? teammateCount * TeammateDamageBonusUpgraded : teammateCount * TeammateDamageBonus;
            int totalDamage = BaseDamage + damageBonus;

            await DamageCmd.Attack(totalDamage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    public int GetTeammateCount()
    {
        if (CombatState == null) return 0;
        var teammates = CombatState.GetTeammatesOf(Owner.Creature);
        return teammates.Count(t => t.IsAlive);
    }
}

public class PigBullyDamageVar(int baseDamage, int teammateBonus, int teammateBonusUpgraded) : DynamicVar(Key, baseDamage)
{
    public const string Key = "PigBullyDamage";
    private readonly int _baseDamage = baseDamage;

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        if (card is PigBully pigBully)
        {
            int teammateCount = pigBully.GetTeammateCount();
            int damageBonus = card.IsUpgraded ? teammateCount * teammateBonusUpgraded : teammateCount * teammateBonus;
            decimal totalDamage = _baseDamage + damageBonus;
            BaseValue = totalDamage;
            PreviewValue = totalDamage;
        }
        else
        {
            BaseValue = _baseDamage;
            PreviewValue = _baseDamage;
        }
    }
}
