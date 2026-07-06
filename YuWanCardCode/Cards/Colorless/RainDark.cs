using YuWanCard.Core;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class RainDark : YuWanCardModel
{
    public RainDark() : base(
        baseCost: 3,
        type: CardType.Power,
        rarity: CardRarity.Ancient,
        target: TargetType.AllAllies)
    {
        WithPower<IntangiblePower>(3);
        WithPower<RainDarkPower>(3);
        WithVar("HpPercentage", 25);
    }

    public float HpPercentage => DynamicVars["HpPercentage"].IntValue / 100f;

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var players = CombatState!.GetLivingPlayers();

        foreach (var player in players)
        {
            var teammate = player.Creature;
            int targetHp = (int)(teammate.MaxHp * HpPercentage);
            await CreatureCmd.SetCurrentHp(teammate, targetHp);

            await CommonActions.Apply<IntangiblePower>(choiceContext, teammate, this, DynamicVars["IntangiblePower"].IntValue);
            await CommonActions.Apply<RainDarkPower>(choiceContext, teammate, this, DynamicVars["RainDarkPower"].IntValue);

            if (player.PlayerCombatState != null)
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
