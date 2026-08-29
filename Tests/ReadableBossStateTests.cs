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

    // ── The Curator's dial ────────────────────────────────────────────────────────────────────────────────

    // The first thing the player ever sees of this boss is which hour it is about to work in. Before it has
    // acted the dial has never turned, so nothing would have put a face on it — the fight has to start wearing
    // one.
    [Fact]
    public void The_dial_reads_the_present_before_the_Curator_has_acted()
    {
        var (play, curator) = Cycle(CuratorOfMisplacedHours.EnemyId, "immediate_correction");

        Assert.True(Has(Body(play, curator), CuratorOfMisplacedHours.DialPresentId));
        Assert.Single(CuratorOfMisplacedHours.DialFaces, f => Has(Body(play, curator), f));
    }

    // The dial turns after every action, and the face has to turn with it. A single wrong reading is worse
    // than none: the player plans a whole turn against the hour they were shown.
    [Fact]
    public void The_dial_never_shows_an_hour_other_than_the_one_it_stands_on()
    {
        var (play, curator) = Cycle(CuratorOfMisplacedHours.EnemyId,
            "immediate_correction", "schedule_the_collapse");

        for (var turn = 0; turn < 9; turn++)
        {
            var body = Body(play, curator);
            var sector = Counter(body, "curator_dial");
            var worn = CuratorOfMisplacedHours.DialFaces.Where(f => Has(body, f)).ToList();

            Assert.Equal(CuratorOfMisplacedHours.DialFaces[sector], Assert.Single(worn));
            EndTurn(play);
        }
    }

    // PRESENT → FUTURE → PAST → PRESENT, read off the faces alone: this is the whole promise the boss makes,
    // and it is now a thing the player can actually watch happen.
    [Fact]
    public void The_dial_walks_the_three_hours_in_order()
    {
        var (play, curator) = Cycle(CuratorOfMisplacedHours.EnemyId, "immediate_correction");

        var seen = new List<string>();
        for (var turn = 0; turn < 4; turn++)
        {
            seen.Add(CuratorOfMisplacedHours.DialFaces.Single(f => Has(Body(play, curator), f)));
            EndTurn(play);
        }

        Assert.Equal(
            [CuratorOfMisplacedHours.DialPresentId, CuratorOfMisplacedHours.DialFutureId,
             CuratorOfMisplacedHours.DialPastId, CuratorOfMisplacedHours.DialPresentId],
            seen);
    }

    // ── The Warden's announced key ────────────────────────────────────────────────────────────────────────

    // "Inspect the Claim" names the key and seals nothing; the sealing happens at the player's next draw. The
    // turn in between is the one the announcement exists for.
    [Fact]
    public void The_announced_key_stands_on_the_player_from_the_naming_to_the_sealing()
    {
        var (play, _) = Cycle(WardenOfSealedVolumes.EnemyId, "inspect_the_claim");

        // Before it has inspected anything, nothing is announced.
        Assert.Empty(Announced(Hero(play)));

        EndTurn(play);   // it inspects: a key is named, and the player can read which.
        var named = Assert.Single(Announced(Hero(play)));
        Assert.Equal(NamedFor(Counter(Hero(play), "warden_seal_type")), named);
    }

    // And it comes off when the key is turned — an announcement that outlived its sealing would say a card is
    // about to be taken that already has been.
    [Fact]
    public void The_announcement_is_taken_off_when_the_seal_falls()
    {
        var (play, warden) = Cycle(WardenOfSealedVolumes.EnemyId,
            "inspect_the_claim", "seal_the_principal_instrument");

        var wasNamed = false;
        var wasCleared = false;
        for (var turn = 0; turn < 8 && play.CombatDriver?.Current is not null; turn++)
        {
            EndTurn(play);
            if (play.CombatDriver?.Current is null)
                break;
            var hero = Hero(play);
            // Whatever the fight is doing, the face and the counter agree — including agreeing on nothing.
            Assert.Equal(NamedFor(Counter(hero, "warden_seal_type")), Announced(hero).SingleOrDefault());
            wasNamed |= Announced(hero).Count > 0;
            wasCleared |= wasNamed && Announced(hero).Count == 0;
        }

        // A run of turns where nothing was ever announced would satisfy every assertion above and prove
        // nothing at all, so the fight has to have named a key AND turned it.
        Assert.True(wasNamed, "the Warden never announced a key");
        Assert.True(wasCleared, "the announcement was never taken off");
        Assert.True(FightProbe.StacksOf(Body(play, warden), WardenOfSealedVolumes.CustodyId) > 0,
            "the Warden never sealed a volume");
    }

    private static IReadOnlyList<string> Announced(CombatantState hero) =>
        [.. AnnouncedFaces.Where(f => Has(hero, f))];

    private static string? NamedFor(int announcement) => announcement switch
    {
        1 => WardenOfSealedVolumes.AnnouncedRestraintId,
        2 => WardenOfSealedVolumes.AnnouncedProcedureId,
        3 => WardenOfSealedVolumes.AnnouncedEvidenceId,
        _ => null,
    };

    private static readonly string[] AnnouncedFaces =
    [
        WardenOfSealedVolumes.AnnouncedRestraintId,
        WardenOfSealedVolumes.AnnouncedProcedureId,
        WardenOfSealedVolumes.AnnouncedEvidenceId,
    ];
}
