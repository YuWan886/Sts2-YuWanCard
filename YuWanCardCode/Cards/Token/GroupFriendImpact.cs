using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Commands;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(TokenCardPool))]
public class GroupFriendImpact : YuWanCardModel
{
    private bool _createdThroughGroupFriend;

    protected override IEnumerable<string> ExtraRunAssetPaths => [..NSovereignBladeVfx.AssetPaths, GroupFriendCmd.PigVisualScenePath];

    public bool CreatedThroughGroupFriend
    {
        get => _createdThroughGroupFriend;
        set
        {
            AssertMutable();
            _createdThroughGroupFriend = value;
        }
    }

    public int CurrentDisplayDamage => DynamicVars.CalculatedDamage.IntValue;

    public GroupFriendImpact() : base(
        baseCost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Token,
        target: TargetType.AnyEnemy)
    {
        WithCalculatedDamage(
            ValueProp.Move,
            static (card, _) => card.Owner?.Creature?.GetPowerAmount<GroupFriendPower>() ?? 0,
            baseVal: 10,
            extraVal: 1);
        WithKeywords(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        string animName = Owner.Character is Regent ? "sovereignBladeTrigger" : "Cast";
        float delay = Owner.Character is Regent ? 0.25f : Owner.Character.CastAnimDelay;

        AttackCommand attackCommand = DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, null)
            .WithAttackerAnim(animName, delay)
            .WithAttackerFx(null, "event:/sfx/characters/regent/regent_sovereign_blade")
            .Targeting(cardPlay.Target)
            .BeforeDamage(() =>
            {
                NSovereignBladeVfx? vfxNode = GetVfxNode(Owner, this);
                NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(cardPlay.Target);
                if (vfxNode != null && targetNode != null)
                {
                    vfxNode.Attack(targetNode.VfxSpawnPosition);
                }

                return Task.CompletedTask;
            })
            .WithHitVfxNode(NBigSlashVfx.Create)
            .WithHitVfxNode(NBigSlashImpactVfx.Create);

        await attackCommand.Execute(choiceContext);
        WithCostUpgradeBy(-1);
    }



    protected override void AfterCloned()
    {
        base.AfterCloned();
        CreatedThroughGroupFriend = false;
    }

    public override void AfterTransformedFrom()
    {
        RemoveGroupFriendNode();
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this)
        {
            return Task.CompletedTask;
        }

        if ((!CreatedThroughGroupFriend && oldPileType == PileType.None) || oldPileType == PileType.Exhaust)
        {
            GroupFriendCmd.PlayCombatRoomGroupFriendVfx(Owner, this);
        }

        if (card.Pile?.Type == PileType.Exhaust)
        {
            RemoveGroupFriendNode();
        }

        return Task.CompletedTask;
    }

    public static NSovereignBladeVfx? GetVfxNode(Player player, CardModel card)
    {
        return NCombatRoom.Instance?
            .GetCreatureNode(player.Creature)?
            .GetChildren()
            .OfType<NSovereignBladeVfx>()
            .FirstOrDefault(node => node.Card == card);
    }

    private void RemoveGroupFriendNode()
    {
        GetVfxNode(Owner, this)?.RemoveSovereignBlade();
    }
}
