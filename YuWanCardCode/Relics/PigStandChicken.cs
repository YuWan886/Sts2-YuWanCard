using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class PigStandChicken : YuWanRelicModel
{
    private const int AttacksPerTrigger = 3;
    private const int TriggerDamage = 5;

    private int _attackCardsPlayedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public PigStandChicken() : base(true)
    {
    }

    public override async Task BeforeCombatStart()
    {
        _attackCardsPlayedThisCombat = 0;

        if (Owner?.Creature == null)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<FeralPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null
            || cardPlay.Card.Owner != Owner
            || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        _attackCardsPlayedThisCombat++;
        if (_attackCardsPlayedThisCombat % AttacksPerTrigger != 0)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in Owner.Creature.CombatState.Enemies.Where(e => e.IsAlive))
        {
            await CreatureCmd.Damage(choiceContext, enemy, TriggerDamage, ValueProp.Move, Owner.Creature, null);
        }
    }
}
