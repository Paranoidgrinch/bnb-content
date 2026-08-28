using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III boss 2 — The Notary of Old Growth. A violation stops being an incident and becomes durable
// precedent: standing made under the ring that happens to be governing proposes that ring, and the Notary's
// sealing makes it law on top of whatever is rotating. The counterauthority is bought with restitution.
public class ActThreeBossNotaryTests
{
    private const string OneCost = "paper_cut";
    private const string Working = "cower_behind_a_desk";
    private const string TwoCost = "permit_a38";

    private const string RotatingPassage = "ring_rotating_passage";
    private const string RotatingRestraint = "ring_rotating_restraint";
    private const string RotatingKeeping = "ring_rotating_keeping";
    private const string SealPassage = "notarial_seal_passage";
    private const string SealRestraint = "notarial_seal_restraint";
    private const string BrokenPassage = "seal_broken_passage";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Oak(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int OwedTo(RunPlayback play, CombatantId creditor) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId)
                && s.SourceCombatantId == creditor)
            .Sum(s => s.Stacks);

    private static int TrespassFrom(RunPlayback play, CombatantId filer) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)
                && s.SourceCombatantId == filer)
            .Sum(s => s.Stacks);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Oak) Start(
        string intentId, IReadOnlyList<string> deck, int energy = 9, params (string, int)[] statuses) =>
        FightProbe.Start(
            FightProbe.Solo(ActThree.NotaryEnemyId, intentId, energy, statuses),
            deck: deck, health: 900);

    // ── the three rings ───────────────────────────────────────────────────────────────────────────────────

    // The rings open on Passage and turn one step every turn.
    [Fact]
    public void The_rings_turn_every_turn()
    {
        var (play, _, _) = Start("old_growth_signature", [.. Enumerable.Repeat(TwoCost, 5)]);

        Assert.Equal(1, FightProbe.StacksOf(Oak(play), RotatingPassage));
        Assert.Equal(0, FightProbe.StacksOf(Oak(play), RotatingRestraint));
        play.Dispose();
    }

    // Only the ring that is turning is law: under Passage, two cards of one price are the breach.
    [Fact]
    public void Only_the_rotating_ring_is_law()
    {
        var (play, session, oak) = Start("demand_the_countermark",
            [Working, Working, TwoCost, TwoCost, OneCost]);

        Play(play, session, Working, oak);
        Play(play, session, Working, oak); // a matched pair under the Ring of Passage

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // …and a fourth card is not, because the Ring of Restraint is not turning yet.
    [Fact]
    public void A_ring_that_is_not_turning_says_nothing()
    {
        var (play, session, oak) = Start("demand_the_countermark",
            [Working, TwoCost, Working, TwoCost, OneCost]);

        Play(play, session, Working, oak);
        Play(play, session, TwoCost, oak);
        Play(play, session, Working, oak);
        Play(play, session, TwoCost, oak); // four real cards, no matched pair

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, TrespassFrom(play, oak));
        play.Dispose();
    }

    // ── Notarise the Claim ────────────────────────────────────────────────────────────────────────────────

    // Standing made under the governing ring proposes that ring, and the sealing makes it permanent — law
    // whatever is turning. The Ring of Passage governs every third turn, so this is a slow, deliberate
    // record: a matched pair every turn, three of them landing, and the tree seals what it has heard.
    [Fact]
    public void Sealing_a_proposed_ring_makes_it_law_for_good()
    {
        var (play, session, oak) = Start("seal_it_in_sap", [.. Enumerable.Repeat(Working, 5)]);

        for (var turn = 0; turn < 12; turn++)
        {
            Play(play, session, Working, oak);
            Play(play, session, Working, oak); // a matched pair, which only the Passage answers
            play.CombatDriver!.EndTurn();
        }

        Assert.Equal(1, FightProbe.StacksOf(Oak(play), SealPassage));
        Assert.Equal(0, FightProbe.StacksOf(Oak(play), SealRestraint));
        play.Dispose();
    }

    // With nothing proposed the sealing is just the tree closing over itself.
    [Fact]
    public void With_nothing_proposed_the_sealing_is_only_bark()
    {
        var (play, _, _) = Start("seal_it_in_sap", [.. Enumerable.Repeat(TwoCost, 5)], energy: 0);

        play.CombatDriver!.EndTurn();

        Assert.Equal(20, Block(Oak(play)));
        Assert.Equal(0, FightProbe.StacksOf(Oak(play), SealPassage));
        play.Dispose();
    }

    // A sealed ring is law even when another is turning.
    [Fact]
    public void A_sealed_ring_is_law_whatever_is_turning()
    {
        var (play, session, oak) = Start("demand_the_countermark",
            [Working, TwoCost, Working, TwoCost, OneCost], energy: 9,
            (SealRestraint, 1));

        Play(play, session, Working, oak);
        Play(play, session, TwoCost, oak);
        Play(play, session, Working, oak);
        Play(play, session, TwoCost, oak); // the fourth real card: the sealed Ring of Restraint

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── the Counterseal ───────────────────────────────────────────────────────────────────────────────────

    // Settling with the Notary in full buys legal counterauthority, and the card to spend it with.
    [Fact]
    public void Settling_in_full_buys_a_counterseal()
    {
        var (play, session, oak) = Start("demand_the_countermark",
            [.. Enumerable.Repeat(TwoCost, 5)], energy: 9);

        play.CombatDriver!.EndTurn(); // a demand for 1, with no standing to cash
        Assert.Equal(1, OwedTo(play, oak));

        var card = play.CombatDriver.Current!.Hand
            .First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, oak);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.True(session.Error is null, session.Error);

        play.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.CounterSealId));
        Assert.Contains(play.CombatDriver.Current!.Hand,
            c => c.DefinitionId.value == ActThree.CounterSealCardId);
        play.Dispose();
    }

    // A Counterseal prises one seal back out of the wood — and the wood remembers losing it.
    [Fact]
    public void A_counterseal_prises_a_seal_back_out()
    {
        var (play, session, oak) = Start("demand_the_countermark",
            [.. Enumerable.Repeat(TwoCost, 5)], energy: 9, (SealPassage, 1));

        // The counterauthority is handed over at the bell, so the hero opens with it.
        var probe = FightProbe.Solo(ActThree.NotaryEnemyId, "demand_the_countermark", 9, (SealPassage, 1));
        var withSeal = new EncounterDefinition(probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             new StartingStatusSpec(new StatusDefinitionId(ActThree.CounterSealId), 1)],
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);
        play.Dispose();

        var (armed, session2, tree) = FightProbe.Start(withSeal,
            deck: [.. Enumerable.Repeat(TwoCost, 5)], health: 900);

        var card = armed.CombatDriver!.Current!.Hand
            .First(c => c.DefinitionId.value == ActThree.CounterSealCardId);
        armed.CombatDriver.PlayCard(card.Id, tree);
        armed.CombatDriver.SupplyOptionChoice([0]); // the Ring of Passage
        Assert.True(session2.Error is null, session2.Error);

        Assert.Equal(0, FightProbe.StacksOf(Oak(armed), SealPassage));
        Assert.Equal(1, FightProbe.StacksOf(Oak(armed), BrokenPassage));
        Assert.Equal(0, FightProbe.StacksOf(Hero(armed), ActThree.CounterSealId));
        armed.Dispose();
        Assert.NotEqual(default, oak);
        Assert.True(session is not null);
    }

    // ── the heartwood ─────────────────────────────────────────────────────────────────────────────────────

    // The tree's own testimony replaces the Notary's next action, and it is not a blow.
    [Fact]
    public void The_heartwood_bears_witness_instead_of_striking()
    {

        // Two precedents established is what calls the heartwood; the failsafe is 180 HP, and a probe can
        // reach that by starting the tree there.
        var probe = FightProbe.Solo(ActThree.NotaryEnemyId, "read_the_annual_clause", 0);
        var wounded = new EncounterDefinition(probe.Id,
            [probe.Enemies[0] with { MaxHealth = 170 }],
            probe.HeroResources, probe.HeroStartingStatuses, probe.HeroDisplayName,
            probe.CardsDrawnPerTurn, probe.TriggeredEffects);

        var (hurt, _, _) = FightProbe.Start(wounded,
            deck: [.. Enumerable.Repeat(TwoCost, 5)], health: 900);

        var before = Hero(hurt).Health.Current;
        hurt.CombatDriver!.EndTurn(); // the bell queues the testimony, and it replaces the next action

        Assert.Equal(before, Hero(hurt).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Oak(hurt), ActThree.HeartwoodId));
        Assert.Equal(18, Block(Oak(hurt)));
        Assert.True(FightProbe.StacksOf(Hero(hurt), ActThree.SafeConductId) >= 1);
        hurt.Dispose();
    }

    // In the heartwood a sealed ring's first breach each turn is worth two.
    [Fact]
    public void In_the_heartwood_a_sealed_ring_takes_two()
    {
        var (play, session, oak) = Start("demand_the_countermark",
            [Working, Working, TwoCost, TwoCost, OneCost], energy: 9,
            (ActThree.HeartwoodId, 1), (SealPassage, 1));

        Play(play, session, Working, oak);
        Play(play, session, Working, oak);

        var refusals = play.CombatDriver!.Current!.State.CombatLog
            .Where(e => e.Type == StandardCombatLogTypes.StatusApplicationBlocked)
            .Select(e => e.Message).ToList();
        Assert.Contains(refusals, m => m.Contains("prevented 2 stack(s)", StringComparison.Ordinal));
        play.Dispose();
    }

    // ── Every Ring Is Evidence ────────────────────────────────────────────────────────────────────────────

    // "16 damage, +4 per distinct active Ring, +2 per Weight; a demand for 1 per Notarial Seal."
    [Fact]
    public void The_signature_reads_the_whole_record()
    {
        var (play, _, oak) = Start("every_ring_is_evidence", [.. Enumerable.Repeat(TwoCost, 5)], energy: 0,
            (SealPassage, 1), (SealRestraint, 1), (ActThree.WeightOfPrecedentId, 2));

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        // Passage is both sealed and turning, so the two distinct laws are Passage and Restraint.
        Assert.Equal(before - (16 + 8 + 4), Hero(play).Health.Current);
        Assert.Equal(2, OwedTo(play, oak));
        play.Dispose();
    }

    // Without the record or the wounds to unlock it, the slot is an ordinary blow.
    [Fact]
    public void The_signature_waits_for_the_record()
    {
        var (play, _, _) = Start("every_ring_is_evidence", [.. Enumerable.Repeat(TwoCost, 5)], energy: 0);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 16, Hero(play).Health.Current);
        play.Dispose();
    }
}
