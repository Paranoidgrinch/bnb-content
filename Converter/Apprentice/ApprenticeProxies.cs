using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.Imprints;
using static BnbContent.Converter.Cards.CardAuthoring;
using K = BnbContent.Converter.Cards.Keywords;

namespace BnbContent.Converter.Apprentice;

// WHAT EACH GENERAL CARD DOES IN THE WAND (master §11). The seventy general cards (the pool rebuilt on 2026-11-08,
// GENERAL_POOL_PLAN.md) keep their play text, their upgrade and everything the Bureaucrat and the Witch do with
// them; for the Apprentice each gets a small, hand-written wand part — never its full card text replayed, never a
// Rite installed. Its upgrade carries the same proxy (§11.2). Energy in the wand is Exhaustion relief (Ground),
// drawing comes after the cast. "Copy" in the master is each atom's Copyable: what an Echo, a Keep or a Shape may
// take. Written onto the card at assembly; it means something only to a wand.
public static class ApprenticeProxies
{
    public static readonly IReadOnlyDictionary<string, WandImprint> General = new Dictionary<string, WandImprint>(StringComparer.Ordinal)
    {
        ["ash_remembers"] = Speak(Do(Draw(1), afterCast: true)),
        ["ashen_effigy"] = Speak(Hit(9)),
        ["ashen_receipt"] = Speak(Guard(4)),
        ["banked_embers"] = Speak(Gain(HeldEnergy.Id, 1), Do(Draw(1), afterCast: true)),
        ["barred_door"] = Speak(Guard(8)),
        ["beadles_cudgel"] = Speak(Hit(5)),
        ["besom_guard"] = Speak(Guard(5)),
        ["bill_of_attainder"] = Speak(Put(K.Doubt, 1), Put(K.Liable, 1)),
        ["blacklisted"] = Speak(Hit(7)),
        ["bone_meal"] = Speak(Guard(3)),
        ["borrowed_candle"] = Speak(Do(Draw(1), afterCast: true)),
        ["burnt_offering"] = Speak(Guard(5)),
        ["coin_under_the_tongue"] = Speak(Do(Draw(1), afterCast: true)),
        ["cold_hearth"] = Speak(Guard(Amount(Plus(Const(3), AtMost(CardsInZone(CardZone.ExhaustPile), 4))))),
        ["compounded_grievance"] = Heighten(0, stacks: 2, filter: WandFilter.NegativeStatus),
        ["corpse_candle"] = Speak(Hit(3)),
        ["crossed_sigil"] = Speak(Do(Draw(1), afterCast: true)),
        ["curdled_milk"] = Speak(Put(K.Doubt, 1, WandTargeting.AllEnemies)),
        ["dawn_summons"] = Speak(Hit(8).Plus(4, WandPosition.FirstSpeak)),
        ["dog_eared_almanac"] = Speak(Do(Draw(1), afterCast: true)),
        ["errand_rook"] = Speak(Do(Draw(1), afterCast: true)),
        ["false_signature"] = Heighten(25, stacks: 0, filter: WandFilter.DamageOrDefense, strain: 1),
        ["gathered_gossip"] = Speak(Do(Draw(1), afterCast: true)),
        ["hailstones"] = Speak(Hit(2, hits: 2, to: WandTargeting.AllEnemies)),
        ["hares_mark"] = Speak(Put(K.Liable, 1), Put(K.Liable, 1).When(HasStacks(K.Doubt)).Private()),
        ["hearth_kindling"] = Speak(Guard(3), Do(Draw(1), afterCast: true)),
        ["ill_wind"] = Speak(Hit(4, to: WandTargeting.AllEnemies)),
        ["lamplit_study"] = Speak(Guard(3), Do(Draw(1), afterCast: true)),
        ["last_ember"] = Echo(50, WandFilter.Any),
        ["lingering_malison"] = Heighten(0, stacks: 1, filter: WandFilter.NegativeStatus),
        ["melted_down"] = Ground(1),
        ["midnight_toll"] = Speak(Hit(6)),
        ["misers_coffer"] = Speak(Gain(HeldEnergy.Id, 1)),
        ["moonlit_counterfeit"] = Echo(50, WandFilter.DamageOrDefense),
        ["named_in_the_roll"] = Speak(Put(K.Liable, 1)),
        ["night_scrivener"] = Speak(Do(Draw(2), afterCast: true)),
        ["overlooking"] = Speak(Put(K.Doubt, 1)),
        ["poppet_pin"] = Speak(Hit(3)),
        ["pyre_of_old_records"] = Speak(Hit(4), Guard(4)),
        ["raked_from_the_ashes"] = Speak(Do(Draw(1), afterCast: true)),
        ["read_by_firelight"] = Speak(Do(Draw(1), afterCast: true)),
        ["rowan_over_the_door"] = Speak(Guard(4)),
        ["salamander_in_the_grate"] = Speak(Hit(4, hits: 2)),
        ["salt_on_the_sill"] = Speak(Guard(3), Put(K.Doubt, 1).When(TargetIntendsAttack)),
        ["sanctioned_charm"] = Speak(Do(Draw(1), afterCast: true)),
        ["scattered_notes"] = Speak(Do(Draw(1), afterCast: true)),
        ["sealed_mantle"] = Speak(Guard(5).Private(), Do(Apply(Cards.GeneralRites.SealedMantle, 1, You))),
        ["seed_of_misgiving"] = Speak(Put(K.Doubt, 1), Guard(2)),
        ["sieve_and_shears"] = Speak(Do(Draw(1), afterCast: true)),
        ["sifted_ashes"] = Speak(Hit(5)),
        ["smouldering_correspondence"] = Speak(Hit(3, to: WandTargeting.AllEnemies)),
        ["spiteful_draft"] = Speak(Hit(4)),
        ["spreading_rumour"] = Speak(Put(K.Liable, 1, WandTargeting.AllEnemies)),
        ["standing_stones"] = Speak(Guard(5)),
        ["stay_of_proceedings"] = Speak(Put(K.Doubt, 2)),
        ["stolen_hour"] = Ground(2),
        ["tallow_judgment"] = Speak(Do(Judgment(3, 5, 3))),
        ["tallow_reserve"] = Speak(Do(ReserveTallow(4, 3))),
        ["tallow_stub"] = Speak(Gain(HeldEnergy.Id, 1)),
        ["third_and_final_writ"] = Speak(Hit(7)),
        ["thrown_inkpot"] = Speak(Hit(5), Put(K.Doubt, 1).Private()),
        ["unburied"] = Speak(Guard(5)),
        ["vigil_lamp"] = Speak(Guard(6)),
        ["volley_of_quills"] = Speak(Hit(Amount(Times(AtMost(CardsInZone(CardZone.Hand), 5), 1)), to: WandTargeting.AllEnemies)),
        ["wax_reliquary"] = Speak(Wax(3).Private()),
        ["waxen_surety"] = Speak(Wax(4)),
        ["weight_of_paper"] = Speak(Hit(Amount(Plus(Const(3), AtMost(CardsInZone(CardZone.DiscardPile), 6))))),
        ["wheat_from_chaff"] = Speak(Do(Draw(1), afterCast: true)),
        ["whispering_gallery"] = Speak(Put(K.Doubt, 1, WandTargeting.AllEnemies)),
        ["wick_burnt_short"] = Ground(2),
    };

    // The words for each proxy, as a card's "Wand:" line — shown on the card when the Apprentice holds it.
    public static readonly IReadOnlyDictionary<string, string> Text = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ash_remembers"] = "Speak: after the cast, draw 1 card.",
        ["ashen_effigy"] = "Speak: deal 9 damage.",
        ["ashen_receipt"] = "Speak: gain 4 Block.",
        ["banked_embers"] = "Speak: gain 1 Held Energy; after the cast, draw 1 card.",
        ["barred_door"] = "Speak: gain 8 Block.",
        ["beadles_cudgel"] = "Speak: deal 5 damage.",
        ["besom_guard"] = "Speak: gain 5 Block.",
        ["bill_of_attainder"] = "Speak: apply 1 Doubt and 1 Liable.",
        ["blacklisted"] = "Speak: deal 7 damage.",
        ["bone_meal"] = "Speak: gain 3 Block.",
        ["borrowed_candle"] = "Speak: after the cast, draw 1 card.",
        ["burnt_offering"] = "Speak: gain 5 Block.",
        ["coin_under_the_tongue"] = "Speak: after the cast, draw 1 card.",
        ["cold_hearth"] = "Speak: gain 3 Block, +1 for each Exhausted card (4 at most).",
        ["compounded_grievance"] = "Heighten: the next Speak's first negative status gets 2 more stacks.",
        ["corpse_candle"] = "Speak: deal 3 damage.",
        ["crossed_sigil"] = "Speak: after the cast, draw 1 card.",
        ["curdled_milk"] = "Speak: apply 1 Doubt to ALL enemies.",
        ["dawn_summons"] = "Speak: deal 8 damage, +4 if it is the cast's first Speak.",
        ["dog_eared_almanac"] = "Speak: after the cast, draw 1 card.",
        ["errand_rook"] = "Speak: after the cast, draw 1 card.",
        ["false_signature"] = "Heighten: the next damaging or defensive Speak gives 25% more; adds 1 Exhaustion.",
        ["gathered_gossip"] = "Speak: after the cast, draw 1 card.",
        ["hailstones"] = "Speak: deal 2 damage to ALL enemies twice.",
        ["hares_mark"] = "Speak: apply 1 Liable, 2 if the target has Doubt.",
        ["hearth_kindling"] = "Speak: gain 3 Block; after the cast, draw 1 card.",
        ["ill_wind"] = "Speak: deal 4 damage to ALL enemies.",
        ["lamplit_study"] = "Speak: gain 3 Block; after the cast, draw 1 card.",
        ["last_ember"] = "Echo: repeat the last Speak at 50%.",
        ["lingering_malison"] = "Heighten: the next Speak's first negative status gets 1 more stack.",
        ["melted_down"] = "Ground: 1 less Exhaustion.",
        ["midnight_toll"] = "Speak: deal 6 damage.",
        ["misers_coffer"] = "Speak: gain 1 Held Energy.",
        ["moonlit_counterfeit"] = "Echo: repeat the last damaging or defensive Speak at 50%.",
        ["named_in_the_roll"] = "Speak: apply 1 Liable.",
        ["night_scrivener"] = "Speak: after the cast, draw 2 cards.",
        ["overlooking"] = "Speak: apply 1 Doubt.",
        ["poppet_pin"] = "Speak: deal 3 damage.",
        ["pyre_of_old_records"] = "Speak: deal 4 damage and gain 4 Block.",
        ["raked_from_the_ashes"] = "Speak: after the cast, draw 1 card.",
        ["read_by_firelight"] = "Speak: after the cast, draw 1 card.",
        ["rowan_over_the_door"] = "Speak: gain 4 Block.",
        ["salamander_in_the_grate"] = "Speak: deal 4 damage twice.",
        ["salt_on_the_sill"] = "Speak: gain 3 Block; if the target intends to attack, apply 1 Doubt.",
        ["sanctioned_charm"] = "Speak: after the cast, draw 1 card.",
        ["scattered_notes"] = "Speak: after the cast, draw 1 card.",
        ["sealed_mantle"] = "Speak: gain 5 Block; if an enemy attacks and nothing gets through, gain Ward Wax.",
        ["seed_of_misgiving"] = "Speak: apply 1 Doubt; gain 2 Block.",
        ["sieve_and_shears"] = "Speak: after the cast, draw 1 card.",
        ["sifted_ashes"] = "Speak: deal 5 damage.",
        ["smouldering_correspondence"] = "Speak: deal 3 damage to ALL enemies.",
        ["spiteful_draft"] = "Speak: deal 4 damage.",
        ["spreading_rumour"] = "Speak: apply 1 Liable to ALL enemies.",
        ["standing_stones"] = "Speak: gain 5 Block.",
        ["stay_of_proceedings"] = "Speak: apply 2 Doubt.",
        ["stolen_hour"] = "Ground: 2 less Exhaustion.",
        ["tallow_judgment"] = "Speak: spend up to 3 Ward Wax; deal 5 damage, +3 for each.",
        ["tallow_reserve"] = "Speak: lose up to 4 Block; if you lost 4, gain 3 Ward Wax.",
        ["tallow_stub"] = "Speak: gain 1 Held Energy.",
        ["third_and_final_writ"] = "Speak: deal 7 damage.",
        ["thrown_inkpot"] = "Speak: deal 5 damage and apply 1 Doubt.",
        ["unburied"] = "Speak: gain 5 Block.",
        ["vigil_lamp"] = "Speak: gain 6 Block.",
        ["volley_of_quills"] = "Speak: deal 1 damage to ALL enemies for each card in your hand (5 at most).",
        ["wax_reliquary"] = "Speak: gain 3 Ward Wax.",
        ["waxen_surety"] = "Speak: gain 4 Ward Wax.",
        ["weight_of_paper"] = "Speak: deal 3 damage, +1 for each card in your discard pile (6 at most).",
        ["wheat_from_chaff"] = "Speak: after the cast, draw 1 card.",
        ["whispering_gallery"] = "Speak: apply 1 Doubt to ALL enemies.",
        ["wick_burnt_short"] = "Ground: 2 less Exhaustion.",
    };

    public const string WandTextKey = "wand";

    // The presentation of every general card (and its upgrade) carries its proxy's words under Extra["wand"].
    public static PresentationManifest Describe(PresentationManifest manifest)
    {
        var cards = new Dictionary<string, EntityPresentation>(manifest.Cards, StringComparer.Ordinal);
        foreach (var (id, look) in manifest.Cards)
            if (Text.TryGetValue(id.TrimEnd('+'), out var words))
                cards[id] = look with { Extra = new Dictionary<string, string>(look.Extra, StringComparer.Ordinal) { [WandTextKey] = words } };
        return manifest with { Cards = cards };
    }

    // Each general card and its upgrade gets its proxy (master §11.2: the upgrade's wand part is the same).
    public static IReadOnlyList<CardData> Mark(IReadOnlyList<CardData> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        return [.. cards.Select(card =>
            card.Wand is null && General.TryGetValue(card.Id.TrimEnd('+'), out var proxy) ? card with { Wand = proxy } : card)];
    }

    // ── shorthand ──────────────────────────────────────────────────────────────────────────────────────────

    private static CombatAmountSpec Const(int value) => CombatAmountSpec.FromConst(value);

    private static CounterId Scratch => new("apprentice_proxy_scratch");
    private static CombatAmountSpec ScratchValue(string on = Target) => CombatAmountSpec.Counter(on, Scratch.value);
    private static CombatNodeModel SetScratch(CombatAmountSpec amount, string on = Target) =>
        new("setCombatantCounter", on, amount, CounterId: Scratch.value, Relative: false);
    private static CombatConditionSpec ScratchAtLeast(int value, string on = Target) =>
        new("compare", on, ValueKind: "counter", Op: ComparisonOperator.GreaterOrEqual, Right: value, Id: Scratch.value);

    private static CombatConditionSpec TargetIntendsAttack => new("intends", Target, Id: nameof(IntentKind.Attack));

    private static CombatNodeModel Judgment(int most, int damage, int per) =>
        Seq(
            SetScratch(AtMost(Stacks(K.WardWax, You), most), You),
            new CombatNodeModel("modifyStatusStacks", You, CombatAmountSpec.Unary("neg", ScratchValue(You)), StatusId: K.WardWax),
            Damage(Plus(Const(damage), Times(ScratchValue(You), per))));

    // "Lose up to N Block; if all N were lost, gain W Ward Wax."
    private static CombatNodeModel ReserveTallow(int price, int wax) =>
        Seq(
            SetScratch(new CombatAmountSpec("defensivePool", SelectorKey: You, ReadId: StandardCombatIds.BlockDefensivePool.value), You),
            new CombatNodeModel("modifyDefensivePool", You, CombatAmountSpec.Unary("neg", AtMost(ScratchValue(You), price)),
                PoolId: StandardCombatIds.BlockDefensivePool.value),
            If(ScratchAtLeast(price, You), Apply(K.WardWax, wax, You)));
}
