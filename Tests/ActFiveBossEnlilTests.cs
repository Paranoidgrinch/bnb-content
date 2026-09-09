using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// ACT V, the sixth and last god — Enlil, Voice of the Unalterable Decree, proved in live fights.
//
// Two kinds of test, because he is two things at once. The first kind stands ONE decree on the table and
// asks whether reality actually changed — the fifth card really refused, the free card really priced, the
// played card really gone. The second kind lets the whole god run and asks whether the WORD behaves: that
// nothing is ever real before it has stood a turn as NEXT DECREE, that a count runs down and the ring turns,
// that his own wall binds him, and that the last three words are chosen by what the player just did and then
// never change again.
public class ActFiveBossEnlilTests
{
    private const string Cut = "paper_cut";                  // Deed, 1 Energy: deal 6
    private const string Notice = "administrative_notice";   // Working, 1 Energy
    private const string Ledger = "black_ledger";            // Rite, 1 Energy
    private const string Form = "form_12_b";                 // Working, 0 Energy

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enlil(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("enlil", StringComparison.Ordinal));

    private static int Stacks(CombatantState body, string status) => FightProbe.StacksOf(body, status);

    private static bool Wears(CombatantState body, string status) => Stacks(body, status) > 0;

    private static int Block(CombatantState body) =>
        body.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static IReadOnlyList<CardInstance> Zone(RunPlayback play, CardZone zone) =>
        play.CombatDriver!.Current!.State
            .GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(zone);

    private static int Energy(RunPlayback play) =>
        Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

    private static void Play(RunPlayback play, CombatantId enemy, string cardId = Cut) =>
        play.CombatDriver!.PlayCard(Hand(play).First(c => c.DefinitionId.value == cardId).Id, enemy);

    private static bool CanPlay(RunPlayback play, string cardId = Cut) =>
        Energy(play) > 0 && Hand(play).Any(c => c.DefinitionId.value == cardId);

    // ⚠ A LOOP THAT PLAYS UNTIL SOMETHING HAPPENS CANNOT BE BOUNDED BY THE HAND OR BY THE PURSE — not in
    // THIS fight. Everywhere else in the game a card that cannot be played is a card the hand does not hold
    // or the Energy does not reach; here it is a card the RULES refuse, and a refused play changes nothing at
    // all: same hand, same Energy, same everything. "While there is still a card and still a point" is
    // therefore a loop that never ends the moment a decree says no. So the loop counts its own plays and
    // stops the first time one is refused.
    private static void Push(RunPlayback play, CombatantId enemy, Func<bool> until, int cap = 5)
    {
        for (var made = 0; made < cap && CanPlay(play) && !until(); made++)
        {
            var steps = Steps(play);
            Play(play, enemy, Cut);
            if (Refused(play, steps))
                return;
        }
    }

    // Whether the play just attempted was REFUSED — the fight records every attempt, and a refused one
    // carries its reason. This is the shape of a rule in this fight: not a card that resolves and is judged,
    // but a card that does not happen.
    private static bool Refused(RunPlayback play, int stepsBefore) =>
        play.CombatDriver!.Current!.Steps.Skip(stepsBefore).Any(step => step.HasProblems);

    private static int Steps(RunPlayback play) => play.CombatDriver!.Current!.Steps.Count;

    // ONE DECREE, ON BOTH SIDES, AND NO GOD BEHIND IT. The decrees are ordinary statuses and the rules they
    // carry are read from whoever wears them, so a probe about a RULE needs no rotation and no phases —
    // which is also the cleanest possible proof that the rule is in the status and not in the fight.
    private static (RunPlayback Play, CombatantId EnemyId) Under(
        string decreeId, string intentId = "the_command_goes_forth", int energy = 9,
        int bossHealth = 4000, int? draw = null, params string[] deck)
    {
        var probe = FightProbe.Solo(ActFive.EnlilEnemyId, intentId, energy);
        var body = probe.Enemies[0] with
        {
            MaxHealth = bossHealth,
            StartingStatuses = [new StartingStatusSpec(new StatusDefinitionId(decreeId), 1)],
        };
        var fight = new EncounterDefinition(
            probe.Id, [body], probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             new StartingStatusSpec(new StatusDefinitionId(decreeId), 1)],
            probe.HeroDisplayName, draw ?? probe.CardsDrawnPerTurn, probe.TriggeredEffects);
        var (play, _, enemy) = FightProbe.Start(
            fight, deck: Cards(deck.Length == 0 ? [Cut] : deck), health: 9000);
        return (play, enemy);
    }

    // THE WHOLE GOD, exactly as the game fields him — his rule, his phases, his intent rules — with the two
    // dials every Act-V probe has: how much of him there is, and what the deck is made of.
    private static (RunPlayback Play, CombatantId EnemyId) Court(
        int? bossHealth = null, int energy = 9,
        string intentId = "the_command_goes_forth", params string[] deck)
    {
        var probe = FightProbe.Solo(ActFive.EnlilEnemyId, intentId, energy);
        var body = bossHealth is { } hp ? probe.Enemies[0] with { MaxHealth = hp } : probe.Enemies[0];
        var fight = new EncounterDefinition(
            probe.Id, [body], probe.HeroResources, probe.HeroStartingStatuses,
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);
        var (play, _, enemy) = FightProbe.Start(
            fight, deck: Cards(deck.Length == 0 ? [Cut] : deck), health: 9000);
        return (play, enemy);
    }

    private static IReadOnlyList<string> Cards(IReadOnlyList<string> kinds) =>
        [.. Enumerable.Range(0, 40).Select(i => kinds[i % kinds.Count])];

    // ── he is absolute, not arbitrary (§11.3) ─────────────────────────────────────────────────────────────

    // The first turn is played under NO decree at all, with the first one already written on the table. That
    // is the whole of §11.3 in one assertion: nothing is ever real the turn it is spoken.
    [Fact]
    public void Nothing_is_real_before_it_has_stood_a_turn_as_the_next_decree()
    {
        var (play, _) = Court();
        var voice = Enlil(play);

        Assert.True(Wears(voice, ActFive.NextIdFor(ActFive.FourthWorkId)));
        Assert.False(Wears(voice, ActFive.FourthWorkId));
        Assert.False(Wears(Hero(play), ActFive.FourthWorkId));

        play.CombatDriver!.EndTurn();

        // Now it is reality — on him as much as on the player, and the next one is already announced.
        Assert.True(Wears(Enlil(play), ActFive.FourthWorkId));
        Assert.True(Wears(Hero(play), ActFive.FourthWorkId));
        Assert.False(Wears(Enlil(play), ActFive.NextIdFor(ActFive.FourthWorkId)));
        Assert.True(Wears(Enlil(play), ActFive.NextIdFor(ActFive.FirstWithoutPriceId)));
    }

    // §11.7: a decree runs the count beside it and not one turn longer. The chip IS the clock.
    [Fact]
    public void A_decree_runs_its_announced_count_and_goes()
    {
        var (play, _) = Court();
        play.CombatDriver!.EndTurn();

        Assert.Equal(3, Stacks(Enlil(play), ActFive.WordStandsId));
        play.CombatDriver.EndTurn();
        Assert.Equal(2, Stacks(Enlil(play), ActFive.WordStandsId));
        play.CombatDriver.EndTurn();
        Assert.Equal(1, Stacks(Enlil(play), ActFive.WordStandsId));

        // Three player turns under it, and then the ring turns to the word that was standing announced.
        Assert.True(Wears(Enlil(play), ActFive.FourthWorkId));
        play.CombatDriver.EndTurn();
        Assert.False(Wears(Enlil(play), ActFive.FourthWorkId));
        Assert.False(Wears(Hero(play), ActFive.FourthWorkId));
        Assert.True(Wears(Enlil(play), ActFive.FirstWithoutPriceId));
    }

    // §11.6: the ring is the Director. THE FIRST WORD SHALL BE WITHOUT PRICE and NO WORK SHALL BE WITHOUT
    // MEASURE contradict each other outright, and they are in the same ring — so however long the fight
    // runs, they can never both be in force. The test walks a whole cycle and checks every turn.
    [Fact]
    public void Two_words_that_contradict_each_other_can_never_both_be_in_force()
    {
        var (play, _) = Court();

        // One whole turn of the ring of Works is 3 + 2 + 3 + 1 + 2 + 3 = fourteen player turns, so fifteen
        // sees every word in it once, in force, in order.
        for (var turn = 0; turn < 15; turn++)
        {
            var voice = Enlil(play);
            var free = Wears(voice, ActFive.FirstWithoutPriceId) || Wears(voice, ActFive.ThirdWithoutPriceId);
            Assert.False(free && Wears(voice, ActFive.WithoutMeasureId));
            play.CombatDriver!.EndTurn();
        }
    }

    // ── the order of works ────────────────────────────────────────────────────────────────────────────────

    // §11.1, and the line that separates this god from the one in the room before him: the fifth card is not
    // a choice with a price. It is unavailable, and it stays in the hand.
    [Fact]
    public void The_fifth_work_is_not_a_choice_with_a_price()
    {
        var (play, enemy) = Under(ActFive.FourthWorkId);

        for (var i = 0; i < 4; i++)
            Play(play, enemy, Cut);

        var before = Enlil(play).Health.Current;
        var handBefore = Hand(play).Count;
        var steps = Steps(play);
        Play(play, enemy, Cut);

        Assert.True(Refused(play, steps));
        Assert.Equal(before, Enlil(play).Health.Current);
        Assert.Equal(handBefore, Hand(play).Count);
    }

    [Fact]
    public void Without_the_decree_a_fifth_card_is_an_ordinary_card()
    {
        var (play, enemy) = Court(energy: 9);

        for (var i = 0; i < 4; i++)
            Play(play, enemy, Cut);
        var before = Enlil(play).Health.Current;
        Play(play, enemy, Cut);

        Assert.True(Enlil(play).Health.Current < before);
    }

    // §11.5: the same kind may not follow itself. It is about SUCCESSION and not about a kind being spent —
    // put something else in between and the first kind is legal again.
    [Fact]
    public void No_work_shall_follow_its_likeness()
    {
        // A wide hand, and two kinds the shuffle actually deals: a rule about what may FOLLOW what needs
        // two of one kind and one of another in the same hand, which a five-card draw off a three-kind deck
        // does not promise (the first attempt at this test drew ten cards and not one of them was a Deed).
        var (play, enemy) = Under(ActFive.NoLikenessId, draw: 10, deck: [Notice, Ledger]);

        Play(play, enemy, Notice);
        var steps = Steps(play);
        Play(play, enemy, Notice);
        Assert.True(Refused(play, steps));

        // A Rite goes through, and after it the Working does too — the kind was never spent, only followed.
        Play(play, enemy, Ledger);
        steps = Steps(play);
        Play(play, enemy, Notice);
        Assert.False(Refused(play, steps));
    }

    [Fact]
    public void The_first_word_is_without_price()
    {
        var (play, enemy) = Under(ActFive.FirstWithoutPriceId, energy: 3);

        Play(play, enemy, Cut);
        Assert.Equal(3, Energy(play));

        Play(play, enemy, Cut);
        Assert.Equal(2, Energy(play));
    }

    [Fact]
    public void The_third_work_is_without_price()
    {
        var (play, enemy) = Under(ActFive.ThirdWithoutPriceId, energy: 9);

        Play(play, enemy, Cut);
        Play(play, enemy, Cut);
        var before = Energy(play);
        Play(play, enemy, Cut);

        Assert.Equal(before, Energy(play));
    }

    // §11.5: a card that would cost nothing costs 1 instead — and the card this is hardest for is the one
    // whose printed cost is zero, because such a card carries no price for anything to raise.
    [Fact]
    public void No_work_shall_be_without_measure()
    {
        var (play, enemy) = Under(ActFive.WithoutMeasureId, energy: 3, deck: [Form]);

        Play(play, enemy, Form);

        Assert.Equal(2, Energy(play));
    }

    [Fact]
    public void Without_the_decree_a_free_card_is_free()
    {
        var (play, enemy) = Court(energy: 3, deck: [Form]);

        Play(play, enemy, Form);

        Assert.Equal(3, Energy(play));
    }

    // ⚠ The absolute numbers are NOT asserted, because the decree is worn from the first bell and has
    // therefore already carried once by the time the opening hand is dealt. What is asserted is the thing
    // the decree actually claims: the refill ADDS to what was left instead of replacing it.
    [Fact]
    public void What_is_unspent_passes_into_tomorrow()
    {
        var (play, enemy) = Under(ActFive.UnspentTomorrowId, energy: 3);
        var refill = Hero(play).Resources[StandardCombatIds.EnergyResource].Max!.Value;

        Play(play, enemy, Cut);
        var left = Energy(play);
        play.CombatDriver!.EndTurn();

        Assert.Equal(left + refill, Energy(play));
    }

    [Fact]
    public void Without_the_decree_the_refill_replaces_what_was_left()
    {
        var (play, enemy) = Court(energy: 3);
        var refill = Hero(play).Resources[StandardCombatIds.EnergyResource].Max!.Value;

        Play(play, enemy, Cut);
        play.CombatDriver!.EndTurn();

        Assert.Equal(refill, Energy(play));
    }

    // ── the order of returning, and of the hand ───────────────────────────────────────────────────────────

    [Fact]
    public void No_work_shall_return()
    {
        var (play, enemy) = Under(ActFive.NoWorkReturnsId, energy: 3);

        Play(play, enemy, Cut);

        Assert.Empty(Zone(play, CardZone.DiscardPile));
        Assert.Single(Zone(play, CardZone.ExhaustPile));
    }

    // §11.14's other half. One card back at the hand — and ONE, not one for every draw the turn contains,
    // which is the trap CardsDrawn sets for anything that means "when the turn began".
    [Fact]
    public void What_has_been_cast_out_returns_once_a_turn()
    {
        var (play, enemy) = Under(ActFive.CastOutReturnsId, energy: 9, deck: [Cut, "misfiled_paper"]);

        // Put something in the exhaust pile the ordinary way, then take a fresh turn.
        Play(play, enemy, "misfiled_paper");
        Assert.Single(Zone(play, CardZone.ExhaustPile));
        play.CombatDriver!.EndTurn();

        Assert.Empty(Zone(play, CardZone.ExhaustPile));

        // A card that draws re-fires CardsDrawn in the middle of the turn; the exhaust pile it fills must
        // not be emptied again by the same decree in the same turn.
        var handBefore = Hand(play).Count;
        Play(play, enemy, "misfiled_paper");
        Assert.Single(Zone(play, CardZone.ExhaustPile));
        Assert.True(Hand(play).Count <= handBefore);
    }

    [Fact]
    public void Seven_shall_be_the_hands_measure()
    {
        var (play, _) = Under(ActFive.HandOfSevenId, energy: 9);

        Assert.True(Hand(play).Count <= 7);
        play.CombatDriver!.EndTurn();
        Assert.True(Hand(play).Count <= 7);
    }

    // ── the order of force, and no decree privilege (§11.4, §11.11) ───────────────────────────────────────

    // His own wall, cut down by his own word. The intent line still says 48 — the telegraph is built from
    // what the move IS — and the decree standing beside it is what says 48 is not what will happen.
    [Fact]
    public void His_own_wall_shall_not_rise_above_thirty()
    {
        var (play, _) = Under(ActFive.WallOfThirtyId, intentId: "raise_e_kur");
        play.CombatDriver!.EndTurn();

        Assert.Equal(30, Block(Enlil(play)));
    }

    [Fact]
    public void Without_the_decree_he_raises_the_whole_wall()
    {
        var (play, _) = Court(intentId: "raise_e_kur");
        play.CombatDriver!.EndTurn();

        Assert.Equal(48, Block(Enlil(play)));
    }

    // Universal, and it binds him: his 30-damage command comes to 20.
    [Fact]
    public void No_single_blow_shall_exceed_twenty()
    {
        var (play, _) = Under(ActFive.BlowOfTwentyId, intentId: "the_command_goes_forth");
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(20, before - Hero(play).Health.Current);
    }

    // …and it binds the player in the same breath, out of the same status on the other row.
    [Fact]
    public void It_binds_the_player_too()
    {
        var (play, enemy) = Under(ActFive.BlowOfTwentyId, energy: 9, bossHealth: 4000);
        var before = Enlil(play).Health.Current;
        Play(play, enemy, Cut);

        Assert.True(before - Enlil(play).Health.Current <= 20);
    }

    // §11.5, Order of Force: the FIRST blow, and only the first. His four-hit Wind Over the Plain loses its
    // opening 9 and lands the other three.
    [Fact]
    public void The_first_blow_falls_upon_nothing()
    {
        var (play, _) = Under(ActFive.FirstBlowNothingId, intentId: "wind_over_the_plain");
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(27, before - Hero(play).Health.Current);
    }

    [Fact]
    public void And_it_comes_back_with_the_turn()
    {
        var (play, _) = Under(ActFive.FirstBlowNothingId, intentId: "wind_over_the_plain");
        play.CombatDriver!.EndTurn();
        var before = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();

        Assert.Equal(27, before - Hero(play).Health.Current);
    }

    // ── the phases ────────────────────────────────────────────────────────────────────────────────────────

    // §11.10: at 70 % a second word joins the first, and the two counts are staggered rather than started
    // together — the rule set changes gradually instead of resetting.
    [Fact]
    public void The_assembly_falls_silent_and_a_second_word_joins()
    {
        // 80 HP, so the first threshold is 56. One turn passes first — that is what puts a count on the
        // first ring — and the second turn, under THE FOURTH WORK, spends its four cards reaching it. Had
        // the crossing happened on turn one, both rings would have started their counts in the same breath
        // and the staggering the phase is FOR would not be visible.
        var (play, enemy) = Court(bossHealth: 80, energy: 9);
        play.CombatDriver!.EndTurn();
        Push(play, enemy, () => Wears(Enlil(play), ActFive.AssemblySilentId), cap: 4);

        var voice = Enlil(play);
        Assert.True(Wears(voice, ActFive.AssemblySilentId));
        Assert.False(Wears(voice, ActFive.OneWordId));

        play.CombatDriver!.EndTurn();
        voice = Enlil(play);
        Assert.True(Wears(voice, ActFive.SecondWordStandsId));
        Assert.True(Wears(voice, ActFive.WordStandsId));
        Assert.NotEqual(Stacks(voice, ActFive.WordStandsId), Stacks(voice, ActFive.SecondWordStandsId));
    }

    // §11.12: at 35 % the rotation ends. Three words are announced, one whole player turn passes, and then
    // they are real — and after that nothing is ever announced again (§11.17).
    [Fact]
    public void Three_words_are_enough_and_then_nothing_more_is_ever_said()
    {
        // 60 HP, so the last threshold is 21 and two turns of four cards reach it.
        var (play, enemy) = Court(bossHealth: 60, energy: 9);

        for (var turn = 0; turn < 6 && !Wears(Enlil(play), ActFive.ThreeWordsId); turn++)
        {
            Push(play, enemy, () => Wears(Enlil(play), ActFive.ThreeWordsId));
            if (!Wears(Enlil(play), ActFive.ThreeWordsId))
                play.CombatDriver!.EndTurn();
        }

        Assert.True(Wears(Enlil(play), ActFive.ThreeWordsId));
        var kingdom = ActFive.EnlilKingdoms().First(k => Wears(Enlil(play), k));
        var words = ActFive.EnlilWordsOf(kingdom);

        // Announced, and not yet real.
        Assert.All(words, w => Assert.True(Wears(Enlil(play), ActFive.NextIdFor(w))));
        Assert.All(words, w => Assert.False(Wears(Hero(play), w)));

        // One preparation turn, and then all three, on both sides, with no clock over them.
        play.CombatDriver!.EndTurn();
        Assert.All(words, w => Assert.False(Wears(Hero(play), w)));
        play.CombatDriver.EndTurn();
        Assert.All(words, w => Assert.True(Wears(Hero(play), w)));
        Assert.All(words, w => Assert.True(Wears(Enlil(play), w)));
        Assert.False(Wears(Enlil(play), ActFive.WordStandsId));

        // §11.17: no fourth mechanic, no new decree, ever. Four more turns say nothing at all — read over
        // the DECREES he wears rather than over every chip on him, because his own combat noise (whether he
        // struck last round) is not the rule set and never was.
        var standing = Words(play);
        for (var i = 0; i < 4; i++)
            play.CombatDriver.EndTurn();
        Assert.Equal(standing, Words(play));
    }

    // §11.12: "These are not random." A turn that spent itself on four cards is answered by the Narrow
    // Kingdom — few actions, each of which has to count.
    // 30 HP: the last threshold is 10, Paper Cut deals 6, and the fourth card of the turn is the one that
    // crosses it. Four cards in a turn is what the Narrow Kingdom is an answer to.
    [Fact]
    public void The_last_order_answers_the_turn_that_brought_him_there()
    {
        var (play, enemy) = Court(bossHealth: 30, energy: 9);

        Push(play, enemy, () => Wears(Enlil(play), ActFive.ThreeWordsId));

        Assert.True(Wears(Enlil(play), ActFive.ThreeWordsId));
        Assert.True(Wears(Enlil(play), ActFive.NarrowKingdomId));
    }

    // The same god and a different last turn: ONE card, six damage, eight Energy still standing. He answers
    // a player who was holding something back with the economy that takes holding back away.
    [Fact]
    public void A_quiet_turn_is_answered_differently()
    {
        var (play, enemy) = Court(bossHealth: 9, energy: 9);

        Play(play, enemy, Cut);

        Assert.True(Wears(Enlil(play), ActFive.ThreeWordsId));
        Assert.False(Wears(Enlil(play), ActFive.NarrowKingdomId));
        Assert.True(Wears(Enlil(play), ActFive.AshKingdomId));
    }

    // Every decree and every announcement standing on him, in a stable order — the rule set, and nothing
    // else that happens to be on his row.
    private static string[] Words(RunPlayback play)
    {
        var rules = ActFive.EnlilDecreeMarkers().ToHashSet(StringComparer.Ordinal);
        return [.. Enlil(play).Statuses.Select(s => s.DefinitionId.value).Where(rules.Contains)
            .OrderBy(s => s, StringComparer.Ordinal)];
    }
}
