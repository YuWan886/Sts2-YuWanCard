using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;
using YuWanCard.Monsters;
using YuWanCard.Powers;
using YuWanCard.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YuWanCard.Relics;

[Pool(typeof(PigRelicPool))]
public class PigDemonForm : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override int MerchantCost => 266;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromPowerWithPowerHoverTips<StrengthPower>();

    public PigDemonForm() : base(true)
    {
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (entry is MerchantRelicEntry relicEntry && relicEntry.Model?.CanonicalInstance == CanonicalInstance)
        {
            return MerchantCost;
        }
        return originalPrice;
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner == null || Owner.Creature == null)
        {
            return;
        }

        bool isPigCharacter = Owner.Character is Pig;

        if (isPigCharacter)
        {
            var pigMinion = PetManager.FindPetByType<PigMinion>(Owner.Creature);
            CreatureVisualUtils.PlayPigTransformationSequence(
                Owner.Creature,
                "Tf",
                4.7f,
                "demon",
                pigMinion);
        }

        Flash();
        await PowerCmd.Apply<PigDemonFormPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    }
}
