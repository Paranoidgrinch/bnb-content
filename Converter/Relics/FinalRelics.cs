using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The final relic pools, from `source-data/design/BnB_Final_Relics_Master_PostAudit.md`.
//
// Four pools that never mix: Normal (Treasure, standard rewards, a shop's normal slots), Shop (shop stock
// and the three shop-like events), Event (one named branch each) and Boss (a forced 1-of-3 after a boss).
// The design is explicit that a Boss or Event relic must never turn up anywhere else.
public static class FinalRelics
{
    public static IReadOnlyList<BnbRelic> All() =>
    [
        .. NormalRelics.All(),
        .. ShopRelics.All(),
        .. EventRelics.All(),
        .. BossRelics.All(),
    ];

    public static IReadOnlyList<RelicData> Compile() => All().Select(r => r.Compile()).ToList();

    // The in-combat rules the relics install, as the statuses that carry them.
    public static IReadOnlyList<StatusData> Statuses() =>
        [.. RelicRules.All(), .. ShopRelicRules.All(), .. EventRelicRules.All(), .. ActTwoEventRelicRules.All(),
         .. BossRelicRules.All(), .. ActThreeEventRelicRules.All()];

    // What a given pool offers a given character. Character-specific relics are only eligible while that
    // character is played; everything else is open to everyone.
    public static IReadOnlyList<BnbRelic> Pool(Pool pool, Eligibility character = Eligibility.Bureaucrat) =>
        All().Where(r => r.Pool == pool && (r.Eligibility == Eligibility.General || r.Eligibility == character))
            .ToList();
}
