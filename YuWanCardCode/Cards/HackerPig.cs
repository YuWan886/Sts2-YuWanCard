using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class HackerPig : YuWanCardModel
{
    private const string VfxMatrixRainPath = "res://YuWanCard/scenes/vfx/vfx_matrix_rain.tscn";

    public HackerPig() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var allCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != this 
                && !c.Keywords.Contains(CardKeyword.Unplayable) 
                && (c.EnergyCost == null || !c.EnergyCost.CostsX))
            .ToList();

        if (allCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selectedCards = await CardSelectCmd.FromSimpleGrid(choiceContext, allCards, Owner, prefs);

        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard != null)
        {
            PlayMatrixRainVfx();
            
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
            var power = await PowerCmd.Apply<HackerPigPower>(Owner.Creature, 1, Owner.Creature, this);
            CardCmd.ApplyKeyword(selectedCard, CardKeyword.Retain);
            if (power != null)
            {
                HackerPigPower.MarkCard(selectedCard, power);
            }
        }
    }

    private void PlayMatrixRainVfx()
    {
        try
        {
            VfxUtils.PlayCentered(VfxMatrixRainPath);
            MainFile.Logger.Debug("HackerPig: Matrix rain VFX triggered");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"HackerPig: Failed to play matrix rain VFX: {ex.Message}");
        }
    }
}
