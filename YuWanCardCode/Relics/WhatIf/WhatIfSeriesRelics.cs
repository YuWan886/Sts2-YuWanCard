using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using System.Security.Cryptography;
using System.Text;
using YuWanCard.RelicPools;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfSeriesRelics : WhatIfRelicModel, IWhatIfUniformRelicSource
{
    private static readonly Lazy<RelicModel[]> SevenSinRelics = new(() =>
    [
        ModelDb.Relic<ArrogantPig>(),
        ModelDb.Relic<JealousPig>(),
        ModelDb.Relic<FuriousPig>(),
        ModelDb.Relic<LazyPig>(),
        ModelDb.Relic<GreedyPig>(),
        ModelDb.Relic<GluttonousPig>(),
        ModelDb.Relic<LustfulPig>()
    ]);

    public WhatIfSeriesRelics() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        var relics = SevenSinRelics.Value;
        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                RelicModel? sinRelic = DeterministicRandomUtils.PickDeterministicRelic(relics, player.PlayerRng.Rewards);
                if (sinRelic != null)
                {
                    rewards[i] = new RelicReward(sinRelic.ToMutable(), player);
                }
            }
        }
        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        var relics = SevenSinRelics.Value;
        var seedKey = $"{runState.Rng.StringSeed}|{Id.Entry}";
        var seedBytes = Encoding.UTF8.GetBytes(seedKey);
        var hashBytes = SHA256.HashData(seedBytes);
        var index = (int)(BitConverter.ToUInt32(hashBytes, 0) % (uint)relics.Length);
        return relics[index];
    }
}
