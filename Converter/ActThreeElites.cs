using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III — the elite layer. Nine encounters that add NO fifth Act-wide mechanic: every one of them is
// Safe-Conduct, Trespass, Claim and Wergild read a different way.
//
//   permission → custom → crossing → restitution → formation →
//   injunction → obsolete right → appeal → judgment
//
// The standards showed that law is remembered socially. The elites show that remembered law can be
// negotiated, cut, appealed, enjoined and weaponised — and each one owns exactly one local resource that
// lives and dies inside its own encounter (Sanction, a Thread, Return Standing, Toll, the formation, an
// Injunction, an Old Right, the tribunal's order, a Binding Judgment).
//
// They are `partial class ActThree` files like the stages, because they speak the act's vocabulary and file
// their violations through the act's one filing point. What is shared between them lives here.
public static partial class ActThree
{
    // The nine. An elite is a Green Docket body like any other — the act's customs open on it — but it is
    // never a standard identity, so the pool tests count the two rosters apart.
    public static readonly IReadOnlySet<string> EliteIdentities = new HashSet<string>(StringComparer.Ordinal)
    {
        StagEnemyId,
        GrandmotherWebEnemyId,
    };

    // Every elite's own statuses, gathered where the act's own list can splice them in.
    public static IReadOnlyList<StatusData> EliteStatuses() =>
    [
        .. StagStatuses(),
        .. WebStatuses(),
    ];

    // An elite's intents. Dispatched ahead of the standard pool's, because an elite's pressure is never the
    // standard `Pressure(n)` shape — each of them charges for something of its own.
    public static EffectProgram<EnemyActionContext>? EliteIntent(string enemyId, string intentId) =>
        StagIntent(enemyId, intentId)
        ?? WebIntent(enemyId, intentId);

    // What settling a demand IN FULL does over and above the act's own reward, when the creditor is an elite
    // that has written its own terms. Spliced into the one settlement in `ActThreeWergild`, because only the
    // moment a demand is settled knows that it was settled.
    public static IEffectNode<TurnEndedTriggeredEffectContext> EliteSettlement() =>
        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            ACleanFight(),
        ]);

    // ── the laws the elites add ───────────────────────────────────────────────────────────────────────────
    //
    // Numbered on from the standard pool's, because a law is a number written onto the player as its
    // violation is filed and no two laws may share one.
    public const int PreApprovedViolenceLaw = 12;

    // The suggested ceiling on the player's licences. Only the parties that GRANT Safe-Conduct on a schedule
    // read it; a licence handed over as a reward is never refused.
    public const int SafeConductCeiling = 3;

    // "The party whose local resource this is" — the same address as `Lawgiver`, said of an elite's own
    // marker rather than of a law. Kept separate only so the elite files read as what they mean.
    private static ICombatantTargetSelector Elite(string markerId) => Lawgiver(markerId);
}
