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

    // ── Nisaba ────────────────────────────────────────────────────────────────────────────────────────────

    // Every face of hers — the sentences, their countdown, her phases and what they leave on you — carries a name
    // and an explanation: a chip that names a sentence and nothing else is a rule nobody was told.
    [Fact]
    public void Every_face_of_nisaba_says_what_it_means()
    {
        var faces = BnbContent.Converter.Gods.Nisaba.Statuses().ToList();
        Assert.NotEmpty(faces);
        Assert.All(faces, status =>
        {
            Assert.False(string.IsNullOrWhiteSpace(status.NameKey), status.Id);
            Assert.False(string.IsNullOrWhiteSpace(status.DescriptionKey), status.Id);
            Assert.NotEqual(status.NameKey?.Trim(), status.DescriptionKey?.Trim());
        });
    }

    // ── Inanna ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_face_of_inanna_says_what_it_means()
    {
        var faces = BnbContent.Converter.Gods.Inanna.Statuses().ToList();
        Assert.NotEmpty(faces);
        Assert.All(faces, status =>
        {
            Assert.False(string.IsNullOrWhiteSpace(status.NameKey), status.Id);
            Assert.False(string.IsNullOrWhiteSpace(status.DescriptionKey), status.Id);
            Assert.NotEqual(status.NameKey?.Trim(), status.DescriptionKey?.Trim());
        });
    }

    // ── Nanshe ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_face_of_nanshe_says_what_it_means()
    {
        var faces = BnbContent.Converter.Gods.Nanshe.Statuses().ToList();
        Assert.NotEmpty(faces);
        Assert.All(faces, status =>
        {
            Assert.False(string.IsNullOrWhiteSpace(status.NameKey), status.Id);
            Assert.False(string.IsNullOrWhiteSpace(status.DescriptionKey), status.Id);
            Assert.NotEqual(status.NameKey?.Trim(), status.DescriptionKey?.Trim());
        });
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
