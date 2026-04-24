using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Patches;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class RainDark : YuWanCardModel
{
    public RainDark() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Ancient,
        target: TargetType.AllAllies)
    {
        WithPower<IntangiblePower>(3);
        WithPower<RainDarkPower>(3);
        WithVar("HpPercentage", 25);
        WithKeywords(CardKeyword.Exhaust);
    }

    public float HpPercentage => DynamicVars["HpPercentage"].IntValue / 100f;

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var teammates = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer)
            .ToList();

        foreach (var teammate in teammates)
        {
            int targetHp = (int)(teammate.MaxHp * HpPercentage);
            await CreatureCmd.SetCurrentHp(teammate, targetHp);

            await CommonActions.Apply<IntangiblePower>(choiceContext, teammate, this, DynamicVars["IntangiblePower"].IntValue);
            await CommonActions.Apply<RainDarkPower>(choiceContext, teammate, this, DynamicVars["RainDarkPower"].IntValue);

            var player = teammate.Player;
            if (player != null && player.PlayerCombatState != null)
            {
                int currentEnergy = player.PlayerCombatState.Energy;
                if (currentEnergy > 0)
                {
                    await PlayerCmd.GainEnergy(currentEnergy, player);
                }
            }
        }

        RainDarkEffectPatch.AddRainEffect(DynamicVars["RainDarkPower"].IntValue);
    }
}
