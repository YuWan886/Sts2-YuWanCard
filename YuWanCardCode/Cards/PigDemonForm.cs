using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Characters;
using YuWanCard.Monsters;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigDemonForm : YuWanCardModel
{
    public PigDemonForm() : base(
        baseCost: 3,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<PigDemonFormPower>(1);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<StrengthPower>()));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthGain"].UpgradeValueBy(1m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool isPigCharacter = Owner.Character is Pig;

        if (isPigCharacter)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Tf", 4.5f);
            await Task.Delay(TimeSpan.FromSeconds(4.7f));

            PigDemonFormPower.SwitchCreatureSkin(Owner.Creature, "demon");
            NCombatRoom.Instance?.GetCreatureNode(Owner.Creature)?.SetAnimationTrigger("Idle");

            var pigMinion = PetManager.FindPetByType<PigMinion>(Owner.Creature);
            if (pigMinion != null && pigMinion.IsAlive)
            {
                PigDemonFormPower.SwitchCreatureSkin(pigMinion, "demon");
                NCombatRoom.Instance?.GetCreatureNode(pigMinion)?.SetAnimationTrigger("Idle");
            }
        }

        await PowerCmd.Apply<PigDemonFormPower>(
            Owner.Creature,
            DynamicVars["PigDemonFormPower"].BaseValue,
            Owner.Creature,
            this);
    }
}
