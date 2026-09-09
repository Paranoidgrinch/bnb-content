using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;
namespace BnbContent.Converter.Playtest;

// THE DUMB PLAYER, in one place.
//
// Two instruments drive fights without a human: the WALK, which plays whole runs looking for rooms that never
// end, and the SPARRING RING, which plays one authored fight over and over to see what it costs. They want
// different reports and the same player — and the player is the part with the hard-won corrections in it:
// what counts as affordable, what counts as a play that did nothing, how a question gets answered, when a
// turn has gone on too long to be a turn.
//
// Those corrections were each paid for by a walk that hung, so they live here rather than in either caller.
// A second copy of them would drift, and the drift would show up as "the sparring ring says the fight is
// fine and the walk says it hangs", which is the least useful disagreement two instruments can have.
public static class Greedy
{
    // A turn that never runs out of affordable cards is a finding, not a slow fight. Nothing in this game
    // plays fifty cards in a turn.
    public const int PlaysInATurnNobodyMakes = 50;

    // A card (or an enemy) asked a question mid-resolution: answer it so the fight can go on. True = answered.
    public static bool Answer(InteractiveCombatDriver driver, Random rng)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(rng);

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

    // Whether the hero can pay for the card right now — its composed costs (shreds included), Energy only; a
    // card wanting a resource the hero has none of is simply not playable and the greedy player skips it.
    public static bool Affordable(RunPlayback play, InteractiveCombat combat, CardInstance card, int energy)
    {
        ArgumentNullException.ThrowIfNull(play);
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(card);

        var hero = combat.State.GetCombatant(combat.HeroId);
        var costs = play.ComposedCostsFor(card.DefinitionId.value)
            ?? play.CardFullCosts.GetValueOrDefault(card.DefinitionId.value);
        if (costs is null)
            return play.CardCosts.GetValueOrDefault(card.DefinitionId.value) <= energy;
        foreach (var cost in costs)
        {
            var have = hero.Resources.TryGetValue(cost.ResourceId, out var pool) ? pool.Current : 0;
            if (have < cost.Amount)
                return false;
        }
        return true;
    }

    // Did the play just made go through? The fight records every attempt as a step, and a refused one carries
    // the reason; nothing new at all means the driver dropped it (a prompt opened, say).
    public static bool Refused(InteractiveCombat? combat, int stepsBefore)
    {
        if (combat is null)
            return false;
        var steps = combat.Steps;
        return steps.Count <= stepsBefore || steps.Skip(stepsBefore).Any(step => step.HasProblems);
    }

    // Everything about the table a play could visibly move. Two plays with the same reading either side of
    // them did nothing — which is the only way to tell a card that regenerates itself apart from one that
    // achieves something.
    //
    // The EXHAUST PILE is deliberately not in it. A card that burns itself and puts a fresh copy back in hand
    // grows that pile on every play, so counting it would make every such card look busy for ever — which is
    // exactly the loop this reading exists to find. Statuses are counted by their STACKS as well as their
    // number, because paying a debt down usually moves the stack and not the count.
    public static string TableState(InteractiveCombat combat)
    {
        ArgumentNullException.ThrowIfNull(combat);

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

    // Who is left on the other side of the table, and on how much health.
    public static string Standing(InteractiveCombat? combat) =>
        combat is null
            ? "the fight is over"
            : string.Join(", ", combat.State.Combatants
                .Where(c => c.Id != combat.HeroId && c.IsAlive)
                .Select(c => $"{c.Id.value} {c.Health.Current}/{c.Health.Max}"));

    // The next card this player would play, or null when the turn is over as far as it is concerned.
    public static CardInstance? Choose(
        RunPlayback play, InteractiveCombat combat, Random rng,
        ISet<CardInstanceId> refused, ISet<string> barren)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(refused);
        ArgumentNullException.ThrowIfNull(barren);

        var hero = combat.State.GetCombatant(combat.HeroId);
        var energy = hero.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var pool) ? pool.Current : 0;
        var candidates = combat.Hand
            .Where(c => !refused.Contains(c.Id) && !barren.Contains(c.DefinitionId.value)
                && Affordable(play, combat, c, energy))
            .ToList();
        return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
    }

    // Whom to point a card at: the first enemy still on its feet.
    public static CombatantId? Enemy(InteractiveCombat combat)
    {
        ArgumentNullException.ThrowIfNull(combat);
        return combat.State.Combatants
            .FirstOrDefault(c => c.Id != combat.HeroId && c.TeamId == StandardCombatIds.EnemyTeam && c.IsAlive)?.Id;
    }
}
