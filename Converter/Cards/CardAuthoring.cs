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
        bool RetainInHand = false,
        bool Queued = false)
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
            QueueOnPlay = Queued,
            // A Queue card's program runs ONLY when it resolves — playing it merely files it — so the pulse
            // appended here means exactly "a queued card has just resolved", which is what the Rites and
            // relics that wait for that are listening to.
            Program = CombatProgramModel.Build<CardPlayContext>(
                Queued ? Seq(Program, Apply(Keywords.QueueResolved, 1, You)) : Program),
        };
    }

    // The upgraded twin. Only what the sheet says changes is passed; everything else is inherited, so an
    // upgrade that only moves a number cannot silently drop a tag or a destination zone.
    public static BnbCard Upgraded(
        this BnbCard card, string text, CombatNodeModel? program = null, int? cost = null,
        IReadOnlyList<string>? tags = null, CardZone? destination = null, bool? retainInHand = null,
        bool? queued = null) =>
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
            Queued = queued ?? card.Queued,
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

    // Card text is read top to bottom, and a card's later clauses routinely ask about what its earlier ones
    // did ("apply 2 Seal. If this Ratifies the target, …"). So a card's steps run CAUSALLY: each waits for the
    // one before it to have happened, rather than all starting at once.
    public static CombatNodeModel Seq(params CombatNodeModel[] steps) =>
        steps.Length == 1 ? steps[0] : CombatNodeModel.CausalSequence(steps);

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
        Seq(Apply(Keywords.Seal, stacks, to), ConvertSeals(to));

    // Spend every complete set of three the target is now holding. Written as nested questions rather than a
    // loop on purpose: the engine's repeat-until runs its body once before it ever asks, which would Ratify a
    // single Seal. Two conversions is the ceiling anything in the game can reach in one application — the
    // largest grant is 3, on top of at most 2 already standing.
    public static CombatNodeModel ConvertSeals(string to = Target) =>
        If(HasStacks(Keywords.Seal, RatifyThreshold, to),
            Seq(Ratify(to), If(HasStacks(Keywords.Seal, RatifyThreshold, to), Ratify(to))));

    public const int RatifyThreshold = 3;

    // ── Archive ───────────────────────────────────────────────────────────────────────────────────────────

    // "Archive" a card: it goes to the Exhaust pile, and the act is recorded on the archivist. The record is
    // what separates Archiving from ordinary exhausting — a Rite that says "whenever you Archive" watches the
    // record, and a card that counts "each card you have Archived this combat" reads it. One stack per card,
    // so an effect that Archives several produces several events, as the design requires.
    public static CombatNodeModel Archive(CombatCardSpec card) =>
        Seq(
            new CombatNodeModel("moveCardToZone", You, Card: card, ToZone: CardZone.ExhaustPile),
            Apply(Keywords.Archived, 1, You));

    // The player picks a card in hand to Archive.
    public static CombatNodeModel ArchiveChosen(string purpose = "choose a card to Archive") =>
        Archive(new CombatCardSpec("chosen", CardZone.Hand, Purpose: purpose));

    // Creating Junk, as an event: the count only ever grows, which keeps it clear of the general pool's
    // Blood Ink (which answers statuses LOSING stacks), and Clerk's Familiar reads the growth.
    public static CombatNodeModel AddJunk(string cardId, CardZone zone, int copies = 1) =>
        Seq(AddCard(cardId, zone, copies), Apply(Keywords.JunkFiled, copies, You));

    // A Rite: a persistent combat effect. Playing it puts a status on you, and that status carries the rule —
    // which is also how a Rite watches the ENEMIES, since a status trigger can be scoped to the whole fight.
    public static CombatNodeModel InstallRite(string riteStatusId) =>
        Apply(riteStatusId, 1, You);

    public static CombatAmountSpec ArchivedCount =>
        new("statusStacks", SelectorKey: You, ReadId: Keywords.Archived);

    // How many cards in one of your zones carry a tag — "for each Junk card in your hand".
    public static CombatAmountSpec CardsTagged(string tag, CardZone zone = CardZone.Hand) =>
        new("zoneCards", SelectorKey: You, ReadId: tag, Zone: zone);

    // 1 when the condition amount is non-zero, 0 otherwise — the "if at all" of an amount, used where an
    // effect is worth a flat bonus rather than one per matching thing.
    public static CombatAmountSpec Once(CombatAmountSpec amount) =>
        CombatAmountSpec.Binary("min", amount, CombatAmountSpec.FromConst(1));

    // amount, but never above a ceiling — "count at most 3 Queued cards", "maximum 12 Block".
    public static CombatAmountSpec AtMost(CombatAmountSpec amount, int ceiling) =>
        CombatAmountSpec.Binary("min", amount, CombatAmountSpec.FromConst(ceiling));

    public static CombatAmountSpec CardsInZone(CardZone zone) =>
        new("zoneCards", SelectorKey: You, Zone: zone);

    public static CombatAmountSpec Plus(CombatAmountSpec a, CombatAmountSpec b) =>
        CombatAmountSpec.Binary("add", a, b);

    // How many DIFFERENT statuses of a kind a combatant carries. Stacks are countable; distinct statuses are
    // not, so the ones the game actually files are named and each is counted as present-or-not. A new status
    // of either kind has to be added here, which is the price of being able to ask the question at all.
    public static readonly string[] NegativeStatuses =
    [
        Keywords.Paperwork, Keywords.Doubt, Keywords.Seal, Keywords.Lien, Keywords.Citation, Keywords.BloodInk,
        "panic", "fatigue", "poison",
    ];

    public static readonly string[] PositiveStatuses = [Keywords.WardWax, "strength", "bookworm"];

    public static CombatAmountSpec DistinctStatuses(IReadOnlyList<string> kinds, string on = Target)
    {
        CombatAmountSpec? total = null;
        foreach (var status in kinds)
        {
            var present = Once(Stacks(status, on));
            total = total is null ? present : Plus(total, present);
        }
        return total!;
    }

    public static CombatAmountSpec Times(CombatAmountSpec a, int factor) =>
        CombatAmountSpec.Binary("mul", a, CombatAmountSpec.FromConst(factor));

    // A Paperwork toll paid on the spot: the same HP loss the status deals at the end of a turn, which is
    // what "trigger its Paperwork immediately" means. Not an attack, so nothing that shapes attacks reshapes it.
    public static CombatNodeModel TriggerPaperwork(string on = Target) =>
        new("dealDamage", on, Stacks(Keywords.Paperwork, on),
            IgnoresBlock: true, DamageKind: DamageKind.DamageOverTime);

    // One Ratify event: the three Seals are spent and the enemy is Ratified. A second Ratify in the same turn
    // adds no further damage (Ratified's bonus is flat), but it is still its own event for anything watching.
    public static CombatNodeModel Ratify(string to = Target) =>
        Seq(
            Remove(Keywords.Seal, RatifyThreshold, to),
            Apply(Keywords.Ratified, 1, to),
            // Hieratic Measure: a Ratify calls in the enemy's Paperwork on the spot. Asked here because this
            // is where a Ratify happens; the Rite itself is only a marker the conversion looks for.
            IfRiteInForce(ActIVRites.HieraticMeasure,
                Seq(TriggerPaperwork(to), Remove(Keywords.Paperwork, 3, to))));

    // "Is this Rite in force?" A Rite the player carries is found by counting who wears it or its upgrade —
    // and since a condition can only compare a value read off a combatant, the count goes through a scratch
    // counter first.
    private static CounterId RiteHeld => new("rite_in_force");

    public static CombatNodeModel IfRiteInForce(string rite, CombatNodeModel then) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You, RiteCount(rite),
                CounterId: RiteHeld.value, Relative: false),
            If(new CombatConditionSpec("compare", You, ValueKind: "counter",
                    Op: ComparisonOperator.Greater, Right: 0, Id: RiteHeld.value),
                then));

    // 1 when either the Rite or its upgrade is on the table, 0 otherwise.
    public static CombatAmountSpec RiteCount(string rite) =>
        Once(Plus(
            new CombatAmountSpec("countTargets",
                ReadSelector: new CombatSelectorSpec("withStatus", rite,
                    [new CombatSelectorSpec("allCombatants")])),
            new CombatAmountSpec("countTargets",
                ReadSelector: new CombatSelectorSpec("withStatus", rite + "+",
                    [new CombatSelectorSpec("allCombatants")]))));
}
