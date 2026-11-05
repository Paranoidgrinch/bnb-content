using BnbContent.Converter.Gods;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// G1, Nisaba (ACT_V_REWORK_PLAN.md): her sentences are bodies with a countdown; at most three stand; at 30 % the
// Final Line erases you on the fourth dawn and keeps her at 1 HP while it stands.
public class GodNisabaTests
{
    private static CombatState State(RunPlayback play) => play.CombatDriver!.Current!.State;

    private static CombatantState Hero(RunPlayback play) => State(play).GetCombatant(play.CombatDriver!.Current!.HeroId);

    private static CombatantState Her(RunPlayback play) =>
        State(play).Combatants.Single(c => c.DefinitionId.value == Nisaba.EnemyId);

    private static List<CombatantState> Sentences(RunPlayback play) =>
        [.. State(play).Combatants.Where(c => c.IsAlive && FightProbe.StacksOf(c, Nisaba.SentenceId) > 0)];

    private static CombatantState? FinalLine(RunPlayback play) =>
        State(play).Combatants.FirstOrDefault(c => c.IsAlive && FightProbe.StacksOf(c, Nisaba.FinalLineId) > 0);

    private static int Block(CombatantState who) =>
        who.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private const string Deed = "paper_cut";              // 1 Energy, 6 damage
    private const string Working = "cower_behind_a_desk"; // 1 Energy, 5 Block

    private static (RunPlayback Play, InteractiveRunSession Session) Start(
        string intent, string card = Working, int strength = 0, int energy = 3, int health = 900,
        params (string Status, int Stacks)[] statuses)
    {
        var probe = strength > 0
            ? FightProbe.SoloAgainstHero(Nisaba.EnemyId, intent, energy, ("strength", strength))
            : FightProbe.Solo(Nisaba.EnemyId, intent, energy, statuses);
        var (play, session, _) = FightProbe.Start(probe, [.. Enumerable.Repeat(card, 12)], health: health);
        return (play, session);
    }

    private static void Strike(RunPlayback play, InteractiveRunSession session, CombatantState target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Deed);
        play.CombatDriver.PlayCard(card.Id, target.Id);
        Assert.True(session.Error is null, session.Error);
    }

    // A blow, from this turn's hand or the next one's.
    private static void StrikeOrWait(RunPlayback play, InteractiveRunSession session, Func<CombatantState> target)
    {
        if (!play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Deed) || play.CombatDriver.Current.HeroEnergy == 0)
            play.CombatDriver.EndTurn();
        Strike(play, session, target());
    }

    [Fact]
    public void A_sentence_is_a_body_of_sixty_with_a_countdown_of_two()
    {
        var (play, _) = Start("inscribe");
        Assert.Empty(Sentences(play));
        play.CombatDriver!.EndTurn();
        var sentence = Assert.Single(Sentences(play));
        Assert.Equal(Nisaba.SentenceHealth, sentence.Health.Max);
        Assert.Equal(Nisaba.SentenceCountdown, FightProbe.StacksOf(sentence, Nisaba.ComesTrueId));
        Assert.Single(Nisaba.Sentences, s => FightProbe.StacksOf(sentence, Nisaba.SentenceKindId(s.Key)) > 0);
        Assert.Equal(Nisaba.InscribeBlock, Block(Her(play)));
        play.Dispose();
    }

    // Two of her dawns after it is written, a sentence comes true — what it says happens — and it leaves.
    [Fact]
    public void An_unbroken_sentence_comes_true_on_her_second_dawn_and_leaves()
    {
        var (play, _) = Start("inscribe", health: 2000);
        play.CombatDriver!.EndTurn();
        var first = Assert.Single(Sentences(play));
        var kind = Nisaba.Sentences.Single(s => FightProbe.StacksOf(first, Nisaba.SentenceKindId(s.Key)) > 0).Key;
        var id = first.Id;
        play.CombatDriver.EndTurn();
        Assert.Equal(1, FightProbe.StacksOf(State(play).GetCombatant(id), Nisaba.ComesTrueId));

        var hpBefore = Hero(play).Health.Current;
        var exhaustBefore = State(play).GetCardZones(Hero(play).Id).GetCardsInZone(CardZone.ExhaustPile).Count;
        play.CombatDriver.EndTurn();
        Assert.False(State(play).GetCombatant(id).IsAlive);
        var hero = Hero(play);
        switch (kind)
        {
            case "forty":
                Assert.True(hpBefore - hero.Health.Current >= Nisaba.BearForty);
                break;
            case "measure":
                Assert.Equal(3 - Nisaba.WithheldEnergy, play.CombatDriver.Current!.HeroEnergy);
                break;
            case "hand":
                Assert.Equal(Nisaba.HandSize, play.CombatDriver.Current!.Hand.Count);
                break;
            case "works":
                Assert.Equal(exhaustBefore + Nisaba.WorksBroken,
                    State(play).GetCardZones(hero.Id).GetCardsInZone(CardZone.ExhaustPile).Count);
                break;
            case "guard":
                Assert.Equal(1, FightProbe.StacksOf(hero, Nisaba.GuardAsNothingId));
                break;
        }
        play.Dispose();
    }

    [Fact]
    public void Every_kind_of_sentence_does_what_it_says()
    {
        // Each sentence's own words, read off a long fight: whichever comes true, its effect is on the table.
        var (play, _) = Start("inscribe", card: Working, health: 5000);
        var seen = new HashSet<string>();
        for (var turn = 0; turn < 40 && seen.Count < Nisaba.Sentences.Count; turn++)
        {
            var due = Sentences(play).Where(s => FightProbe.StacksOf(s, Nisaba.ComesTrueId) == 1)
                .Select(s => Nisaba.Sentences.Single(k => FightProbe.StacksOf(s, Nisaba.SentenceKindId(k.Key)) > 0).Key)
                .ToHashSet();
            var hp = Hero(play).Health.Current;
            var herHp = Her(play).Health.Current;
            play.CombatDriver!.EndTurn();
            var hero = Hero(play);
            if (due.Contains("forty"))
                Assert.True(hp - hero.Health.Current >= Nisaba.BearForty);
            if (due.Contains("measure"))
                Assert.Equal(3 - Nisaba.WithheldEnergy, play.CombatDriver.Current!.HeroEnergy);
            if (due.Contains("hand"))
                Assert.True(play.CombatDriver.Current!.Hand.Count <= Nisaba.HandSize);
            if (due.Contains("guard"))
            {
                Assert.Equal(0, Block(hero));
                Assert.Equal(1, FightProbe.StacksOf(hero, Nisaba.GuardAsNothingId));
            }
            seen.UnionWith(due);
        }
        Assert.Contains("forty", seen);
        play.Dispose();
    }

    [Fact]
    public void The_guard_as_nothing_lets_no_block_be_gained_that_turn()
    {
        // The Guard is worn from her dawn to the end of your next turn.
        var probe = FightProbe.SoloAgainstHero(Nisaba.EnemyId, "inscribe", 3, (Nisaba.GuardAsNothingId, 1));
        var (guarded, guardedSession, _) = FightProbe.Start(probe, [.. Enumerable.Repeat(Working, 12)], health: 900);
        var card = guarded.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Working);
        guarded.CombatDriver.PlayCard(card.Id, null);
        Assert.True(guardedSession.Error is null, guardedSession.Error);
        Assert.Equal(0, Block(Hero(guarded)));
        guarded.CombatDriver.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Hero(guarded), Nisaba.GuardAsNothingId));
        guarded.Dispose();
    }

    [Fact]
    public void A_broken_sentence_is_erased_and_never_comes_true()
    {
        var (play, session) = Start("inscribe", card: Deed, strength: 30, energy: 9, health: 2000);
        play.CombatDriver!.EndTurn();
        var sentence = Assert.Single(Sentences(play));
        var hp = Hero(play).Health.Current;
        Strike(play, session, sentence);
        Strike(play, session, State(play).GetCombatant(sentence.Id));
        Assert.False(State(play).GetCombatant(sentence.Id).IsAlive);
        // Nothing of it reaches you: the next sentence is two dawns away.
        play.CombatDriver.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Equal(hp, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Nisaba.MeasureWithheldId));
        play.Dispose();
    }

    [Fact]
    public void No_more_than_three_sentences_ever_stand_even_in_lapis()
    {
        var (play, _) = Start("inscribe", health: 9000, statuses: (Nisaba.LapisRecordId, 1));
        play.CombatDriver!.EndTurn();
        // Lapis writes two at a time, of ninety.
        Assert.Equal(2, Sentences(play).Count);
        Assert.All(Sentences(play), s => Assert.Equal(Nisaba.LapisSentenceHealth, s.Health.Max));
        for (var turn = 0; turn < 12; turn++)
        {
            Assert.InRange(Sentences(play).Count, 0, Nisaba.MostStanding);
            // never the same sentence twice at once
            var kinds = Sentences(play).Select(s => Nisaba.Sentences.Single(k => FightProbe.StacksOf(s, Nisaba.SentenceKindId(k.Key)) > 0).Key).ToList();
            Assert.Equal(kinds.Count, kinds.Distinct().Count());
            play.CombatDriver.EndTurn();
        }
        play.Dispose();
    }

    // With three standing her cycle does not pick Inscribe: the slot telegraphs the Strike instead.
    [Fact]
    public void With_three_standing_she_strikes_instead_of_writing()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloCycle(Nisaba.EnemyId, "inscribe", "strike_of_the_reed", "dry_the_clay", "read_aloud"),
            [.. Enumerable.Repeat(Working, 12)], health: 9000);
        var her = Her(play).Id;
        Assert.StartsWith("Inscribe", play.CombatDriver!.Current!.UpcomingIntentFor(her)!.Label);
        for (var turn = 0; turn < 12; turn++)
        {
            var slot = (play.CombatDriver.Current!.State.CurrentRound - 1) % 4;
            var label = play.CombatDriver.Current.UpcomingIntentFor(her)!.Label;
            if (slot == 0)
                Assert.StartsWith(Sentences(play).Count >= 3 ? "Strike of the Reed" : "Inscribe", label);
            play.CombatDriver.EndTurn();
        }
        play.Dispose();
    }

    [Fact]
    public void Dry_the_clay_raises_every_sentence_by_thirty()
    {
        var (play, _, _) = FightProbe.Start(FightProbe.SoloCycle(Nisaba.EnemyId, "inscribe", "dry_the_clay"),
            [.. Enumerable.Repeat(Working, 12)], health: 9000);
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        var sentence = Assert.Single(Sentences(play));
        Assert.Equal(Nisaba.SentenceHealth + Nisaba.DryHealth, sentence.Health.Max);
        Assert.Equal(Nisaba.SentenceHealth + Nisaba.DryHealth, sentence.Health.Current);
        play.Dispose();
    }

    [Fact]
    public void Read_aloud_deals_fourteen_for_each_standing_sentence()
    {
        // One sentence written on the first round still stands when she reads on the second.
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloCycle(Nisaba.EnemyId, "inscribe", "read_aloud"),
            [.. Enumerable.Repeat("ash_register", 12)], health: 9000);
        play.CombatDriver!.EndTurn();
        var standing = Sentences(play).Count;
        Assert.Equal(1, standing);
        var hp = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        Assert.Equal(Nisaba.ReadPerSentence * standing, hp - Hero(play).Health.Current);
        play.Dispose();
    }

    // ── the Final Line ────────────────────────────────────────────────────────────────────────────────────

    private static (RunPlayback Play, InteractiveRunSession Session) AtTheFinalLine()
    {
        var (play, session) = Start("strike_of_the_reed", card: Deed, strength: 100, energy: 9, health: 5000);
        // 106 a blow: seven of them take her from 950 to 208, under 30 %.
        for (var i = 0; i < 20 && FightProbe.StacksOf(Her(play), Nisaba.FinalLineWrittenId) == 0; i++)
        {
            if (play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Deed))
                Strike(play, session, Her(play));
            else
                play.CombatDriver.EndTurn();
        }
        Assert.Equal(1, FightProbe.StacksOf(Her(play), Nisaba.FinalLineWrittenId));
        return (play, session);
    }

    [Fact]
    public void At_thirty_percent_the_tablet_is_wiped_and_only_the_final_line_stands()
    {
        var (play, _) = AtTheFinalLine();
        var her = Her(play);
        Assert.Equal(1, FightProbe.StacksOf(her, Nisaba.FinalLineWrittenId));
        Assert.Equal(1, FightProbe.StacksOf(her, Nisaba.IndelibleId));
        Assert.Equal(1, FightProbe.StacksOf(her, Nisaba.LapisRecordId));
        Assert.Empty(Sentences(play));
        var line = FinalLine(play)!;
        Assert.Equal(Nisaba.FinalLineHealth, line.Health.Max);
        Assert.Equal(Nisaba.FinalLineCountdown, FightProbe.StacksOf(line, Nisaba.ComesTrueId));
        play.Dispose();
    }

    [Fact]
    public void While_the_final_line_stands_she_cannot_fall_below_one()
    {
        var (play, session) = AtTheFinalLine();
        StrikeOrWait(play, session, () => Her(play));
        StrikeOrWait(play, session, () => Her(play));
        Assert.Equal(1, Her(play).Health.Current);
        Assert.True(Her(play).IsAlive);
        play.Dispose();
    }

    [Fact]
    public void Breaking_the_final_line_turns_the_reed_and_she_can_die()
    {
        var (play, session) = AtTheFinalLine();
        var hp = Her(play).Health.Current;
        for (var blow = 0; blow < 5 && FinalLine(play) is not null; blow++)
        {
            hp = Her(play).Health.Current;
            StrikeOrWait(play, session, () => FinalLine(play)!);
        }
        Assert.Null(FinalLine(play));
        Assert.Equal(0, FightProbe.StacksOf(Her(play), Nisaba.IndelibleId));
        Assert.Equal(hp - Nisaba.ReedTurns, Her(play).Health.Current);
        // …and now she falls like anything else, and the fight is won.
        for (var blow = 0; blow < 6 && play.Session!.Run.Result == RunResult.Ongoing; blow++)
            StrikeOrWait(play, session, () => Her(play));
        Assert.Equal(RunResult.Victory, play.Session!.Run.Result);
        play.Dispose();
    }

    [Fact]
    public void On_the_fourth_dawn_the_final_line_erases_you()
    {
        var (play, _) = AtTheFinalLine();
        for (var dawn = 0; dawn < Nisaba.FinalLineCountdown && play.CombatDriver!.Current is { IsOver: false }; dawn++)
            play.CombatDriver.EndTurn();
        Assert.Equal(RunResult.Defeat, play.Session!.Run.Result);
        play.Dispose();
    }

    // In phase 3 she strikes and reads aloud in turn, and the Final Line is read as one sentence.
    [Fact]
    public void After_the_final_line_she_strikes_and_reads_in_turn()
    {
        var (play, _) = AtTheFinalLine();
        var her = Her(play).Id;
        var labels = new List<string>();
        for (var turn = 0; turn < 3; turn++)
        {
            labels.Add(play.CombatDriver!.Current!.UpcomingIntentFor(her)!.Label);
            play.CombatDriver.EndTurn();
        }
        Assert.Contains(labels, l => l.StartsWith("Strike of the Reed", StringComparison.Ordinal));
        Assert.Contains(labels, l => l.StartsWith("Read Aloud", StringComparison.Ordinal));
        Assert.DoesNotContain(labels, l => l.StartsWith("Inscribe", StringComparison.Ordinal) || l.StartsWith("Dry", StringComparison.Ordinal));
        play.Dispose();
    }

    // One blow from above 30 % to nothing still leaves her the Final Line.
    [Fact]
    public void She_cannot_be_killed_before_the_final_line_is_written()
    {
        var (play, session) = Start("strike_of_the_reed", card: Deed, strength: 2000, energy: 3, health: 5000);
        Strike(play, session, Her(play));
        Assert.True(Her(play).IsAlive);
        Assert.Equal(1, Her(play).Health.Current);
        Assert.NotNull(FinalLine(play));
        Assert.Equal(1, FightProbe.StacksOf(Her(play), Nisaba.IndelibleId));
        play.Dispose();
    }
}
