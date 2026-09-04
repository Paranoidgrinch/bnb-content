using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, boss — The Vizier of the King's Mouth, proved in live fights.
//
// The fight is a kill order, so the tests are about what a body's LIFE is worth and what its DEATH costs:
// what the offices lend him while they stand, what only one of them acting per turn looks like, what the
// mouth swallows at 295 and what it finds already gone, and what the last blow is worth per office.
public class ActFourBossVizierTests
{
    private const string Cut = "paper_cut";   // Deed, 1: deal 6

    private static CombatantState Body(RunPlayback play, string fragment) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains(fragment, StringComparison.Ordinal));

    private static CombatantState Vizier(RunPlayback play) => Body(play, "vizier");
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static bool Wears(CombatantState c, string status) =>
        c.Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    private static string ActingOffice(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .Where(c => Wears(c, ActFour.ActingOfficeId))
            .Select(c => c.DefinitionId.value)
            .SingleOrDefault() ?? "nobody";

    // Authority descends by 6 an office, so a probe that wants his HEALTH moved has to get through it first.
    private static void CutHim(RunPlayback play, InteractiveRunSession session, int times)
    {
        for (var i = 0; i < times; i++)
        {
            var card = play.CombatDriver!.Current!.Hand.FirstOrDefault(c => c.DefinitionId.value == Cut);
            if (card is null)
                return;
            play.CombatDriver.PlayCard(card.Id, Vizier(play).Id);
            Assert.True(session.Error is null, session.Error);
        }
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // The whole court, at whatever health the test needs to reach the moment it is about. The Vizier's own
    // intent is named; the offices choose theirs by their own rules, exactly as they do in the real room.
    private static (RunPlayback Play, InteractiveRunSession Session) TheCourt(
        string vizierIntent, int? vizierHp = null,
        int? sealHp = null, int? keeperHp = null, int? captainHp = null, int energy = 9) =>
        FightProbe.Start(
            FightProbe.Roster("kings_mouth", energy,
                (ActFour.VizierEnemyId, vizierIntent, vizierHp),
                (ActFour.SealBearerEnemyId, "display_the_seal", sealHp),
                (ActFour.KeeperOfTalliesEnemyId, "count_again", keeperHp),
                (ActFour.CaptainOfTheStairEnemyId, "hold_the_stair", captainHp)),
            deck: [.. Enumerable.Repeat(Cut, 14)], health: 900) switch
        { var (p, s, _) => (p, s) };

    // §12.3 and §12.2 in one turn: authority descends by 6 per office at the top of the player's turn — the
    // turn in which the player would be attacking him — and exactly one office is holding the token.
    [Fact]
    public void Authority_descends_by_office_and_only_one_office_acts()
    {
        var (play, _) = TheCourt("royal_words_mortal_bones");

        Assert.Equal(18, Block(Vizier(play)));
        Assert.Equal("royal_seal_bearer", ActingOffice(play));

        play.CombatDriver!.EndTurn();
        Assert.Equal("keeper_of_tallies", ActingOffice(play));

        play.CombatDriver.EndTurn();
        Assert.Equal("captain_of_the_inner_stair", ActingOffice(play));

        play.CombatDriver.EndTurn();
        Assert.Equal("royal_seal_bearer", ActingOffice(play));
        play.Dispose();
    }

    // The rotation is a list of the LIVING: kill the office holding the token's place and the turn passes
    // over it, and authority descends by one office less.
    [Fact]
    public void A_dead_office_is_out_of_the_rotation_and_off_his_shoulder()
    {
        var (play, session) = TheCourt("royal_words_mortal_bones", keeperHp: 6);

        Play(play, session, Cut, Body(play, "keeper_of_tallies").Id);
        play.CombatDriver!.EndTurn();

        // The Keeper is gone, so the token walks from the Seal Bearer straight to the Captain …
        Assert.Equal("captain_of_the_inner_stair", ActingOffice(play));
        // … and the Vizier is armoured for two offices, not three.
        Assert.Equal(12, Block(Vizier(play)));
        // … and what the Keeper lent him went with it.
        Assert.False(Wears(Vizier(play), ActFour.CountedFailureId));
        play.Dispose();
    }

    // §12.1 Royal Impression, worn where the player can read it: one seal a round, and it makes the first
    // affliction to reach them one stack larger. Renewed at the top of each of their turns and never stacked
    // twice by the rule itself — the register is the one thing that can make a seal bigger, and it does.
    [Fact]
    public void The_seal_makes_the_first_affliction_of_the_round_larger()
    {
        var (play, _) = TheCourt("return_to_your_place");

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.RoyalSealImpressedId));

        play.CombatDriver!.EndTurn();      // Burdened 2 lands as 3, and the seal is spent doing it
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));

        play.CombatDriver.EndTurn();       // a new round, a new seal: 2 lands as 3 again
        Assert.Equal(6, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // …and the seal is the SEAL BEARER's: kill it and the impression stops being renewed.
    [Fact]
    public void A_dead_seal_bearer_stops_the_impression()
    {
        var (play, session) = TheCourt("return_to_your_place", sealHp: 6);

        Play(play, session, Cut, Body(play, "royal_seal_bearer").Id);
        Assert.False(Wears(Vizier(play), ActFour.RoyalImpressionId));

        play.CombatDriver!.EndTurn();      // this round's seal was already stamped: 2 + 1
        play.CombatDriver.EndTurn();       // the next round has none: a plain 2 on top
        Assert.Equal(5, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // §12.1 Counted Failure: a measure missed is a sheet filed and 8 Block bought, once per resolution.
    [Fact]
    public void A_missed_measure_is_filed_against_you_and_buys_him_block()
    {
        // He is alone with the warrant: another Keeper in the room would raise a FRESH measure every round,
        // and the thing being proved is that ONE missed measure is answered once.
        var (play, _, _) = FightProbe.Start(
            FightProbe.RosterAgainstHero("kings_mouth_tally", energy: 9, [(ActFour.WeighedId, 2)],
                (ActFour.VizierEnemyId, "speak_for_the_king", null)),
            deck: [.. Enumerable.Repeat(Cut, 14)], health: 900);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Paperwork));

        play.CombatDriver!.EndTurn();      // nothing spent against a measure of 2: missed

        // ONE sheet is filed — and it arrives as two, because the seal is still in force and a filed sheet
        // is the first affliction of the round to reach the player. The two rules meeting is the fight.
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Paperwork));
        Assert.True(Block(Vizier(play)) >= 8, $"block was {Block(Vizier(play))}");

        // …and the same missed measure is never read twice, however many bodies look at it.
        play.CombatDriver.EndTurn();
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Paperwork));
        play.Dispose();
    }

    // §12.5: at 295 the mouth opens. Every office still standing is set aside rather than killed, and what
    // it lent him stays — the Captain's at the smaller inherited size. An office already beaten grants
    // nothing, and its function is not there to inherit.
    [Fact]
    public void The_mouth_swallows_what_still_stands_and_finds_the_rest_already_gone()
    {
        var (play, session) = TheCourt("royal_words_mortal_bones", vizierHp: 300, keeperHp: 6);

        Play(play, session, Cut, Body(play, "keeper_of_tallies").Id);

        for (var turn = 0; turn < 6; turn++)
        {
            CutHim(play, session, 9);
            if (Wears(Vizier(play), ActFour.MouthOpensNextId))
                break;
            play.CombatDriver!.EndTurn();
        }

        Assert.True(Wears(Vizier(play), ActFour.MouthOpensNextId), "the transition is telegraphed");

        play.CombatDriver.EndTurn();                   // the mouth opens: no attack, the offices are absorbed

        var vizier = Vizier(play);
        Assert.True(Wears(vizier, ActFour.MouthHasOpenedId));
        Assert.False(Wears(vizier, ActFour.MouthOpensNextId));
        Assert.True(Wears(vizier, ActFour.RoyalImpressionId), "the Seal Bearer was swallowed");
        Assert.False(Wears(vizier, ActFour.CountedFailureId), "the Keeper was beaten, so it grants nothing");
        Assert.False(Wears(vizier, ActFour.ArmedAuthorityId));
        Assert.True(Wears(vizier, ActFour.ArmedAuthorityInheritedId), "the Captain's, one smaller");

        Assert.Empty(play.CombatDriver.Current!.State.Combatants
            .Where(c => c.IsAlive && Wears(c, ActFour.RoyalOfficeId)));
        play.Dispose();
    }

    // The signature, and the counterplay the audit asked for: the blow itself hands the player the sheets,
    // one per office he actually swallowed.
    [Fact]
    public void The_king_is_not_here_is_worth_what_he_swallowed_and_hands_back_the_sheets()
    {
        var (play, session) = TheCourt("speak_for_the_king", vizierHp: 99);

        Play(play, session, Cut, Vizier(play).Id);
        Assert.True(Wears(Vizier(play), ActFour.MouthOpensNextId));
        Assert.True(Wears(Vizier(play), ActFour.KingNotHereId), "both are announced by the one blow");

        play.CombatDriver!.EndTurn();   // the mouth opens first: the signature waits for the second half
        Assert.True(Wears(Vizier(play), ActFour.MouthHasOpenedId));

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();    // 32, and 4 for each of the three offices — capped at 44

        // 32 + 4 × 3 offices, capped at 44 — and then the Captain's own inherited 5 on top of it, because
        // the signature is a direct attack like any other and armed authority does not care which one.
        Assert.Equal(49, before - Hero(play).Health.Current);
        Assert.False(Wears(Vizier(play), ActFour.KingNotHereId), "once per combat");

        var hand = play.CombatDriver.Current!.Hand.Select(c => c.DefinitionId.value).ToList();
        Assert.Contains("silence_the_royal_seal", hand);
        Assert.Contains("silence_the_tally", hand);
        Assert.Contains("silence_the_inner_stair", hand);
        play.Dispose();
    }

    // …and a sheet is worth exactly one of his actions. The Captain's is the one a passive modifier cannot
    // be asked about, so it is the one worth proving: the silence carries the opposite modifier.
    [Fact]
    public void A_silenced_office_is_quiet_for_one_action_and_no_longer()
    {
        var (play, session) = TheCourt("royal_words_mortal_bones", vizierHp: 99);

        Play(play, session, Cut, Vizier(play).Id);
        play.CombatDriver!.EndTurn();    // the mouth opens
        play.CombatDriver.EndTurn();     // the signature, and the sheets

        Play(play, session, "silence_the_inner_stair", null);
        Assert.True(Wears(Vizier(play), ActFour.SilencedStairId));

        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();     // 35, without the Captain's 5
        Assert.Equal(35, before - Hero(play).Health.Current);
        Assert.False(Wears(Vizier(play), ActFour.SilencedStairId), "spent by the action it silenced");

        before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();     // and the stair is back: 35 + 5
        Assert.Equal(40, before - Hero(play).Health.Current);
        play.Dispose();
    }
}
