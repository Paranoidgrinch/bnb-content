using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, boss — The First Scribe of the House of Life, proved in live fights.
//
// The player writes his next turn. The tests follow the record: what gets written, what a written record
// costs when it is read back, what scraping an entry buys and what it costs, and what changes about all of
// that once the scroll has been written over.
public class ActFourBossScribeTests
{
    private const string Cut = "paper_cut";      // Deed, 1: deal 6
    private const string Wax = "waxen_surety";   // Working, 1: gain 4 Ward Wax

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Scribe(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("scribe", StringComparison.Ordinal));

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Entry(RunPlayback play, string slot) => FightProbe.StacksOf(Scribe(play), slot);

    private static IReadOnlyList<string> InHand(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.Hand.Select(c => c.DefinitionId.value)];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // Seal the Scroll is the one intent of his that does the player no damage, which makes it the honest
    // yardstick: everything the player loses in these fights is the record being read back.
    private const string Quiet = "seal_the_scroll";

    // Three Deeds written down are three Deeds read back — 6 apiece, at the end of HIS window, after the
    // intent that was telegraphed.
    [Fact]
    public void The_first_three_cards_are_written_down_and_read_back()
    {
        var (play, session, scribe) = FightProbe.Start(
            FightProbe.Solo(ActFour.ScribeEnemyId, Quiet, energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        for (var i = 0; i < 3; i++)
            Play(play, session, Cut, scribe);

        // The tablet carries the KIND of each card: 1 is a Deed.
        Assert.Equal(1, Entry(play, ActFour.FirstEntryId));
        Assert.Equal(1, Entry(play, ActFour.SecondEntryId));
        Assert.Equal(1, Entry(play, ActFour.ThirdEntryId));

        var whole = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(3 * 6, whole - Hero(play).Health.Current);

        // …and the scroll is clean again for the next turn.
        Assert.Equal(0, Entry(play, ActFour.FirstEntryId));
        play.Dispose();
    }

    // A recorded Working is his Block and your Paperwork; a recorded anything-else is one Strength for him —
    // once per tablet, however many are written — and Inscribed for you every time.
    [Fact]
    public void A_recorded_working_arms_him_and_files_you()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.ScribeEnemyId, Quiet, energy: 9),
            deck: [.. Enumerable.Repeat(Wax, 10)], health: 900);

        for (var i = 0; i < 3; i++)
            Play(play, session, Wax, null);

        Assert.Equal(2, Entry(play, ActFour.SecondEntryId));

        var whole = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        // Nothing recorded struck, and the seal's own 24 stands on top of three Workings' 6.
        Assert.Equal(whole, Hero(play).Health.Current);
        Assert.Equal(24 + (3 * 6), Block(Scribe(play)));
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Paperwork));
        play.Dispose();
    }

    // Scraping is offered the moment the first entry is written, blanks exactly one slot for a sheet of
    // Paperwork, and is spent for the turn the moment it is used.
    [Fact]
    public void One_entry_a_turn_may_be_scraped_for_a_sheet()
    {
        var (play, session, scribe) = FightProbe.Start(
            FightProbe.Solo(ActFour.ScribeEnemyId, Quiet, energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        // Nothing written, nothing to scrape.
        Assert.DoesNotContain(ActFour.ScrapeFirstCardId, InHand(play));

        Play(play, session, Cut, scribe);
        var hand = InHand(play);
        Assert.Contains(ActFour.ScrapeFirstCardId, hand);
        Assert.Contains(ActFour.ScrapeSecondCardId, hand);
        Assert.Contains(ActFour.ScrapeThirdCardId, hand);

        Play(play, session, Cut, scribe);
        Play(play, session, Cut, scribe);

        Play(play, session, ActFour.ScrapeSecondCardId, null);
        Assert.Equal(0, Entry(play, ActFour.SecondEntryId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Paperwork));

        // One a turn: the second sheet is a dead sheet.
        Play(play, session, ActFour.ScrapeThirdCardId, null);
        Assert.Equal(1, Entry(play, ActFour.ThirdEntryId));

        var whole = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        // Two Deeds read back, the blank between them read back as Doubt — and the sheet the scraping cost
        // ticks once at the player's own turn end before any of that.
        Assert.Equal((2 * 6) + 1, whole - Hero(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Doubt));
        play.Dispose();
    }

    // Two whole tablets read and the scroll is scraped down and written over: from then on he keeps the LAST
    // three cards of the turn, and the final entry of each tablet is inherited into the next.
    [Fact]
    public void Two_whole_tablets_write_the_scroll_over()
    {
        var (play, session, scribe) = FightProbe.Start(
            FightProbe.Solo(ActFour.ScribeEnemyId, Quiet, energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 10).Concat(Enumerable.Repeat(Wax, 10))], health: 900);

        for (var i = 0; i < 3; i++)
            Play(play, session, Cut, scribe);
        play.CombatDriver!.EndTurn();

        // The second tablet ends on a Deed, and a Deed is what the new scroll inherits.
        Play(play, session, Wax, null);
        Play(play, session, Cut, scribe);
        Play(play, session, Cut, scribe);
        play.CombatDriver.EndTurn();

        Assert.True(Scribe(play).Statuses.Any(
            s => s.DefinitionId == new StatusDefinitionId(ActFour.PalimpsestId)));
        Assert.Equal(1, Entry(play, ActFour.InheritedEntryId));

        // Four cards now, and only the last three are kept: the leading Deed falls off the end and the
        // tablet is three Workings.
        Play(play, session, Cut, scribe);
        for (var i = 0; i < 3; i++)
            Play(play, session, Wax, null);

        Assert.Equal(2, Entry(play, ActFour.FirstEntryId));
        Assert.Equal(2, Entry(play, ActFour.SecondEntryId));
        Assert.Equal(2, Entry(play, ActFour.ThirdEntryId));

        play.CombatDriver.EndTurn();

        // …and the tablet just read leaves its own final entry behind, which is now a Working.
        Assert.Equal(2, Entry(play, ActFour.InheritedEntryId));
        play.Dispose();
    }

    // 290 is the failsafe on the palimpsest: a fight that never let two whole tablets be written still meets
    // the second half of him.
    [Fact]
    public void The_scroll_is_written_over_at_two_hundred_and_ninety()
    {
        var (play, session, scribe) = FightProbe.Start(
            FightProbe.Roster("scribe_failsafe", energy: 9,
                (ActFour.ScribeEnemyId, Quiet, 292)),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        Assert.False(Scribe(play).Statuses.Any(
            s => s.DefinitionId == new StatusDefinitionId(ActFour.PalimpsestId)));

        Play(play, session, Cut, scribe);   // 292 → 286

        Assert.True(Scribe(play).Statuses.Any(
            s => s.DefinitionId == new StatusDefinitionId(ActFour.PalimpsestId)));
        Assert.Equal(16, Block(Scribe(play)));

        // Nothing whole was ever read, so there is nothing to inherit.
        Assert.Equal(0, Entry(play, ActFour.InheritedEntryId));
        play.Dispose();
    }

    // Below 100 the text is declared canon. It is announced on the turn his blood says so, it BINDS the turn
    // after — no scraping at all — and then he reads it out for 24 and the sheets come back.
    [Fact]
    public void The_canon_closes_the_scraping_for_one_whole_turn()
    {
        var (play, session, scribe) = FightProbe.Start(
            FightProbe.Roster("scribe_canon", energy: 9,
                (ActFour.ScribeEnemyId, Quiet, 102)),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        // The turn it is announced in is a turn the player was already standing in: the sheets still come.
        Play(play, session, Cut, scribe);   // 102 → 96
        Assert.Contains(ActFour.ScrapeFirstCardId, InHand(play));
        play.CombatDriver!.EndTurn();

        // The turn after is the bound one, and it says so on him.
        Assert.True(Scribe(play).Statuses.Any(
            s => s.DefinitionId == new StatusDefinitionId(ActFour.TextIsCanonId)));
        Play(play, session, Cut, scribe);
        Assert.DoesNotContain(ActFour.ScrapeFirstCardId, InHand(play));

        var whole = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();

        // His answer is the canon read out — 24 — and the record under it. At 102 he is long past 290, so
        // this is a palimpsest: the entry just written AND the Deed inherited from the turn before.
        Assert.Equal(24 + 6 + 6, whole - Hero(play).Health.Current);
        Assert.False(Scribe(play).Statuses.Any(
            s => s.DefinitionId == new StatusDefinitionId(ActFour.TextIsCanonId)));

        // …and with the announcement spent, the sheets are back.
        Play(play, session, Cut, scribe);
        Assert.Contains(ActFour.ScrapeFirstCardId, InHand(play));
        play.Dispose();
    }

    // Correct the Margin rewrites the earliest entry it can into what the LAST card of the turn was — the
    // only correction a record admits — and a scroll with nothing else represented on it gets the reed.
    [Fact]
    public void He_corrects_the_earliest_entry_into_the_last_kind_played()
    {
        var (play, session, scribe) = FightProbe.Start(
            FightProbe.Solo(ActFour.ScribeEnemyId, "correct_the_margin", energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 6).Concat(Enumerable.Repeat(Wax, 6))], health: 900);

        Play(play, session, Cut, scribe);   // a Deed first…
        Play(play, session, Wax, null);     // …and a Working last

        var whole = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        // The first entry was corrected into a Working before it was read: no blow at all, and his Block and
        // your Paperwork twice over.
        Assert.Equal(whole, Hero(play).Health.Current);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Paperwork));
        Assert.Equal(2 * 6, Block(Scribe(play)));
        play.Dispose();
    }

    // Nothing written is nothing to correct — and an intent the engine has already reached cannot step
    // aside, so he uses the reed instead.
    [Fact]
    public void An_uncorrectable_margin_gets_the_reed()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo(ActFour.ScribeEnemyId, "correct_the_margin", energy: 9),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        var whole = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        // 12 for the empty margin, and three blank entries read back as three Doubt.
        Assert.Equal(12, whole - Hero(play).Health.Current);
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Doubt));
        play.Dispose();
    }
}
