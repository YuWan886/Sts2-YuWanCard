using BaseLib.Utils;
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
using YuWanCard.Cards;
using YuWanCard.Utils;

namespace YuWanCard.GameActions;

public class BugPigAction : GameAction
{
    private readonly Player _player;
    private readonly int _targetIndex;
    private readonly int _totalDamage;

    public override ulong OwnerId => _player.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    public BugPigAction(Player player, int targetIndex, int totalDamage)
    {
        _player = player;
        _targetIndex = targetIndex;
        _totalDamage = totalDamage;
    }

    protected override async Task ExecuteAction()
    {
        var target = GetTargetCreature();
        if (target == null)
        {
            MainFile.Logger.Warn($"BugPigAction: Target not found (index={_targetIndex})");
            return;
        }

        var card = GetCardFromPiles();
        if (card == null)
        {
            card = RunManager.Instance?.DebugOnlyGetState()?.CreateCard(ModelDb.Card<BugPig>(), _player);
            if (card == null)
            {
                MainFile.Logger.Warn($"BugPigAction: Failed to create card instance");
                return;
            }
        }

        MainFile.Logger.Info($"BugPigAction: Dealing {_totalDamage} damage");

        await DamageCmd.Attack(_totalDamage)
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
        if (_targetIndex < 0) return null;
        
        var combatState = _player.Creature?.CombatState;
        if (combatState == null) return null;

        var creatures = combatState.Creatures.ToList();
        if (_targetIndex >= creatures.Count) return null;

        return creatures[_targetIndex];
    }

    private CardModel? GetCardFromPiles()
    {
        var combatState = _player.PlayerCombatState;
        if (combatState == null) return null;

        if (combatState.Hand != null)
        {
            foreach (var card in combatState.Hand.Cards)
            {
                if (card.Id.Entry == "BUG_PIG")
                {
                    return card;
                }
            }
        }

        if (combatState.ExhaustPile != null)
        {
            foreach (var card in combatState.ExhaustPile.Cards)
            {
                if (card.Id.Entry == "BUG_PIG")
                {
                    return card;
                }
            }
        }

        if (combatState.DiscardPile != null)
        {
            foreach (var card in combatState.DiscardPile.Cards)
            {
                if (card.Id.Entry == "BUG_PIG")
                {
                    return card;
                }
            }
        }

        return null;
    }

    public override INetAction ToNetAction()
    {
        return new NetBugPigAction(_targetIndex, _totalDamage);
    }

    public override string ToString()
    {
        return $"BugPigAction player={_player.NetId} targetIndex={_targetIndex} damage={_totalDamage}";
    }
}

public struct NetBugPigAction : INetAction, IPacketSerializable
{
    private int _targetIndex;
    private int _totalDamage;

    public NetBugPigAction(int targetIndex, int totalDamage)
    {
        _targetIndex = targetIndex;
        _totalDamage = totalDamage;
    }

    public GameAction ToGameAction(Player owner)
    {
        return new BugPigAction(owner, _targetIndex, _totalDamage);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(_targetIndex);
        writer.WriteInt(_totalDamage);
    }

    public void Deserialize(PacketReader reader)
    {
        _targetIndex = reader.ReadInt();
        _totalDamage = reader.ReadInt();
    }
}
