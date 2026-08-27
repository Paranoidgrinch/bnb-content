using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III — The Moonlit Jurisdictions. Everyone agrees the inscription is law; the dispute is about what it
// says, and about which court has standing to say so. Two of these four bodies are old ones come back
// changed, which is the act's recurrence paying off.
public class ActThreeStageNineTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static IReadOnlyList<StatusInstance> Trespasses(RunPlayback play) =>
        [.. Hero(play).Statuses.Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId))];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private const string OneCost = "paper_cut";
    private const string AlsoOneCost = "cower_behind_a_desk";
    private const string TwoCost = "permit_a38";
    private const string Free = "red_tape"; // Junk, Base Cost 0 — and unplayable, so only the hand cares

    // ── The Untranslated Trail Marker — Three Readings ────────────────────────────────────────────────────

    [Fact]
    public void The_inscription_opens_on_its_plainest_reading()
    {
        var (play, session, marker) = FightProbe.Start(
            FightProbe.Solo("untranslated_trail_marker", "turn_the_sign", energy: 9),
            deck: [OneCost, AlsoOneCost, TwoCost, OneCost, AlsoOneCost], health: 300);

        Play(play, session, OneCost, marker);
        Play(play, session, AlsoOneCost, marker); // two of the same Base Cost — Reading I

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, so it fired
        play.Dispose();
    }

    // Arguing with the inscription is what changes what it says: spending a licence against its own Trespass
    // turns the sign to the next reading, and the repeated measure stops being a breach.
    [Fact]
    public void Spending_a_licence_against_it_turns_the_sign()
    {
        var (play, session, marker) = FightProbe.Start(
            FightProbe.Solo("untranslated_trail_marker", "turn_the_sign", energy: 9),
            deck: [OneCost, AlsoOneCost, OneCost, AlsoOneCost, OneCost], health: 300);

        Play(play, session, OneCost, marker);
        Play(play, session, AlsoOneCost, marker); // Reading I fires and the licence refuses it — sign turned

        play.CombatDriver!.EndTurn();
        Play(play, session, OneCost, marker);
        Play(play, session, AlsoOneCost, marker); // the same measure again, and now nobody minds

        Assert.Empty(Trespasses(play));
        play.Dispose();
    }

    // Reading II is about where your attention went, not what you played.
    [Fact]
    public void Under_the_second_reading_a_wandering_eye_is_the_breach()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_jurisdictions_duo_03", energy: 9),
            deck: [OneCost, TwoCost, OneCost, TwoCost, OneCost], health: 500);

        var marker = Enemies(play)[0].Id;
        var path = Enemies(play)[1].Id;

        // Turn the sign to Reading II by spending the opening licence on Reading I.
        Play(play, session, OneCost, marker);
        Play(play, session, OneCost, marker);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver!.EndTurn();

        var before = Trespasses(play).Count;
        Play(play, session, OneCost, marker);
        Play(play, session, TwoCost, path);   // the attention wanders once …
        Play(play, session, OneCost, marker); // … and twice

        Assert.True(Trespasses(play).Count > before, "the second wandering is the breach");
        play.Dispose();
    }

    // ── Elsewhere Path — Destination ──────────────────────────────────────────────────────────────────────

    // The law is not about what you did to the Destination. It is about whether you went where the path said.
    // The intents are pinned so that the only rule that can speak is the Path's own.
    [Fact]
    public void Ending_a_turn_without_going_where_the_path_said_owes_the_path()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("jurisdictions", energy: 9,
                ("elsewhere_path", "name_the_way", null),
                ("hawthorn_tenant", "thorn_lease", null)),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        var path = Enemies(play)[0];
        var tenant = Enemies(play)[1];
        Assert.Equal(1, FightProbe.StacksOf(tenant, ActThree.DestinationId));

        Play(play, session, OneCost, path.Id); // everything aimed at the Path, nothing at the Destination
        play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, so it fired
        play.Dispose();
    }

    [Fact]
    public void Going_where_the_path_said_is_no_breach()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("jurisdictions", energy: 9,
                ("elsewhere_path", "name_the_way", null),
                ("hawthorn_tenant", "thorn_lease", null)),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        var tenant = Enemies(play)[1].Id;
        Play(play, session, OneCost, tenant);
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── Two Courts Claim the Hare ─────────────────────────────────────────────────────────────────────────

    // The same character from the act's first room, now governed by a different court: at 2 Claims the road
    // law gives way to hill law, and it is the free card that offends rather than the third.
    [Fact]
    public void Under_hill_law_the_free_card_is_the_breach()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_jurisdictions_duo_02", energy: 9),
            deck: [OneCost, AlsoOneCost, TwoCost, OneCost, AlsoOneCost], health: 500);

        var hare = Enemies(play)[0];
        Assert.Equal(2, FightProbe.StacksOf(hare, ActThree.ClaimId)); // two courts, and the hill one wins

        // Three cards, none of them free: under road law this was the breach, and under hill law it is not.
        Play(play, session, OneCost, hare.Id);
        Play(play, session, AlsoOneCost, hare.Id);
        Play(play, session, TwoCost, hare.Id);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Empty(Trespasses(play));
        play.Dispose();
    }

    // ── Superior Jurisdiction ─────────────────────────────────────────────────────────────────────────────

    // While the Stone stands, no foreign rule may move a Claim downhill — and the Stone's own title still
    // wanders, because the precedence is its.
    [Fact]
    public void The_stones_own_title_still_wanders_under_its_own_precedence()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_jurisdictions_duo_02"), health: 400);

        var hare = Enemies(play)[0];
        var stone = Enemies(play)[1];

        // The Hare opens holding two; the Stone holds none and is granted nothing yet, so nothing has moved.
        Assert.Equal(2, FightProbe.StacksOf(hare, ActThree.ClaimId));
        Assert.Equal(0, FightProbe.StacksOf(stone, ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(stone, ActThree.SuperiorJurisdictionId));
        play.Dispose();
    }
}
