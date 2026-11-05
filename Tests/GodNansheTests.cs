using BnbContent.Converter.Gods;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// G3, Nanshe (ACT_V_REWORK_PLAN.md): a day's portion of 3 Energy and 5 cards, Take From Tomorrow writing Debt, Debt
// taken off the next day down to 1 and 1, collected after Day III; the Lean Years; the Measure Made Equal.
public class GodNansheTests
{
    private static CombatState State(RunPlayback play) => play.CombatDriver!.Current!.State;

    private static CombatantState Hero(RunPlayback play) => State(play).GetCombatant(play.CombatDriver!.Current!.HeroId);

    private static CombatantState Her(RunPlayback play) =>
        State(play).Combatants.Single(c => c.DefinitionId.value == Nanshe.EnemyId);

    private static int Debt(RunPlayback play) => FightProbe.StacksOf(Hero(play), Nanshe.DebtId);

    private const string Deed = "paper_cut";
    private const string Rite = "ash_register";
    private const string Charm = "sanctioned_charm"; // 0: gain 1 Energy, draw 1 card, Exhaust

    private static (RunPlayback Play, InteractiveRunSession Session) Start(
        string intent, string card = Rite, int strength = 0, int health = 2000, params (string Status, int Stacks)[] hero)
    {
        (string, int)[] statuses = [.. hero, .. strength > 0 ? [("strength", strength)] : Array.Empty<(string, int)>()];
        var probe = FightProbe.SoloAgainstHero(Nanshe.EnemyId, intent, 3, statuses);
        var (play, session, _) = FightProbe.Start(probe, [.. Enumerable.Repeat(card, 20)], health: health);
        return (play, session);
    }

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, bool target = false)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target ? Her(play).Id : null);
        Assert.True(session.Error is null, session.Error);
    }

    private static int InHand(RunPlayback play, string cardId) =>
        play.CombatDriver!.Current!.Hand.Count(c => c.DefinitionId.value == cardId);

    [Fact]
    public void Day_one_is_a_portion_of_three_and_five_with_take_from_tomorrow_in_hand()
    {
        var (play, _) = Start("open_the_canals");
        var hero = Hero(play);
        Assert.Equal(1, FightProbe.StacksOf(Her(play), Nanshe.DayId));
        Assert.Equal(Nanshe.PortionEnergy, FightProbe.StacksOf(hero, Nanshe.PortionEnergyId));
        Assert.Equal(Nanshe.PortionCards, FightProbe.StacksOf(hero, Nanshe.PortionCardsId));
        Assert.Equal(1, InHand(play, Nanshe.TakeFromTomorrowId));
        Assert.Equal(Nanshe.PortionCards, play.CombatDriver!.Current!.Hand.Count - 1);
        Assert.Equal(Nanshe.PortionEnergy, play.CombatDriver.Current.HeroEnergy);
        play.Dispose();
    }

    // Her whole Distribution is on the tablet before Day I.
    [Fact]
    public void Her_three_days_are_readable_before_day_one()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.SoloCycle(Nanshe.EnemyId, "open_the_canals", "the_net_of_nanshe", "judgment_of_the_new_year"),
            [.. Enumerable.Repeat(Rite, 12)], health: 2000);
        var days = play.CombatDriver!.Current!.UpcomingIntentsFor(Her(play).Id);
        Assert.Equal(3, days.Count);
        Assert.StartsWith("Day I,", days[0].Label);
        Assert.StartsWith("Day II,", days[1].Label);
        Assert.StartsWith("Day III,", days[2].Label);
        play.Dispose();
    }

    [Fact]
    public void Take_from_tomorrow_gives_a_point_and_a_card_now_writes_debt_and_comes_back()
    {
        var (play, session) = Start("open_the_canals");
        Play(play, session, Rite);
        Play(play, session, Rite);
        var hand = play.CombatDriver!.Current!.Hand.Count;
        var energy = play.CombatDriver.Current.HeroEnergy;
        Play(play, session, Nanshe.TakeFromTomorrowId);
        Play(play, session, Nanshe.TakeFromTomorrowId);
        Assert.Equal(2, Debt(play));
        Assert.Equal(energy + 2, play.CombatDriver.Current!.HeroEnergy);
        Assert.Equal(hand + 2, play.CombatDriver.Current.Hand.Count);
        Assert.Equal(1, InHand(play, Nanshe.TakeFromTomorrowId));
        // Retain: it stays for tomorrow too
        play.CombatDriver.EndTurn();
        Assert.Equal(1, InHand(play, Nanshe.TakeFromTomorrowId));
        play.Dispose();
    }

    [Fact]
    public void Debt_comes_off_the_next_day_one_energy_and_one_card_a_point()
    {
        var (play, session) = Start("open_the_canals");
        Play(play, session, Nanshe.TakeFromTomorrowId);
        play.CombatDriver!.EndTurn();
        var hero = Hero(play);
        Assert.Equal(2, FightProbe.StacksOf(Her(play), Nanshe.DayId));
        Assert.Equal(Nanshe.PortionEnergy - 1, FightProbe.StacksOf(hero, Nanshe.PortionEnergyId));
        Assert.Equal(Nanshe.PortionCards - 1, FightProbe.StacksOf(hero, Nanshe.PortionCardsId));
        Assert.Equal(Nanshe.PortionEnergy - 1, play.CombatDriver.Current!.HeroEnergy);
        Assert.Equal(Nanshe.PortionCards - 1, play.CombatDriver.Current.Hand.Count - InHand(play, Nanshe.TakeFromTomorrowId));
        Assert.Equal(0, Debt(play));
        play.Dispose();
    }

    // The minimum portion: four Debt make a day of 1 and 1 and settle all four; more stays owed.
    [Theory]
    [InlineData(4, 0)]
    [InlineData(6, 2)]
    public void A_day_never_falls_below_one_energy_and_one_card(int debt, int left)
    {
        var (play, _) = Start("open_the_canals", hero: (Nanshe.DebtId, debt));
        play.CombatDriver!.EndTurn();
        var hero = Hero(play);
        Assert.Equal(1, FightProbe.StacksOf(hero, Nanshe.PortionEnergyId));
        Assert.Equal(1, FightProbe.StacksOf(hero, Nanshe.PortionCardsId));
        Assert.Equal(1, play.CombatDriver!.Current!.HeroEnergy);
        Assert.Equal(1, play.CombatDriver.Current.Hand.Count - InHand(play, Nanshe.TakeFromTomorrowId));
        Assert.Equal(left, Debt(play));
        play.Dispose();
    }

    [Fact]
    public void After_day_three_the_open_debt_is_collected_through_block()
    {
        var (play, session) = Start("open_the_canals", card: "cower_behind_a_desk");
        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Equal(3, FightProbe.StacksOf(Her(play), Nanshe.DayId));
        Play(play, session, Nanshe.TakeFromTomorrowId);
        Play(play, session, Nanshe.TakeFromTomorrowId);
        Play(play, session, Nanshe.TakeFromTomorrowId);
        Play(play, session, "cower_behind_a_desk");
        var hp = Hero(play).Health.Current;
        play.CombatDriver.EndTurn();
        // Open the Canals hits 22 into 5 Block; then 3 Debt × 10, through Block.
        Assert.Equal(hp - (Nanshe.CanalsDamage - 5) - 3 * Nanshe.DebtCollected, Hero(play).Health.Current);
        Assert.Equal(0, Debt(play));
        play.Dispose();
    }

    [Fact]
    public void Judgment_deals_thirty_and_ten_a_debt()
    {
        var (play, session) = Start("judgment_of_the_new_year");
        Play(play, session, Nanshe.TakeFromTomorrowId);
        Play(play, session, Nanshe.TakeFromTomorrowId);
        var hp = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        Assert.Equal(hp - Nanshe.JudgmentDamage - 2 * Nanshe.JudgmentPerDebt, Hero(play).Health.Current);
        play.Dispose();
    }

    // ── the later phases ──────────────────────────────────────────────────────────────────────────────────

    private static (RunPlayback Play, InteractiveRunSession Session) Wounded(int below, string card)
    {
        var (play, session) = Start("the_net_of_nanshe", card: Deed, strength: 200, health: 5000);
        while (Her(play).Health.Current * 100 > Her(play).Health.Max * below)
        {
            if (InHand(play, Deed) == 0 || play.CombatDriver!.Current!.HeroEnergy == 0)
                play.CombatDriver!.EndTurn();
            else
                Play(play, session, Deed, target: true);
        }
        return (play, session);
    }

    [Fact]
    public void In_the_lean_years_a_day_is_four_cards_and_what_your_cards_make_writes_debt()
    {
        var (play, session) = Wounded(65, Deed);
        Assert.Equal(1, FightProbe.StacksOf(Her(play), Nanshe.LeanYearsId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(Nanshe.LeanCards, FightProbe.StacksOf(Hero(play), Nanshe.PortionCardsId));
        // the day's own refill and draw are hers, and write nothing
        var debt = Debt(play);
        Assert.Equal(0, debt);
        // Take From Tomorrow is counted once, as itself: 1 Debt, nothing more.
        Play(play, session, Nanshe.TakeFromTomorrowId);
        Assert.Equal(debt + 1, Debt(play));
        play.Dispose();
    }

    [Fact]
    public void In_the_lean_years_a_card_that_gives_energy_and_a_card_writes_two_debt()
    {
        var probe = FightProbe.SoloAgainstHero(Nanshe.EnemyId, "the_net_of_nanshe", 3, ("strength", 200));
        var (play, session, _) = FightProbe.Start(probe, [.. Enumerable.Repeat(Deed, 10), .. Enumerable.Repeat(Charm, 10)], health: 5000);
        while (FightProbe.StacksOf(Her(play), Nanshe.LeanYearsId) == 0)
        {
            if (InHand(play, Deed) == 0 || play.CombatDriver!.Current!.HeroEnergy == 0)
                play.CombatDriver!.EndTurn();
            else
                Play(play, session, Deed, target: true);
        }
        for (var turn = 0; turn < 6 && (InHand(play, Charm) == 0 || InHand(play, Deed) == 0); turn++)
            play.CombatDriver!.EndTurn();
        Play(play, session, Deed, target: true); // room for the Energy the charm makes
        var debt = Debt(play);
        Play(play, session, Charm);
        Assert.Equal(debt + 2, Debt(play));
        play.Dispose();
    }

    [Fact]
    public void The_measure_made_equal_lowers_the_healthier_side_to_the_other()
    {
        var (play, _) = Wounded(30, Deed);
        Assert.Equal(1, FightProbe.StacksOf(Her(play), Nanshe.MeasureEqualId));
        var share = Her(play).Health.Current * 100 / Her(play).Health.Max;
        play.CombatDriver!.EndTurn();
        var hero = Hero(play);
        Assert.InRange(hero.Health.Current * 100 / hero.Health.Max, share - 1, share);
        play.Dispose();
    }
}
