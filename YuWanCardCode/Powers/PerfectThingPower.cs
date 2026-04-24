using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Powers;

public class PerfectThingPower : YuWanPowerModel
{
    private class Data
    {
        public int CardsPlayedThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsPerEnergyVar(this)];

    [SavedProperty]
    public int YUWANCARD_CardsPerEnergy { get; set; } = 3;

    public void SetCardsPerEnergy(int value)
    {
        YUWANCARD_CardsPerEnergy = value;
        DynamicVars["CardsPerEnergy"].BaseValue = value;
    }

    private class CardsPerEnergyVar(PerfectThingPower? power = null) : DynamicVar("CardsPerEnergy", power?.YUWANCARD_CardsPerEnergy ?? 3m)
    {
        private PerfectThingPower? _power = power;

        public override void SetOwner(AbstractModel owner)
        {
            base.SetOwner(owner);
            _power = owner as PerfectThingPower;
            if (_power != null)
            {
                BaseValue = _power.YUWANCARD_CardsPerEnergy;
            }
        }
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            GetInternalData<Data>().CardsPlayedThisTurn = 0;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        var data = GetInternalData<Data>();
        data.CardsPlayedThisTurn++;

        if (data.CardsPlayedThisTurn % YUWANCARD_CardsPerEnergy == 0)
        {
            Flash();
            await PlayerCmd.GainEnergy(Amount, Owner.Player);
        }
    }
}
