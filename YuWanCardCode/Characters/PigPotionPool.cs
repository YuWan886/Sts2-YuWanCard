using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using YuWanCard.Potions;
using YuWanCard.Timeline;
using YuWanCard.Timeline.Epochs;

namespace YuWanCard.Characters;

public class PigPotionPool : YuWanPotionPoolModel
{
    public override string? TextEnergyIconPath => "res://YuWanCard/images/characters/pig_text_enery.png";
    
    public override string? BigEnergyIconPath => "res://YuWanCard/images/characters/pig_enery_counter.png";

    public override bool IsShared => false;

    protected override IEnumerable<PotionModel> GenerateAllPotions()
    {
        return
        [
            ModelDb.Potion<CarrotFeastPotion>(),
            ModelDb.Potion<SweetDreamPotion>(),
            ModelDb.Potion<OinkChargePotion>()
        ];
    }

    public override IEnumerable<PotionModel> GetUnlockedPotions(UnlockState unlockState)
    {
        PigTimelineRegistry.EnsureRegistered();

        if (ReferenceEquals(unlockState, UnlockState.all))
        {
            return GenerateAllPotions();
        }

        if (!unlockState.IsEpochRevealed<Pig4Epoch>())
        {
            return Array.Empty<PotionModel>();
        }

        return GenerateAllPotions();
    }
}
