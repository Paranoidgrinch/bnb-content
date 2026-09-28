using BnbContent.Converter.Playtest;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The audit (CONTENT_FIX_PLAN F5) is only worth its report if it measures: these pin the harness to things whose
// answer is known, and prove it flags a text that lies.
public class AuditTests
{
    private static RunBlueprint Game => FightProbe.Game;

    [Fact]
    public void Paper_cut_takes_exactly_six_off_the_quiet_dummy_on_the_turn_it_is_played()
    {
        var audit = Audit.AuditCard(Game, "paper_cut");
        Assert.Null(audit.Quiet.Error);
        Assert.Equal(6, audit.QuietBase.AfterPlay.EnemyHp - audit.Quiet.AfterPlay.EnemyHp);
        Assert.DoesNotContain(Audit.Judge(Game, audit, starterFloor: 0), f => f.Severity is "BUG" or "SIDE");
    }

    // Paperwork ticks at the end of every enemy turn and never decays: the whole reason the audit watches rounds.
    [Fact]
    public void Permit_a38_s_paperwork_takes_five_on_each_enemy_turn_it_is_watched()
    {
        var audit = Audit.AuditCard(Game, "permit_a38");
        var lost = audit.Quiet.Snaps.Zip(audit.QuietBase.Snaps, (with, without) => without.EnemyHp - with.EnemyHp).ToList();
        // start of turn 1, after the play, then the start of turns 2, 3, 4, 5.
        Assert.Equal([0, 0, 5, 10, 15, 20], lost);
    }

    // The direction: the same fight read against a text that promises more must be flagged.
    [Fact]
    public void A_text_that_promises_more_than_the_card_does_is_a_bug()
    {
        var honest = Audit.AuditCard(Game, "paper_cut");
        var lying = honest with { Text = "Deal 9 damage." };
        Assert.Contains(Audit.Judge(Game, lying, starterFloor: 0), f => f.Severity == "BUG");
    }

    // A relic with a plain opening effect shows it: Black Salt Charm opens every fight with 4 Block.
    [Fact]
    public void A_relic_s_opening_block_is_seen_by_the_relic_fight()
    {
        var audit = Audit.AuditRelic(Game, "black_salt_charm");
        Assert.Null(audit.Quiet.Error);
        Assert.Equal(4, audit.Quiet.Start.HeroBlock - audit.QuietBase.Start.HeroBlock);
        Assert.DoesNotContain(Audit.JudgeRelic(Game, audit), f => f.Severity == "UNUSED");
    }
}
