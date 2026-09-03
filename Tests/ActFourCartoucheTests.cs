using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stage 15 — The Cartouche Chambers, proved in live fights.
//
// Two bodies decide what a blessing of yours is for. The chisel decides it was a mistake in the stone and
// strikes it out before it happens; the wall decides it was always ancestral property and enters it in the
// lineage. The tests follow the distinction the whole stage rests on — ERASED is not the same as taken away,
// and what the wall takes is the fact of a blessing, never its stacks.
public class ActFourCartoucheTests
{
    private const string Wax = "waxen_surety";  // Working, 1: gain 4 Ward Wax

    private static readonly BabData Data = BabData.Load(TestData.Directory);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, null);
        Assert.True(session.Error is null, session.Error);
    }

    private static IReadOnlyList<string> WaxDeck => [.. Enumerable.Repeat(Wax, 12)];

    private static int BlockOf(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;

    // ── the Name-Erasing Chisel Spirit ────────────────────────────────────────────────────────────────────

    // The chisel is set from the first bell. A round top-up alone would miss the opening round — a fight's
    // first round starts before its bodies are dressed — so the spirit serves it with the fight.
    [Fact]
    public void The_chisel_is_set_against_your_name_from_the_first_bell()
    {
        var (chamber, _, _) = FightProbe.Start(FightProbe.Authored("labyrinth_cartouche_01"), health: 400);
        Assert.Equal(1, FightProbe.StacksOf(Hero(chamber), ActFour.ChiselSetId));
        chamber.Dispose();

        var (wall, _, _) = FightProbe.Start(FightProbe.Authored("labyrinth_cartouche_02"), health: 400);
        Assert.Equal(0, FightProbe.StacksOf(Hero(wall), ActFour.ChiselSetId));
        wall.Dispose();
    }

    // ERASED, not removed: the blessing never lands at all, and a doubt is cut where it would have been. The
    // second blessing of the same round is untouched — the chisel is one cut a round.
    [Fact]
    public void The_first_blessing_of_a_round_is_never_gained_and_the_second_is()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("name_erasing_chisel_spirit", "stone_dust"), deck: WaxDeck, health: 800);

        Play(play, session, Wax);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.ChiselSetId));

        Play(play, session, Wax);
        Assert.Equal(4, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        play.Dispose();
    }

    // …and it is set again when the round turns. One cut a round, every round the spirit is standing.
    [Fact]
    public void The_chisel_is_set_again_each_round()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("name_erasing_chisel_spirit", "stone_dust"), deck: WaxDeck, health: 800);

        Play(play, session, Wax);              // erased
        play.CombatDriver!.EndTurn();          // the round turns
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.ChiselSetId));

        Play(play, session, Wax);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        play.Dispose();
    }

    // ── the Royal Genealogy Wall ──────────────────────────────────────────────────────────────────────────

    // The lineage takes the first blessing you actually gain each round, worth its stacks up to three — and
    // you keep every one of them. Nothing is stolen; what the wall takes is the fact of it.
    [Fact]
    public void The_first_blessing_of_a_round_is_entered_in_the_lineage_and_you_keep_your_own()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("royal_genealogy_wall", "dynastic_rebuke"), deck: WaxDeck, health: 800);

        Play(play, session, Wax);
        Assert.Equal(4, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(ActFour.RoyalFavorCap,
            FightProbe.StacksOf(Body(play, "royal_genealogy_wall"), ActFour.RoyalFavorId));

        // A second blessing the same round is not a second entry: the lineage records one a round.
        Play(play, session, Wax);
        Assert.Equal(ActFour.RoyalFavorCap,
            FightProbe.StacksOf(Body(play, "royal_genealogy_wall"), ActFour.RoyalFavorId));
        play.Dispose();
    }

    // Royal retaliation: the lineage is cashed in — three damage a Favor — and the wall is a plain wall again
    // until the next blessing feeds it.
    [Fact]
    public void The_lineage_is_spent_on_retaliation_and_then_it_is_gone()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("royal_genealogy_wall", "ancestral_claim"), deck: WaxDeck, health: 800);

        var before = Hero(play).Health.Current;
        Play(play, session, Wax);
        play.CombatDriver!.EndTurn();

        // 20 + 3 per Favor, and 4 Ward Wax standing in front of it.
        var wall = Body(play, "royal_genealogy_wall");
        Assert.Equal(0, FightProbe.StacksOf(wall, ActFour.RoyalFavorId));
        Assert.True(before - Hero(play).Health.Current > 20,
            $"the claim struck for {before - Hero(play).Health.Current}, which is no better than an unfed wall");
        play.Dispose();
    }

    // …and the same lineage spent the other way, on the wall the dynasty puts between you and it.
    [Fact]
    public void The_lineage_is_spent_on_defence_and_then_it_is_gone()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("royal_genealogy_wall", "royal_line"), deck: WaxDeck, health: 800);

        Play(play, session, Wax);
        play.CombatDriver!.EndTurn();

        var wall = Body(play, "royal_genealogy_wall");
        Assert.Equal(32 + (4 * ActFour.RoyalFavorCap), BlockOf(wall));
        Assert.Equal(0, FightProbe.StacksOf(wall, ActFour.RoyalFavorId));
        play.Dispose();
    }

    // The telegraph carries the whole formula on BOTH sides of the ledger. A scaling defence had no way to
    // say what it came to before — the program could compute it, the intent line showed only the floor — and
    // a status id of more than one word was printed raw into it ("Royal_favor"), which is the line a player
    // is meant to plan against.
    [Fact]
    public void The_royal_line_telegraphs_its_whole_formula()
    {
        var wall = Data.Enemies.Single(e => e.Id == "royal_genealogy_wall");
        var actions = EnemyMapper.MapActions([wall]);

        Assert.Equal("Royal Line · 32 block +4 per own Royal Favor (max +12)",
            Assert.Single(actions, a => a.Id == "royal_genealogy_wall.royal_line").Intent.Label);
        Assert.Equal("Ancestral Claim · 20 dmg +3 per own Royal Favor (max +9)",
            Assert.Single(actions, a => a.Id == "royal_genealogy_wall.ancestral_claim").Intent.Label);
    }

    // ── §3.8, in one room ─────────────────────────────────────────────────────────────────────────────────

    // The whole point of Encounter 49, and it needs no priority table: a blessing the chisel erases raises no
    // gain at all, so the lineage is fed nothing by it — and a later one that survives the same round still
    // counts as the first the wall hears. The player can spend the chisel on a blessing they were willing to
    // lose, which is the choice the stage exists to offer.
    [Fact]
    public void An_erased_blessing_feeds_the_lineage_nothing_and_a_later_one_still_may()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("cartouche_duo", energy: 3,
                ("name_erasing_chisel_spirit", "stone_dust", null),
                ("royal_genealogy_wall", "royal_line", null)),
            deck: WaxDeck, health: 800);

        Play(play, session, Wax);  // struck out: never gained, so the lineage hears nothing
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, "royal_genealogy_wall"), ActFour.RoyalFavorId));

        Play(play, session, Wax);  // …and the one that survives is still the first the wall hears
        Assert.Equal(4, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(ActFour.RoyalFavorCap,
            FightProbe.StacksOf(Body(play, "royal_genealogy_wall"), ActFour.RoyalFavorId));
        play.Dispose();
    }
}
