using RogueDeck.Core.Combat;

namespace BnbContent.Converter;

// A few elite intents reach past the flat effect DSL: they touch a SPECIFIC other body (the Moth Cloud feeding
// the Waiting Room's Lost Time), which needs a parameterised selector the curated CombatNodeModel has no key
// for. Those intents are authored here as RAW EffectPrograms — the same escape EncounterPassives uses for
// cross-combatant reactions — and EnemyMapper prefers this program when one exists for "<enemy>.<intent>".
public static class RawIntentPrograms
{

    private static EffectProgram<EnemyActionContext> Misfiling(int damage, string mark, int count = 1) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                CombatantTargetSelectors.LowestHealthEnemyOfSource,
                new ConstantExpression<EnemyActionContext>(damage)),
            ActTwo.MisfileOne(mark, count),
        ]));

    public static EffectProgram<EnemyActionContext>? For(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            // Act II: an attack that also misfiles. Which SHELF did it decides where the card goes when it is
            // taken back, so the destination is written into the mark itself.
            "crabwise_shelf.mis_shelve" => Misfiling(11, ActTwo.MisfiledSidewaysMark, count: 2),
            "lower_appellate_step.await_the_ruling" => AwaitTheRuling(),
            "middle_appellate_step.await_the_ruling" => AwaitTheRuling(),
            "upper_appellate_step.await_the_ruling" => AwaitTheRuling(),
            // Act III: every pressure intent files its Trespass through the act's one filing point, so the
            // Contrary Magpie can contest an intent's violation the same way it contests a Local Law's.
            _ when ActThree.Intent(enemyId, intentId) is { } greenDocket => greenDocket,
            // Act IV: the measure is raised, and its result answered, through the act's own vocabulary.
            _ when ActFour.Intent(enemyId, intentId) is { } labyrinth => labyrinth,
            // Act V: a god's whole fight is its own rule, so its intents are raw programs from the first.
            _ when ActFive.Intent(enemyId, intentId) is { } god => god,
            _ => enemyId switch
            {
                "deputy_undersecretary" => DeputyUndersecretary.Intent(intentId),
                "queue_commissioner" => QueueCommissioner.Intent(intentId),
                "municipal_dragon" => MunicipalDragon.Intent(intentId),
                "living_charter" => LivingCharter.Intent(intentId),
                Elites.WaitingRoom.EnemyId => Elites.WaitingRoom.Intent(intentId),
                Elites.ReturnBell.EnemyId => Elites.ReturnBell.Intent(intentId),
                Elites.RollingStacksColossus.EnemyId => Elites.RollingStacksColossus.Intent(intentId),
                Elites.CatalogueOfUnwiseNames.EnemyId => Elites.CatalogueOfUnwiseNames.Intent(intentId),
                Elites.BlackInkOracle.EnemyId => Elites.BlackInkOracle.Intent(intentId),
                Elites.DrawerOfInfiniteReturns.EnemyId => Elites.DrawerOfInfiniteReturns.Intent(intentId),
                Elites.ObituaryWithThreeEndings.EnemyId => Elites.ObituaryWithThreeEndings.Intent(intentId),
                Bosses.WhisperingCatalogue.EnemyId => Bosses.WhisperingCatalogue.Intent(intentId),
                Bosses.WardenOfSealedVolumes.EnemyId => Bosses.WardenOfSealedVolumes.Intent(intentId),
                Bosses.CuratorOfMisplacedHours.EnemyId => Bosses.CuratorOfMisplacedHours.Intent(intentId),
                Bosses.AuditorOfReturnedLives.EnemyId => Bosses.AuditorOfReturnedLives.Intent(intentId),
                Bosses.GrandCrossReference.PremiseId
                    or Bosses.GrandCrossReference.AuthorityId
                    or Bosses.GrandCrossReference.ConclusionId =>
                    Bosses.GrandCrossReference.Intent(enemyId, intentId),
                Elites.VolumesOfCauseAndConsequence.CausesId
                    or Elites.VolumesOfCauseAndConsequence.ConsequencesId =>
                    Elites.VolumesOfCauseAndConsequence.Intent(enemyId, intentId),
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

}
