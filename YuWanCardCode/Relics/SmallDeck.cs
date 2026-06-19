using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class SmallDeck : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("DeckThreshold", 20m)];

    public SmallDeck() : base(true)
    {
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex < 2;
    }

    public override async Task AfterActEntered()
    {
        await base.AfterActEntered();

        var runState = RunManager.Instance?.State;
        if (runState == null || runState.CurrentActIndex != 2) return;
        if (Owner == null) return;

        int deckSize = Owner.Deck.Cards.Count;
        int threshold = (int)DynamicVars["DeckThreshold"].BaseValue;
        var act = runState.Act;

        //     if (deckSize <= threshold)
        //     {
        //         if (act.BossEncounter is DoormakerBoss)
        //         {
        //             var otherBoss = act.AllBossEncounters.FirstOrDefault(e => e is not DoormakerBoss);
        //             if (otherBoss != null)
        //             {
        //                 MapCmd.SetBossEncounter(runState, otherBoss);
        //                 Flash();
        //                 MainFile.Logger.Info($"[SmallDeck] Deck <= {threshold}, replaced Doormaker with {otherBoss.Id.Entry}");
        //             }
        //         }
        //     }
        //     else
        //     {
        //         var doormaker = act.AllBossEncounters.FirstOrDefault(e => e is DoormakerBoss);
        //         if (doormaker != null && act.BossEncounter is not DoormakerBoss)
        //         {
        //             MapCmd.SetBossEncounter(runState, doormaker);
        //             Flash();
        //             MainFile.Logger.Info($"[SmallDeck] Deck > {threshold}, forced boss to Doormaker");
        //         }
        //     }
    }
}
