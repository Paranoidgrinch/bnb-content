using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Black-Ink Oracle asks about something it has blacked out. Answer and be right, buy the certainty, or
// refuse — and refusing costs exactly what being wrong costs, which is what makes it a decision.
public class BlackInkOracleTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Ink(RunPlayback play, CombatantId id) =>
        FightProbe.StacksOf(Enemy(play, id), BlackInkOracle.BlackInkId);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static CardInstance? Queried(RunPlayback play) =>
        Hand(play).FirstOrDefault(c => c.HasMark(new TagId(BlackInkOracle.QueriedMark)));

    // paper_cut is a 1-Energy card, so option 1 ("costs 1") is the correct answer and options 0 and 2 are not.
    private static (RunPlayback, InteractiveRunSession, CombatantId) Riddle(int energy = 3)
    {
        var fight = FightProbe.Start(
            FightProbe.Solo(BlackInkOracle.EnemyId, "pose_the_missing_question", energy: energy),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);
        fight.Play.CombatDriver!.EndTurn(); // the Oracle poses; the player's draw raises the riddle
        return fight;
    }

    // 9.3: the Oracle does not ask every turn — the riddle is PREPARED by an intent and asked after the next
    // normal draw, about one card it has picked.
    [Fact]
    public void The_riddle_is_posed_before_it_is_asked()
    {
        var (play, _, _) = Riddle();

        Assert.NotNull(play.CombatDriver!.PendingOptionChoice);
        Assert.Equal(5, play.CombatDriver.PendingOptionChoice!.Count);
        Assert.NotNull(Queried(play));
        play.Dispose();
    }

    // 9.5 ANSWER, correct: the Oracle loses 8 HP and an ink. The HP loss is not a Damage event, so its own
    // Block — 12 from posing the question — does not stop it.
    [Fact]
    public void A_correct_answer_costs_the_oracle_eight_hp_through_its_block()
    {
        var (play, session, oracle) = FightProbe.Start(
            FightProbe.Solo(BlackInkOracle.EnemyId, "pose_the_missing_question",
                (BlackInkOracle.BlackInkId, 2)),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);
        play.CombatDriver!.EndTurn();

        var before = Enemy(play, oracle);
        var hp = before.Health.Current;
        var block = before.DefensivePools[StandardCombatIds.BlockDefensivePool].Current;
        Assert.Equal(12, block);

        play.CombatDriver.SupplyOptionChoice([1]); // "costs 1" — and paper_cut costs 1
        Assert.Null(session.Error);

        var after = Enemy(play, oracle);
        Assert.Equal(hp - 8, after.Health.Current);
        Assert.Equal(block, after.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
        Assert.Equal(1, Ink(play, oracle));
        play.Dispose();
    }

    // …incorrect: an ink for the Oracle, and the card is blacked out for its next play.
    [Fact]
    public void A_wrong_answer_inks_the_oracle_and_redacts_the_card()
    {
        var (play, session, oracle) = Riddle();
        var queried = Queried(play)!;

        play.CombatDriver!.SupplyOptionChoice([0]); // "costs 0" — it does not
        Assert.Null(session.Error);

        Assert.Equal(1, Ink(play, oracle));
        var after = Hand(play).First(c => c.Id == queried.Id);
        Assert.True(after.HasMark(new TagId(ActTwo.RedactedMark)));
        play.Dispose();
    }

    // 9.5 DECLINE: no resource cost, but exactly the price of being wrong.
    [Fact]
    public void Declining_costs_what_being_wrong_costs()
    {
        var (play, session, oracle) = Riddle();
        var queried = Queried(play)!;
        var energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

        play.CombatDriver!.SupplyOptionChoice([4]); // decline
        Assert.Null(session.Error);

        Assert.Equal(1, Ink(play, oracle));
        Assert.True(Hand(play).First(c => c.Id == queried.Id).HasMark(new TagId(ActTwo.RedactedMark)));
        Assert.Equal(energy, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);
        play.Dispose();
    }

    // 9.5 REVEAL: an Energy buys certainty. No ink, no redaction — that is the whole point of the option.
    [Fact]
    public void Revealing_buys_certainty_for_an_energy()
    {
        var (play, session, oracle) = Riddle();
        var queried = Queried(play)!;
        var energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

        play.CombatDriver!.SupplyOptionChoice([3]);
        Assert.Null(session.Error);

        Assert.Equal(energy - 1, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Equal(0, Ink(play, oracle));
        Assert.False(Hand(play).First(c => c.Id == queried.Id).HasMark(new TagId(ActTwo.RedactedMark)));
        play.Dispose();
    }

    // "REVEAL must never be presented as a supposedly safe option that is impossible to select." With no
    // Energy left it is still selectable, and costs an Overdue owed to the Oracle instead.
    [Fact]
    public void Revealing_with_no_energy_costs_an_overdue_instead()
    {
        var (play, session, oracle) = Riddle(energy: 0);
        var queried = Queried(play)!;

        play.CombatDriver!.SupplyOptionChoice([3]);
        Assert.Null(session.Error);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId));
        Assert.Equal(0, Ink(play, oracle));
        Assert.False(Hand(play).First(c => c.Id == queried.Id).HasMark(new TagId(ActTwo.RedactedMark)));
        play.Dispose();
    }

    // 9.2: "Maximum 3." Refusing every riddle never inks it past the ceiling.
    [Fact]
    public void The_ink_never_passes_three()
    {
        var (play, _, oracle) = FightProbe.Start(
            FightProbe.Solo(BlackInkOracle.EnemyId, "pose_the_missing_question"),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

        for (var turn = 0; turn < 6; turn++)
        {
            play.CombatDriver!.EndTurn();
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([4]); // decline every time
        }

        Assert.Equal(3, Ink(play, oracle));
        play.Dispose();
    }

    // Signature — Devour the Unstated Answer: at Black Ink 3 the next offensive intent becomes 14 + 4 × 3, and
    // the ink is spent.
    [Fact]
    public void At_three_inks_the_next_attack_devours_the_answer()
    {
        var (play, _, oracle) = FightProbe.Start(
            FightProbe.Solo(BlackInkOracle.EnemyId, "stone_paw_of_omission",
                (BlackInkOracle.BlackInkId, 3)),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(26, before - Hero(play).Health.Current); // not the Stone Paw's own 18
        Assert.Equal(0, Ink(play, oracle));
        play.Dispose();
    }
}
