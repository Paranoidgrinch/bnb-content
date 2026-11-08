using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — The Hall of Returns, proved in live fights. The act's pressure is SOURCE-BOUND: Overdue is not one
// debt the player owes the room, it is a separate debt owed to each enemy that filed it, and each collects its
// own. These tests are mostly about that word "own".
public class ActTwoStageOneTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    // Each filing is its own instance, because the threshold is "2 from the same source" and merged stacks
    // remember only the last source.
    [Fact]
    public void Every_filing_of_overdue_is_its_own_debt()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("brass_maw_of_returns", "return_intake"));

        play.CombatDriver!.EndTurn(); // Return Intake: 9 damage + 1 Overdue
        play.CombatDriver.EndTurn();  // again

        var overdue = Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActTwo.OverdueId))
            .ToList();
        Assert.Equal(2, overdue.Count);
        Assert.All(overdue, s => Assert.Equal(1, s.Stacks));
        play.Dispose();
    }

    // At 2 owed, the source collects: it takes back what it is owed and files a Paperwork for the trouble.
    [Fact]
    public void At_two_owed_the_source_collects_seven_damage_and_no_paperwork()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("brass_maw_of_returns", "return_intake"));

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn(); // two Overdue standing
        play.CombatDriver.EndTurn(); // the Maw's next turn opens: Delinquency

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        play.Dispose();
    }

    // ── the Returns Window (CONTENT_REVIEW E6, 2026-11-08): Overdue is payable ───────────────────────────────

    private static CardInstance? Slip(RunPlayback play) =>
        play.CombatDriver!.Current!.Hand.FirstOrDefault(c => c.DefinitionId.value == ActTwo.ReturnSlipId);

    private static IReadOnlyList<CardInstance> DrawPile(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(CardZone.DrawPile);

    // A turn that is offered the slip and ends still owing hands the Maw a Return Parcel: +6 on its next attack,
    // which spends it.
    [Fact]
    public void Refusing_to_pay_the_maw_feeds_its_next_bite()
    {
        var (play, _, enemyId) = FightProbe.Start(
            FightProbe.Solo("brass_maw_of_returns", "return_intake"), health: 400);
        Assert.Null(Slip(play));                         // nothing owed yet
        play.CombatDriver!.EndTurn();                    // Return Intake: 9 damage + 1 Overdue
        Assert.NotNull(Slip(play));

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();                     // refused: a parcel, spent by this turn's Intake
        Assert.Equal(before - 9 - ActTwo.ReturnParcelDamage, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), ActTwo.ReturnParcelId));
        play.Dispose();
    }

    // Paying: the chosen card goes on top of the draw pile and 1 Overdue is struck off — and nothing is fed.
    [Fact]
    public void A_return_slip_puts_a_card_back_and_strikes_off_an_overdue()
    {
        // Intake files the Overdue, then the Shutter files nothing — so a parcel, had one been fed, would stand.
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.SoloCycle("brass_maw_of_returns", "return_intake", "brass_shutter"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);
        play.CombatDriver!.EndTurn();                    // 1 Overdue
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));

        play.CombatDriver.PlayCard(Slip(play)!.Id, null);
        Assert.Null(session.Error);
        var offered = play.CombatDriver.PendingCardChoice;
        Assert.NotNull(offered);
        var returned = offered![0].Id;
        play.CombatDriver.SupplyCardChoice([returned]);
        Assert.Null(session.Error);

        Assert.Equal(returned, DrawPile(play)[0].Id);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        Assert.Equal(0, DrawPile(play)[0].GetMarkCounter(StandardCombatIds.CardCostDeltaCounter)); // no Ouroboros here

        play.CombatDriver.EndTurn();                     // Shutter; nothing to feed the Maw
        Assert.Equal(0, FightProbe.StacksOf(Enemy(play, enemyId), ActTwo.ReturnParcelId));
        play.Dispose();
    }

    // Paid to the Ouroboros, the returned card comes back costing 1 more.
    [Fact]
    public void A_card_returned_to_the_ouroboros_costs_one_more()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("dead_letter_ouroboros", "forwarding_loop"),
            deck: [.. Enumerable.Repeat("paper_cut", 12)], health: 400);
        play.CombatDriver!.EndTurn();
        play.CombatDriver.PlayCard(Slip(play)!.Id, null);
        var returned = play.CombatDriver.PendingCardChoice![0].Id;
        play.CombatDriver.SupplyCardChoice([returned]);
        Assert.Null(session.Error);
        Assert.Equal(1, DrawPile(play)[0].GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));

        play.CombatDriver.EndTurn();                     // it is drawn again next turn…
        var card = play.CombatDriver.Current!.Hand.First(c => c.Id == returned);
        var energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
        play.CombatDriver.PlayCard(card.Id, play.CombatDriver.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current.HeroId).Id);
        Assert.Null(session.Error);
        Assert.Equal(energy - 2, Hero(play).Resources[StandardCombatIds.EnergyResource].Current); // …at 2
        play.Dispose();
    }

    // "Whenever this enemy's Delinquency fully resolves: immediately apply 1 new Overdue from this same
    // source." The loop never quite closes.
    [Fact]
    public void The_ouroboros_re_owes_itself_the_moment_it_collects()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("dead_letter_ouroboros", "forwarding_loop"));

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn(); // two Overdue
        play.CombatDriver.EndTurn(); // collected — and immediately re-owed

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Paperwork));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId)); // 1 re-owed + 1 from this turn's intent
        var ouroboros = play.CombatDriver.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current.HeroId);
        Assert.True(ouroboros.DefensivePools[StandardCombatIds.BlockDefensivePool].Current >= 6); // and it guards
        play.Dispose();
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }
}
