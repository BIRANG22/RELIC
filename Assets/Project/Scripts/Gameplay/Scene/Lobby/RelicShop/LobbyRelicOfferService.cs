using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public readonly struct LobbyRelicOffer
{
    public LobbyRelicOffer(string relicId, int price)
    {
        RelicId = relicId;
        Price = price;
    }

    public string RelicId { get; }
    public int Price { get; }
}

public sealed class LobbyRelicOfferService
{
    private readonly ILobbyRelicShopRandom random;

    public LobbyRelicOfferService(ILobbyRelicShopRandom random)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public IReadOnlyList<LobbyRelicOffer> BuildOffers(
        IEnumerable<RelicData> relics,
        IEnumerable<string> ownedRelicIds,
        int maximumCount)
    {
        var owned = new HashSet<string>(StringComparer.Ordinal);
        if (ownedRelicIds != null)
        {
            foreach (string id in ownedRelicIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    owned.Add(id.Trim());
            }
        }

        var candidates = new List<LobbyRelicOffer>();
        var candidateIds = new HashSet<string>(StringComparer.Ordinal);

        if (relics != null)
        {
            foreach (RelicData relic in relics)
            {
                string id = relic?.FragmentId?.Trim();
                if (string.IsNullOrEmpty(id) || owned.Contains(id) || !candidateIds.Add(id))
                    continue;

                if (!ActiveRelicEffectResolver.IsActiveRelic(relic) ||
                    !LobbyRelicPricePolicy.TryGetPrice(relic.Rarity, out int price))
                {
                    continue;
                }

                candidates.Add(new LobbyRelicOffer(id, price));
            }
        }

        int offerCount = Math.Min(Math.Max(0, maximumCount), candidates.Count);
        var offers = new List<LobbyRelicOffer>(offerCount);

        for (int i = 0; i < offerCount; i++)
        {
            int index = random.NextIndex(candidates.Count);
            offers.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return offers;
    }
}
