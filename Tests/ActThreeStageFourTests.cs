using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III — The Tollwater Crossings, where the act stops asking what you did and names a price for it.
// Wergild is a demand owed to ONE party: its clock, its settlement and its reward all belong to the creditor,
// and these tests are mostly about keeping two creditors' books apart.
public class ActThreeStageFourTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static int OwedTo(RunPlayback play, CombatantId creditor) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId)
                && s.SourceCombatantId == creditor)
            .Sum(s => s.Stacks);

    private static bool HasMakeAmends(RunPlayback play) =>
        play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);

    // Plays Make Amends and answers its choice: 0 = pay in coin, 1 = offer a card.
    private static void MakeAmends(RunPlayback play, InteractiveRunSession session, int option, CombatantId at)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, at);
        Assert.True(session.Error is null, session.Error);

        // The choices park one after another under the replay model, so keep answering until nothing is
        // waiting: which one is pending next depends on the option taken.
        for (var guard = 0; guard < 4; guard++)
        {
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([option]);
            else if (play.CombatDriver.PendingCardChoice is { } cards)
                play.CombatDriver.SupplyCardChoice([cards[0].Id]);
            else
                break;
            Assert.True(session.Error is null, session.Error);
        }
    }

    // ── the demand and its clock ──────────────────────────────────────────────────────────────────────────

    // A demand arrives with the means to answer it: the fight puts Make Amends in the player's hand, and it
    // is still there next turn, because a card that vanished with the hand would be no use at all.
    [Fact]
    public void A_demand_comes_with_the_means_to_answer_it()
    {
        var (play, _, snail) = FightProbe.Start(
            FightProbe.Solo("charter_shell_snail", "charter_toll"));

        Assert.False(HasMakeAmends(play));
        play.CombatDriver!.EndTurn(); // Charter Toll: 11 damage and a demand for 1

        Assert.Equal(1, OwedTo(play, snail));
        Assert.True(HasMakeAmends(play), "the fight hands you the means to pay");
        play.Dispose();
    }

    // Unpaid, a demand costs 2 HP a point and becomes the creditor's standing.
    [Fact]
    public void A_demand_left_owing_costs_health_and_becomes_a_claim()
    {
        var (play, _, snail) = FightProbe.Start(
            FightProbe.Solo("charter_shell_snail", "charter_toll"), health: 200);

        play.CombatDriver!.EndTurn(); // the demand is raised
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();  // it matures at this turn's start and falls due at its end

        // 2 for the unpaid point, plus the Toll's own 11 on the enemy turn that follows.
        Assert.True(Hero(play).Health.Current <= before - 2);
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));
        Assert.Equal(1, OwedTo(play, snail)); // …and the fresh demand from the next Toll
        play.Dispose();
    }

    // Settled in full, it grants Safe-Conduct instead.
    [Fact]
    public void A_demand_settled_in_full_grants_safe_conduct()
    {
        var (play, session, snail) = FightProbe.Start(
            FightProbe.Solo("charter_shell_snail", "charter_toll", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 5)], health: 200);

        play.CombatDriver!.EndTurn(); // demand raised; the opening licence is untouched, nothing refused it
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        MakeAmends(play, session, option: 0, at: snail); // pay in coin
        Assert.Equal(0, OwedTo(play, snail));

        play.CombatDriver.EndTurn(); // it falls due, and finds nothing owing

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));
        play.Dispose();
    }

    // The card comes back for as long as anything is still owed, and stops coming back when nothing is.
    [Fact]
    public void The_means_to_pay_lasts_exactly_as_long_as_the_debt()
    {
        var (play, session, ford) = FightProbe.Start(
            FightProbe.Solo("two_bank_toll_ford", "collect_both_banks", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 5)], health: 400);

        // Three Trespass from the Ford become its Claim, and the Claim becomes a demand.
        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));
        Assert.Equal(1, OwedTo(play, ford));

        MakeAmends(play, session, option: 0, at: ford);

        Assert.Equal(0, OwedTo(play, ford));
        Assert.False(HasMakeAmends(play), "nothing is owed, so nothing is offered");
        play.Dispose();
    }

    // ── Two-Bank Toll Ford — Toll on Both Banks ───────────────────────────────────────────────────────────

    // "A recognised right and an active demand are separate legal facts." The Ford keeps the Claim AND
    // charges for it.
    [Fact]
    public void The_ford_keeps_its_claim_and_charges_for_it()
    {
        var (play, _, ford) = FightProbe.Start(
            FightProbe.Solo("two_bank_toll_ford", "collect_both_banks"), health: 400);

        for (var turn = 0; turn < 4; turn++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId)); // not spent
        Assert.Equal(1, OwedTo(play, ford));                                      // and charged for
        play.Dispose();
    }

    // ── Charter-Shell Snail — Payment According to Charter ────────────────────────────────────────────────

    // The charter is written on the shell: a card that costs nothing is not an offering.
    [Fact]
    public void A_free_card_is_no_offering_under_the_charter()
    {
        var (play, session, snail) = FightProbe.Start(
            FightProbe.Solo("charter_shell_snail", "charter_toll", energy: 9),
            // Red Tape is Junk and costs nothing; Paper Cut costs 1.
            deck: ["red_tape", "red_tape", "red_tape", "red_tape", "red_tape"], health: 200);

        play.CombatDriver!.EndTurn();
        Assert.Equal(1, OwedTo(play, snail));

        MakeAmends(play, session, option: 1, at: snail); // offer a card — and every card here is free

        Assert.Equal(1, OwedTo(play, snail)); // the shell does not accept it
        play.Dispose();
    }

    // The offering is never Make Amends itself: it is still in hand while its own program runs, and being a
    // free card the charter would refuse it — a trap rather than a decision.
    [Fact]
    public void Make_amends_never_offers_itself_as_the_payment()
    {
        var (play, session, snail) = FightProbe.Start(
            FightProbe.Solo("charter_shell_snail", "charter_toll", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 5)], health: 200);

        play.CombatDriver!.EndTurn();
        var card = play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, snail);
        play.CombatDriver.SupplyOptionChoice([1]); // offer a card

        Assert.DoesNotContain(play.CombatDriver.PendingCardChoice!,
            c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.Dispose();
    }

    [Fact]
    public void A_card_that_cost_something_settles_the_charter()
    {
        var (play, session, snail) = FightProbe.Start(
            FightProbe.Solo("charter_shell_snail", "charter_toll", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 5)], health: 200);

        play.CombatDriver!.EndTurn();
        MakeAmends(play, session, option: 1, at: snail); // offer a Paper Cut, which costs 1

        Assert.Equal(0, OwedTo(play, snail));
        play.Dispose();
    }

    // ── Streamside Oath-Fish — Oath Accepted ──────────────────────────────────────────────────────────────

    // The Oath-Fish treats restitution as a sacred thing: settling with it is worth twice as much.
    [Fact]
    public void Settling_with_the_oath_fish_is_worth_two_safe_conducts()
    {
        var (play, session, fish) = FightProbe.Start(
            FightProbe.Solo("streamside_oath_fish", "oath_bite", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 5)], health: 200);

        play.CombatDriver!.EndTurn();
        MakeAmends(play, session, option: 0, at: fish);
        play.CombatDriver.EndTurn(); // the demand falls due with nothing owing

        // The one the fight opened with, plus the two the oath is worth.
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── two creditors, two sets of books ──────────────────────────────────────────────────────────────────

    // The whole point of a source-bound demand: paying one party does not pay the other, and each settles
    // its own account when it falls due.
    [Fact]
    public void Two_creditors_keep_separate_books()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("green_docket_tollwater_duo_01", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 5)], health: 400);

        var ford = Enemies(play)[0].Id;
        var fish = Enemies(play)[1].Id;

        play.CombatDriver!.EndTurn(); // the Fish bites and demands; the Ford only trespasses
        Assert.Equal(1, OwedTo(play, fish));
        Assert.Equal(0, OwedTo(play, ford));

        MakeAmends(play, session, option: 0, at: fish); // the oldest demand is the Fish's
        Assert.Equal(0, OwedTo(play, fish));

        play.CombatDriver.EndTurn(); // the Fish's falls due settled: two Safe-Conduct, no Claim

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[1], ActThree.ClaimId));
        Assert.True(FightProbe.StacksOf(Hero(play), ActThree.SafeConductId) >= 2);
        play.Dispose();
    }
}
