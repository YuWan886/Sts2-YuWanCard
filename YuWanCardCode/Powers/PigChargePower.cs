using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Cards;

namespace YuWanCard.Powers;

public class PigChargePower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<PigCharge>();
}
