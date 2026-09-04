using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;
using Xunit.Abstractions;

namespace BnbContent.Tests;

// How long a boss TAKES, not what it does — the mechanics are pinned one file per boss next door.
//
// The Warden of Sealed Volumes is why this file exists: he was carried for a week as "does not end within 100
// turns", which is the one kind of fault a mechanics test cannot see. He turned out to be fine — the walk that
// accused him was dying of something else — and the boss that really does not end was the Grandmother Clause,
// two acts further on, which nothing had drawn yet. That is the argument for a net rather than a probe: a
// walk only ever finds the boss its seed happens to pick, and every boss here is one seed away from being it.
//
// The tester is the walker's: greedy, unkillable, and carrying the same three guards — never repeat a play the
// engine refused, never repeat a play that moved nothing on the table, and cap the turn. It brings the
// character's OWN starting deck, which is deliberately the weakest hand a player can arrive with: a boss that
// falls to that will fall to anything, and the number this pins is the fight's ceiling, not its balance.
public class BossLengthTests(ITestOutputHelper output)
{
    // Generous on purpose: the point is to catch a fight that has stopped ending, not to hold a boss to a
    // design target it does not have. The slowest that passes today needs well under half of it.
    private const int TurnBudget = 40;
    private const int PlaysInATurnNobodyMakes = 50;

    [Theory]
    [InlineData("city_boss_01")]
    [InlineData("city_boss_02")]
    [InlineData("city_boss_03")]
    [InlineData("city_boss_04")]
    [InlineData("city_boss_05")]
    [InlineData("archives_boss_warden_of_sealed_volumes")]
    [InlineData("archives_boss_whispering_catalogue_boss")]
    [InlineData("archives_boss_curator_of_misplaced_hours")]
    [InlineData("archives_boss_auditor_of_returned_lives")]
    [InlineData("archives_boss_grand_cross_reference")]
    [InlineData("green_docket_boss_ombudsman_of_root_and_road")]
    [InlineData("green_docket_boss_notary_of_old_growth")]
    [InlineData("green_docket_boss_grandmother_clause")]
    [InlineData("green_docket_boss_the_answering_hill")]
    [InlineData("green_docket_boss_queen_under_the_hill")]
    // Act IV's bosses are priced against a deck three acts of upgrades deep, and this walker brings the
    // starting one — AND never engages: it refuses every Royal Command, so the Pharaoh's Cartouche Ward
    // stands at a fifth reduction for the whole fight and never once opens into an exposure window. That is
    // the worst case the fight has by construction, so it gets a longer rope. The property this file exists
    // for is unchanged: the fight still ENDS.
    [InlineData("labyrinth_boss_pharaoh_of_the_sealed_name", 80)]
    [InlineData("labyrinth_boss_weigher_of_the_unspoken_heart", 80)]
    [InlineData("labyrinth_boss_architect_of_the_impossible_pyramid", 80)]
    [InlineData("labyrinth_boss_lady_of_the_black_granaries", 80)]
    [InlineData("labyrinth_boss_first_scribe_of_the_house_of_life", 80)]
    [InlineData("labyrinth_boss_mother_of_natron_and_resin", 80)]
    // Four bodies rather than one, and 940 HP between them: the offices are the fight's own reason to be
    // longer, and a walker that kills them all before touching the Vizier has taken the worst road there is.
    [InlineData("labyrinth_boss_vizier_of_the_kings_mouth", 100)]
    [InlineData("labyrinth_boss_queen_of_the_flood_reckoning", 80)]
    public void A_boss_dies_inside_the_turn_budget(string encounterId, int budget = TurnBudget)
    {
        var (play, session, _) = FightProbe.Start(FightProbe.Authored(encounterId), health: 9999);
        using (play)
        {
            var driver = play.CombatDriver!;
            var rng = new Random(4711);
            var refused = new HashSet<CardInstanceId>();
            var barren = new HashSet<string>(StringComparer.Ordinal);
            var turn = 0;

            while (turn < budget && driver.Current is not null)
            {
                turn++;
                refused.Clear();
                barren.Clear();
                var plays = 0;
                string? lastPlayed = null;
                var tableBeforeThePlay = "";

                while (driver.Current is { } combat)
                {
                    if (Answer(driver, rng))
                        continue;

                    // A play only FINISHES here: a card that asks a question parks halfway through its own
                    // resolution, so a reading taken the moment PlayCard returns straddles an open question.
                    if (lastPlayed is { } finished)
                    {
                        if (TableState(combat) == tableBeforeThePlay)
                            barren.Add(finished);
                        lastPlayed = null;
                    }

                    var hero = combat.State.GetCombatant(combat.HeroId);
                    var energy = hero.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var pool)
                        ? pool.Current : 0;
                    var card = combat.Hand.FirstOrDefault(c =>
                        !refused.Contains(c.Id) && !barren.Contains(c.DefinitionId.value)
                        && Affordable(play, combat, c, energy));
                    if (card is null)
                        break;

                    Assert.True(++plays < PlaysInATurnNobodyMakes,
                        $"'{encounterId}': turn {turn} played {plays} cards without ending — "
                        + $"last '{card.DefinitionId.value}'.");

                    var enemy = combat.State.Combatants.FirstOrDefault(
                        c => c.Id != combat.HeroId && c.TeamId == StandardCombatIds.EnemyTeam && c.IsAlive);
                    var needsTarget = play.CardNeedsTarget.GetValueOrDefault(card.DefinitionId.value);
                    var stepsBefore = combat.Steps.Count;
                    tableBeforeThePlay = TableState(combat);
                    lastPlayed = card.DefinitionId.value;
                    driver.PlayCard(card.Id, needsTarget ? enemy?.Id : null);
                    if (Refused(driver.Current, stepsBefore))
                        refused.Add(card.Id);
                }

                if (driver.Current is not { } live)
                    break;
                output.WriteLine($"turn {turn,3}: {Standing(live)}");
                while (Answer(driver, rng)) { }
                driver.EndTurn();
            }

            Assert.Null(session.Error);
            Assert.Null(play.Error);
            // Written as a branch, not an Assert.True with an interpolated message: the message would be built
            // on the passing path too, where there is no combat left to describe.
            if (driver.Current is { } stillStanding)
                Assert.Fail($"'{encounterId}' was still standing after {turn} turns: {Standing(stillStanding)}");
            output.WriteLine($"{encounterId}: down in {turn} turns (budget {budget})");
        }
    }

    private static string Standing(InteractiveCombat combat) => string.Join(", ",
        combat.State.Combatants.Where(c => c.Id != combat.HeroId)
            .Select(c => $"{c.Id.value} {c.Health.Current}/{c.Health.Max}"));

    // The walker's reading of everything a play could visibly move. The exhaust pile is deliberately not in
    // it: a card that burns itself and puts a fresh copy back in hand grows that pile on every play, and
    // counting it would make exactly the loop this reading exists to find look busy for ever.
    private static string TableState(InteractiveCombat combat)
    {
        var hero = combat.State.GetCombatant(combat.HeroId);
        var energy = hero.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var pool) ? pool.Current : 0;
        var enemies = combat.State.Combatants.Where(c => c.Id != combat.HeroId).ToList();
        var zones = combat.State.GetCardZones(combat.HeroId);
        int Count(CardZone zone) => zones.GetCardsInZone(zone).Count;
        static int Stacks(IEnumerable<StatusInstance> statuses) => statuses.Sum(status => status.Stacks);
        return $"{energy}/{hero.Health.Current}/{hero.Statuses.Count}/{Stacks(hero.Statuses)}/"
            + $"{Count(CardZone.Hand)}/{Count(CardZone.DiscardPile)}/{Count(CardZone.DrawPile)}/"
            + $"{enemies.Sum(e => e.Health.Current)}/{enemies.Sum(e => e.Statuses.Count)}/"
            + $"{enemies.Sum(e => Stacks(e.Statuses))}";
    }

    private static bool Refused(InteractiveCombat? combat, int stepsBefore) =>
        combat is not null
        && (combat.Steps.Count <= stepsBefore || combat.Steps.Skip(stepsBefore).Any(step => step.HasProblems));

    private static bool Answer(InteractiveCombatDriver driver, Random rng)
    {
        if (driver.PendingCardChoice is { } cards)
        {
            var take = Math.Min(driver.PendingCardChoiceCount, cards.Count);
            driver.SupplyCardChoice([.. cards.OrderBy(_ => rng.Next()).Take(take).Select(c => c.Id)]);
            return true;
        }
        if (driver.PendingOptionChoice is { } options)
        {
            var take = Math.Min(driver.PendingOptionChoiceCount, options.Count);
            driver.SupplyOptionChoice([.. Enumerable.Range(0, options.Count).OrderBy(_ => rng.Next()).Take(take)]);
            return true;
        }
        return false;
    }

    private static bool Affordable(RunPlayback play, InteractiveCombat combat, CardInstance card, int energy)
    {
        var hero = combat.State.GetCombatant(combat.HeroId);
        var costs = play.ComposedCostsFor(card.DefinitionId.value)
            ?? play.CardFullCosts.GetValueOrDefault(card.DefinitionId.value);
        if (costs is null)
            return play.CardCosts.GetValueOrDefault(card.DefinitionId.value) <= energy;
        return costs.All(cost =>
            (hero.Resources.TryGetValue(cost.ResourceId, out var pool) ? pool.Current : 0) >= cost.Amount);
    }
}
