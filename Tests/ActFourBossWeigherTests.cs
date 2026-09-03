using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, boss — The Weigher of the Unspoken Heart, proved in live fights.
//
// It does not ask for a number. It weighs the COMPOSITION of the turn, and the tests follow the pan: a Deed
// toward the Heart, a Working toward the Feather, and the judgment that reads where it came to rest.
public class ActFourBossWeigherTests
{
    private const string OneCost = "paper_cut";  // Deed, 1: deal 6
    private const string Wax = "waxen_surety";   // Working, 1: gain 4 Ward Wax

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Scale(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("weigher", StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static IReadOnlyList<string> Both => [.. Enumerable.Repeat(OneCost, 10).Concat(
        Enumerable.Repeat(Wax, 10))];

    // A Deed tips the pan toward the Heart and a Working toward the Feather, by KIND and never by size.
    [Fact]
    public void The_pan_moves_by_the_kind_of_card_and_not_its_size()
    {
        // A two-card deck, so the hand is exactly one of each and the pan can be watched card by card.
        var (play, session, weigher) = FightProbe.Start(
            FightProbe.Solo("weigher_of_the_unspoken_heart", "feathers_silence", energy: 9),
            deck: [OneCost, Wax], health: 900);

        Play(play, session, OneCost, weigher);
        Assert.Equal(1, Scale(play).GetCounter(ActFour.Balance));
        Assert.Equal(1, FightProbe.StacksOf(Scale(play), ActFour.TowardTheHeartId));

        Play(play, session, Wax, null);
        Assert.Equal(0, Scale(play).GetCounter(ActFour.Balance));
        Assert.Equal(0, FightProbe.StacksOf(Scale(play), ActFour.TowardTheHeartId));
        Assert.Equal(0, FightProbe.StacksOf(Scale(play), ActFour.TowardTheFeatherId));

        // …and the other way, on a turn that leads with a working.
        play.CombatDriver!.EndTurn();
        Play(play, session, Wax, null);
        Assert.Equal(-1, Scale(play).GetCounter(ActFour.Balance));
        Assert.Equal(1, FightProbe.StacksOf(Scale(play), ActFour.TowardTheFeatherId));
        play.Dispose();
    }

    // A turn that comes to rest at nought is the only kindness in the fight: cover off, a burial off the
    // player, its next blow softer, and a Feather.
    [Fact]
    public void A_turn_weighed_true_is_answered_with_a_feather()
    {
        var (play, session, weigher) = FightProbe.Start(
            FightProbe.SoloAgainstHero("weigher_of_the_unspoken_heart", "feathers_silence", energy: 9,
                (ActFour.EntombedId, 2)),
            deck: Both, health: 900);

        Play(play, session, OneCost, weigher);
        Play(play, session, Wax, null);  // one each: the pan rests at nought
        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Scale(play), ActFour.FeatherId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // …and a turn of nothing but Deeds is condemned: two burials, a point of Strength for it, and the scale
    // is levelled by force rather than by the player.
    [Fact]
    public void A_condemned_turn_buries_the_player_and_resets_the_scale()
    {
        var (play, session, weigher) = FightProbe.Start(
            FightProbe.Solo("weigher_of_the_unspoken_heart", "feathers_silence", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 20)], health: 900);

        for (var i = 0; i < 4; i++)
            Play(play, session, OneCost, weigher);  // the pan is capped at three

        Assert.Equal(ActFour.BalanceLimit, Scale(play).GetCounter(ActFour.Balance));
        play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        Assert.Equal(1, FightProbe.StacksOf(Scale(play), "strength"));
        Assert.Equal(0, Scale(play).GetCounter(ActFour.Balance));
        play.Dispose();
    }

    // Two either way is heavy or hollow, and costs two forms.
    [Fact]
    public void Two_steps_off_true_is_two_forms()
    {
        var (play, session, weigher) = FightProbe.Start(
            FightProbe.Solo("weigher_of_the_unspoken_heart", "feathers_silence", energy: 9),
            deck: [.. Enumerable.Repeat(OneCost, 20)], health: 900);

        Play(play, session, OneCost, weigher);
        Play(play, session, OneCost, weigher);
        play.CombatDriver!.EndTurn();

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), BnbContent.Converter.Cards.Keywords.Paperwork));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EntombedId));
        play.Dispose();
    }

    // Three feathers and the heart is declared light: it bleeds 22, loses everything it was standing behind,
    // and takes a fifth more for a whole turn.
    [Fact]
    public void Three_feathers_declare_the_heart_light()
    {
        var (play, session, weigher) = FightProbe.Start(
            FightProbe.Solo("weigher_of_the_unspoken_heart", "feathers_silence", energy: 9),
            deck: Both, health: 900);

        var before = Scale(play).Health.Current;
        for (var turn = 0; turn < ActFour.FeathersToDeclare; turn++)
        {
            Play(play, session, OneCost, weigher);
            Play(play, session, Wax, null);
            play.CombatDriver!.EndTurn();
        }

        var judged = Scale(play);
        Assert.Equal(0, FightProbe.StacksOf(judged, ActFour.FeatherId));
        Assert.Equal(1, FightProbe.StacksOf(judged, ActFour.HeartDeclaredLightId));
        Assert.Equal(1, judged.GetCounter(ActFour.LightDeclarations));

        // Three cuts of 6 landed on it along the way, plus 22 of its own blood.
        Assert.True(before - judged.Health.Current >= 22, "the declaration did not cost it 22");
        play.Dispose();
    }

    // Half its blood and the heart remembers: the scale is levelled, the feathers are gone, and from here the
    // first card of every turn moves the pan two steps.
    [Fact]
    public void Half_its_blood_and_the_heart_remembers()
    {
        // Fielded one cut above the failsafe.
        var (play, session, weigher) = FightProbe.Start(
            FightProbe.Roster("weigher_half", energy: 9,
                ("weigher_of_the_unspoken_heart", "feathers_silence", 308)),
            deck: [.. Enumerable.Repeat(OneCost, 6)], health: 900);

        Assert.Equal(0, FightProbe.StacksOf(Scale(play), ActFour.HeartRemembersId));

        Play(play, session, OneCost, weigher);  // 308 → 302: the failsafe
        Assert.Equal(1, FightProbe.StacksOf(Scale(play), ActFour.HeartRemembersId));
        Assert.Equal(0, FightProbe.StacksOf(Scale(play), ActFour.FeatherId));

        // The scale is levelled, and it stays levelled: the pan is tipped by a card BEFORE that card's blow
        // lands, so the transition has the last word on the turn that caused it.
        Assert.Equal(0, Scale(play).GetCounter(ActFour.Balance));

        // The road is a line now: the first card of a turn moves the pan two steps instead of one.
        play.CombatDriver!.EndTurn();
        Play(play, session, OneCost, weigher);
        Assert.Equal(2, Scale(play).GetCounter(ActFour.Balance));

        // …and every card after it moves it one, as before.
        Play(play, session, OneCost, weigher);
        Assert.Equal(ActFour.BalanceLimit, Scale(play).GetCounter(ActFour.Balance));
        play.Dispose();
    }
}
