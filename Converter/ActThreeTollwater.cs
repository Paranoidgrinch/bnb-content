using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 3 — Tollwater. Since 2026-11-03 (user): a snail that charges for a busy turn, a fish that leaves
// you be once you have kept its law three turns running, and a ford that banks every right it is granted.
public static partial class ActThree
{
    public const string PaymentAccordingToCharterId = "payment_according_to_charter";
    public const string OathAcceptedId = "oath_accepted";
    public const string FishNoDeedsId = "fish_law_no_deeds";
    public const string FishLowGuardId = "fish_law_low_guard";
    public const string OathKeptId = "oath_kept";
    public const string TollOnBothBanksId = "toll_on_both_banks";

    public const int SnailCardLimit = 3;
    public const int OathTurnsToLeave = 3;
    public const int FishGuardLimit = 10;
    public const int FordBlockPerClaim = 6;

    // ── Charter-Shell Snail ───────────────────────────────────────────────────────────────────────────────
    public static StatusData PaymentAccordingToCharter() =>
        LawOnPlay(PaymentAccordingToCharterId, "A Toll on Haste",
            "Playing a third card in a turn breaks its law.",
            Is(PlayedThisTurn, ComparisonOperator.GreaterOrEqual, SnailCardLimit));

    // ── Streamside Oath-Fish ──────────────────────────────────────────────────────────────────────────────
    //
    // Its law follows what it is about to do: while it means to guard, no Deeds; while it means to strike, keep your
    // Block to 10 or less. Each intent sets the law for the player's next turn (the two alternate, starting with
    // the guard). Keep its law three turns running and it leaves in peace, counted as defeated.
    public static StatusData OathAccepted()
    {
        var fish = Lawgiver(OathAcceptedId);
        var player = CombatantTargetSelectors.Source;

        var deed = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    Alive<CardPlayedTriggeredEffectContext>(fish),
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        Wearing<CardPlayedTriggeredEffectContext>(fish, FishNoDeedsId),
                        PlayedIs(Cards.CardAuthoring.DeedTag))),
                Breach<CardPlayedTriggeredEffectContext>(fish)));

        var reckon = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(), Alive<TurnEndedTriggeredEffectContext>(fish)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Wearing<TurnEndedTriggeredEffectContext>(fish, FishLowGuardId),
                        Is(new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
                            player, StandardCombatIds.BlockDefensivePool), ComparisonOperator.Greater, FishGuardLimit)),
                    Breach<TurnEndedTriggeredEffectContext>(fish)),
                // The oath: a turn kept is a mark on the fish, a turn broken wipes them.
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Wearing<TurnEndedTriggeredEffectContext>(fish, LawBrokenThisTurnId),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(fish, new StatusDefinitionId(OathKeptId)),
                    @else: new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            fish, new StatusDefinitionId(OathKeptId), new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            Is(new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(fish, new StatusDefinitionId(OathKeptId)),
                                ComparisonOperator.GreaterOrEqual, OathTurnsToLeave),
                            new SetHealthNode<TurnEndedTriggeredEffectContext>(fish, new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                    ])),
            ]));

        return Rule(OathAcceptedId, "The Oath of the Stream",
            "While it guards: no Deeds. While it strikes: no more than 10 Block. Keep its law 3 turns running and it leaves, defeated.",
            [
                new StatusTriggerData("CardPlayed", System.Text.Json.JsonSerializer.SerializeToElement(
                    deed, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()), StatusTriggerScope.Anywhere),
                On("TurnEnded", reckon, StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData FishNoDeeds() =>
        Marker(FishNoDeedsId, "Law: No Deeds", "It is about to guard. Playing a Deed this turn breaks its law.");

    public static StatusData FishLowGuard() =>
        Marker(FishLowGuardId, "Law: Low Guard", "It is about to strike. Ending this turn on more than 10 Block breaks its law.");

    public static StatusData OathKept() => new()
    {
        Id = OathKeptId,
        NameKey = "Oath Kept",
        DescriptionKey = "Turns in a row you kept its law. At 3 it leaves in peace, counted as defeated.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // Its two intents: the act, and the law for your next turn (the other one's).
    private static EffectProgram<EnemyActionContext> FishActs(IEffectNode<EnemyActionContext> act, string nextLaw, string lastLaw)
    {
        var self = CombatantTargetSelectors.Source;
        return new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            act,
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(lastLaw)),
            new ApplyStatusNode<EnemyActionContext>(self, new StatusDefinitionId(nextLaw), new ConstantExpression<EnemyActionContext>(1)),
        ]));
    }

    // ── Two-Bank Toll Ford ────────────────────────────────────────────────────────────────────────────────
    //
    // Every Claim granted the Ford is banked behind its walls: 6 Block.
    public static StatusData TollOnBothBanks()
    {
        IEffectNode<TContext> Bank<TContext>() where TContext : class =>
            new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                new GainBlockNode<TContext>(CombatantTargetSelectors.EventTarget, new ConstantExpression<TContext>(FordBlockPerClaim)));

        return Rule(TollOnBothBanksId, "Toll on Both Banks", "Every Claim it gains also gives it 6 Block.",
            [On("StatusApplied", Bank<StatusAppliedTriggeredEffectContext>()), On("StatusMerged", Bank<StatusMergedTriggeredEffectContext>())]);
    }

    // The Act-III normal bodies whose intents are their own programs (the pressure intents are in ActThree.Intent).
    private static EffectProgram<EnemyActionContext>? NormalIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "streamside_oath_fish.river_shelter" => FishActs(
                new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, new ConstantExpression<EnemyActionContext>(12)),
                nextLaw: FishLowGuardId, lastLaw: FishNoDeedsId),
            "streamside_oath_fish.current_strike" => FishActs(
                new DealDamageNode<EnemyActionContext>(Applicant, new ConstantExpression<EnemyActionContext>(12)),
                nextLaw: FishNoDeedsId, lastLaw: FishLowGuardId),
            "cairn_of_stray_paths.loose_stone" => Stonefall(),
            _ => null,
        };
}
