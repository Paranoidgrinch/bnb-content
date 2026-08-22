using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// A biography that will not be finished badly. Each life has a condition under which its death is FINAL, and
// the whole fight is about meeting one — kill it without meeting the condition and the account is simply
// rewritten. These tests kill it three ways.
public class ObituaryWithThreeEndingsTests
{
    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static bool Has(RunPlayback play, CombatantId id, string status) =>
        Enemy(play, id).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    // The Obituary at its authored 128 HP. The rewrites set HP to a fixed number, and death prevention
    // clamps that number to the combatant's MAXIMUM — so a frail probe body would quietly turn "survives at
    // 46" into "survives at 12" and prove nothing.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Fight(
        string intent, params (string, int)[] statuses) =>
        FightProbe.Start(
            FightProbe.Solo(ObituaryWithThreeEndings.EnemyId, intent, 9, statuses),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

    // Beat it down until it is dead or `until` appears on it, ending turns as the hand runs out. Stopping on
    // the phase marker matters: the next life's clause only goes on at the player's next turn start, so a
    // loop that kept swinging would kill the rewritten body before it had a clause to protect it.
    // Returns false if the combat ended — a final death takes the driver's combat away with it.
    private static bool GrindDown(RunPlayback play, CombatantId id, string? until = null)
    {
        for (var turn = 0; turn < 12; turn++)
        {
            while (play.CombatDriver?.Current is { } combat && combat.Hand.Count > 0)
            {
                var enemy = combat.State.Combatants.FirstOrDefault(c => c.Id == id);
                if (enemy is null || enemy.Health.Current <= 0)
                    return true;
                if (until is not null && Has(play, id, until))
                    return true;
                play.CombatDriver.PlayCard(combat.Hand.First().Id, id);
            }
            if (play.CombatDriver?.Current is null)
                return false;
            if (until is not null && Has(play, id, until))
                return true;
            play.CombatDriver.EndTurn();
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([0]);
        }
        return play.CombatDriver?.Current is not null;
    }

    // 13.1: with nothing owed, the first death is the last one. The encounter really can end after one life,
    // and when it does the combat ends with it.
    [Fact]
    public void A_settled_record_lets_the_first_death_stand()
    {
        var (play, session, obituary) = Fight("an_orderly_decline");

        // Nothing has been issued, so no clause stands.
        Assert.False(Has(play, obituary, ObituaryWithThreeEndings.RespectableLifeId));

        var stillFighting = GrindDown(play, obituary);
        Assert.Null(session.Error);

        // Either the combat is over, or the body is down — and in neither case was it rewritten.
        if (play.CombatDriver?.Current is not null)
            Assert.False(Has(play, obituary, ObituaryWithThreeEndings.HeroicPhaseId));
        Assert.False(stillFighting && Has(play, obituary, ObituaryWithThreeEndings.HeroicPhaseId));
        play.Dispose();
    }

    // 13.3 / 13.4: owe it a notice and the same death does not take — the account is rewritten at 46 HP with
    // 8 Block, and it is living a Heroic Life.
    [Fact]
    public void An_unsettled_record_rewrites_the_death()
    {
        var (play, session, obituary) = Fight("no_outstanding_matters");

        play.CombatDriver!.EndTurn(); // "No Outstanding Matters": 10 damage and a debt of its own
        Assert.True(Has(play, obituary, ObituaryWithThreeEndings.RespectableLifeId));

        GrindDown(play, obituary, until: ObituaryWithThreeEndings.HeroicPhaseId);
        Assert.Null(session.Error);

        var after = Enemy(play, obituary);
        Assert.Equal(46, after.Health.Current);
        Assert.True(Has(play, obituary, ObituaryWithThreeEndings.HeroicPhaseId));
        // The clause is spent: it rewrites once.
        Assert.False(Has(play, obituary, ObituaryWithThreeEndings.RespectableLifeId));
        play.Dispose();
    }

    // 13.2: the Notice is the tool for settling the record before lethal damage — and settling it takes the
    // clause off, which is what makes the next death final.
    [Fact]
    public void Settling_the_record_takes_the_clause_off()
    {
        var (play, _, obituary) = Fight("no_outstanding_matters");

        play.CombatDriver!.EndTurn();
        Assert.True(Has(play, obituary, ObituaryWithThreeEndings.RespectableLifeId));

        // A Notice cited and then played settles one debt; here the debt is settled directly by playing the
        // cited card the Obituary issued on the next hand.
        play.CombatDriver.EndTurn();
        var cited = Hand(play).FirstOrDefault(c => c.HasMark(new TagId(ObituaryWithThreeEndings.NoticeMark)));
        if (cited is not null)
        {
            play.CombatDriver.PlayCard(cited.Id, obituary);
            Assert.False(Has(play, obituary, ObituaryWithThreeEndings.RespectableLifeId));
        }
        play.Dispose();
    }

    // 13.5: in its second life only a death you redacted something for is final. Without that it is rewritten
    // again — down to a Completely Accurate Account at 32.
    [Fact]
    public void A_heroic_death_without_a_redaction_is_rewritten_again()
    {
        var (play, session, obituary) = Fight("an_orderly_decline",
            (ObituaryWithThreeEndings.HeroicPhaseId, 1));

        play.CombatDriver!.EndTurn(); // the player's turn opens: the heroic clause goes on
        Assert.True(Has(play, obituary, ObituaryWithThreeEndings.HeroicLifeId));

        GrindDown(play, obituary, until: ObituaryWithThreeEndings.AccuratePhaseId);
        Assert.Null(session.Error);

        Assert.Equal(32, Enemy(play, obituary).Health.Current);
        Assert.True(Has(play, obituary, ObituaryWithThreeEndings.AccuratePhaseId));
        play.Dispose();
    }

    // …and a Redacted card played that turn takes the heroic clause off, so the death would stand.
    [Fact]
    public void A_redacted_card_played_makes_the_heroic_death_final()
    {
        var (play, session, obituary) = Fight("record_a_respectable_career",
            (ObituaryWithThreeEndings.HeroicPhaseId, 1));

        play.CombatDriver!.EndTurn(); // Suppress the Witness: it redacts one of your cards for you
        Assert.True(Has(play, obituary, ObituaryWithThreeEndings.HeroicLifeId));

        var redacted = Hand(play).FirstOrDefault(c => c.HasMark(new TagId(ActTwo.RedactedMark)));
        Assert.NotNull(redacted);
        play.CombatDriver.PlayCard(redacted!.Id, obituary);
        Assert.Null(session.Error);

        Assert.False(Has(play, obituary, ObituaryWithThreeEndings.HeroicLifeId));
        play.Dispose();
    }

    // 13.8 Phase III: all lethal events are final — no clause is ever put on, whatever the record says.
    [Fact]
    public void The_accurate_account_has_no_further_amendments()
    {
        var (play, session, obituary) = Fight("no_outstanding_matters",
            (ObituaryWithThreeEndings.AccuratePhaseId, 1));

        play.CombatDriver!.EndTurn(); // it makes a debt of its own — and it buys nothing
        Assert.Null(session.Error);

        Assert.False(Has(play, obituary, ObituaryWithThreeEndings.RespectableLifeId));
        Assert.False(Has(play, obituary, ObituaryWithThreeEndings.HeroicLifeId));
        play.Dispose();
    }
}
