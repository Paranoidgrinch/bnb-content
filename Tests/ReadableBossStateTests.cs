using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// STATE THE PLAYER IS ASKED TO ACT ON HAS TO BE STATE THE PLAYER CAN SEE. Two Act-II bosses kept theirs in a
// counter, and a counter reaches no screen: the Curator's own text promises "its dial shows which hour it is
// working in" and the dial showed nothing, and the Warden ANNOUNCES a key one turn before it turns it, which
// is a promise about a turn spent planning against a number that was never on the table.
//
// Both now wear the answer as a marker. The counter is still the mechanism — turning a dial is arithmetic —
// so what these tests check is not that a status exists but that it can never disagree with the counter it
// speaks for: exactly one face, and the right one, after every write.
public class ReadableBossStateTests
{
    private static readonly RunBlueprint Game =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260829);

    private const string Deed = "paper_cut";
    private const string Working = "strong_binder";

    private static CombatantState Body(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId);

    private static bool Has(CombatantState body, string status) =>
        body.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    private static int Counter(CombatantState body, string counter) =>
        body.Counters.TryGetValue(new CounterId(counter), out var value) ? value : 0;

    private static void EndTurn(RunPlayback play, int option = 0)
    {
        play.CombatDriver!.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([option]);
    }

    private static (RunPlayback Play, CombatantId Id) Cycle(string enemy, params string[] intents)
    {
        var probe = FightProbe.Solo(enemy, intents[0], 9);
        var body = probe.Enemies.Single() with
        {
            Actions = [.. intents.Select(i => new EnemyActionDefinitionId($"{enemy}.{i}"))],
        };
        var (play, _, id) = FightProbe.Start(
            new EncounterDefinition(probe.Id, [body], probe.HeroResources, probe.HeroStartingStatuses,
                probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects),
            deck: [.. Enumerable.Repeat(Deed, 12), .. Enumerable.Repeat(Working, 12)],
            health: 900);
        return (play, id);
    }

    // ── The phase a boss is in ────────────────────────────────────────────────────────────────────────────

    // A phased boss telegraphs its Phase-I intent name for the whole fight, because the engine rotates one
    // intent list. What keeps that readable as the boss CHANGING is the phase marker — so the marker has to be
    // findable, and a frontend can only find it if the document says which statuses are phases.
    [Fact]
    public void Every_phase_marker_names_a_status_the_game_actually_has()
    {
        var statuses = Game.Statuses.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(BossPhases.Markers);
        Assert.All(BossPhases.Markers, id => Assert.Contains(id, statuses));
        Assert.Equal(BossPhases.Markers.Count, BossPhases.Markers.Distinct(StringComparer.Ordinal).Count());
    }

    // …and every one of them says so in the manifest, which is the only thing a frontend reads.
    [Fact]
    public void Every_phase_marker_is_tagged_as_a_phase_in_the_document()
    {
        Assert.All(BossPhases.Markers, id =>
        {
            Assert.True(Game.Presentation.Statuses.TryGetValue(id, out var look), id);
            Assert.Contains(BossPhases.PhaseTag, look!.Tags);
        });
    }

    // A phase marker has to READ as one too: the banner shows the name, and the hover shows the rules text.
    [Fact]
    public void Every_phase_marker_has_a_name_and_says_what_it_changes()
    {
        Assert.All(BossPhases.Markers, id =>
        {
            var status = Game.Statuses.Single(s => s.Id == id);
            Assert.False(string.IsNullOrWhiteSpace(status.NameKey), id);
            Assert.False(string.IsNullOrWhiteSpace(status.DescriptionKey), id);
        });
    }

    // ── Nisaba's tablet ───────────────────────────────────────────────────────────────────────────────────

    // A god may be "almost a separate game mode" (boss master §Act V §3) but it may not be a riddle, and the
    // whole of Nisaba is state the player is asked to plan several turns against. So every face her tablet
    // can wear — the sentences, their revisions, their seals, the reed and the margin — has to carry a name
    // AND an explanation, because a chip that says "Sealed: Thirty-Six" and nothing else is a rule nobody
    // was told.
    [Fact]
    public void Every_face_of_the_first_tablet_says_what_it_means()
    {
        var faces = ActFive.NisabaStatuses();
        Assert.NotEmpty(faces);
        Assert.All(faces, status =>
        {
            Assert.False(string.IsNullOrWhiteSpace(status.NameKey), status.Id);
            Assert.False(string.IsNullOrWhiteSpace(status.DescriptionKey), status.Id);
            Assert.NotEqual(status.NameKey?.Trim(), status.DescriptionKey?.Trim());
        });
    }

    // …and the hand is the readout of what may still be corrected. A line that stands unsealed and unspent
    // is offered a sheet; one that is sealed, spent or gone is not. The two must never disagree, because the
    // ONLY way the player learns a line has been closed to them is that its sheet is missing.
    [Fact]
    public void The_tablet_and_the_hand_never_disagree_about_what_can_be_revised()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFive.NisabaEnemyId, "impress_the_seal", 9, (ActFive.LapisRecordId, 1)),
            deck: [.. Enumerable.Repeat("paper_cut", 30)], health: 2000);

        for (var turn = 0; turn < 6; turn++)
        {
            var keeper = play.CombatDriver!.Current!.State.Combatants
                .First(c => c.DefinitionId.value.Contains("nisaba", StringComparison.Ordinal));
            var hand = play.CombatDriver.Current.Hand.Select(c => c.DefinitionId.value).ToHashSet(StringComparer.Ordinal);

            foreach (var line in ActFive.LineFaces)
            {
                var slug = line["nisaba_line_".Length..];
                var correctable = Has(keeper, line)
                    && !Has(keeper, $"nisaba_sealed_{slug}")
                    && FightProbe.StacksOf(keeper, $"nisaba_revised_{slug}") < 4;
                Assert.Equal(correctable, hand.Contains($"revise_{slug}"));
            }

            play.CombatDriver.EndTurn();
        }

        play.Dispose();
    }

    // ── Inanna's ledger ───────────────────────────────────────────────────────────────────────────────────

    // The same rule for the second god, and she needs it more than the first: her state is spread over four
    // rows and a mark on the cards themselves, and a player who cannot read "Temple Due 8, Procession in 2"
    // off the screen is being asked to decide whether to pay a number they were never shown.
    [Fact]
    public void Every_face_of_the_eanna_ledger_says_what_it_means()
    {
        var faces = ActFive.InannaStatuses();
        Assert.NotEmpty(faces);
        Assert.All(faces, status =>
        {
            Assert.False(string.IsNullOrWhiteSpace(status.NameKey), status.Id);
            Assert.False(string.IsNullOrWhiteSpace(status.DescriptionKey), status.Id);
            Assert.NotEqual(status.NameKey?.Trim(), status.DescriptionKey?.Trim());
        });
    }

    // …and the hand is the readout of what may still be PAID. Both ways of settling stand in hand exactly
    // while something is owed and never otherwise, because the only way the player learns the ledger is
    // clear is that the sheets have stopped coming.
    [Fact]
    public void The_ledger_and_the_hand_never_disagree_about_what_may_be_paid()
    {
        var (play, _, enemy) = FightProbe.Start(
            FightProbe.Solo(ActFive.InannaEnemyId, "claim_the_finest_work", 9),
            deck: [.. Enumerable.Repeat(Deed, 12), .. Enumerable.Repeat(Working, 12)], health: 2000);

        for (var turn = 0; turn < 8; turn++)
        {
            var owed = FightProbe.StacksOf(Hero(play), ActFive.TempleDueId) >= 1;
            var hand = play.CombatDriver!.Current!.Hand
                .Select(c => c.DefinitionId.value).ToHashSet(StringComparer.Ordinal);

            Assert.Equal(owed, hand.Contains(ActFive.OfferSurplusCardId));
            Assert.Equal(owed, hand.Contains(ActFive.DedicateWorkCardId));

            while (play.CombatDriver.Current!.Hand.FirstOrDefault(c => c.DefinitionId.value == Deed) is { } deed)
                play.CombatDriver.PlayCard(deed.Id, enemy);

            play.CombatDriver.EndTurn();
        }

        play.Dispose();
    }

    // ── Nanshe's tablet ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_face_of_the_ration_tablet_says_what_it_means()
    {
        var faces = ActFive.NansheStatuses();
        Assert.NotEmpty(faces);
        Assert.All(faces, status =>
        {
            Assert.False(string.IsNullOrWhiteSpace(status.NameKey), status.Id);
            Assert.False(string.IsNullOrWhiteSpace(status.DescriptionKey), status.Id);
            Assert.NotEqual(status.NameKey?.Trim(), status.DescriptionKey?.Trim());
        });
    }

    // THE PORTION AND THE ROW THAT EXPLAINS IT CAN NEVER DISAGREE. A pool that is short and no chip saying
    // why is precisely the hidden gotcha the act forbids — so every point the day is short by is on the
    // table as Withheld, on every day of the fight, whatever the player has been doing with their portions.
    [Fact]
    public void The_portion_and_the_row_that_explains_it_never_disagree()
    {
        var (play, _, enemy) = FightProbe.Start(
            FightProbe.Solo(ActFive.NansheEnemyId, "the_quiet_measure", 3),
            deck: [.. Enumerable.Repeat(Deed, 20)], health: 2000);

        for (var day = 0; day < 9; day++)
        {
            var hero = Hero(play);
            var share = hero.Resources[StandardCombatIds.EnergyResource].Max ?? 0;
            var pool = hero.Resources[StandardCombatIds.EnergyResource].Current;

            Assert.Equal(share - pool, FightProbe.StacksOf(hero, ActFive.WithheldEnergyId));
            // …and which of her three days this is, always, because the pattern beside her is read against it.
            Assert.InRange(FightProbe.StacksOf(hero, ActFive.DayOfDistributionId), 1, 3);

            // Take from a later day now and then, so the row is proved on days that are actually short.
            if (day % 2 == 0 &&
                play.CombatDriver!.Current!.Hand.FirstOrDefault(
                    c => c.DefinitionId.value == ActFive.TakeAheadCardId) is { } sheet)
                play.CombatDriver.PlayCard(sheet.Id, null);

            // Bounded by the POOL, not by the hand: she rations Energy, so a card the day can no longer pay
            // for stays in hand, and a loop that waits for it to leave waits for ever.
            for (var guard = 0; guard < 12; guard++)
            {
                if (Hero(play).Resources[StandardCombatIds.EnergyResource].Current <= 0)
                    break;
                if (play.CombatDriver!.Current!.Hand.FirstOrDefault(c => c.DefinitionId.value == Deed)
                    is not { } deed)
                    break;
                play.CombatDriver.PlayCard(deed.Id, enemy);
            }

            play.CombatDriver!.EndTurn();
        }

        play.Dispose();
    }

    // ── Nanna-Sin's ring ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_face_of_the_lunar_calendar_says_what_it_means()
    {
        var faces = ActFive.NannaSinStatuses();
        Assert.NotEmpty(faces);
        Assert.All(faces, status =>
        {
            Assert.False(string.IsNullOrWhiteSpace(status.NameKey), status.Id);
            Assert.False(string.IsNullOrWhiteSpace(status.DescriptionKey), status.Id);
            Assert.NotEqual(status.NameKey?.Trim(), status.DescriptionKey?.Trim());
        });
    }

    // THE RING IS THE RULE, SO THE RING IS ALWAYS ON THE TABLE. Where the moon stands, what that means for
    // tonight's cards, and how many times the sky has come round are three facts every other rule of this
    // fight is written against — and the counter his intent rules read may never disagree with the chip that
    // speaks for it, because the move he telegraphs and the night it belongs to are the same fact.
    [Fact]
    public void The_ring_and_the_counter_that_drives_it_never_disagree()
    {
        int[] counts = [0, 1, 2, 3, 4, 3, 2, 1];
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFive.NannaSinEnemyId, "count_the_unseen", 3),
            deck: [.. Enumerable.Repeat(Deed, 20)], health: 4000);

        for (var night = 0; night < 18; night++)
        {
            var lord = play.CombatDriver!.Current!.State.Combatants
                .First(c => c.DefinitionId.value.Contains("nanna_sin", StringComparison.Ordinal));
            var phase = night % 8 + 1;

            Assert.Equal(phase, Counter(lord, "nanna_sin_phase"));
            Assert.Equal(phase, FightProbe.StacksOf(lord, ActFive.LunarPhaseId));
            Assert.Equal(counts[night % 8], FightProbe.StacksOf(lord, ActFive.LunarCountId));
            Assert.Equal(night / 8 + 1, FightProbe.StacksOf(lord, ActFive.OrbitId));

            play.CombatDriver!.EndTurn();
        }

        play.Dispose();
    }
}
