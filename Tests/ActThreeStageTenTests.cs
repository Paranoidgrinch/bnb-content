using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III — The Court Beneath the Hill. No new universal mechanic: the difficulty is that reciprocity has
// become self-sustaining. A repeated name is both the guilt and the payment, and a coin counts every
// exchange more clearly than the parties who made it.
public class ActThreeStageTenTests
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

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static void MakeAmends(RunPlayback play, InteractiveRunSession session, int option, CombatantId at)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, at);
        for (var guard = 0; guard < 4; guard++)
        {
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([option]);
            else if (play.CombatDriver.PendingCardChoice is { } cards)
                play.CombatDriver.SupplyCardChoice([cards[0].Id]);
            else
                break;
        }
        Assert.True(session.Error is null, session.Error);
    }

    private const string OneCost = "paper_cut";

    // ── Keeper of Buried Names ────────────────────────────────────────────────────────────────────────────

    // A name spoken once is fine. Spoken twice, the hill remembers it.
    [Fact]
    public void Speaking_a_name_a_second_time_owes_the_keeper()
    {
        var (play, session, keeper) = FightProbe.Start(
            FightProbe.Solo("keeper_of_buried_names", "crypt_seal", energy: 9),
            // Five cards, so the deck is the hand and the second turn deals the same five back.
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 400);

        Play(play, session, OneCost, keeper);
        Play(play, session, OneCost, keeper); // a different copy: a different piece of paper
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver!.EndTurn(); // the hand is reshuffled and dealt back

        Play(play, session, OneCost, keeper); // one of them has certainly been played before

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, so it fired
        play.Dispose();
    }

    // Once a turn: the Keeper recognises a name, not every name.
    [Fact]
    public void The_keeper_recognises_one_name_a_turn()
    {
        var (play, session, keeper) = FightProbe.Start(
            FightProbe.Solo("keeper_of_buried_names", "crypt_seal", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 400);

        for (var i = 0; i < 5; i++)
            Play(play, session, OneCost, keeper); // every copy spoken once: nothing to recognise yet
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver!.EndTurn();
        Play(play, session, OneCost, keeper); // a name already spoken — refused by the licence
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver.EndTurn();
        Play(play, session, OneCost, keeper);
        Play(play, session, OneCost, keeper);
        Play(play, session, OneCost, keeper); // three more, all of them already heard

        Assert.Equal(1, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)
                && s.SourceCombatantId == keeper));
        play.Dispose();
    }

    // The paradox the Keeper is given a solo encounter to teach: the same repetition that makes the guilt
    // makes the restitution worth double.
    [Fact]
    public void A_name_already_spoken_pays_twice_as_much()
    {
        var (play, session, keeper) = FightProbe.Start(
            FightProbe.Solo("keeper_of_buried_names", "buried_demand", energy: 9,
                ("claim", 1), ("claim_created", 1)),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 400);

        // Play the whole hand so that every copy has been spoken, then let the demand arrive.
        for (var i = 0; i < 5; i++)
            Play(play, session, OneCost, keeper);
        play.CombatDriver!.EndTurn();

        Assert.Equal(2, OwedTo(play, keeper));

        MakeAmends(play, session, option: 1, at: keeper); // offer a card the hill has already heard

        Assert.Equal(0, OwedTo(play, keeper)); // two points, one offering
        play.Dispose();
    }

    // ── Handworn Tally Coin ───────────────────────────────────────────────────────────────────────────────

    // Spending is not moving and not losing. The Coin counts exactly the one announcement the act keeps for
    // it — and it counts anybody's, which is what makes the loop close on itself.
    [Fact]
    public void A_spent_claim_is_a_notch_on_the_coin()
    {
        // The Keeper opens holding standing, because Buried Demand is the act's plainest way of SPENDING it
        // and the Keeper has to have some to spend.
        var authored = FightProbe.Authored("green_docket_court_duo_02");
        var withStanding = new EncounterDefinition(
            authored.Id,
            [
                authored.Enemies[0] with
                {
                    Actions = [new EnemyActionDefinitionId("keeper_of_buried_names.buried_demand")],
                    StartingStatuses =
                    [
                        .. authored.Enemies[0].StartingStatuses ?? [],
                        new StartingStatusSpec(new StatusDefinitionId(ActThree.ClaimId), 1),
                    ],
                },
                authored.Enemies[1] with
                {
                    Actions = [new EnemyActionDefinitionId("handworn_tally_coin.minted_shelter")],
                },
            ],
            authored.HeroResources, authored.HeroStartingStatuses, authored.HeroDisplayName,
            authored.CardsDrawnPerTurn, authored.TriggeredEffects);

        var (play, _, _) = FightProbe.Start(withStanding, health: 500);

        var keeper = Enemies(play)[0];
        var coin = Enemies(play)[1];
        Assert.Equal(1, FightProbe.StacksOf(keeper, ActThree.ClaimId));
        Assert.Equal(0, FightProbe.StacksOf(coin, ActThree.TallyId));

        play.CombatDriver!.EndTurn(); // Buried Demand: the standing is spent for a price

        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[0], ActThree.ClaimConsumedId));
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[1], ActThree.TallyId));
        play.Dispose();
    }

    // The Coin cannot be paid off and cannot be argued with. It can only be worn down by other people
    // keeping their word.
    [Fact]
    public void Keeping_your_word_wears_the_coin_down()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("court", energy: 9,
                ("charter_shell_snail", "charter_toll", null),
                ("handworn_tally_coin", "minted_shelter", null)),
            deck: [.. Enumerable.Repeat(OneCost, 5)], health: 500);

        play.CombatDriver!.EndTurn(); // the Snail names a price

        var snail = Enemies(play)[0].Id;
        var before = Enemies(play)[1].Health.Current;
        MakeAmends(play, session, option: 0, at: snail); // pay it in coin
        play.CombatDriver.EndTurn();                     // and the demand falls due settled

        Assert.Equal(before - 4, Enemies(play)[1].Health.Current);
        play.Dispose();
    }
}
