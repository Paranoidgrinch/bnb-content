using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Rolling Stacks Colossus: the aisles narrow every time a misfiling is actually skipped, and the card the
// skip hands back is the path out. These tests are about that exchange — what closes the shelves, and what
// opens them again.
public class RollingStacksColossusTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Compression(RunPlayback play, CombatantId id) =>
        FightProbe.StacksOf(Enemy(play, id), RollingStacksColossus.CompressionId);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static CardInstance? OpenAisle(RunPlayback play) =>
        Hand(play).FirstOrDefault(c => c.HasMark(new TagId(RollingStacksColossus.OpenAisleMark)));

    // 6.2/6.3: the Colossus marks two draw-pile cards; they reach the hand at the player's next turn start,
    // are taken back by the act's rule, and it is that SKIP the shelves count — one aisle closed per skip, and
    // one path handed back per skip.
    [Fact]
    public void Every_skipped_misfiling_narrows_the_aisles_and_opens_one()
    {
        var (play, _, colossus) = FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, "misfile_an_entire_section"),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        play.CombatDriver!.EndTurn(); // two cards marked; the player's next draw brings both up

        // Two skips, so two aisles closed — which is also the proof that the intent marked two DIFFERENT
        // cards: one card marked twice would have been one skip.
        Assert.Equal(2, Compression(play, colossus));
        Assert.Equal(2, Hand(play).Count(c => c.HasMark(new TagId(RollingStacksColossus.OpenAisleMark))));
        // The skipped cards themselves are in the discard, and no longer misfiled.
        var zones = play.CombatDriver.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);
        Assert.Empty(zones.GetCardsInZone(CardZone.Hand)
            .Where(c => c.HasMark(new TagId(ActTwo.MisfiledMark))));
        play.Dispose();
    }

    // 6.2: "Maximum 3." The Archive cannot compress past its own ceiling however much is misfiled.
    [Fact]
    public void The_aisles_never_close_past_three()
    {
        var (play, _, colossus) = FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, "misfile_an_entire_section"),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        for (var i = 0; i < 10; i++)
            play.CombatDriver!.EndTurn();

        Assert.True(Compression(play, colossus) <= 3);
        play.Dispose();
    }

    // 6.3: playing the exact card the skip handed back pushes the walls out by one — and only once a turn,
    // however many paths are open.
    [Fact]
    public void Walking_an_open_aisle_pushes_the_walls_back_once_a_turn()
    {
        var (play, session, colossus) = FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, "misfile_an_entire_section", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();

        var before = Compression(play, colossus);
        Assert.True(before >= 1);

        var first = OpenAisle(play);
        Assert.NotNull(first);
        play.CombatDriver.PlayCard(first!.Id, colossus);
        Assert.Null(session.Error);
        Assert.Equal(before - 1, Compression(play, colossus));

        // A second path walked in the same turn is still a path — but the walls have already given today.
        if (OpenAisle(play) is { } second)
        {
            var afterFirst = Compression(play, colossus);
            play.CombatDriver.PlayCard(second.Id, colossus);
            Assert.Null(session.Error);
            Assert.Equal(afterFirst, Compression(play, colossus));
        }

        play.Dispose();
    }

    // "If the card leaves hand without being played, Open Aisle ends" — the path is a path for that turn only.
    // The sweep runs at the player's turn start, before the turn's draw, so yesterday's marks are gone.
    [Fact]
    public void A_path_not_walked_is_gone_by_the_next_turn()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, "misfile_an_entire_section"),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        var opened = OpenAisle(play);
        Assert.NotNull(opened);

        play.CombatDriver.EndTurn(); // the turn ends unwalked

        var zones = play.CombatDriver.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);
        var stale = Enum.GetValues<CardZone>()
            .SelectMany(zones.GetCardsInZone)
            .Where(c => c.Id == opened!.Id && c.HasMark(new TagId(RollingStacksColossus.OpenAisleMark)));
        Assert.Empty(stale);
        play.Dispose();
    }

    // 6.4 Close the Remaining Passage: 12 + 5 per Compression. With the aisles open it is the floor value.
    [Fact]
    public void The_passage_closes_by_twelve_when_nothing_is_compressed()
    {
        var (play, _, colossus) = FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, "close_the_remaining_passage"),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        play.CombatDriver!.EndTurn();

        Assert.Equal(12, Enemy(play, colossus).DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
        play.Dispose();
    }

    // …and at Compression 2 it is 22. (At 3 the intent is no longer itself — see below.)
    [Fact]
    public void The_passage_closes_harder_the_narrower_the_aisles()
    {
        var (play, _, colossus) = FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, "close_the_remaining_passage",
                (RollingStacksColossus.CompressionId, 2)),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        play.CombatDriver!.EndTurn();

        Assert.Equal(22, Enemy(play, colossus).DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
        play.Dispose();
    }

    // Signature — Shelf Collapse. At Compression 3 whatever the Colossus was going to do becomes the collapse:
    // 23 damage, two more misfilings, and the aisles open again. Proved on an intent that normally deals none.
    [Fact]
    public void At_three_the_next_intent_is_the_collapse_whatever_it_was()
    {
        var (play, _, colossus) = FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, "close_the_remaining_passage",
                (RollingStacksColossus.CompressionId, 3)),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        // The block intent dealt 23 damage and gained nothing.
        Assert.Equal(23, before - Hero(play).Health.Current);
        Assert.Equal(0, Enemy(play, colossus).DefensivePools
            .TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0);
        // …and the shelves rolled back open — then closed again by two, because the collapse's OWN two
        // misfilings came up on the very next draw and were skipped. The cash-out re-seeds itself; what the
        // player buys by walking an aisle is the turn, not the loop.
        Assert.Equal(2, Compression(play, colossus));
        // The collapse's two misfilings are not in the draw pile any more — they were drawn, taken back, and
        // replaced. The two paths in hand are what is left of them.
        Assert.Equal(2, Hand(play).Count(c => c.HasMark(new TagId(RollingStacksColossus.OpenAisleMark))));
        play.Dispose();
    }

    // 6.4 Displace the Ladder: "the NEXT Open-Aisle replacement card this player turn costs +1 Energy —
    // maximum one card." Walked through the elite's real intent cycle, because the tax and the path it taxes
    // are two different intents apart.
    [Fact]
    public void Displacing_the_ladder_taxes_the_next_path_and_only_the_next_one()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("archives_elite_rolling_stacks_colossus", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

        // The cycle: Roll (misfiles one) · Close the Passage · Displace the Ladder · Stone-Wheel Crush ·
        // Misfile an Entire Section. Five turns brings the section's two misfilings up on the sixth draw,
        // with the ladder displaced two intents earlier and still pending.
        for (var i = 0; i < 5; i++)
            play.CombatDriver!.EndTurn();

        var paths = Hand(play)
            .Where(c => c.HasMark(new TagId(RollingStacksColossus.OpenAisleMark)))
            .ToList();
        Assert.Equal(2, paths.Count);

        // Exactly one of the two replacements is a rung higher, and it is the first of them.
        Assert.Equal(1, paths[0].GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
        Assert.Equal(0, paths[1].GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
        play.Dispose();
    }

    // "Roll Across the Aisle: mark the last valid card instance played during the previous player turn
    // Misfiled." The wall rolls over the thing you just used.
    [Fact]
    public void The_wall_rolls_over_the_card_you_last_played()
    {
        var (play, session, colossus) = FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, "roll_across_the_aisle", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

        var played = Hand(play)[2].Id;
        play.CombatDriver!.PlayCard(played, colossus);
        Assert.Null(session.Error);

        play.CombatDriver.EndTurn(); // the Colossus rolls

        var zones = play.CombatDriver.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);
        var card = Enum.GetValues<CardZone>()
            .SelectMany(zones.GetCardsInZone)
            .First(c => c.Id == played);
        Assert.True(card.HasMark(new TagId(ActTwo.MisfiledMark)));
        play.Dispose();
    }

    // A turn where nothing was played leaves the wall nothing to roll over — it deals its damage and marks no
    // card at all, rather than picking one at random.
    [Fact]
    public void A_turn_spent_playing_nothing_leaves_the_wall_nothing_to_mark()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(RollingStacksColossus.EnemyId, "roll_across_the_aisle"),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

        play.CombatDriver!.EndTurn(); // nothing was played first

        var zones = play.CombatDriver.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);
        Assert.DoesNotContain(
            Enum.GetValues<CardZone>().SelectMany(zones.GetCardsInZone),
            c => c.HasMark(new TagId(ActTwo.MisfiledMark)));
        play.Dispose();
    }
}
