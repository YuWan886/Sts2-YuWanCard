using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Characters;
using YuWanCard.Monsters;
using YuWanCard.Powers;
using YuWanCard.Utils;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Relics;

[Pool(typeof(PigRelicPool))]
public class PigDemonForm : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override int MerchantCost => 250;

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

        Flash();
        await PowerCmd.Apply<PigDemonFormPower>(Owner.Creature, 1, Owner.Creature, null);
    }
}
