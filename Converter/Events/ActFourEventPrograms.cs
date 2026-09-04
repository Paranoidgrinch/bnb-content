using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Relics;

namespace BnbContent.Converter.Events;

// What Act IV's doors promise for AFTER the door closes.
//
// The Labyrinth's promises are almost all about a STRETCH of road rather than one fight — "the next two
// combats start Burdened 2", "the next three start Inscribed 1" — because that is what an office does: it
// files something about you, and the filing outlives the room. An opening is consumed by ONE fight, so a
// promise about N of them is a CHAIN: the door arms the first and installs the body that arms the second,
// which installs the body that arms the third, each stepping down as it is kept. No counter, no state — the
// remaining length IS the name of the program still installed, which is also what a save writes down.
//
// The other four are the act's fight doors. ADAPTATION, and the biggest one of the events: a door cannot
// open a fight. Nothing lets an event hand the run into a combat and take it back, and splicing a combat node into
// the map would need the event to know which node it is standing on, which an event never knows. So a door
// that offers a fight SETS ONE ON THE ROAD: the next ordinary combat is fought against the party the design
// names — they are in it, the enemies standing there are reinforced by them, their pressure is on you — and
// the prize is paid only if that fight is won.
public static class ActFourEventPrograms
{
    // ── the stretches ─────────────────────────────────────────────────────────────────────────────────────

    public const string Inscribed1 = "inscribed_1";
    public const string Burdened2 = "burdened_2";
    public const string Paperwork3 = "paperwork_3";
    public const string PanicAndBurden = "panic_1_burdened_1";
    public const string Weighed3 = "weighed_3";
    public const string Weighed2 = "weighed_2";
    public const string Doubt1 = "doubt_1";
    public const string Panic2 = "panic_2";
    public const string Paperwork2 = "paperwork_2";
    public const string Burdened1 = "burdened_1";
    public const string Embalmed1 = "embalmed_1";
    public const string Inscribed2 = "inscribed_2";
    public const string EnergyAndPanic = "energy_1_panic_1";

    // ── the four fight doors ──────────────────────────────────────────────────────────────────────────────

    public const string TabletDemanded = "act_four_tablet_demanded";
    public const string TabletPrize = "act_four_tablet_prize";
    public const string RobbersJoined = "act_four_robbers_joined";
    public const string RobbersPrize = "act_four_robbers_prize";
    public const string TitheRefused = "act_four_tithe_refused";
    public const string TithePrize = "act_four_tithe_prize";
    public const string CountRefused = "act_four_count_refused";
    public const string CountPrize = "act_four_count_prize";

    public static IReadOnlyDictionary<string, ITriggeredRunEffectDefinition> All(ConversionPools pools)
    {
        var programs = new Dictionary<string, ITriggeredRunEffectDefinition>
        {
            [TabletDemanded] = FightWaitsBody(TabletDemanded, TabletPrize,
                // The Reed-Pen Scribe, the Cartouche Recarver and the Palette-Bearing Apprentice: three
                // writers, so whoever is standing there writes harder, and what they are writing is about you.
                EveryEnemyGains("strength", 2), Applies(ActFour.InscribedId, 1)),
            [TabletPrize] = VictoryBody(TabletPrize,
                new OfferRewardRunEffect(
                    new RewardId("event:forewritten_tablet:relic"),
                    pools.NormalRelicOfRarity("the Forewritten Tablet's prize",
                        (RelicAuthoring.Rarity.Uncommon, 60), (RelicAuthoring.Rarity.Rare, 40)),
                    1)),
            [RobbersJoined] = FightWaitsBody(RobbersJoined, RobbersPrize,
                // The Grave-Cut Robber, the Lamp Thief and the Cursed Loot Bearer: the lamp goes out first.
                EveryEnemyGains("strength", 2), Applies("panic", 1)),
            [RobbersPrize] = VictoryBody(RobbersPrize,
                new ChangeResourceRunEffect(StandardRunIds.Gold, 120),
                new OfferRewardRunEffect(
                    new RewardId("event:tomb_robbers_fire:relic"),
                    pools.NormalRelicOfRarity("the Tomb Robbers' share",
                        (RelicAuthoring.Rarity.Common, 100)),
                    1)),
            [TitheRefused] = FightWaitsBody(TitheRefused, TithePrize,
                // The Copper Tribute Bearer and the Ivory-Weight Jackal: a collector and the thing that goes
                // with the collector, and the collector never comes to the door unaccompanied.
                EveryEnemyGains("strength", 2), Applies(ActFour.BurdenedId, 1)),
            // Refusing the tithe keeps the Gold you were holding — the branch takes nothing — and winning
            // adds seventy to it.
            [TithePrize] = VictoryBody(TithePrize,
                new ChangeResourceRunEffect(StandardRunIds.Gold, 70)),
            [CountRefused] = FightWaitsBody(CountRefused, CountPrize,
                // The Gate Tally Scribe, the Uncounted Pilgrim and the Ancestral Witness: three ways of being
                // counted, and every one of them has your name half-written already.
                EveryEnemyGains("strength", 3), Applies(ActFour.InscribedId, 1)),
            // Struck out BEFORE entered correctly, deliberately: nobody improves a card they are about to
            // have struck from the file, and asking in that order would let them.
            [CountPrize] = VictoryBody(CountPrize,
                new ChangeResourceRunEffect(StandardRunIds.Gold, 90),
                new RemoveCardsRunEffect(RunSelectors.DeckCards
                    .ChooseByPlayer(1, "choose what the survey strikes out")),
                new UpgradeCardsRunEffect(RunSelectors.DeckCards.Upgradable()
                    .ChooseByPlayer(1, "choose what the survey enters correctly"))),
        };

        // Every link of every stretch. A chain of N is registered down to 1, so a door that promises two
        // fights and a door that promises three share every link they have in common.
        foreach (var (key, longest) in Stretches)
            for (var remaining = 1; remaining < longest; remaining++)
                programs[Again(key, remaining)] = AgainBody(key, remaining);

        return programs;
    }

    // "The next N combats start with …": the first is armed at the door, and the rest are a chain of one-shot
    // bodies, each named for how many fights are still owed.
    public static IReadOnlyList<IRunEffectRequest> Stretch(string key, int combats)
    {
        if (combats < 1)
            throw new ArgumentOutOfRangeException(nameof(combats));
        return combats == 1
            ? [Openings.NextCombat([.. Nodes(key)])]
            : [Openings.NextCombat([.. Nodes(key)]), Install(Again(key, combats - 1))];
    }

    private static IReadOnlyDictionary<string, int> Stretches => new Dictionary<string, int>
    {
        [Inscribed1] = 3,
        [Burdened2] = 2,
        [Paperwork3] = 2,
        [PanicAndBurden] = 2,
        [Weighed3] = 2,
        [Weighed2] = 2,
        [Doubt1] = 2,
        [Panic2] = 1,
        [Paperwork2] = 3,
        [Burdened1] = 1,
        [Embalmed1] = 3,
        [Inscribed2] = 1,
        [EnergyAndPanic] = 2,
    };

    private static IReadOnlyList<CombatNodeModel> Nodes(string key) => key switch
    {
        Inscribed1 => [Applies(ActFour.InscribedId, 1)],
        Burdened2 => [Applies(ActFour.BurdenedId, 2)],
        Paperwork3 => [Applies("paperwork", 3)],
        PanicAndBurden => [Applies("panic", 1), Applies(ActFour.BurdenedId, 1)],
        Weighed3 => [Applies(ActFour.WeighedId, 3)],
        Weighed2 => [Applies(ActFour.WeighedId, 2)],
        Doubt1 => [Applies("doubt", 1)],
        Panic2 => [Applies("panic", 2)],
        Paperwork2 => [Applies("paperwork", 2)],
        Burdened1 => [Applies(ActFour.BurdenedId, 1)],
        Embalmed1 => [Applies(ActFour.EmbalmedId, 1)],
        Inscribed2 => [Applies(ActFour.InscribedId, 2)],
        // The festival's drum: the turn opens louder and worse. Both halves land at the first turn start,
        // which is the one moment an opening is read — and the "+1 Energy" is a Spare Hand, because the
        // pool an opening gives into has just been refilled to its maximum (see ActFourEventRelicRules).
        EnergyAndPanic => [Applies(ActFourEventRelicRules.SpareId, 1), Applies("panic", 1)],
        _ => throw new ConversionException("act IV event promise", $"no stretch named '{key}'"),
    };

    private static string Again(string key, int remaining) => $"act_four_{key}_again_{remaining}";

    private static ITriggeredRunEffectDefinition AgainBody(string key, int remaining) =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            Literal(Openings.NextCombat([.. Nodes(key)])),
            .. remaining > 1
                ? new[] { Literal(Install(Again(key, remaining - 1))) }
                : [],
            Done(Again(key, remaining)),
        ]);

    // A fight the door set on the road: the next ORDINARY combat is that party's, and its prize is armed with
    // it so that losing pays nothing.
    private static ITriggeredRunEffectDefinition FightWaitsBody(
        string name, string prize, params CombatNodeModel[] terms) =>
        RunPrograms.When<NodeEnteredRunEvent>(NormalCombat(),
        [
            Literal(Openings.NextCombat(terms)),
            Literal(Install(prize)),
            Done(name),
        ]);

    private static ITriggeredRunEffectDefinition VictoryBody(string name, params IRunEffectRequest[] paid) =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
            [.. paid.Select(Literal), Done(name)]);

    // ── shorthands ────────────────────────────────────────────────────────────────────────────────────────

    // An ordinary fight — not an elite, not a boss, not a mimic. The same reading Act III's doors use.
    private static IRunExpression<bool> NormalCombat() =>
        RunExpr.And(
            new EventBoolValueExpression(RunEventFields.NodeIsCombat),
            RunExpr.Or(
                RunEventValues.NodeHasTag(MapNodeTags.Combat),
                RunEventValues.NodeHasTag(MapNodeTags.MultiCombat)));

    private static CombatNodeModel Applies(string statusId, int stacks) =>
        new("applyStatus", "source", CombatAmountSpec.FromConst(stacks), StatusId: statusId);

    private static CombatNodeModel EveryEnemyGains(string statusId, int stacks) =>
        CombatNodeModel.ForEach("allEnemies",
            new CombatNodeModel("applyStatus", "iterationTarget",
                CombatAmountSpec.FromConst(stacks), StatusId: statusId));

    private static IRunEffectRequest Install(string program) =>
        new InstallProgramByIdRunEffect(new RunProgramSourceId(program));

    private static IRunEffectTemplate Literal(IRunEffectRequest effect) => RunEffectTemplates.Literal(effect);

    private static IRunEffectTemplate Done(string program) =>
        Literal(new UninstallRunProgramRunEffect(new RunProgramId(program)));
}
