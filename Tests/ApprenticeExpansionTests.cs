using BnbContent.Converter.Apprentice;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using static BnbContent.Tests.ApprenticeTests;

namespace BnbContent.Tests;

// The expansion (sorcerers_apprentice_expansion.md): growth over a fight — Rote and Poise, the casts of the combat,
// "whenever you cast" — and the shelf it fills out to the others' size. Fights of the real document.
public class ApprenticeExpansionTests
{
    private const string Rote = ApprenticeKeywords.Rote;
    private const string Poise = ApprenticeKeywords.Poise;
    private const string Singe = ApprenticeKeywords.Singe;
    private const string Grit = ApprenticeKeywords.Grit;

    private static int Stacks(CombatantState c, string status) => FightProbe.StacksOf(c, status);

    private static void EndTurn(RunPlayback play)
    {
        play.CombatDriver!.EndTurn();
        while (play.CombatDriver.PendingCardChoice is { } offered)
            play.CombatDriver.SupplyCardChoice([.. offered.Take(play.CombatDriver.PendingCardChoiceCount).Select(c => c.Id)]);
    }

    // ── the shelf ──────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void His_shelf_holds_eighty_reward_cards_gated_over_four_acts_like_the_others()
    {
        int Count(int act) => ApprenticeCards.RewardPool(act).Count;
        Assert.Equal(48, Count(1));
        Assert.Equal(64, Count(2));
        Assert.Equal(73, Count(3));
        Assert.Equal(80, Count(4));
        Assert.Equal(80, Count(5));
        Assert.Equal(Converter.Cards.FinalCards.CharacterPool(5).Count, Count(5));
        Assert.Equal(Converter.Witch.WitchCards.RewardPool(5).Count, Count(5));

        var byRarity = ApprenticeCards.RewardPool(5).GroupBy(c => c.Rarity).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal((22, 35, 23), (byRarity["common"], byRarity["uncommon"], byRarity["rare"]));
        // Every new card has an upgrade, and every id is his own.
        var expansion = ApprenticeCards.Expansion();
        Assert.Equal(84, expansion.Count);
        Assert.All(expansion, c => Assert.StartsWith("ap_", c.Id, StringComparison.Ordinal));
        Assert.Equal(42, expansion.Count(c => !c.Id.EndsWith('+')));
    }

    // ── Rote and Poise ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Rote_makes_every_speak_hit_harder_and_poise_every_speak_guard_larger_but_not_a_played_card()
    {
        var (play, session, enemy) = Fight(["ap_recitation", "ap_steady_hand", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_recitation", enemy);
        Play(play, session, "ap_steady_hand", enemy);
        Assert.Equal((2, 2), (Stacks(Hero(play), Rote), Stacks(Hero(play), Poise)));

        // A played Chalk Line is still 6 Block.
        var block = Block(Hero(play));
        Play(play, session, "ap_chalk_line", enemy);
        Assert.Equal(block + 6, Block(Hero(play)));

        Inscribe(play, session, "ap_candle_stub", 0);
        Inscribe(play, session, "ap_chalk_line", 1);
        var before = Standing(Body(play, enemy));
        block = Block(Hero(play));
        Cast(play, session, enemy);
        Assert.Equal(before - 7, Standing(Body(play, enemy)));   // 5 + 2 Rote
        Assert.Equal(block + 8, Block(Hero(play)));                // 6 + 2 Poise
        play.Dispose();
    }

    [Fact]
    public void The_borrowed_grimoire_teaches_a_rote_with_every_cast()
    {
        var (play, session, enemy) = Fight(["ap_borrowed_grimoire", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_borrowed_grimoire", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        Cast(play, session, enemy);
        Assert.Equal(1, Stacks(Hero(play), Rote));
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        var before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        // 5 + the Rote the first cast taught + 3 for the Singe the first slot put on it last time.
        Assert.Equal(before - 9, Standing(Body(play, enemy)));
        Assert.Equal(2, Stacks(Hero(play), Rote));
        Assert.Equal(2, Wand(play).CastsThisCombat);
        play.Dispose();
    }

    [Fact]
    public void The_copybook_and_the_examiners_bell_grow_him_between_turns()
    {
        var (play, session, enemy) = Fight(["ap_copybook", "ap_examiners_bell", "ap_candle_stub", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_copybook", enemy);
        Play(play, session, "ap_examiners_bell", enemy);
        Inscribe(play, session, "ap_candle_stub", 0);
        // Two casts this turn: the bell rings at its end.
        Cast(play, session, enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        Cast(play, session, enemy);
        Assert.Equal(2, Wand(play).CastsThisTurn);
        EndTurn(play);
        // Bell: 2 Rote at the turn's end; Copybook: 1 Rote and 1 Poise at the next one's start.
        Assert.Equal((3, 1), (Stacks(Hero(play), Rote), Stacks(Hero(play), Poise)));
        play.Dispose();
    }

    [Fact]
    public void The_apprentices_oath_and_burning_glass_answer_every_cast()
    {
        var (play, session, enemy) = Fight(["ap_apprentices_oath", "ap_burning_glass", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_apprentices_oath", enemy);
        Play(play, session, "ap_burning_glass", enemy);
        Inscribe(play, session, "ap_chalk_line", 0);
        var before = Standing(Body(play, enemy));
        var block = Block(Hero(play));
        Cast(play, session, enemy);
        Assert.Equal(before - 3, Standing(Body(play, enemy)));
        Assert.Equal(block + 6 + 3, Block(Hero(play)));            // the Chalk Line's Speak, and the Oath's
        Assert.Equal(1, Stacks(Body(play, enemy), Singe));
        play.Dispose();
    }

    // ── what reads the fight ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Slate_pencil_and_thunderclap_reward_a_second_cast_in_the_turn()
    {
        var (play, session, enemy) = Fight(["ap_slate_pencil", "ap_thunderclap", "ap_chalk_line", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_slate_pencil", 0);
        var before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        Assert.Equal(before - 5, Standing(Body(play, enemy)));
        Play(play, session, "ap_chalk_line", enemy);
        Play(play, session, "ap_chalk_line", enemy);
        before = Standing(Body(play, enemy));
        Cast(play, session, enemy);
        Assert.Equal(before - 9, Standing(Body(play, enemy)));   // 5 + 4: a cast already made this turn
        before = Standing(Body(play, enemy));
        Play(play, session, "ap_thunderclap", enemy);
        Assert.Equal(before - 15, Standing(Body(play, enemy)));  // 5 + 5 × 2 casts
        play.Dispose();
    }

    [Fact]
    public void Drill_book_bookend_and_the_unfinished_spell_count_casts_and_cards()
    {
        var (play, session, enemy) = Fight(["ap_drill_book", "ap_bookend", "ap_unfinished_spell", "ap_chalk_line", "ap_chalk_line"]);
        var before = Standing(Body(play, enemy));
        Play(play, session, "ap_drill_book", enemy);
        Assert.Equal(before - 6, Standing(Body(play, enemy)));   // no cast yet this combat
        Play(play, session, "ap_chalk_line", enemy);
        Inscribe(play, session, "ap_bookend", 0);
        Inscribe(play, session, "ap_chalk_line", 1);
        var block = Block(Hero(play));
        Cast(play, session, enemy);
        Assert.Equal(1, Wand(play).CastsThisCombat);
        Assert.Equal(block + 8 + 6, Block(Hero(play)));            // Bookend 4 + 2 × 2 cards, Chalk Line 6
        before = Standing(Body(play, enemy));
        Play(play, session, "ap_unfinished_spell", enemy);
        Assert.Equal(before - 18, Standing(Body(play, enemy)));  // 6 + 6 × 2 cards in the wand
        play.Dispose();
    }

    [Fact]
    public void Kindling_ember_tongs_and_wildfire_feed_and_read_singe_without_making_it_tick()
    {
        var (play, session, enemy) = Fight(["ap_kindling", "ap_ember_tongs", "ap_wildfire", "ap_chalk_line", "ap_chalk_line"]);
        var before = Standing(Body(play, enemy));
        Play(play, session, "ap_kindling", enemy);
        // 4, then a Singe; 4 + 3 spending it, then another.
        Assert.Equal(before - 11, Standing(Body(play, enemy)));
        Assert.Equal(1, Stacks(Body(play, enemy), Singe));
        Play(play, session, "ap_wildfire", enemy);
        Assert.Equal(2, Stacks(Body(play, enemy), Singe));
        before = Standing(Body(play, enemy));
        Play(play, session, "ap_ember_tongs", enemy);
        // 4 + 3 × 2, and the hit itself spends one for 3 more.
        Assert.Equal(before - 13, Standing(Body(play, enemy)));
        Assert.Equal(1, Stacks(Body(play, enemy), Singe));
        play.Dispose();
    }

    [Fact]
    public void The_millstone_grinds_grit_into_block_and_the_staff_lengthens_the_wand()
    {
        var (play, session, enemy) = Fight(["ap_stone_tablet", "ap_millstone", "ap_sorcerers_staff", "ap_chalk_line", "ap_chalk_line"]);
        Play(play, session, "ap_stone_tablet", enemy);
        Assert.Equal(2, Stacks(Hero(play), Grit));
        var block = Block(Hero(play));
        Play(play, session, "ap_millstone", enemy);
        Assert.Equal(block + 12, Block(Hero(play)));
        Assert.Equal(0, Stacks(Hero(play), Grit));
        var focus = Wand(play).Focus;
        Play(play, session, "ap_sorcerers_staff", enemy);
        Assert.Equal((5, 10), (Wand(play).Capacity, Wand(play).ExhaustionCap));
        Assert.Equal(Math.Min(6, focus + 2 + 1), Wand(play).Focus); // 2 from the card, 1 for playing it
        play.Dispose();
    }
}
