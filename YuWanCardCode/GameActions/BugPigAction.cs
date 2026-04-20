using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.GameActions;

public class BugPigAction : GameAction
{
    private readonly Player _player;
    private readonly ulong _targetNetId;
    private readonly int _errorCount;
    private readonly bool _isUpgraded;

    public override ulong OwnerId => _player.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    public BugPigAction(Player player, ulong targetNetId, int errorCount, bool isUpgraded)
    {
        _player = player;
        _targetNetId = targetNetId;
        _errorCount = errorCount;
        _isUpgraded = isUpgraded;
    }

    protected override async Task ExecuteAction()
    {
        var target = GetTargetCreature();
        if (target == null)
            return;

        var card = GetCardFromHand();
        if (card == null)
        {
            MainFile.Logger.Warn($"BugPigAction: Card not found in hand");
            return;
        }

        const int baseDamage = 7;
        const int errorBonus = 3;
        const int errorBonusUpgraded = 5;

        int damageBonus = _isUpgraded ? _errorCount * errorBonusUpgraded : _errorCount * errorBonus;
        int totalDamage = baseDamage + damageBonus;

        MainFile.Logger.Info($"BugPig: Dealing {totalDamage} damage (base: {baseDamage}, errors: {_errorCount}, bonus: {damageBonus})");

        await DamageCmd.Attack(totalDamage)
            .FromCard(card)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(new ThrowingPlayerChoiceContext());

        if (!TestMode.IsOn)
        {
            VfxUtils.PlayCentered("res://YuWanCard/scenes/vfx/vfx_glitch.tscn");
        }
    }

    private Creature? GetTargetCreature()
    {
        var combatState = _player.Creature?.CombatState;
        if (combatState == null) return null;

        foreach (var creature in combatState.Creatures)
        {
            if (creature.Player != null && creature.Player.NetId == _targetNetId)
            {
                return creature;
            }
        }
        return null;
    }

    private CardModel? GetCardFromHand()
    {
        var hand = _player.PlayerCombatState?.Hand;
        if (hand == null) return null;

        foreach (var card in hand.Cards)
        {
            if (card.Id.Entry == "BUG_PIG")
            {
                return card;
            }
        }
        return null;
    }

    public override INetAction ToNetAction()
    {
        return new NetBugPigAction(_targetNetId, _errorCount, _isUpgraded);
    }

    public override string ToString()
    {
        return $"BugPigAction player={_player.NetId} target={_targetNetId} errors={_errorCount} upgraded={_isUpgraded}";
    }
}

public struct NetBugPigAction : INetAction, IPacketSerializable
{
    private ulong _targetNetId;
    private int _errorCount;
    private bool _isUpgraded;

    public NetBugPigAction(ulong targetNetId, int errorCount, bool isUpgraded)
    {
        _targetNetId = targetNetId;
        _errorCount = errorCount;
        _isUpgraded = isUpgraded;
    }

    public GameAction ToGameAction(Player owner)
    {
        return new BugPigAction(owner, _targetNetId, _errorCount, _isUpgraded);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(_targetNetId);
        writer.WriteInt(_errorCount);
        writer.WriteBool(_isUpgraded);
    }

    public void Deserialize(PacketReader reader)
    {
        _targetNetId = reader.ReadULong();
        _errorCount = reader.ReadInt();
        _isUpgraded = reader.ReadBool();
    }
}
