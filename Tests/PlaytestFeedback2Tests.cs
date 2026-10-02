using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Run;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Playtest feedback 2 (2026-10-02, bnb-godot/PLAYTEST_FEEDBACK_2_PLAN.md): each test is one point a player
// reported, measured in a live fight.
public class PlaytestFeedback2Tests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";   // guards itself, does nothing to the hero

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    // A1 — "Ward Wax X: at the start of your turn, gain X Block." Two Waxen Sureties in one turn are 8 Wax, and
    // the next turn opens behind 8 Block — not behind the first card's 4, and not behind both counted twice.
    [Fact]
    public void Ward_wax_from_two_cards_pays_its_whole_stack_next_turn()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9), [.. Enumerable.Repeat("waxen_surety", 10)]);

        Play(play, session, "waxen_surety", enemyId);
        Play(play, session, "waxen_surety", enemyId);
        Assert.Equal(8, FightProbe.StacksOf(Hero(play), Keywords.WardWax));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        // The quiet enemy struck nothing: one stack paid for the enemy turn, the opening Block is what was worn
        // when the turn began.
        var hero = Hero(play);
        Assert.Equal(7, FightProbe.StacksOf(hero, Keywords.WardWax));
        Assert.Equal(7, Block(hero));

        // A third card on top of what is left: 7 + 4, and the next turn pays all of it.
        Play(play, session, "waxen_surety", enemyId);
        play.CombatDriver.EndTurn();
        hero = Hero(play);
        Assert.Equal(10, FightProbe.StacksOf(hero, Keywords.WardWax));
        Assert.Equal(10, Block(hero));
        play.Dispose();
    }

    // A1, the actual bug: the Wax paid its Block on EVERY draw, not once at the start of the turn — a card that
    // draws mid-turn handed out the whole stack again. Four Wax, then a turn whose hand draws one more card,
    // must stand behind exactly the four.
    [Fact]
    public void Ward_wax_pays_once_per_turn_not_once_per_draw()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.SoloAgainstHero(Quiet, QuietIntent, energy: 9, (Keywords.WardWax, 4)),
            [.. Enumerable.Repeat("misfiled_paper", 12)]);
        Assert.Equal(4, Block(Hero(play)));

        Play(play, session, "misfiled_paper", enemyId);
        Play(play, session, "misfiled_paper", enemyId);
        Assert.Equal(4, Block(Hero(play)));
        play.Dispose();
    }

    // A1 — Votive Covenant: "If you take no unblocked Attack damage during an enemy turn, Ward Wax does not
    // decay." Before the fix the Covenant was never read and the Wax lost its stack like any other turn.
    [Fact]
    public void Votive_covenant_keeps_the_wax_through_a_quiet_enemy_turn()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.SoloAgainstHero(Quiet, QuietIntent, energy: 9,
                (Keywords.WardWax, 4), (GeneralWax.VotiveCovenant, 1)),
            [.. Enumerable.Repeat("paper_cut", 12)]);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(4, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();
    }

    // A1 — Wax Reliquary and Wax Indemnity hold "until your next turn". They used to hold for the whole fight.
    [Theory]
    [InlineData("wax_reliquary", GeneralWax.WaxReliquary)]
    [InlineData("wax_indemnity", GeneralWax.WaxIndemnity)]
    [InlineData("wax_indemnity+", GeneralWax.WaxIndemnity + "+")]
    public void A_wax_rule_until_your_next_turn_lapses_when_it_comes(string card, string rule)
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9), [.. Enumerable.Repeat(card, 12)]);
        Play(play, session, card, enemyId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), rule));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), rule));
        play.Dispose();
    }

    // C1 — no fight asks "take the spoils?": every victory reward of every act, by role and by encounter, is granted.
    [Fact]
    public void Every_victory_reward_is_granted_not_offered()
    {
        var specs = (FightProbe.Game.Acts ?? []).Select(a => a.MapGeneration).OfType<MapGenerationSpec>().ToList();
        Assert.NotEmpty(specs);
        Assert.All(specs.SelectMany(s => s.VictoryRewards.Values.Concat(s.VictoryRewardsByEncounter.Values)),
            reward => Assert.True(reward.Granted));
    }

    // C2 — a boss's relic is a pick of all three, and a pick may be declined like any reward.
    [Fact]
    public void A_boss_offers_all_three_of_its_relics()
    {
        var bossRelics =
            from act in FightProbe.Game.Acts ?? []
            where act.MapGeneration is not null
            from entry in act.MapGeneration!.VictoryRewardsByEncounter
            from offer in ((FixedRewardSource)entry.Value.Source).Offers
            from grant in offer.Grant.OfType<OfferRewardRunEffect>()
            where grant.Kind == RewardKinds.Relic && grant.Source is PoolRewardSource
            select (PoolRewardSource)grant.Source;
        var pools = bossRelics.ToList();
        Assert.NotEmpty(pools);
        Assert.All(pools, pool => Assert.Equal(pool.Pool.Entries.Count, pool.Count));
        Assert.All(pools, pool => Assert.Equal(3, pool.Count));
    }

    // C3 — every act's shop strikes a card for 75 and 25 more after each use, run-wide.
    [Fact]
    public void The_card_strike_grows_dearer_with_every_use()
    {
        var shops = FightProbe.Game.Shops.Values.ToList();
        Assert.NotEmpty(shops);
        var strikes = shops.SelectMany(shop => shop.Services ?? []).Where(service => service.Id == "remove-card").ToList();
        Assert.Equal(shops.Count, strikes.Count);
        Assert.All(strikes, service => Assert.Equal((75, 25), (service.Price, service.PriceStep)));
    }

    // C4 — a card reward turns up improved at 10 / 20 / 30 / 40 % in Acts I–IV.
    [Fact]
    public void Reward_cards_come_improved_at_the_acts_rate()
    {
        var rates =
            from act in FightProbe.Game.Acts ?? []
            where act.MapGeneration is not null
            from reward in act.MapGeneration!.VictoryRewards.Values
            from offer in ((FixedRewardSource)reward.Source).Offers
            from grant in offer.Grant.OfType<OfferRewardRunEffect>()
            where grant.Kind == RewardKinds.Card
            select (Act: (FightProbe.Game.Acts!.ToList().IndexOf(act) + 1), ((PoolRewardSource)grant.Source).UpgradeChancePercent);
        var byAct = rates.Distinct().ToList();
        foreach (var (act, rate) in byAct.Where(r => r.Act <= 4))
            Assert.Equal(act * 10, rate);
        Assert.Contains(byAct, r => r.Act == 4);
    }
}
