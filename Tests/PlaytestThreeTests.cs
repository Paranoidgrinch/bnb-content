using BnbContent.Converter;
using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using static BnbContent.Tests.LieutenantTests;

namespace BnbContent.Tests;

// Playtest feedback 3 (bnb-godot/PLAYTEST_FEEDBACK_3_PLAN.md): the bugs the player met, each pinned by a fight.
public class PlaytestThreeTests
{
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Two(string[] deck, int authority) =>
        FightProbe.Start(FightProbe.RosterAgainstHero("pt3_two", 9, [(LieutenantKeywords.Authority, authority)],
                (Quiet, QuietIntent, null), (Biter, BiteIntent, null)),
            deck, character: LieutenantCharacter.Id);

    [Fact]
    public void Flank_offers_its_authority_against_two_enemies()
    {
        var (play, session, enemy) = Two(["lt_flank", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut", "lt_watch_cut"], 2);
        var other = Now(play).State.Combatants.First(c => c.Id != Now(play).HeroId && c.Id != enemy).Id;
        var before = Body(play, other).Health.Current + Block(Body(play, other));
        Play(play, session, "lt_flank", enemy, choose: 0);
        Assert.Equal(1, Stacks(Hero(play), LieutenantKeywords.Authority));
        Assert.Equal(before - 8, Body(play, other).Health.Current + Block(Body(play, other)));
        play.Dispose();
    }

    [Fact]
    public void Every_log_only_status_exists_and_is_tagged()
    {
        var statuses = FightProbe.Shipped.Statuses.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var id in LogOnlyStatuses.Ids)
        {
            Assert.True(statuses.Contains(id), $"{id} is not a status");
            Assert.Contains(LogOnlyStatuses.Tag, FightProbe.Shipped.Presentation.Statuses[id].Tags);
        }
    }

    // D1 — a fight against several enemies opens with at most 20 damage in the enemies' first turn, all of it
    // counted: the player does nothing, has no Block, and loses what the first enemy turn takes.
    internal const int AmbushOpeningCap = AmbushFooting.Budget;

    internal static IEnumerable<(string Encounter, int Damage)> OpeningDamage()
    {
        var encounters = FightProbe.Game.Encounters
            .Where(e => e.Enemies.Count >= 2
                && !e.Id.Value.StartsWith("tutorial", StringComparison.Ordinal)
                && !e.Id.Value.Contains("_boss_", StringComparison.Ordinal));
        foreach (var encounter in encounters)
        {
            var (play, session, _) = FightProbe.Start(
                FightProbe.Authored(encounter.Id.Value), [.. Enumerable.Repeat("lt_watch_cut", 12)], health: 999,
                character: LieutenantCharacter.Id);
            var before = Hero(play).Health.Current;
            play.CombatDriver!.EndTurn();
            var lost = play.CombatDriver.Current is null || session.Error is not null ? -1 : before - Hero(play).Health.Current;
            play.Dispose();
            yield return (encounter.Id.Value, lost);
        }
    }

    [Fact]
    public void No_fight_against_several_enemies_opens_with_more_than_twenty_damage()
    {
        var offenders = OpeningDamage().Where(o => o.Damage > AmbushOpeningCap).Select(o => $"{o.Encounter} ({o.Damage})").ToList();
        Assert.True(offenders.Count == 0, $"{offenders.Count} encounters: {string.Join(", ", offenders)}");
    }

    // D2 — no enemy spends two turns in a row doing nothing but guard itself. Read from the built programs: a move
    // is "only Block" when its program has no node but Block and the sequences around it. A body whose moves are
    // chosen by rules (the Signpost answers the player's first card) is judged by the fights, not by its cycle.
    private static readonly HashSet<string> RuleDriven = new(StringComparer.Ordinal) { "contradictory_signpost" };

    [Fact]
    public void No_enemy_cycles_two_turns_of_nothing_but_block()
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(RogueDeck.Run.RunJson.ToJson(
            FightProbe.Game, RogueDeck.Run.RunJson.CreateOptions(indented: false)))!;
        string[] blockOnly = ["node.gainBlock", "node.sequence", "node.causalSequence", "node.forEachTarget"];
        var pure = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var action in json["EnemyActions"]!.AsArray())
        {
            var kinds = new HashSet<string>(StringComparer.Ordinal);
            Kinds(action!["Program"], kinds);
            pure[action["Id"]!.GetValue<string>()] = kinds.Count > 0 && kinds.All(blockOnly.Contains);
        }
        var offenders = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var encounter in FightProbe.Game.Encounters.Where(e => !e.Id.Value.Contains("_boss_", StringComparison.Ordinal)))
            foreach (var enemy in encounter.Enemies.Where(e => !RuleDriven.Contains(e.Id.Split('#')[0])))
            {
                var cycle = enemy.Actions.Select(a => a.value).ToList();
                for (var k = 0; k < cycle.Count; k++)
                    if (pure.GetValueOrDefault(cycle[k]) && pure.GetValueOrDefault(cycle[(k + 1) % cycle.Count]))
                        offenders.Add($"{cycle[k]} → {cycle[(k + 1) % cycle.Count]}");
            }
        Assert.True(offenders.Count == 0, $"{offenders.Count}: {string.Join(", ", offenders)}");

        static void Kinds(System.Text.Json.Nodes.JsonNode? node, HashSet<string> into)
        {
            switch (node)
            {
                case System.Text.Json.Nodes.JsonObject o:
                    if (o["kind"] is System.Text.Json.Nodes.JsonValue v && v.GetValue<string>() is { } k && k.StartsWith("node.", StringComparison.Ordinal))
                        into.Add(k);
                    foreach (var (_, child) in o)
                        Kinds(child, into);
                    break;
                case System.Text.Json.Nodes.JsonArray a:
                    foreach (var child in a)
                        Kinds(child, into);
                    break;
            }
        }
    }

    // E3 — an event's relic always costs something: HP, max HP, a card, Gold, a curse, harder fights after it, or a
    // gamble (an `outcome:` situation is the result of a wager the player chose to take). Treasure rooms are chests,
    // not events, and are left out — and so are the encounter trial's stage packages (EncounterTrial), which stand in
    // for the chests a run would have opened by then.
    [Fact]
    public void No_event_gives_a_relic_for_nothing()
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(RogueDeck.Run.RunJson.ToJson(
            FightProbe.Game, RogueDeck.Run.RunJson.CreateOptions(indented: false)))!;
        string[] prices =
        [
            "fx.damage", "fx.computedDamage", "fx.removeCards", "fx.addCard", "fx.installNextCombatOpening",
            "fx.installProgramById", "fx.computedResource", "fx.transformCards",
        ];
        var offenders = new List<string>();
        foreach (var (id, node) in json["Events"]!.AsObject())
        {
            if (id.StartsWith("treasure:", StringComparison.Ordinal) || id.StartsWith("trial_", StringComparison.Ordinal))
                continue;
            foreach (var situation in node!["situations"]!.AsArray())
            {
                if (situation!["Id"]!.GetValue<string>().StartsWith("outcome:", StringComparison.Ordinal))
                    continue;
                foreach (var choice in situation["Choices"]!.AsArray())
                {
                    var effects = choice!["Effects"]?.AsArray().Select(e => e!["kind"]!.GetValue<string>()).ToList() ?? [];
                    var text = choice["Effects"]?.ToJsonString() ?? "";
                    var relic = effects.Contains("fx.addRelicById")
                        || (effects.Contains("fx.offerReward") && text.Contains("fx.addRelicById", StringComparison.Ordinal));
                    var paid = effects.Any(prices.Contains)
                        || (choice["Costs"] is System.Text.Json.Nodes.JsonArray costs && costs.Count > 0)
                        || System.Text.RegularExpressions.Regex.IsMatch(text, @"""kind"":""fx\.(changeMaxHealth|changeResource)"",""value"":\{[^}]*""Delta"":-");
                    if (relic && !paid)
                        offenders.Add($"{id}/{choice["Id"]}");
                }
            }
        }
        Assert.True(offenders.Count == 0, $"{offenders.Count}: {string.Join(", ", offenders)}");
    }

    // E1 — an elite's card reward is five cards, every one already improved.
    [Fact]
    public void Every_elite_offers_five_improved_cards()
    {
        var elites = 0;
        foreach (var act in FightProbe.Game.Acts!)
            foreach (var (id, reward) in act.MapGeneration!.VictoryRewardsByEncounter)
            {
                if (!id.Contains("_elite_", StringComparison.Ordinal))
                    continue;
                var cards = ((RogueDeck.Run.FixedRewardSource)reward.Source).Offers[0].Grant
                    .OfType<RogueDeck.Run.OfferRewardRunEffect>()
                    .Single(o => o.Reward.Value.StartsWith("cards:", StringComparison.Ordinal));
                var pool = Assert.IsType<RogueDeck.Run.PoolRewardSource>(cards.Source);
                Assert.Equal(ConversionPools.EliteCardOffers, pool.Count);
                Assert.Equal(100, pool.UpgradeChancePercent);
                elites++;
            }
        Assert.True(elites >= 20, $"only {elites} elites found");
    }

    // B7 — every rule of an elite or a boss is said plainly: the rulebook names only real statuses, each line is at
    // most 25 words, and the shipped status carries it.
    [Fact]
    public void The_rulebook_is_short_and_shipped()
    {
        var statuses = FightProbe.Game.Statuses.ToDictionary(s => s.Id, StringComparer.Ordinal);
        foreach (var (id, (name, text)) in RuleTexts.All)
        {
            Assert.True(statuses.ContainsKey(id), $"{id} is not a status");
            Assert.True(text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 25, $"{id}: {text}");
            Assert.Equal(text, statuses[id].DescriptionKey);
            if (name is not null)
                Assert.Equal(name, statuses[id].NameKey);
        }
    }

    // F1 — card texts are short: every part of a card's text (its play, and its wand part) is at most 25 words, the
    // median part at most 12, and every short text names a real card and is what that card ships with.
    [Fact]
    public void Card_texts_are_short()
    {
        var data = BabData.Load(TestData.Directory);
        var cards = FightProbe.Shipped.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
        foreach (var (id, text) in data.CardTexts)
        {
            Assert.True(cards.ContainsKey(id), $"{id} is not a card");
            Assert.Equal(text, cards[id].DescriptionKey);
            Assert.Equal(text, FightProbe.Shipped.Presentation.Cards[id].FlavorText);
        }
        var parts = cards.Values.SelectMany(c => (c.DescriptionKey ?? "").Split("\nWand: "))
            .Select(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length).Order().ToList();
        var over = cards.Values.Where(c => (c.DescriptionKey ?? "").Split("\nWand: ")
            .Any(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 25)).Select(c => c.Id).ToList();
        Assert.True(over.Count == 0, string.Join(", ", over));
        Assert.True(parts[parts.Count / 2] <= 12, $"median {parts[parts.Count / 2]}");
    }
}
