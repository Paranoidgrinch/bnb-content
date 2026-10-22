using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Lieutenant.Statuses;
using D = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.DamageReceivedTriggeredEffectContext>;
using TS = BnbContent.Converter.Lieutenant.N<RogueDeck.Core.Combat.TurnStartedTriggeredEffectContext>;

namespace BnbContent.Converter.Lieutenant;

// The Lieutenant's own words (lieutenant_master.md §2): Authority, the bounded reserve her kept promises earn, and
// Open Guard, the exposure she takes on purpose. Strength is the game's own status; what is hers about it is that
// she may SPEND it (CardShapes.SpendStrength). Numbers are the canon's BALANCE DRAFT.
public static class LieutenantKeywords
{
    public const string Authority = "lt_authority";
    public const string OpenGuard = "lt_open_guard";
    public const string Strength = "strength";

    // Her standing status, worn every fight: the enemy phase she has just come through is counted here, and the
    // Orders and Challenges she issues are kept in their places (Obligations).
    public const string Command = "lt_command";

    public const int AuthorityCap = 6;
    public const int OpenGuardPerStack = 2;

    // ── counters on her ──────────────────────────────────────────────────────────────────────────────────

    // The enemy phase so far: how many enemy hits struck her and how much HP they took. Reset when her turn ends,
    // so during her own turn they describe the phase just past.
    public const string PhaseHits = "lt_phase_hits";
    public const string PhaseHp = "lt_phase_hp";

    // What Open Guard did to the hit that spent it: the HP that would not have been lost without it (§2.5),
    // for this hit and for the whole fight.
    public const string GuardExtra = "lt_guard_extra";
    public const string GuardExtraTotal = "lt_guard_extra_total";

    // The round in which she last really paid positive Strength for a card (Whetstone), and how much.
    public const string StrengthPaidRound = "lt_strength_paid_round";
    public const string StrengthPaidLast = "lt_strength_paid_last";

    // ── announcements (L11) ──────────────────────────────────────────────────────────────────────────────

    public const string GuardSpent = "lt_open_guard_spent";       // an enemy hit spent her Open Guard
    public const string GuardHurt = "lt_open_guard_hurt";         // …and it cost her HP she would have kept
    public const string StrengthPaid = "lt_strength_paid";        // a card took positive Strength from her
    public const string AuthorityPaid = "lt_authority_paid";      // a card took positive Authority from her

    // A card program cannot announce, so a payment leaves one of these on her, its stacks the amount; her
    // standing status writes it down, announces it and takes the pulse away again (Obligations).
    public const string StrengthPulse = "lt_pulse_strength";
    public const string AuthorityPulse = "lt_pulse_authority";
    public const string AuthorityPaidLast = "lt_authority_paid_last";

    public static IReadOnlyList<StatusData> All() =>
    [
        Status(Authority, "Authority", StatusPolarity.Buff,
            $"Earned by fulfilling Orders and Challenges; cards spend it. At most {AuthorityCap}. It does not fade, and " +
            "it is gone when the fight ends."),
        OpenGuardStatus(),
        Status(StrengthPulse, "Strength spent", StatusPolarity.Neutral, "You have just spent this much Strength."),
        Status(AuthorityPulse, "Authority spent", StatusPolarity.Neutral, "You have just spent this much Authority."),
        .. Obligations.All(),
        .. LieutenantRules.All(),
    ];

    // +2 a stack to the first enemy Attack hit of an enemy action, before Block (once per action is the engine's
    // own claim); that hit spends all of it. Untouched, it lapses when her next turn begins. Not a Debuff: no
    // cleanse lifts it and nothing that counts negative statuses sees it (§2.5).
    private static StatusData OpenGuardStatus()
    {
        var she = D.Event;
        var spend = D.Seq(
            D.Set(she, GuardExtra,
                D.Min(
                    D.Amount,
                    D.Times(
                        D.Stacks(she, OpenGuard), OpenGuardPerStack))),
            D.Add(she, GuardExtraTotal,
                D.Counter(she, GuardExtra)),
            D.Remove(she, OpenGuard),
            D.Announce(she, GuardSpent),
            D.If(
                D.AtLeast(D.Counter(she, GuardExtra), 1),
                D.Announce(she, GuardHurt)));

        return Status(OpenGuard, "Open Guard", StatusPolarity.Neutral,
            $"The next enemy Attack hit on you deals {OpenGuardPerStack} more damage per stack, before Block, and " +
            "spends all of it. Lapses at the start of your turn.",
            triggers:
            [
                On(TriggerEvent.DamageTaken, D.If(ByAnEnemy<DamageReceivedTriggeredEffectContext>(), spend)),
                On(TriggerEvent.TurnStarted, TS.Remove(TS.Self, OpenGuard)),
            ],
            passives:
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.AddPerStack,
                    OpenGuardPerStack, OncePerAction: true),
            ]);
    }

    // Inside a damage trigger worn by her: the damage came from the other side. The event's source is the
    // striker, and she is one of the striker's enemies exactly when the striker is one of hers.
    internal static ICombatExpression<T, bool> ByAnEnemy<T>() where T : class =>
        N<T>.AtLeast(N<T>.Count(CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllEnemiesOfSource, new StatusDefinitionId(Command))), 1);

    // The Lieutenant, from anywhere: whoever wears her standing status.
    // One body, as the scalar reads need: in a fight with two Lieutenants the first of them (L14).
    internal static ICombatantTargetSelector Her => BearerOf(Command);

    // Whoever wears this status — one body; `fallen` finds it on a body that has just been downed.
    internal static ICombatantTargetSelector BearerOf(string status, bool fallen = false) =>
        CombatantTargetSelectors.FirstTarget(CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(status), includeFallen: fallen));

    // ── Authority, from any rule ─────────────────────────────────────────────────────────────────────────

    // Gain up to the cap; what would pass it is lost (§2.2).
    internal static IEffectNode<T> GainAuthority<T>(ICombatantTargetSelector who, int amount) where T : class =>
        N<T>.If(N<T>.Cmp(N<T>.Stacks(who, Authority), ComparisonOperator.Less, N<T>.C(AuthorityCap)),
            N<T>.Apply(who, Authority, N<T>.Min(N<T>.C(amount), N<T>.Minus(N<T>.C(AuthorityCap), N<T>.Stacks(who, Authority)))));
}
