using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Relics;

// The cards six of the Act-IV boss relics hand over.
//
// The design writes six of these twenty-four as a "once per turn / once per combat FREE ACTION", and a combat
// in this game has no free actions — only cards. So the relic puts a card in the holder's hand at the bell,
// exactly as the Act-III courtesies do: free, exhausting, returned at the next bell while the relic is worn,
// and inert once it has been spent for the period it belongs to.
//
// The counters they set are the relics' own: the card is the hand the player reaches out with, and the relic
// is what remembers whether the offer has been taken.
public static class ActFourBossRelicCards
{
    public const string AudienceId = "edict_of_the_open_audience_action";
    public const string ErasureId = "erasure_tablet_action";
    public const string CorrectionId = "correction_reed_action";
    public const string ThroneId = "vacant_throne_decree_action";
    public const string SluiceId = "sluice_gate_of_the_two_lands_action";
    public const string VesselId = "black_flood_vessel_action";

    public static readonly TagId AudienceTag = new("edict_of_the_open_audience_action");
    public static readonly TagId ErasureTag = new("erasure_tablet_action");
    public static readonly TagId CorrectionTag = new("correction_reed_action");
    public static readonly TagId ThroneTag = new("vacant_throne_decree_action");
    public static readonly TagId SluiceTag = new("sluice_gate_of_the_two_lands_action");
    public static readonly TagId VesselTag = new("black_flood_vessel_action");

    // The mark the Edict leaves on what it made free, so the turn's end can put the price back on exactly
    // those cards and not on anything drawn afterwards.
    public static readonly TagId AudienceMark = new("open_audience");

    // How deep a cost mark has to go to reach zero: no card in the game costs more than a handful, and the
    // engine floors a card's cost at nothing rather than paying the holder to play it.
    public const int FreeEnough = -9;

    private static ICombatantTargetSelector Self => CombatantTargetSelectors.Source;

    public static IReadOnlyList<CardData> All() =>
    [
        OpenTheAudience(), EraseTheLine(), ASmallCorrection(), TheThroneStandsEmpty(),
        WorkTheTwoLands(), EmptyTheVessel(),
    ];

    // A free card that is not spent by being played: it exhausts, survives the turn boundary, and the relic
    // that offered it puts another in the hand at the next bell.
    private static CardData Action(
        string id, string name, string text, TagId tag, IEffectNode<CardPlayContext> program) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = text,
            Costs = [],
            Tags = [tag, new TagId(CardAuthoring.FormTag)],
            Program = new EffectProgram<CardPlayContext>(program),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.Hand,
        };

    // The offer, taken once for the period its relic keeps: the counter is written FIRST, so a body that
    // asks a question of the state cannot be answered twice by a re-entrant play.
    private static IEffectNode<CardPlayContext> Once(string counter, IEffectNode<CardPlayContext> body) =>
        new ConditionalEffectNode<CardPlayContext>(
            new ComparisonExpression<CardPlayContext>(
                new CombatantCounterExpression<CardPlayContext>(Self, new CounterId(counter)),
                ComparisonOperator.Equal, new ConstantExpression<CardPlayContext>(0)),
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new SetCombatantCounterNode<CardPlayContext>(
                    Self, new CounterId(counter), new ConstantExpression<CardPlayContext>(1), relative: false),
                body,
            ]));

    // ── The Pharaoh's audience ────────────────────────────────────────────────────────────────────────────

    // "All cards currently in hand cost 0 for the rest of that turn; later-drawn cards retain normal cost."
    //
    // Each card the audience hears is MARKED as well as cheapened, because a cost mark lives on the card
    // instance and would otherwise follow it into the discard and back out again three turns later. The
    // relic's own turn-end trigger reads the mark and puts the price back.
    private static CardData OpenTheAudience() =>
        Action(AudienceId, "Open the Audience",
            "Every card in your hand costs 0 for the rest of this turn. Once a combat.",
            AudienceTag,
            Once("edict_of_the_open_audience",
                new ForEachCardInZoneNode<CardPlayContext>(
                    Self, CardZone.Hand,
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        new MarkCardInstanceNode<CardPlayContext>(
                            Self, new IteratedCardExpression<CardPlayContext>(), AudienceMark),
                        new SetCardInstanceMarkCounterNode<CardPlayContext>(
                            Self, new IteratedCardExpression<CardPlayContext>(),
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<CardPlayContext>(FreeEnough), relative: true),
                    ]))));

    // ── The First Scribe's tablet ─────────────────────────────────────────────────────────────────────────

    // "Erase the revealed intent; the enemy does not perform it and gains 20 Block instead."
    //
    // ADAPTATION: an intent is the enemy's own rotation and no rule can take a turn out of it, so what the
    // erasure removes is what the line was FOR — the action lands for no damage, and the enemy spends the
    // turn guarding instead. The guard is paid by the erasure itself as the enemy's turn ends, not here:
    // Block handed out during the player's turn expires at the start of the enemy's. See ADAPTATIONS.md.
    private static CardData EraseTheLine() =>
        Action(ErasureId, "Erase the Line",
            "Every enemy's next action is erased: it deals no damage, and they guard for 20 instead. "
            + "Once a combat.",
            ErasureTag,
            Once("erasure_tablet",
                new ApplyStatusNode<CardPlayContext>(
                    CombatantTargetSelectors.AllEnemiesOfSource,
                    new StatusDefinitionId(BossRelicRules.ErasedLineId),
                    new ConstantExpression<CardPlayContext>(1))));

    // "Swap 1 non-Junk hand card with 1 non-Junk Discard card; the retrieved card costs 1 less that turn. If
    // no eligible Discard card, draw 1 instead."
    //
    // The retrieved card comes back on TOP of the hand, which is what makes it findable: a card moved into a
    // zone cannot be named afterwards, but the first card in hand can, and the placement decides which that
    // is. Junk is not excluded from either pick — the holder is choosing, and a holder who wants to send a
    // Junk card away and take a Junk card back has made a decision, not a mistake.
    private static CardData ASmallCorrection() =>
        Action(CorrectionId, "A Small Correction",
            "Send a card away and take one back out of your discard pile; it costs 1 less this turn. With "
            + "nothing to take back, draw 1. Once a turn.",
            CorrectionTag,
            Once("correction_reed",
                new ConditionalEffectNode<CardPlayContext>(
                    new ComparisonExpression<CardPlayContext>(
                        new CombatantZoneCardCountExpression<CardPlayContext>(Self, CardZone.DiscardPile),
                        ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0)),
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        new MoveCardToZoneNode<CardPlayContext>(
                            Self,
                            new ChosenCardInZoneExpression<CardPlayContext>(
                                CardZone.Hand, "send a card away", excludeTag: CorrectionTag),
                            CardZone.DiscardPile),
                        new MoveCardToZoneNode<CardPlayContext>(
                            Self,
                            new ChosenCardInZoneExpression<CardPlayContext>(
                                CardZone.DiscardPile, "take one back"),
                            CardZone.Hand, placement: ZonePlacement.Top),
                        new ForEachCardInZoneNode<CardPlayContext>(
                            Self, CardZone.Hand,
                            new SetCardInstanceMarkCounterNode<CardPlayContext>(
                                Self, new IteratedCardExpression<CardPlayContext>(),
                                StandardCombatIds.CardCostDeltaCounter,
                                new ConstantExpression<CardPlayContext>(-1), relative: true),
                            takeFirst: 1),
                    ]),
                    new DrawCardsNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(1)))));

    // ── The Vizier's empty throne ─────────────────────────────────────────────────────────────────────────

    private static CardData TheThroneStandsEmpty() =>
        Action(ThroneId, "The Throne Stands Empty",
            "Gain 3 Energy, draw 3, and gain 20 Block. Once a combat.",
            ThroneTag,
            Once("vacant_throne_decree",
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    HeldEnergy.Hold<CardPlayContext>(3),
                    new DrawCardsNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(3)),
                    new GainBlockNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(20)),
                ])));

    // ── The Queen's sluice ────────────────────────────────────────────────────────────────────────────────

    // "OPEN — lose 12 Block, gain 1 Energy; CLOSE — spend 1 Energy, gain 12 Block. Must fully pay cost."
    //
    // "Must fully pay" is the whole gate: each side is inside a condition that asks whether the holder can
    // afford it, so choosing a side you cannot pay for opens nothing. The offer is still spent — naming a
    // gate you cannot work is a decision the day is allowed to cost you.
    private static CardData WorkTheTwoLands() =>
        Action(SluiceId, "Work the Two Lands",
            "Open the gate — lose 12 Block for 1 Energy — or close it — spend 1 Energy for 12 Block. You "
            + "must be able to pay in full. Once a turn.",
            SluiceTag,
            Once("sluice_gate_of_the_two_lands",
                new ChooseOptionsNode<CardPlayContext>(
                [
                    new ConditionalEffectNode<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            new CombatantDefensivePoolExpression<CardPlayContext>(
                                Self, StandardCombatIds.BlockDefensivePool),
                            ComparisonOperator.GreaterOrEqual, new ConstantExpression<CardPlayContext>(12)),
                        new CausalSequenceEffectNode<CardPlayContext>(
                        [
                            new ModifyDefensivePoolNode<CardPlayContext>(
                                Self, StandardCombatIds.BlockDefensivePool,
                                new ConstantExpression<CardPlayContext>(-12)),
                            HeldEnergy.Hold<CardPlayContext>(1),
                        ])),
                    new ConditionalEffectNode<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            new CombatantCurrentResourceExpression<CardPlayContext>(
                                Self, StandardCombatIds.EnergyResource),
                            ComparisonOperator.GreaterOrEqual, new ConstantExpression<CardPlayContext>(1)),
                        new CausalSequenceEffectNode<CardPlayContext>(
                        [
                            new LoseResourceNode<CardPlayContext>(
                                Self, StandardCombatIds.EnergyResource,
                                new ConstantExpression<CardPlayContext>(1)),
                            new GainBlockNode<CardPlayContext>(
                                Self, new ConstantExpression<CardPlayContext>(12)),
                        ])),
                ],
                ["open the gate: 12 Block for 1 Energy", "close the gate: 1 Energy for 12 Block"],
                count: 1, purpose: "the sluice")));

    // "Discard entire hand, then draw 7 and gain 2 Energy. Discards trigger normal Discard effects."
    private static CardData EmptyTheVessel() =>
        Action(VesselId, "Empty the Vessel",
            "Discard your hand, draw 7, and gain 2 Energy. Once a combat.",
            VesselTag,
            Once("black_flood_vessel",
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new MoveAllCardsFromZoneNode<CardPlayContext>(
                        Self, CardZone.Hand, CardZone.DiscardPile),
                    new DrawCardsNode<CardPlayContext>(Self, new ConstantExpression<CardPlayContext>(7)),
                    HeldEnergy.Hold<CardPlayContext>(2),
                ])));
}
