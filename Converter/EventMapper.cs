using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Original events → engine EventScript. Each event is one "start" situation; a choice with a
// result_text chains to a result situation whose single "continue" ends the event — so the player
// reads the outcome exactly like the original console did.
//
// Effect semantics mirror the original's event_effects.py:
// - lose_hp cannot kill (clamped to leave 1 HP) → computed damage min(N, currentHealth − 1)
// - heal_percent_max_hp rounds UP → (maxHealth·p + 99) / 100
// - remove_card / upgrade_card are player choices; transform/duplicate hit random cards
// - gain_relic grants ONE random eligible relic (pickup effects bundled into the offer)
// - next_combat_* effects become one-shot combat openings for the next fight
// Deviations (ADAPTATIONS.md): open_shop and next_combat_card_reward_bonus become card-reward offers.
public static class EventMapper
{
    public static EventScript Map(BabEvent babEvent, ConversionPools pools)
    {
        var where = $"event '{babEvent.Id}'";
        var situations = new List<EventSituation>();
        var choices = new List<EventChoice>();
        foreach (var choice in babEvent.Choices)
        {
            var effects = (choice.Effects ?? [])
                .SelectMany((effect, i) => MapEffect($"{where} choice '{choice.Id}' effect[{i}]", effect, pools))
                .ToList();
            if (choice.ResultText is { } result)
            {
                var resultId = $"result:{choice.Id}";
                situations.Add(new EventSituation(resultId, result,
                    [new EventChoice("continue", [], TextKey: "Continue")]));
                choices.Add(new EventChoice(choice.Id, effects, NextSituationId: resultId, TextKey: choice.Text));
            }
            else
            {
                choices.Add(new EventChoice(choice.Id, effects, TextKey: choice.Text));
            }
        }
        situations.Insert(0, new EventSituation("start", babEvent.Text, choices));
        return new EventScript("start", situations);
    }

    private static IEnumerable<IRunEffectRequest> MapEffect(string where, BabEffect effect, ConversionPools pools)
    {
        int Amount(int fallback) => effect.Amount ?? fallback;
        RewardId Reward(string suffix) => new($"{where.Replace(' ', '-').Replace("'", "")}-{suffix}");

        switch (effect.Type)
        {
            case "lose_hp":
                yield return new ComputedDamageRunEffect(RunExpr.Min(
                    RunExpr.Const(Amount(0)),
                    RunExpr.Subtract(RunExpr.CurrentHealth, RunExpr.Const(1))));
                break;
            case "gain_gold":
                yield return new ChangeResourceRunEffect(StandardRunIds.Gold, Amount(0));
                break;
            case "lose_gold":
                yield return new ChangeResourceRunEffect(StandardRunIds.Gold, -Amount(0));
                break;
            case "heal_percent_max_hp":
                yield return new ComputedHealRunEffect(RunExpr.Divide(
                    RunExpr.Add(RunExpr.Multiply(RunExpr.MaxHealth, RunExpr.Const(Amount(0))), RunExpr.Const(99)),
                    RunExpr.Const(100)));
                break;
            case "add_card_to_deck":
                var cardId = CardMapper.MapCardId(effect.CardId
                    ?? throw new ConversionException(where, "add_card_to_deck without card_id"));
                for (var i = 0; i < Math.Max(1, Amount(1)); i++)
                    yield return new AddCardToDeckRunEffect(new CardDefinitionId(cardId));
                break;
            case "remove_card":
                RequireNoFilter(where, effect);
                yield return new RemoveCardsRunEffect(
                    RunSelectors.DeckCards.ChooseByPlayer(Math.Max(1, Amount(1)), "remove a card from your deck"));
                break;
            case "duplicate_card":
                RequireNoFilter(where, effect);
                yield return new DuplicateCardsRunEffect(RunSelectors.DeckCards.Random(Math.Max(1, Amount(1))));
                break;
            case "transform_card":
                RequireNoFilter(where, effect);
                yield return new TransformCardsRunEffect(RunSelectors.DeckCards.Random(1), pools.TransformPool());
                break;
            case "upgrade_card":
                yield return new UpgradeCardsRunEffect(
                    RunSelectors.DeckCards.Upgradable().ChooseByPlayer(Math.Max(1, Amount(1)), "upgrade a card"));
                break;
            case "gain_card_reward":
                for (var i = 0; i < Math.Max(1, Amount(1)); i++)
                    yield return new OfferRewardRunEffect(Reward($"card-{i}"), pools.CardRewardSource(), 1);
                break;
            case "gain_relic":
                yield return new OfferRewardRunEffect(Reward("relic"), pools.RelicGrantSource(effect.Tag, where), 1);
                break;

            case "next_combat_player_panic":
                yield return Openings.NextCombat(new CombatNodeModel("applyStatus", "source",
                    CombatAmountSpec.FromConst(Amount(1)), StatusId: "panic"));
                break;
            case "next_combat_player_fatigue":
                yield return Openings.NextCombat(new CombatNodeModel("applyStatus", "source",
                    CombatAmountSpec.FromConst(Amount(1)), StatusId: "fatigue"));
                break;
            case "next_combat_player_strength":
                yield return Openings.NextCombat(new CombatNodeModel("applyStatus", "source",
                    CombatAmountSpec.FromConst(Amount(1)), StatusId: "strength"));
                break;
            case "next_combat_enemy_strength":
                yield return Openings.NextCombat(new CombatNodeModel("applyStatus", "allEnemies",
                    CombatAmountSpec.FromConst(Amount(1)), StatusId: "strength"));
                break;
            case "next_combat_enemy_hp_loss_percent":
                // ADAPTATION: "every enemy loses p% of ITS max HP" needs a per-target amount the curated
                // model can't express — ported as flat opening damage (p% of a typical Act-1 enemy, ~30 HP).
                yield return Openings.NextCombat(new CombatNodeModel("dealDamage", "allEnemies",
                    CombatAmountSpec.FromConst(Math.Max(1, Amount(0) * 30 / 100)), IgnoresBlock: true));
                break;

            case "open_shop":
                // ADAPTATION: events cannot open shops — a card reward offer keeps the choice worthwhile.
                yield return new OfferRewardRunEffect(Reward("shop"), pools.CardRewardSource(), 1);
                break;
            case "next_combat_card_reward_bonus":
                // ADAPTATION: reward-offer modifiers are not data — an immediate extra card offer.
                yield return new OfferRewardRunEffect(Reward("bonus"), pools.CardRewardSource(), 1);
                break;

            default:
                throw new ConversionException(where, $"unmapped event effect type '{effect.Type}'");
        }
    }

    private static void RequireNoFilter(string where, BabEffect effect)
    {
        if (effect.CardId is not null || effect.Tag is not null)
            throw new ConversionException(where, $"'{effect.Type}' card filters are not ported (found card_id/tag)");
    }
}
