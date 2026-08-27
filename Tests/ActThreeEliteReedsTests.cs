using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III elite 8 — Three Reeds of Appeal. Standing does not sit where it was made: one Claim travels one
// position down the chain at the end of every enemy turn, and what waits at the end is a demand for three.
// The player edits the route by choosing what to kill.
public class ActThreeEliteReedsTests
{
    private const string Deed = "paper_cut";
    private const string Working = "cower_behind_a_desk";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Reed(RunPlayback play, string marker) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(marker)));

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

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

    // The tribunal as the game fields it, with each reed narrowed to one action and optionally standing
    // already made at the Hearing.
    private static EncounterDefinition Tribunal(
        string hearing = "take_the_testimony",
        string remand = "hold_under_review",
        string refusal = "written_refusal",
        int hearingClaims = 0,
        int? hearingHealth = null,
        int? remandHealth = null,
        int? refusalHealth = null)
    {
        var roster = FightProbe.Roster("three_reeds", energy: 9,
            (ActThree.HearingReedId, hearing, hearingHealth),
            (ActThree.RemandReedId, remand, remandHealth),
            (ActThree.RefusalReedId, refusal, refusalHealth));

        if (hearingClaims == 0)
            return roster;

        var stated = roster.Enemies[0] with
        {
            StartingStatuses =
            [
                .. roster.Enemies[0].StartingStatuses ?? [],
                new StartingStatusSpec(new StatusDefinitionId(ActThree.ClaimId), hearingClaims),
            ],
        };
        return new EncounterDefinition(roster.Id, [stated, .. roster.Enemies.Skip(1)],
            roster.HeroResources, roster.HeroStartingStatuses, roster.HeroDisplayName,
            roster.CardsDrawnPerTurn, roster.TriggeredEffects);
    }

    // ── the chain ─────────────────────────────────────────────────────────────────────────────────────────

    // At the end of every enemy turn one matter travels one living position further along.
    [Fact]
    public void Standing_travels_one_position_an_enemy_turn()
    {
        var (play, _, _) = FightProbe.Start(
            Tribunal(remand: "send_it_upstream", hearingClaims: 1),
            deck: [.. Enumerable.Repeat(Working, 5)], health: 600);

        Assert.Equal(1, FightProbe.StacksOf(Reed(play, ActThree.HearingReedId), ActThree.ClaimId));

        play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Reed(play, ActThree.HearingReedId), ActThree.ClaimId));
        Assert.True(FightProbe.StacksOf(Reed(play, ActThree.RemandReedId), ActThree.ClaimId) >= 1
            || FightProbe.StacksOf(Reed(play, ActThree.RefusalReedId), ActThree.ClaimId) >= 1,
            "the matter went downstream");
        play.Dispose();
    }

    // A transfer is never a creation: nothing along the chain raises the announcement, which is the one rule
    // that keeps a tribunal from manufacturing standing out of its own procedure.
    [Fact]
    public void Travelling_the_chain_creates_no_standing()
    {
        var (play, _, _) = FightProbe.Start(
            Tribunal(hearingClaims: 1),
            deck: [.. Enumerable.Repeat(Working, 5)], health: 600);

        for (var turn = 0; turn < 3; turn++)
            play.CombatDriver!.EndTurn();

        var announced = new[] { ActThree.HearingReedId, ActThree.RemandReedId, ActThree.RefusalReedId }
            .Sum(m => FightProbe.StacksOf(Reed(play, m), ActThree.ClaimCreatedId));
        Assert.Equal(0, announced);
        play.Dispose();
    }

    // Reaching the Refusal Reed is refused at a price: 3 Wergild, and the standing is spent doing it.
    [Fact]
    public void A_matter_that_reaches_the_refusal_is_refused_for_three()
    {
        var (play, _, _) = FightProbe.Start(
            Tribunal(remand: "send_it_upstream", refusal: "no_further_appeal", hearingClaims: 1),
            deck: [.. Enumerable.Repeat(Working, 5)], health: 600);

        var refusal = Reed(play, ActThree.RefusalReedId).Id;
        play.CombatDriver!.EndTurn(); // the bell moves it to the Remand, which sends it straight on
        play.CombatDriver.EndTurn();  // …and the Refusal refuses it

        Assert.Equal(3, OwedTo(play, refusal));
        Assert.Equal(0, FightProbe.StacksOf(Reed(play, ActThree.RefusalReedId), ActThree.ClaimId));
        play.Dispose();
    }

    // "Hold Under Review — delay the next automatic Claim transfer one enemy turn."
    [Fact]
    public void Holding_a_matter_under_review_delays_the_chain()
    {
        var (play, _, _) = FightProbe.Start(
            Tribunal(hearingClaims: 1),
            deck: [.. Enumerable.Repeat(Working, 5)], health: 600);

        // The tribunal opens on the first bell without moving anything, so the first EndTurn is where a
        // matter would ordinarily travel — and the Remand's hold is what stops it.
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Reed(play, ActThree.HearingReedId), ActThree.ClaimId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.AppealHeldId)); // the hold is spent doing it
        play.Dispose();
    }

    // ── State the Matter Clearly ──────────────────────────────────────────────────────────────────────────

    // The turn's first real card must differ in kind from the last real card of the turn before.
    [Fact]
    public void Opening_a_turn_as_the_last_one_closed_owes_the_hearing()
    {
        var (play, session, _) = FightProbe.Start(
            Tribunal(), deck: [.. Enumerable.Repeat(Working, 5)], health: 600);

        var hearing = Reed(play, ActThree.HearingReedId).Id;
        Play(play, session, Working, hearing); // the first turn is exempt
        play.CombatDriver!.EndTurn();

        Play(play, session, Working, hearing); // …and the second is not
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // Saying something else is what the Hearing asked for.
    [Fact]
    public void Changing_the_kind_of_card_states_the_matter_clearly()
    {
        var (play, session, _) = FightProbe.Start(
            Tribunal(), deck: [Working, Working, Deed, Deed, Deed], health: 600);

        var hearing = Reed(play, ActThree.HearingReedId).Id;
        Play(play, session, Working, hearing);
        play.CombatDriver!.EndTurn();

        Play(play, session, Deed, hearing);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── kill order ────────────────────────────────────────────────────────────────────────────────────────

    // Cut the Remand and matters pass straight from the Hearing to the Refusal.
    [Fact]
    public void Cutting_the_remand_shortens_the_route()
    {
        var (play, session, _) = FightProbe.Start(
            Tribunal(hearingClaims: 1, remandHealth: 5),
            deck: [.. Enumerable.Repeat(Deed, 5)], health: 600);

        Play(play, session, Deed, Reed(play, ActThree.RemandReedId).Id); // the middle of the chain falls
        Assert.Equal(0, Reed(play, ActThree.RemandReedId).Health.Current);

        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Reed(play, ActThree.RefusalReedId), ActThree.ClaimId));
        play.Dispose();
    }

    // Cut the Refusal and standing falls out of the chain, handing the traveller a licence — and every reed
    // that remains grows a Strength for the insult.
    [Fact]
    public void Cutting_the_refusal_frees_the_matter_and_angers_the_rest()
    {
        var (play, session, _) = FightProbe.Start(
            Tribunal(hearingClaims: 1, refusalHealth: 5),
            deck: [.. Enumerable.Repeat(Deed, 5)], health: 600);

        Play(play, session, Deed, Reed(play, ActThree.RefusalReedId).Id);

        Assert.Equal(1, FightProbe.StacksOf(Reed(play, ActThree.HearingReedId), new StatusDefinitionId("strength").value));
        Assert.Equal(1, FightProbe.StacksOf(Reed(play, ActThree.RemandReedId), new StatusDefinitionId("strength").value));

        var licences = FightProbe.StacksOf(Hero(play), ActThree.SafeConductId);
        for (var turn = 0; turn < 2; turn++)
            play.CombatDriver!.EndTurn(); // the matter travels to the end of the surviving chain and falls out

        Assert.True(FightProbe.StacksOf(Hero(play), ActThree.SafeConductId) > licences,
            "a matter that runs out of chain hands the traveller a licence");
        play.Dispose();
    }

    // ── Nothing Ends Here ─────────────────────────────────────────────────────────────────────────────────

    // A demand settled in full anywhere in the fight feeds the middle of the chain — and the Remand puts
    // the Block up at the bell, which is the turn it was ever meant to survive.
    [Fact]
    public void Keeping_your_word_feeds_the_remand()
    {
        var roster = Tribunal(remand: "send_it_upstream", refusal: "no_further_appeal");
        var withStanding = new EncounterDefinition(roster.Id,
            [
                roster.Enemies[0], roster.Enemies[1],
                roster.Enemies[2] with
                {
                    StartingStatuses =
                    [
                        .. roster.Enemies[2].StartingStatuses ?? [],
                        new StartingStatusSpec(new StatusDefinitionId(ActThree.ClaimId), 1),
                    ],
                },
            ],
            roster.HeroResources, roster.HeroStartingStatuses, roster.HeroDisplayName,
            roster.CardsDrawnPerTurn, roster.TriggeredEffects);

        var (play, session, _) = FightProbe.Start(withStanding,
            deck: [.. Enumerable.Repeat(Working, 5)], health: 900);

        var creditor = Reed(play, ActThree.RefusalReedId).Id;
        play.CombatDriver!.EndTurn(); // No Further Appeal cashes its Claim: 2 owed
        Assert.Equal(2, OwedTo(play, creditor));

        for (var i = 0; i < 2; i++)
        {
            var card = play.CombatDriver.Current!.Hand
                .First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
            play.CombatDriver.PlayCard(card.Id, creditor);
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([0]);
        }
        Assert.True(session.Error is null, session.Error);
        Assert.Equal(0, OwedTo(play, creditor));

        play.CombatDriver.EndTurn(); // the demand settles, and the Remand is fed for the next bell

        Assert.True(Block(Reed(play, ActThree.RemandReedId)) >= 6,
            $"the middle of the chain is fed by promises kept, saw {Block(Reed(play, ActThree.RemandReedId))}");
        play.Dispose();
    }
}
