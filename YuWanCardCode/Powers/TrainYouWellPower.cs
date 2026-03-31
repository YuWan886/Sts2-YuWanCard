using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Cards;

namespace YuWanCard.Powers;

public class TrainYouWellPower : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<TiaoJiao>();
}
