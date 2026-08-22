using RogueDeck.Core.Combat;

namespace BnbContent.Converter;

// A few elite intents reach past the flat effect DSL: they touch a SPECIFIC other body (the Moth Cloud feeding
// the Waiting Room's Lost Time), which needs a parameterised selector the curated CombatNodeModel has no key
// for. Those intents are authored here as RAW EffectPrograms — the same escape EncounterPassives uses for
// cross-combatant reactions — and EnemyMapper prefers this program when one exists for "<enemy>.<intent>".
public static class RawIntentPrograms
{
    private static EffectProgram<EnemyActionContext> Redacting(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                CombatantTargetSelectors.LowestHealthEnemyOfSource,
                new ConstantExpression<EnemyActionContext>(damage)),
            ActTwo.RedactOne(),
        ]));

    private static EffectProgram<EnemyActionContext> Misfiling(int damage, string mark) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                CombatantTargetSelectors.LowestHealthEnemyOfSource,
                new ConstantExpression<EnemyActionContext>(damage)),
            ActTwo.MisfileOne(mark),
        ]));

    public static EffectProgram<EnemyActionContext>? For(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            // Act II: an attack that also misfiles. Which SHELF did it decides where the card goes when it is
            // taken back, so the destination is written into the mark itself.
            // Act II's redacting attacks. The halving itself is the engine's; what the intent adds is the
            // mark beside it, which is what an enemy rule can see.
            "palimpsest_husk.scrape_the_surface" => Redacting(13),
            "expunged_name.strike_the_name" => Redacting(14),
            "vacant_portrait.erase_the_face" => Redacting(13),
            "errata_doppelganger.errata_transfer" => Redacting(14),
            "miscellany_index.cross_list" => Redacting(12),
            "crabwise_shelf.mis_shelve" => Misfiling(11, ActTwo.MisfiledSidewaysMark),
            "volume_q_null.null_index" => Misfiling(10, ActTwo.MisfiledMark),
            "minute_moth_cloud.steal_a_minute" => StealAMinute(),
            "living_petition_chorus.read_into_the_record" => ReadIntoTheRecord(),
            "escalation_writ.elevate_the_case" => ElevateTheCase(),
            "lower_appellate_step.await_the_ruling" => AwaitTheRuling(),
            "middle_appellate_step.await_the_ruling" => AwaitTheRuling(),
            "upper_appellate_step.await_the_ruling" => AwaitTheRuling(),
            _ => enemyId switch
            {
                "deputy_undersecretary" => DeputyUndersecretary.Intent(intentId),
                "queue_commissioner" => QueueCommissioner.Intent(intentId),
                "lord_sealkeeper" => LordSealkeeper.Intent(intentId),
                "municipal_dragon" => MunicipalDragon.Intent(intentId),
                "living_charter" => LivingCharter.Intent(intentId),
                Elites.ReturnBell.EnemyId => Elites.ReturnBell.Intent(intentId),
                Elites.RollingStacksColossus.EnemyId => Elites.RollingStacksColossus.Intent(intentId),
                Elites.CatalogueOfUnwiseNames.EnemyId => Elites.CatalogueOfUnwiseNames.Intent(intentId),
                Elites.SilenceBetweenTwoWords.EnemyId => Elites.SilenceBetweenTwoWords.Intent(intentId),
                Elites.BlackInkOracle.EnemyId => Elites.BlackInkOracle.Intent(intentId),
                _ => null,
            },
        };

    // "Await the Ruling": the other Steps stop attacking and guard whoever holds the Case.
    private static EffectProgram<EnemyActionContext> AwaitTheRuling()
    {
        var holder = CombatantTargetSelectors.AllAlliesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.HoldsTheCaseId));

        return new EffectProgram<EnemyActionContext>(
            new ForEachTargetEffectNode<EnemyActionContext>(holder,
                new GainBlockNode<EnemyActionContext>(
                    CombatantTargetSelectors.IterationTarget, new ConstantExpression<EnemyActionContext>(4))));
    }

    // "Elevate the Case": the Writ makes its Phantom stronger, found through the Phantom's marker.
    private static EffectProgram<EnemyActionContext> ElevateTheCase()
    {
        var phantom = CombatantTargetSelectors.AllAlliesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.PhantomId));

        return new EffectProgram<EnemyActionContext>(
            new ForEachTargetEffectNode<EnemyActionContext>(phantom,
                new ApplyStatusNode<EnemyActionContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId("strength"),
                    new ConstantExpression<EnemyActionContext>(1))));
    }

    // "Read Into the Record": 8 damage, then every liability the player signed for, in the order the clauses
    // are listed, and the petition starts a fresh reading cycle.
    private static EffectProgram<EnemyActionContext> ReadIntoTheRecord()
    {
        var self = CombatantTargetSelectors.Source;
        var player = CombatantTargetSelectors.EventTarget;

        IEffectNode<EnemyActionContext> Liability(CounterId flag, params IEffectNode<EnemyActionContext>[] effects) =>
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantCounterExpression<EnemyActionContext>(self, flag),
                    ComparisonOperator.Greater,
                    new ConstantExpression<EnemyActionContext>(0)),
                new SequenceEffectNode<EnemyActionContext>(effects));

        IEffectNode<EnemyActionContext> Apply(string status, int stacks) =>
            new ApplyStatusNode<EnemyActionContext>(player, new StatusDefinitionId(status),
                new ConstantExpression<EnemyActionContext>(stacks));

        IEffectNode<EnemyActionContext> Clear(CounterId counter) =>
            new SetCombatantCounterNode<EnemyActionContext>(self, counter,
                new ConstantExpression<EnemyActionContext>(0), relative: false);

        var clauses = ClauseCards.All;
        return new EffectProgram<EnemyActionContext>(
            new SequenceEffectNode<EnemyActionContext>(new IEffectNode<EnemyActionContext>[]
            {
                new DealDamageNode<EnemyActionContext>(player, new ConstantExpression<EnemyActionContext>(8)),
                Liability(clauses[0].Liability, Apply("fatigue", 1)),
                Liability(clauses[1].Liability, Apply("paperwork", 2)),
                Liability(clauses[2].Liability, Apply("doubt", 1), Apply("paperwork", 1)),
                Clear(clauses[0].Liability),
                Clear(clauses[1].Liability),
                Clear(clauses[2].Liability),
                Clear(PassiveStatuses.SignaturesCounter),
                Clear(PassiveStatuses.ClauseIndexCounter),
            }));
    }

    // "Gain 1 Lost Time for the Waiting Room": the Moth writes the track its partner keeps, capped at 3, found
    // through the marker the Room carries — so killing the Room really does erase the resource with it.
    private static EffectProgram<EnemyActionContext> StealAMinute()
    {
        var room = CombatantTargetSelectors.AllAlliesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.LostTimeLedgerId));
        var ledger = CombatantTargetSelectors.IterationTarget;

        return new EffectProgram<EnemyActionContext>(
            new ForEachTargetEffectNode<EnemyActionContext>(room,
                new SetCombatantCounterNode<EnemyActionContext>(ledger, PassiveStatuses.LostTimeCounter,
                    new MinExpression<EnemyActionContext>(
                        new AddExpression<EnemyActionContext>(
                            new CombatantCounterExpression<EnemyActionContext>(ledger, PassiveStatuses.LostTimeCounter),
                            new ConstantExpression<EnemyActionContext>(1)),
                        new ConstantExpression<EnemyActionContext>(PassiveStatuses.LostTimeMaximum)),
                    relative: false)));
    }
}
