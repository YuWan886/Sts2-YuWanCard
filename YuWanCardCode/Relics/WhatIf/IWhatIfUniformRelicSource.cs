using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Relics;

public interface IWhatIfUniformRelicSource
{
    RelicModel GetUniformRelic(IRunState runState);
}
