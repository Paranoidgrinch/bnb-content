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

    // ── the three findings the audit made and 2026-09-28 fixed — each pinned so it stays fixed ──────────────

    // "Gain 1 Energy" on a full pool gave nothing: energy was capped at its max. A gain may pass it now.
    [Fact]
    public void Gain_one_energy_on_a_full_pool_is_one_more_energy()
    {
        var audit = Audit.AuditCard(Game, "formal_dissent");
        Assert.Null(audit.Quiet.Error);
        Assert.Equal(1, audit.Quiet.AfterPlay.Energy - audit.QuietBase.AfterPlay.Energy + audit.Cost);
    }

    // "Then remove 1 stack of ANOTHER negative Status": with nothing else on the target, its own Blood Ink stays.
    [Fact]
    public void Sanguine_errata_keeps_its_own_blood_ink_when_the_target_has_nothing_else()
    {
        var audit = Audit.AuditCard(Game, "sanguine_errata");
        Assert.Null(audit.Quiet.Error);
        Assert.Equal(5, audit.Quiet.AfterPlay.EnemyStatuses.GetValueOrDefault(Converter.Cards.Keywords.BloodInk));
    }

    // Dubious Authority beside Strong Binder: the attack is blocked entirely and the Doubt is still spent by it —
    // so the Paperwork must come. It did not while the rule asked for damage dealt.
    [Fact]
    public void Dubious_authority_answers_an_attack_the_hero_blocked_entirely()
    {
        var striker = Audit.Dummy(Game, strikes: true, energy: 6);
        string[] deck = ["strong_binder", "dubious_authority", "paper_cut", "paper_cut", "paper_cut"];
        var trace = Audit.Fight(Game, striker, deck, null, Audit.Plays("strong_binder", "dubious_authority"));
        Assert.Null(trace.Error);

        var afterTheBlow = trace.Snaps[2];
        Assert.Equal(trace.Snaps[0].HeroHp, afterTheBlow.HeroHp); // blocked: no health lost
        Assert.True(afterTheBlow.EnemyStatuses.GetValueOrDefault(Converter.Cards.Keywords.Paperwork) >= 2,
            "the Doubt was spent on a blocked attack and Dubious Authority filed nothing");
    }
}
