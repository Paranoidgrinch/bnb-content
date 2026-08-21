using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The After-Hours Return Bell: a debt engine that prints its own counterplay. Every Overdue it files comes
// with a Return Receipt; two collected Overdue become a Late Fee it keeps; the signature cashes every fee at
// once. These tests walk that loop from both ends — what the Bell charges, and what the Receipt can pay.
public class ReturnBellTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Receipts(RunPlayback play, CardZone zone) =>
        play.CombatDriver!.Current!.State
            .GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(zone)
            .Count(c => c.DefinitionId == new CardDefinitionId(ReturnBell.ReceiptCardId));

    // 5.2 Proof of Return: the Bell cannot file a debt without also handing over the paperwork that undoes it.
    [Fact]
    public void Every_overdue_the_bell_files_comes_with_a_receipt()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "issue_the_closing_notice"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        Assert.Equal(1, Receipts(play, CardZone.DiscardPile));
        play.Dispose();
    }

    // "Maximum simultaneous Bell-generated Receipts: 3. If three already exist, the Overdue still applies
    // normally, but no fourth Receipt is created." Both halves of that sentence, in one fight.
    [Fact]
    public void The_bell_prints_three_receipts_and_no_more_but_keeps_filing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "issue_the_closing_notice"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);

        for (var i = 0; i < 5; i++)
            play.CombatDriver!.EndTurn();

        var live = Receipts(play, CardZone.Hand) + Receipts(play, CardZone.DrawPile)
            + Receipts(play, CardZone.DiscardPile);
        Assert.Equal(3, live);
        // …and the filing never stopped: five Overdue issued, four of them collected by the Bell at its own
        // turn start (twice, two at a time), one still standing. The printer's ceiling is not the ledger's.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        play.Dispose();
    }

    // 5.3: two of the Bell's Overdue collecting through the normal Delinquency rule also books a Late Fee.
    [Fact]
    public void Collecting_two_overdue_books_a_late_fee()
    {
        var (play, _, enemyId) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "issue_the_closing_notice"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn(); // two Overdue standing
        play.CombatDriver.EndTurn(); // the Bell's turn opens: it collects

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), ReturnBell.LateFeeId));
        play.Dispose();
    }

    // "Maximum: 3 Late Fees." Enough turns to book five; the ledger stops at three.
    [Fact]
    public void The_ledger_stops_at_three_late_fees()
    {
        var (play, _, enemyId) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "issue_the_closing_notice"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);

        for (var i = 0; i < 12; i++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(3, FightProbe.StacksOf(Enemy(play, enemyId), ReturnBell.LateFeeId));
        play.Dispose();
    }

    // 5.4 Signature — Toll for Every Unreturned Thing: 14 + 4 per Late Fee, then the fees are gone. Proved at
    // both ends of the scale, because a toll that never varies is just an attack.
    [Theory]
    [InlineData(0, 14)]
    [InlineData(1, 18)]
    [InlineData(2, 22)]
    [InlineData(3, 26)]
    public void The_toll_charges_for_every_fee_and_then_clears_them(int fees, int expected)
    {
        var (play, _, enemyId) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "toll_for_every_unreturned_thing",
                (ReturnBell.LateFeeId, fees)),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(expected, before - Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), ReturnBell.LateFeeId));
        play.Dispose();
    }

    // FILE THE RECEIPT: one Overdue struck, and the Bell pays five HP for having filed it. The design calls
    // that "direct HP Loss, not a Damage event" — so the Bell's Block does not stop it and does not shrink.
    [Fact]
    public void Filing_the_receipt_strikes_a_debt_and_costs_the_bell_five_hp_through_block()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "reopen_for_one_final_minute",
                (ActTwo.OverdueId, 1)),
            deck: [.. Enumerable.Repeat(ReturnBell.ReceiptCardId, 12)], health: 400);

        play.CombatDriver!.EndTurn(); // the Bell guards: 18 Block
        var bell = Enemy(play, enemyId);
        var hpBefore = bell.Health.Current;
        var blockBefore = bell.DefensivePools[StandardCombatIds.BlockDefensivePool].Current;
        Assert.Equal(18, blockBefore);

        var receipt = play.CombatDriver.Current!.Hand
            .First(c => c.DefinitionId == new CardDefinitionId(ReturnBell.ReceiptCardId));
        play.CombatDriver.PlayCard(receipt.Id, enemyId);
        Assert.Equal(["file the receipt", "contest the fee"], play.CombatDriver.PendingOptionChoice);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.Null(session.Error);

        var after = Enemy(play, enemyId);
        Assert.Equal(5, hpBefore - after.Health.Current);
        Assert.Equal(blockBefore, after.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        play.Dispose();
    }

    // CONTEST THE FEE: the fee is struck from the record, and the record is worse for it — a draw-pile card
    // comes back Misfiled.
    [Fact]
    public void Contesting_the_fee_strikes_it_and_misfiles_a_card()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "reopen_for_one_final_minute",
                (ReturnBell.LateFeeId, 2)),
            deck: [.. Enumerable.Repeat(ReturnBell.ReceiptCardId, 12)], health: 400);

        var receipt = play.CombatDriver!.Current!.Hand
            .First(c => c.DefinitionId == new CardDefinitionId(ReturnBell.ReceiptCardId));
        play.CombatDriver.PlayCard(receipt.Id, enemyId);
        play.CombatDriver.SupplyOptionChoice([1]);
        Assert.Null(session.Error);

        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemyId), ReturnBell.LateFeeId));
        var misfiled = play.CombatDriver.Current!.State
            .GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(CardZone.DrawPile)
            .Count(c => c.Marks.Contains(new TagId(ActTwo.MisfiledMark)));
        Assert.Equal(1, misfiled);
        play.Dispose();
    }

    // With no fee on the record there is nothing to contest, and the card says so by doing nothing — no
    // misfiling, no phantom deduction. (The design makes the option "unavailable"; see ADAPTATIONS.)
    [Fact]
    public void Contesting_nothing_does_nothing()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "reopen_for_one_final_minute"),
            deck: [.. Enumerable.Repeat(ReturnBell.ReceiptCardId, 12)], health: 400);

        var receipt = play.CombatDriver!.Current!.Hand
            .First(c => c.DefinitionId == new CardDefinitionId(ReturnBell.ReceiptCardId));
        play.CombatDriver.PlayCard(receipt.Id, enemyId);
        play.CombatDriver.SupplyOptionChoice([1]);
        Assert.Null(session.Error);

        var misfiled = play.CombatDriver.Current!.State
            .GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(CardZone.DrawPile)
            .Count(c => c.Marks.Contains(new TagId(ActTwo.MisfiledMark)));
        Assert.Equal(0, misfiled);
        play.Dispose();
    }

    // The Receipt is Retain · Exhaust: it survives the turn's end in hand, and playing it removes it from the
    // fight for good — which is also what frees a slot under the three-receipt ceiling.
    [Fact]
    public void The_receipt_is_retained_and_then_exhausted()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(ReturnBell.EnemyId, "reopen_for_one_final_minute"),
            deck: [.. Enumerable.Repeat(ReturnBell.ReceiptCardId, 12)], health: 400);

        var held = Receipts(play, CardZone.Hand);
        play.CombatDriver!.EndTurn();
        // Retain: nothing was discarded at the turn's end — the hand kept every Receipt and drew on top of it.
        Assert.Equal(0, Receipts(play, CardZone.DiscardPile));
        Assert.True(Receipts(play, CardZone.Hand) >= held);

        var receipt = play.CombatDriver.Current!.Hand
            .First(c => c.DefinitionId == new CardDefinitionId(ReturnBell.ReceiptCardId));
        play.CombatDriver.PlayCard(receipt.Id, enemyId);
        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.Null(session.Error);

        Assert.Equal(1, Receipts(play, CardZone.ExhaustPile));
        play.Dispose();
    }
}
