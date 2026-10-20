using BnbContent.Converter.Apprentice;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// The Sorcerer's Apprentice in live fights of the real document (sorcerers_apprentice_master.md): his start, his
// wand and its actions, and each card's two modes. The numbers are the master's BALANCE DRAFTS.
public class ApprenticeTests
{
    internal const string Quiet = "ordinance_tablet";
    internal const string QuietIntent = "stone_precedent";   // guards itself, does nothing to the hero

    internal static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Fight(
        string[] deck, int energy = 9, int? health = null, string enemy = Quiet, string intent = QuietIntent) =>
        FightProbe.Start(FightProbe.Solo(enemy, intent, energy: energy), deck, health, character: ApprenticeCharacter.Id);

    internal static InteractiveCombat Now(RunPlayback play) => play.CombatDriver!.Current!;
    internal static CombatantState Hero(RunPlayback play) => Now(play).State.GetCombatant(Now(play).HeroId);
    internal static CombatantState Body(RunPlayback play, CombatantId id) => Now(play).State.GetCombatant(id);
    internal static WandState Wand(RunPlayback play) => Now(play).State.GetWand(Now(play).HeroId)!;
    internal static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    internal static int Standing(CombatantState c) => c.Health.Current + Block(c);

    internal static void Play(RunPlayback play, InteractiveRunSession session, string card, CombatantId target)
    {
        play.CombatDriver!.PlayCard(Now(play).Hand.First(c => c.DefinitionId.value == card).Id, target);
        Assert.Null(session.Error);
    }

    // Inscribe the first card of this kind in hand into this slot.
    internal static void Inscribe(RunPlayback play, InteractiveRunSession session, string card, int slot)
    {
        var driver = play.CombatDriver!;
        driver.UseAction(new CardDefinitionId(ApprenticeWand.Inscribe), null);
        Assert.Null(session.Error);
        var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(driver.PendingCardChoice);
        driver.SupplyCardChoice([offered.First(c => c.DefinitionId.value == card).Id]);
        if (driver.PendingOptionChoice is { } slots)
            driver.SupplyOptionChoice([slots.ToList().IndexOf($"Slot {slot + 1}")]);
        Assert.Null(session.Error);
    }

    internal static void Cast(RunPlayback play, InteractiveRunSession session, CombatantId target)
    {
        play.CombatDriver!.UseAction(new CardDefinitionId(ApprenticeWand.Cast), target);
        Assert.Null(session.Error);
    }

    [Fact]
    public void He_starts_with_his_own_deck_his_five_actions_and_a_wand_of_four_slots()
    {
        var (play, _, _) = FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 3), character: ApprenticeCharacter.Id);
        var combat = Now(play);
        Assert.Equal(ApprenticeWand.Actions, combat.Actions.Select(a => a.value));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ApprenticeKeywords.Wand));
        Assert.Equal(10, combat.State.GetCardZones(combat.HeroId).AllCards.Count);
        Assert.Equal(70, Hero(play).Health.Max);
        Assert.Equal((4, 1, 0, 8), (Wand(play).Capacity, Wand(play).Focus, Wand(play).Exhaustion, Wand(play).ExhaustionCap));
        play.Dispose();

        // …and the Bureaucrat has none of it.
        var (bureaucrat, _, _) = FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: 3));
        Assert.Empty(Now(bureaucrat).Actions);
        Assert.Null(Now(bureaucrat).State.GetWand(Now(bureaucrat).HeroId));
        bureaucrat.Dispose();
    }

    // Master §12.1 with the real starter cards: Ink Blot → Candle Stub → Flyleaf deals 6 + 3.
    [Fact]
    public void The_first_lesson_heightens_a_candle_and_echoes_it()
    {
        var (play, session, enemy) = Fight(["ap_ink_blot", "ap_candle_stub", "ap_flyleaf", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Assert.Equal(3, Wand(play).Focus);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ApprenticeKeywords.Grit));
        Inscribe(play, session, "ap_ink_blot", 0);
        Inscribe(play, session, "ap_candle_stub", 1);
        Inscribe(play, session, "ap_flyleaf", 2);
        play.CombatDriver!.EndTurn();
        // Next turn the discard is the whole hand again: two Chalk Lines for Focus.
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        // The tablet guards itself, so what the cast took is its HP and its Block together.
        var before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        Assert.Equal(before - 9, Standing(Body(play, enemy)));
        Assert.Equal(3, Wand(play).Exhaustion);
        play.Dispose();
    }
}
