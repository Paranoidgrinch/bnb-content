using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The in-combat halves of the Shop relics. All four are the same shape and it is worth naming: the relic pays
// out AFTER the fight, in Gold or in something banked, but what it pays for happens INSIDE the fight — cards
// Archived, enemies Ratified, enemies buried under Paperwork. A keyword tally is a status whose stacks only
// grow, and the run cannot read a status; it can read a COUNTER the fight left behind (`event.combatCounter`).
// So each of these rules is a mirror: it watches the tally and writes the number where the run will find it.
public static class ShopRelicRules
{
    public static CounterId Salvage => new("salvage");
    public static CounterId ArchiveTally => new("archive_tally");
    public static CounterId RatifyTally => new("ratify_tally");
    public static CounterId FilingFee => new("filing_fee");

    public static IReadOnlyList<StatusData> All() =>
        [WastebrokersPermit, ArchiveVoucherRoll, NotarysWaiver, FilingFeeStamp];

    // "Whenever you Archive a Junk card, record 1 Salvage, max 3 per combat." The Archive mark does not say
    // WHAT was archived, so every Archive counts — see ADAPTATIONS.
    public static readonly StatusData WastebrokersPermit =
        MirrorsOwnTally("wastebrokers_permit_rule", "Wastebroker's Permit",
            "Every card you Archive is worth something to a scrap dealer.", Keywords.Archived, Salvage, cap: 3);

    // "After winning a combat in which you Archived at least 2 cards…" — the same tally, uncapped, because the
    // relic asks a question about the whole fight rather than paying per card.
    public static readonly StatusData ArchiveVoucherRoll =
        MirrorsOwnTally("archive_voucher_roll_rule", "Archive Voucher Roll",
            "The archive counts what you filed away.", Keywords.Archived, ArchiveTally);

    // "Whenever you Ratify, gain 1 Waiver." Ratified lands on the ENEMY, so this rule cannot be bearer-scoped:
    // it watches the whole fight and writes on whoever is wearing it.
    public static readonly StatusData NotarysWaiver =
        CountsWhatLandsOnOthers("notarys_waiver_rule", "Notary's Waiver",
            "Every ratification is a favour owed to you.", Keywords.Ratified, RatifyTally);

    // "At combat end, each enemy that died with 5+ Paperwork grants 6 Gold; 10+ grants 4 more." Read as the
    // enemy goes down, because a moment later its Paperwork is gone with it. The per-combat maximum is applied
    // when the run collects, not here — capping a running total would silently lose the last enemy's share.
    public static readonly StatusData FilingFeeStamp = Rule(
        "filing_fee_stamp_rule", "Filing-Fee Stamp",
        "A fee is due on every heavily papered corpse.",
        [
            Trigger(new EffectProgram<CombatantDownedTriggeredEffectContext>(
                    OnEveryWearer<CombatantDownedTriggeredEffectContext>(
                        new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                            Papered<CombatantDownedTriggeredEffectContext>(5),
                            new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                            [
                                Award<CombatantDownedTriggeredEffectContext>(6),
                                new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                                    Papered<CombatantDownedTriggeredEffectContext>(10),
                                    Award<CombatantDownedTriggeredEffectContext>(4)),
                            ])),
                        "filing_fee_stamp_rule")),
                nameof(TriggerEvent.Downed), StatusTriggerScope.Anywhere),
        ]);

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // A tally that grows on the WEARER — Archive is marked on the archivist — copied into a counter, optionally
    // capped. Written as "set to the tally" rather than "add one", so a rule installed mid-fight catches up and
    // a doubled trigger cannot double-count.
    private static StatusData MirrorsOwnTally(
        string id, string name, string description, string tally, CounterId counter, int? cap = null)
    {
        IEffectNode<TContext> Body<TContext>() where TContext : class
        {
            ICombatExpression<TContext, int> stacks = new CombatantStatusStacksExpression<TContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(tally));
            if (cap is { } limit)
                stacks = new MinExpression<TContext>(stacks, new ConstantExpression<TContext>(limit));

            return new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(tally)),
                new SetCombatantCounterNode<TContext>(
                    CombatantTargetSelectors.Source, counter, stacks, relative: false));
        }

        return Rule(id, name, description,
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                    Body<StatusAppliedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusApplied)),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                    Body<StatusMergedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusMerged)),
        ]);
    }

    // A status landing on SOMEONE ELSE, counted by whoever wears the rule. Ratifying is something you do to an
    // enemy, so the rule has to watch the fight rather than its own bearer.
    private static StatusData CountsWhatLandsOnOthers(
        string id, string name, string description, string status, CounterId counter)
    {
        IEffectNode<TContext> Body<TContext>() where TContext : class =>
            new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(status)),
                OnEveryWearer<TContext>(
                    new SetCombatantCounterNode<TContext>(
                        CombatantTargetSelectors.IterationTarget, counter,
                        new ConstantExpression<TContext>(1), relative: true),
                    id));

        return Rule(id, name, description,
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                    Body<StatusAppliedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                    Body<StatusMergedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
        ]);
    }

    // An Anywhere-scoped rule fires once for the whole fight, not once per wearer, so the body has to find the
    // wearer itself — and everything it writes must be written on THAT combatant, never on `source`, which
    // means something different in every event.
    private static IEffectNode<TContext> OnEveryWearer<TContext>(IEffectNode<TContext> body, string id)
        where TContext : class =>
        new ForEachTargetEffectNode<TContext>(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(id)),
            body);

    private static ICombatExpression<TContext, bool> Papered<TContext>(int at) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(
                CombatantTargetSelectors.EventTarget, new StatusDefinitionId(Keywords.Paperwork)),
            ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(at));

    private static IEffectNode<TContext> Award<TContext>(int gold) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            CombatantTargetSelectors.IterationTarget, FilingFee,
            new ConstantExpression<TContext>(gold), relative: true);

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
