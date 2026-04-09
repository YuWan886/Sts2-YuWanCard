using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class BlackHole : YuWanCardModel
{
    public BlackHole() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.AllEnemies)
    {
        WithVar("Magic", 5);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Magic"].UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var handCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != this)
            .ToList();

        if (handCards.Count == 0)
        {
            await CardPileCmd.Add(this, PileType.Exhaust);
            return;
        }

        var prefs = new CardSelectorPrefs(
            new LocString("cards", "YUWANCARD-BLACK_HOLE.selectionScreenPrompt"),
            0,
            handCards.Count
        );

        var selectedCards = await CardSelectCmd.FromHand(
            context: choiceContext,
            player: Owner,
            prefs: prefs,
            filter: c => c != this,
            source: this
        );

        var cardsToExhaust = selectedCards.ToList();
        int cardsToExhaustCount = cardsToExhaust.Count;

        if (cardsToExhaustCount > 0)
        {
            PlayBlackHoleVfx(cardsToExhaustCount);
        }

        foreach (var card in cardsToExhaust)
        {
            await CardPileCmd.Add(card, PileType.Exhaust);
        }

        int damagePerCard = DynamicVars["Magic"].IntValue;
        int totalDamage = damagePerCard * cardsToExhaustCount;

        if (totalDamage > 0 && CombatState != null)
        {
            foreach (var enemy in CombatState.Enemies)
            {
                if (enemy.IsAlive)
                {
                    await DamageCmd.Attack(totalDamage)
                        .FromCard(this)
                        .Targeting(enemy)
                        .Execute(choiceContext);
                }
            }
        }
    }

    private void PlayBlackHoleVfx(int cardCount)
    {
        try
        {
            var scenePath = "res://YuWanCard/scenes/vfx/vfx_black_hole.tscn";
            VfxUtils.PlayCentered(scenePath);
            MainFile.Logger.Debug($"BlackHole: VFX spawned with {cardCount} cards");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"BlackHole: VFX error: {ex.Message}");
        }
    }
}
