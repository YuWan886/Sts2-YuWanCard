using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Core.Abstracts;
using YuWanCard.Relics;

namespace YuWanCard.Powers;

public class ThousandCurseScrollDexterityPower : YuWanTemporaryPowerModelWrapper<ThousandCurseScroll, DexterityPower>
{
    public override LocString Title => new("relics", $"{OriginModel.Id.Entry}.title");
}
