using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Registration;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanPotionModel : PotionModel, IYuWanContent
{
    public virtual string? CustomPackedImagePath => null;
    public virtual string? CustomPackedOutlinePath => null;

    protected YuWanPotionModel()
    {
        ContentRegistry.AddModel(GetType());
    }
}
