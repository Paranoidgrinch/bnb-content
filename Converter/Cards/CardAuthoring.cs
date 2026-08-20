using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// Shared vocabulary and builders for the final card pool.
//
// BnB does not use Attack/Skill/Power. Its three primary types are Deed (a one-shot offensive action),
// Working (a one-shot defensive, manipulative or administrative action) and Rite (a persistent combat effect),
// plus Junk for the generated nuisance cards. Form, Argument and Permit are TAGS, not types.
//
// The primary type rides along as a combat tag, because that is what rules read it through: Ratified adds its
// damage only to cards tagged deed, relics count "the first Rite you play each turn", and the enemy passives
// that watch card TYPES (Wrong-Window Scribe, Triplicate Examiner) already read the same tag.
public static class CardAuthoring
{
    public const string DeedTag = "deed";
    public const string WorkingTag = "working";
    public const string RiteTag = "rite";
    public const string JunkTag = "junk";

    // Subtypes. Relics and enemies read these, so they are ordinary tags on top of the primary type.
    public const string FormTag = "form";
    public const string ArgumentTag = "argument";
    public const string PermitTag = "permit";

    // A combat-only generated instance. Nothing may copy, restore or record a card carrying this tag — the
    // anti-loop rule both card pools state.
    public const string TemporaryTag = "temporary";

    // The engine exhausts a played card carrying this tag.
    public const string ExhaustTag = "exhaust";

    // The engine refuses to play a card carrying this tag — Red Tape's whole identity.
    public static readonly string UnplayableTag = StandardCombatIds.UnplayableTag.value;

    public static readonly ResourceId Energy = StandardCombatIds.EnergyResource;

    // A Bureaucrat/general card as the design sheet writes it: the rules text and the numbers on one side,
    // the rarity and Act gate that decide when it can be offered on the other. Upgrade is the "+" version —
    // every regular card has one, and the engine finds it by the "<id>+" suffix at deck-build time.
    public sealed record BnbCard(
        string Id,
        string Name,
        string Type,
        int Cost,
        string Text,
        CombatNodeModel Program,
        string Rarity = "common",
        int Act = 1,
        IReadOnlyList<string>? Tags = null,
        CardZone Destination = CardZone.DiscardPile,
        bool RetainInHand = false)
    {
        public IReadOnlyList<string> AllTags => [Type, .. Tags ?? []];

        public CardData Compile() => new()
        {
            Id = Id,
            NameKey = Name,
            Costs = Cost == 0 ? [] : [new ResourceCost(Energy, Cost)],
            Tags = AllTags.Distinct().Select(t => new TagId(t)).ToArray(),
            PlayedCardDestinationZone = AllTags.Contains(ExhaustTag) ? CardZone.ExhaustPile : Destination,
            RetainInHandOnTurnEnd = RetainInHand,
            Program = CombatProgramModel.Build<CardPlayContext>(Program),
        };
    }

    // The upgraded twin. Only what the sheet says changes is passed; everything else is inherited, so an
    // upgrade that only moves a number cannot silently drop a tag or a destination zone.
    public static BnbCard Upgraded(
        this BnbCard card, string text, CombatNodeModel? program = null, int? cost = null,
        IReadOnlyList<string>? tags = null, CardZone? destination = null, bool? retainInHand = null) =>
        card with
        {
            Id = card.Id + "+",
            Name = card.Name + "+",
            Text = text,
            Program = program ?? card.Program,
            Cost = cost ?? card.Cost,
            Tags = tags ?? card.Tags,
            Destination = destination ?? card.Destination,
            RetainInHand = retainInHand ?? card.RetainInHand,
        };

    // ── program shorthands ────────────────────────────────────────────────────────────────────────────────
    //
    // "the target" is the enemy the player aimed at (eventTarget); "you" is the hero playing the card
    // (source). Both are the engine's own selector keys — named here so a card reads like its rules text.

    public const string Target = "eventTarget";
    public const string You = "source";
    public const string AllEnemies = "allEnemies";

    public static CombatNodeModel Damage(int amount, string to = Target) =>
        new("dealDamage", to, CombatAmountSpec.FromConst(amount));

    public static CombatNodeModel Damage(CombatAmountSpec amount, string to = Target) =>
        new("dealDamage", to, amount);

    public static CombatNodeModel Block(int amount) =>
        new("gainBlock", You, CombatAmountSpec.FromConst(amount));

    public static CombatNodeModel Block(CombatAmountSpec amount) =>
        new("gainBlock", You, amount);

    public static CombatNodeModel Apply(string status, int stacks, string to = Target) =>
        new("applyStatus", to, CombatAmountSpec.FromConst(stacks), StatusId: status);

    public static CombatNodeModel Apply(string status, CombatAmountSpec stacks, string to = Target) =>
        new("applyStatus", to, stacks, StatusId: status);

    public static CombatNodeModel Remove(string status, int stacks, string from = Target) =>
        new("modifyStatusStacks", from, CombatAmountSpec.FromConst(-stacks), StatusId: status);

    public static CombatNodeModel Draw(int cards) =>
        new("drawCards", You, CombatAmountSpec.FromConst(cards));

    public static CombatNodeModel Energy_(int amount) =>
        new("gainResource", You, CombatAmountSpec.FromConst(amount), Energy.value);

    public static CombatNodeModel AddCard(string cardId, CardZone zone, int copies = 1) =>
        new("createCardInstance", You, CombatAmountSpec.FromConst(copies), ToDefinition: cardId, ToZone: zone);

    public static CombatNodeModel Seq(params CombatNodeModel[] steps) =>
        steps.Length == 1 ? steps[0] : CombatNodeModel.Sequence(steps);

    public static CombatNodeModel Repeat(int times, CombatNodeModel body) =>
        CombatNodeModel.Repeat(CombatAmountSpec.FromConst(times), body);

    public static CombatNodeModel If(CombatConditionSpec test, CombatNodeModel then, CombatNodeModel? otherwise = null) =>
        otherwise is null
            ? CombatNodeModel.Conditional(test, then)
            : CombatNodeModel.Conditional(test, then, otherwise);

    // "the target has at least N of this status"
    public static CombatConditionSpec HasStacks(string status, int atLeast = 1, string on = Target) =>
        new("compare", on, ValueKind: "statusStacks", Op: ComparisonOperator.GreaterOrEqual,
            Right: atLeast, Id: status);

    public static CombatAmountSpec Stacks(string status, string on = Target) =>
        new("statusStacks", SelectorKey: on, ReadId: status);

    // ── Seal ──────────────────────────────────────────────────────────────────────────────────────────────

    // "Whenever an enemy reaches 3 Seal, remove exactly 3 Seal and trigger a Ratify event. Excess Seal
    // remains." The conversion lives here rather than on the Seal status because a status cannot react to its
    // own first application: the engine keeps a status' StatusApplied trigger from seeing itself, so the very
    // application that created the Seal would be invisible. Everything that grants Seal — cards, relics,
    // events — therefore grants it through this, and the loop covers a grant large enough to Ratify twice.
    public static CombatNodeModel ApplySeal(int stacks, string to = Target) =>
        Seq(
            Apply(Keywords.Seal, stacks, to),
            CombatNodeModel.RepeatUntil(
                new CombatConditionSpec("compare", to, ValueKind: "statusStacks",
                    Op: ComparisonOperator.Less, Right: RatifyThreshold, Id: Keywords.Seal),
                Ratify(to)));

    public const int RatifyThreshold = 3;

    // One Ratify event: the three Seals are spent and the enemy is Ratified. A second Ratify in the same turn
    // adds no further damage (Ratified's bonus is flat), but it is still its own event for anything watching.
    public static CombatNodeModel Ratify(string to = Target) =>
        Seq(
            Remove(Keywords.Seal, RatifyThreshold, to),
            Apply(Keywords.Ratified, 1, to));
}
