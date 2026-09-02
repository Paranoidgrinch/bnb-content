using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stage 2 — The Gate of Counted Names, proved in live fights.
//
// The stage exists so the player cannot learn only half of Inscribed. Three bodies read the register three
// ways: the Pilgrim as a STATE it is subject to, the Cobra as the amplifier (without a line of code of its
// own), and the Baboon as a thing it can watch WORKING and steal from. These tests are about the word "half":
// each one asks whether a body still behaves correctly when the player uses the register the other way.
public class ActFourGateTests
{
    private const string OneCost = "paper_cut";   // Deed, 1
    private const string Wax = "waxen_surety";    // Working, 1: gain 4 Ward Wax

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── the Uncounted Pilgrim ─────────────────────────────────────────────────────────────────────────────

    // It opens Uncounted and hard to hold to account. Its own petition registers the player — and registering
    // the player is what makes it legible, so the body undoes its own protection by asking to be counted.
    [Fact]
    public void The_pilgrim_is_counted_the_moment_the_player_is_in_the_register()
    {
        var (play, _, pilgrim) = FightProbe.Start(
            FightProbe.Solo("uncounted_pilgrim", "petition_entry"));

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.UncountedId));

        play.CombatDriver!.EndTurn(); // Petition Entry: 11 damage and 1 Inscribed

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.UncountedId));
        play.Dispose();
    }

    // …and it goes back the moment the register is spent, which is the decision the stage is built on: the
    // player who spends Inscribed on a blessing of their own hands the Pilgrim its protection back.
    [Fact]
    public void Spending_the_register_hands_the_pilgrim_its_protection_back()
    {
        var (play, session, pilgrim) = FightProbe.Start(
            FightProbe.Solo("uncounted_pilgrim", "petition_entry"),
            deck: [.. Enumerable.Repeat(Wax, 5)]);

        play.CombatDriver!.EndTurn(); // registered
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActFour.UncountedId));

        Play(play, session, Wax, null); // …and the register is spent enlarging the player's own wax

        Assert.Equal(5, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActFour.UncountedId));
        play.Dispose();
    }

    // What being Uncounted is worth: attack damage lands reduced while the body is unregistered, and whole
    // once the player is in the register.
    [Fact]
    public void An_uncounted_body_takes_less_than_a_counted_one()
    {
        int Struck(int inscribed)
        {
            var (play, session, _) = FightProbe.Start(
                FightProbe.SoloAgainstHero("uncounted_pilgrim", "unregistered_shelter", energy: 3,
                    inscribed > 0 ? [(ActFour.InscribedId, inscribed)] : []),
                deck: [.. Enumerable.Repeat(OneCost, 6)]);

            var pilgrim = Enemies(play)[0];
            var before = pilgrim.Health.Current;
            Play(play, session, OneCost, pilgrim.Id);
            var dealt = before - Enemies(play)[0].Health.Current;
            play.Dispose();
            return dealt;
        }

        // The same Deed, twice: reduced against the unregistered body, whole against the counted one — and a
        // player who walks in already inscribed is counted from the opening bell, without an event to hear.
        var uncounted = Struck(inscribed: 0);
        var counted = Struck(inscribed: 1);

        Assert.True(uncounted < counted, $"uncounted {uncounted} should be less than counted {counted}");
    }

    // ── the Cobra of the Entry Mark ───────────────────────────────────────────────────────────────────────

    // The Cobra has no rule of its own: it marks you, and then the register makes its venom land larger all by
    // itself. Its whole signature is the act's vocabulary doing its work.
    [Fact]
    public void The_register_makes_the_venom_land_larger()
    {
        var plain = VenomAgainst(inscribed: 0);
        var marked = VenomAgainst(inscribed: 1);

        Assert.Equal(1, plain);
        Assert.Equal(2, marked);
    }

    private static int VenomAgainst(int inscribed)
    {
        var (play, _, _) = inscribed > 0
            ? FightProbe.Start(FightProbe.SoloAgainstHero(
                "cobra_of_the_entry_mark", "entry_venom", energy: 3, (ActFour.InscribedId, inscribed)))
            : FightProbe.Start(FightProbe.Solo("cobra_of_the_entry_mark", "entry_venom"));

        play.CombatDriver!.EndTurn();
        var poison = FightProbe.StacksOf(Hero(play), "poison");
        play.Dispose();
        return poison;
    }

    // ── the Name-Eating Baboon ────────────────────────────────────────────────────────────────────────────

    // What the Baboon eats is not the register but the register WORKING: it watches Inscribed magnify another
    // party's affliction, and chews that magnification into a name.
    [Fact]
    public void The_baboon_steals_a_name_when_the_register_magnifies_another_partys_affliction()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.RosterAgainstHero("gate_duo_02", 3, [(ActFour.InscribedId, 1)],
                ("cobra_of_the_entry_mark", "entry_venom", null),
                ("name_eating_baboon", "scramble_the_gate", null)));

        play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "poison"));  // the venom landed larger
        Assert.Equal(1, FightProbe.StacksOf(Body(play, "name_eating_baboon"), ActFour.StolenNameId));
        play.Dispose();
    }

    // Once each round, and no more: a second magnification in the same round is watched and not eaten.
    [Fact]
    public void Only_the_first_magnification_of_a_round_is_worth_a_name()
    {
        // Two afflictions from two different parties in one enemy turn — the Cobra's venom and the
        // Surveyor's measure — with register enough to enlarge both.
        var (play, _, _) = FightProbe.Start(
            FightProbe.RosterAgainstHero("gate_and_stele", 3, [(ActFour.InscribedId, 2)],
                ("cobra_of_the_entry_mark", "entry_venom", null),
                ("reed_cord_surveyor", "set_the_measure", null),
                ("name_eating_baboon", "scramble_the_gate", null)));

        play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "poison"));            // the venom was enlarged
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ActFour.WeighedId));   // …and so was the measure
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId)); // both stacks spent
        Assert.Equal(1, FightProbe.StacksOf(Body(play, "name_eating_baboon"), ActFour.StolenNameId));
        play.Dispose();
    }

    // The register spent on a BLESSING feeds the Baboon nothing: what it eats is an affliction made larger.
    [Fact]
    public void A_blessing_made_larger_is_not_a_name_worth_stealing()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.RosterAgainstHero("gate_duo_02", 3, [(ActFour.InscribedId, 1)],
                ("cobra_of_the_entry_mark", "coiled_seal", null),
                ("name_eating_baboon", "scramble_the_gate", null)),
            deck: [.. Enumerable.Repeat(Wax, 6)]);

        Play(play, session, Wax, null); // the register is spent enlarging the player's own wax

        Assert.Equal(5, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, "name_eating_baboon"), ActFour.StolenNameId));
        play.Dispose();
    }

    // Two names buy a forgery — and the whole cycle is proved on the REAL encounter, where the Cobra marks
    // the player itself, so the register is fed and spent by the fight rather than by the test.
    [Fact]
    public void Two_names_buy_a_forgery_that_enlarges_the_next_affliction_and_never_feeds_the_baboon_again()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_gate_duo_02"), health: 600);

        var names = new List<int>();
        var forgeries = new List<int>();
        for (var turn = 0; turn < 6; turn++)
        {
            play.CombatDriver!.EndTurn();
            names.Add(FightProbe.StacksOf(Body(play, "name_eating_baboon"), ActFour.StolenNameId));
            forgeries.Add(FightProbe.StacksOf(Hero(play), ActFour.ForgedEntryId));
        }

        // The names accumulate to two and are then spent on a forgery that lands on the player's own file.
        Assert.Contains(1, names);
        Assert.Contains(1, forgeries);

        // …and it never sits at two: the second name is spent the moment it is stolen.
        Assert.DoesNotContain(2, names);
        play.Dispose();
    }

    // A forgery may never feed the forger: a magnification the Baboon's own paper caused is the copy working,
    // not the register, and §3.4 says a copy can never start another copy chain.
    [Fact]
    public void A_forgery_never_buys_the_baboon_another_name()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.RosterAgainstHero("gate_forgery", 3,
                [(ActFour.ForgedEntryId, 1)],
                ("cobra_of_the_entry_mark", "entry_venom", null),
                ("name_eating_baboon", "scramble_the_gate", null)));

        play.CombatDriver!.EndTurn(); // the venom is enlarged by the FORGERY, there being no register at all

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "poison"));            // enlarged
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.ForgedEntryId)); // and spent doing it
        Assert.Equal(0, FightProbe.StacksOf(Body(play, "name_eating_baboon"), ActFour.StolenNameId));
        play.Dispose();
    }

}
