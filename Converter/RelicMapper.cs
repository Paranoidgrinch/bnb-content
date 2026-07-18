using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Original relics → engine RelicData plus a list of PICKUP effects. The original applies pickup
// effects (heal / gold / +max energy) when the relic enters the inventory; the engine has no
// per-relic "on pickup" hook, so the converter bundles those effects into every place that GRANTS the
// relic (reward offers, shop entries, event pools) — same moment, same outcome.
//
// Deviations (see ADAPTATIONS.md): increase_card_reward_count and shop_price_discount have no data
// path (reward-offer modifiers / dynamic prices are code escapes) — those relics keep their identity
// but their effect becomes gold after each combat victory / gold back per shop purchase.
public sealed record MappedRelic(BabRelic Source, RelicData Relic, IReadOnlyList<IRunEffectRequest> PickupEffects);

public static class RelicMapper
{
    public static MappedRelic Map(BabRelic relic)
    {
        var where = $"relic '{relic.Id}'";
        var openingNodes = new List<CombatNodeModel>();
        var pickup = new List<IRunEffectRequest>();
        var programs = new List<ITriggeredRunEffectDefinition>();

        foreach (var effect in relic.Effects ?? [])
        {
            int Amount() => effect.Amount
                ?? throw new ConversionException(where, $"'{effect.Type}' is missing its amount");
            switch (effect.Type)
            {
                case "apply_status_to_all_enemies_at_combat_start":
                    openingNodes.Add(new CombatNodeModel("applyStatus", "allEnemies",
                        CombatAmountSpec.FromConst(Amount()),
                        StatusId: effect.Status ?? throw new ConversionException(where, "status missing")));
                    break;
                case "apply_status_to_player_at_combat_start":
                    openingNodes.Add(new CombatNodeModel("applyStatus", "source",
                        CombatAmountSpec.FromConst(Amount()),
                        StatusId: effect.Status ?? throw new ConversionException(where, "status missing")));
                    break;
                case "gain_block_at_combat_start":
                    openingNodes.Add(new CombatNodeModel("gainBlock", "source", CombatAmountSpec.FromConst(Amount())));
                    break;
                case "gain_strength_at_combat_start":
                    openingNodes.Add(new CombatNodeModel("applyStatus", "source",
                        CombatAmountSpec.FromConst(Amount()), StatusId: "strength"));
                    break;
                case "heal_at_combat_start":
                    openingNodes.Add(new CombatNodeModel("heal", "source", CombatAmountSpec.FromConst(Amount())));
                    break;
                case "gain_energy_at_combat_start":
                    // ADAPTATION: the engine has no combat-scoped "overcharge above max" — a combat-start
                    // gainResource just clamps to the energy cap (turn 1 already refills to it). So this
                    // maps to a PERMANENT +N max energy (the resourceMax counter, like increase_max_energy):
                    // meaningful and crash-free, instead of a one-time turn-1 overcharge.
                    pickup.Add(new IncrementCounterRunEffect(
                        new RunCounterId(StandardRunIds.ResourceMaxCounterPrefix + StandardCombatIds.EnergyResource.value),
                        Amount()));
                    break;
                case "create_card_at_combat_start":
                    openingNodes.Add(new CombatNodeModel("createCardInstance", "source",
                        CombatAmountSpec.FromConst(effect.Copies ?? 1),
                        ToDefinition: CardMapper.MapCardId(effect.CardId
                            ?? throw new ConversionException(where, "create_card without card_id")),
                        ToZone: effect.Destination switch
                        {
                            "hand" => CardZone.Hand,
                            "discard_pile" or null => CardZone.DiscardPile,
                            "draw_pile" => CardZone.DrawPile,
                            var other => throw new ConversionException(where, $"unmapped destination '{other}'"),
                        }));
                    break;

                case "heal_on_pickup":
                    pickup.Add(new HealRunEffect(Amount()));
                    break;
                case "gain_gold_on_pickup":
                    pickup.Add(new ChangeResourceRunEffect(StandardRunIds.Gold, Amount()));
                    break;
                case "increase_max_energy":
                    // Permanent while the run lasts: the reserved resourceMax counter (engine E4).
                    pickup.Add(new IncrementCounterRunEffect(
                        new RunCounterId(StandardRunIds.ResourceMaxCounterPrefix + StandardCombatIds.EnergyResource.value),
                        Amount()));
                    break;

                case "increase_gold_rewards":
                    // Faithful: the original adds a flat bonus to every combat's gold reward.
                    programs.Add(RunPrograms.When<CombatResolvedRunEvent>(
                        new EventBoolValueExpression(RunEventFields.CombatVictory),
                        new ChangeResourceRunEffect(StandardRunIds.Gold, Amount())));
                    break;
                case "increase_card_reward_count":
                    // ADAPTATION: reward-offer modifiers are not data — becomes gold per victory.
                    programs.Add(RunPrograms.When<CombatResolvedRunEvent>(
                        new EventBoolValueExpression(RunEventFields.CombatVictory),
                        new ChangeResourceRunEffect(StandardRunIds.Gold, 15)));
                    break;
                case "shop_price_discount":
                    // ADAPTATION: dynamic prices are not data — becomes gold back per shop purchase.
                    programs.Add(RunPrograms.On<ShopItemPurchasedRunEvent>(
                        new ChangeResourceRunEffect(StandardRunIds.Gold, 10)));
                    break;

                default:
                    throw new ConversionException(where, $"unmapped relic effect type '{effect.Type}'");
            }
        }

        if (openingNodes.Count > 0)
            programs.Add(Openings.EveryCombat(openingNodes.ToArray()));

        return new MappedRelic(relic, new RelicData
        {
            Id = relic.Id,
            DisplayName = relic.Name,
            RunPrograms = programs,
        }, pickup);
    }
}
