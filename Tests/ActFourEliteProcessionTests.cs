using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, elites 7 and 8, proved in live fights: the body that sells you a price list, and the three bodies
// whose order of death is the fight.
public class ActFourEliteProcessionTests
{
    private const string OneCost = "paper_cut";  // Deed, 1: deal 6

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static bool Holds(RunPlayback play, string cardId) =>
        play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == cardId);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static int BlockOf(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;

    // ── the Sphinx of the Processional Measure ────────────────────────────────────────────────────────────

    // The riddle is two of three prices, in hand, every other turn — and answering costs exactly what the
    // answer says it does.
    [Fact]
    public void The_riddle_offers_two_of_its_three_prices()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("sphinx_of_the_processional_measure", "first_riddle"), health: 900);

        var offered = new[]
        {
            ActFour.AnswerOfMeasureCardId, ActFour.AnswerOfBurdenCardId, ActFour.AnswerOfBurialCardId,
        }.Count(id => Holds(play, id));
        Assert.Equal(2, offered);

        // …and it is set every OTHER turn: the next one carries no riddle.
        play.CombatDriver!.EndTurn();
        Assert.Equal(0, new[]
        {
            ActFour.AnswerOfMeasureCardId, ActFour.AnswerOfBurdenCardId, ActFour.AnswerOfBurialCardId,
        }.Count(id => Holds(play, id)));
        play.Dispose();
    }

    // Three answers force the procession open: the sphinx loses its cover, and for a turn everything you land
    // on it goes a fifth further.
    [Fact]
    public void Three_answers_open_the_procession()
    {
        var (play, session, sphinx) = FightProbe.Start(
            FightProbe.Solo("sphinx_of_the_processional_measure", "first_riddle"),
            deck: [.. Enumerable.Repeat(OneCost, 12)], health: 900);

        var marks = 0;
        for (var turn = 0; turn < 6 && marks < ActFour.AnswersToOpen; turn++)
        {
            foreach (var id in new[]
                     {
                         ActFour.AnswerOfMeasureCardId, ActFour.AnswerOfBurdenCardId,
                         ActFour.AnswerOfBurialCardId,
                     })
            {
                if (marks >= ActFour.AnswersToOpen || !Holds(play, id))
                    continue;
                Play(play, session, id, null);
                marks++;
            }

            if (marks < ActFour.AnswersToOpen)
                play.CombatDriver!.EndTurn();
        }

        Assert.Equal(ActFour.AnswersToOpen, marks);

        var body = Body(play, "sphinx_of_the_processional_measure");
        Assert.Equal(1, FightProbe.StacksOf(body, ActFour.ProcessionOpenedId));
        Assert.Equal(0, FightProbe.StacksOf(body, ActFour.AnswerMarkId));

        // 6 damage a cut becomes 7 while the procession stands open.
        var standing = body.Health.Current;
        Play(play, session, OneCost, sphinx);
        Assert.Equal(7, standing - Body(play, "sphinx_of_the_processional_measure").Health.Current);
        play.Dispose();
    }

    // The signature counts KINDS of the act's afflictions, not stacks: a player buried five deep in one thing
    // is answered more gently than one carrying a little of everything.
    [Fact]
    public void The_procession_answers_by_how_many_kinds_you_carry()
    {
        int Struck(params (string Status, int Stacks)[] carried)
        {
            var (play, _, _) = FightProbe.Start(
                FightProbe.SoloAgainstHero("sphinx_of_the_processional_measure",
                    "the_procession_has_heard_enough", energy: 3, carried),
                health: 900);

            var before = Hero(play).Health.Current;
            play.CombatDriver!.EndTurn();
            var struck = before - Hero(play).Health.Current;
            play.Dispose();
            return struck;
        }

        Assert.Equal(25, Struck());
        Assert.Equal(25 + 3, Struck((ActFour.EntombedId, 4)));  // four deep in one thing is still one kind
        Assert.Equal(25 + 6, Struck((ActFour.EntombedId, 1), (ActFour.BurdenedId, 1)));

        // A measure would be a third kind, and the formula counts it — but a measure is TAKEN at the end of
        // the turn it stands in and removes itself doing so, so nothing this sphinx does alone can ever meet
        // one standing. Against a body that raises a measure on its own turn the term is live; here the
        // reachable band is 25 to 31.
        Assert.Equal(25 + 6, Struck(
            (ActFour.EntombedId, 1), (ActFour.BurdenedId, 1), (ActFour.WeighedId, 2)));
    }

    // ── the Tombbreakers Three ────────────────────────────────────────────────────────────────────────────

    // They bring Act III's law into the tomb with them: the Lamp Thief files Trespass, so the room opens the
    // player with the licence that refuses one — the audit is emphatic that nobody arrives with unexplained
    // Act-III resources.
    [Fact]
    public void The_tomb_opens_under_act_three_law()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_elite_the_tombbreakers_three"), health: 900);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.GreenDocketCustomsId));
        Assert.Equal(3, Enemies(play).Count(c => FightProbe.StacksOf(c, ActFour.TombbreakerId) > 0));
        play.Dispose();
    }

    // The veteran takes a Claim on the find, and the standing is what makes it dangerous.
    [Fact]
    public void The_veteran_claims_the_find_and_grows_on_it()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("pry_bar_veteran", "take_the_first_share"), health: 900);

        play.CombatDriver!.EndTurn();
        var veteran = Body(play, "pry_bar_veteran");
        Assert.Equal(1, FightProbe.StacksOf(veteran, ActThree.ClaimId));
        Assert.Equal(2, FightProbe.StacksOf(veteran, "strength"));

        // A second share is not a second claim: it braces instead.
        play.CombatDriver.EndTurn();
        var again = Body(play, "pry_bar_veteran");
        Assert.Equal(1, FightProbe.StacksOf(again, ActThree.ClaimId));
        Assert.Equal(16, BlockOf(again));
        play.Dispose();
    }

    // The thief works in the dark it makes: the same knife lands 5 harder once the lamp is out — once per
    // attack, not once per hit.
    [Fact]
    public void The_lamp_thief_hits_harder_in_the_dark()
    {
        var (lit, _, _) = FightProbe.Start(
            FightProbe.Solo("lamp_thief", "knife_between_stones"), health: 900);
        var before = Hero(lit).Health.Current;
        lit.CombatDriver!.EndTurn();
        Assert.Equal(20, before - Hero(lit).Health.Current);
        lit.Dispose();

        // Two, because a Panic fades at the end of the turn it was carried through — one would be gone
        // before the thief ever swung.
        var (dark, _, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero("lamp_thief", "knife_between_stones", energy: 3, ("panic", 2)),
            health: 900);
        var beforeDark = Hero(dark).Health.Current;
        dark.CombatDriver!.EndTurn();
        Assert.Equal(25, beforeDark - Hero(dark).Health.Current);
        dark.Dispose();
    }

    // A robber down is the tomb closing tighter: the survivors are preserved and strengthened, and the last
    // one standing is a different enemy from the one the fight opened with.
    [Fact]
    public void Every_death_preserves_the_survivors_and_the_last_one_is_worse()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("tombbreakers", energy: 30,
                ("pry_bar_veteran", "take_the_first_share", 12),
                ("lamp_thief", "snuff_the_lamp", 12),
                ("curse_bearer", "throw_the_idol", null)),
            deck: [.. Enumerable.Repeat(OneCost, 40)], health: 900);

        var veteran = Body(play, "pry_bar_veteran").Id;
        for (var i = 0; i < 2; i++)
            Play(play, session, OneCost, veteran);  // 12 HP: two cuts

        var bearer = Body(play, "curse_bearer");
        Assert.Equal(1, FightProbe.StacksOf(bearer, ActFour.TombPreservedId));
        Assert.Equal(1, FightProbe.StacksOf(bearer, "strength"));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));

        var thief = Body(play, "lamp_thief").Id;
        for (var i = 0; i < 2; i++)
            Play(play, session, OneCost, thief);

        // One left: the tomb has its answer, and so does the player.
        var last = Body(play, "curse_bearer");
        Assert.Equal(2, FightProbe.StacksOf(last, ActFour.TombPreservedId));
        Assert.Equal(2 + 2, FightProbe.StacksOf(last, "strength"));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }
}
