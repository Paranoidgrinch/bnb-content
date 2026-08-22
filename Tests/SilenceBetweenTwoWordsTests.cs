using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Silence Between Two Words. Each turn two of your cards become its Words, and what it takes depends on
// how many you speak. The interesting case is the one where you speak neither: Perfect Silence hurts it, and
// costs you two cards to buy.
public class SilenceBetweenTwoWordsTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static int Echo(RunPlayback play, CombatantId id) =>
        FightProbe.StacksOf(Enemy(play, id), SilenceBetweenTwoWords.EchoId);

    private static List<CardInstance> Words(RunPlayback play) =>
        Hand(play).Where(c =>
            c.HasMark(new TagId(SilenceBetweenTwoWords.FirstWordMark))
            || c.HasMark(new TagId(SilenceBetweenTwoWords.SecondWordMark))).ToList();

    private static (RunPlayback, InteractiveRunSession, CombatantId) Fight(string intent, int energy = 9) =>
        FightProbe.Start(
            FightProbe.Solo(SilenceBetweenTwoWords.EnemyId, intent, energy: energy),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

    // 8.2: after the player's normal draw, two DIFFERENT cards in hand are the Words.
    [Fact]
    public void Two_different_cards_become_the_words()
    {
        var (play, _, _) = Fight("leave_space_between_the_words");

        var words = Words(play);
        Assert.Equal(2, words.Count);
        Assert.Equal(2, words.Select(c => c.Id).Distinct().Count());
        play.Dispose();
    }

    // 8.4: both Words played — the Silence Echoes twice.
    [Fact]
    public void Speaking_both_words_echoes_twice()
    {
        var (play, session, silence) = Fight("leave_space_between_the_words");

        foreach (var word in Words(play))
            play.CombatDriver!.PlayCard(word.Id, silence);
        Assert.Null(session.Error);

        play.CombatDriver!.EndTurn(); // the Silence's turn opens: it settles the pair

        Assert.Equal(2, Echo(play, silence));
        play.Dispose();
    }

    // …exactly one played — one Echo, and a card is misfiled for the half-spoken sentence.
    [Fact]
    public void Speaking_one_word_echoes_once_and_misfiles_a_card()
    {
        var (play, session, silence) = Fight("leave_space_between_the_words");

        play.CombatDriver!.PlayCard(Words(play)[0].Id, silence);
        Assert.Null(session.Error);
        play.CombatDriver.EndTurn();

        Assert.Equal(1, Echo(play, silence));
        // The misfiling is already on its way back: it was drawn, taken back and replaced this turn, or it is
        // still sitting in the pile. Either way exactly one card was touched.
        var zones = play.CombatDriver.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);
        var misfiled = Enum.GetValues<CardZone>()
            .SelectMany(zones.GetCardsInZone)
            .Count(c => c.HasMark(new TagId(ActTwo.MisfiledMark)));
        Assert.True(misfiled <= 1);
        play.Dispose();
    }

    // 8.4 Perfect Silence: neither Word spoken. It loses 10 HP and up to 10 Block — and the HP loss is not a
    // Damage event, so the Block it is holding does not stop it.
    [Fact]
    public void Perfect_silence_costs_the_creature_ten_of_each()
    {
        var (play, _, silence) = Fight("leave_space_between_the_words");

        play.CombatDriver!.EndTurn(); // nothing spoken; the pair resolves, then the Silence gains 19 Block
        var hp = Enemy(play, silence).Health.Current;

        // A second silent turn. The resolution runs at the PLAYER's turn end, while the 19 Block from last
        // turn is still standing — so it takes ten of each, and the intent's fresh 19 lands afterwards.
        play.CombatDriver.EndTurn();
        var after = Enemy(play, silence);

        Assert.Equal(hp - 10, after.Health.Current);
        Assert.Equal(19, Block(after)); // 19 - 10 = 9 struck at turn end, then the intent's own 19 replaces it
        play.Dispose();
    }

    // 8.3: a Word is spoken only if it is PLAYED. Discarding it at the turn's end is not speaking it — the
    // silence stays perfect and the creature still pays.
    [Fact]
    public void A_word_discarded_is_a_word_unspoken()
    {
        var (play, _, silence) = Fight("leave_space_between_the_words");

        var hp = Enemy(play, silence).Health.Current;
        play.CombatDriver!.EndTurn(); // the whole hand, Words included, is discarded unplayed

        Assert.Equal(hp - 10, Enemy(play, silence).Health.Current);
        play.Dispose();
    }

    // 8.5: the Echo rides on the next direct attack — +4 each — and is spent by it.
    [Fact]
    public void The_echo_rides_the_next_attack_and_is_spent()
    {
        // A hand of nothing but Junk makes no Pair, so the Echo on the table is the only thing in play.
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(SilenceBetweenTwoWords.EnemyId, "a_word_nearly_spoken", energy: 9,
                (SilenceBetweenTwoWords.EchoId, 2)),
            deck: [.. Enumerable.Repeat("red_tape", 16)], health: 400);

        Assert.Empty(Words(play));
        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(24, before - Hero(play).Health.Current); // 16 + 4 × 2
        play.Dispose();
    }

    // Signature — Unspoken Verdict: at Echo 4 the next offensive intent becomes the verdict, and 6 + 4 × 4 is
    // the 22 the design names.
    [Fact]
    public void At_four_echoes_the_next_attack_is_the_verdict()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(SilenceBetweenTwoWords.EnemyId, "a_word_nearly_spoken", energy: 9,
                (SilenceBetweenTwoWords.EchoId, 4)),
            deck: [.. Enumerable.Repeat("red_tape", 16)], health: 400);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        // 6 flat, plus the Echo's own 4 × 4 — the 22 the design names, counted once.
        Assert.Equal(22, before - Hero(play).Health.Current);
        play.Dispose();
    }

    // 8.5: "Maximum 4." Speaking both Words turn after turn never pushes it past the ceiling.
    [Fact]
    public void The_echo_never_passes_four()
    {
        var (play, _, silence) = Fight("leave_space_between_the_words");

        for (var turn = 0; turn < 4; turn++)
        {
            foreach (var word in Words(play))
                play.CombatDriver!.PlayCard(word.Id, silence);
            play.CombatDriver!.EndTurn();
            Assert.True(Echo(play, silence) <= 4);
        }
        play.Dispose();
    }
}
