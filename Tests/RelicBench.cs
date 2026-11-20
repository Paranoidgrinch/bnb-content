using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// A fight held still for a relic test: the relic (or none) in the inventory, a deck drawn in the order given, one or
// more authored enemies narrowed to one intent each, and the hero's Energy per turn. Every number a test reads is
// read off the live combat, so a test says what the relic's TEXT promises and the bench shows what it did.
internal sealed class RelicBench : IDisposable
{
    internal const string Quiet = LieutenantTests.Quiet;           // guards itself, never touches the hero
    internal const string QuietIntent = LieutenantTests.QuietIntent;
    internal const string Biter = LieutenantTests.Biter;           // 6 damage, one hit
    internal const string BiteIntent = LieutenantTests.BiteIntent;
    internal const string Twice = LieutenantTests.Twice;           // 5 damage, twice
    internal const string TwiceIntent = LieutenantTests.TwiceIntent;

    public required RunPlayback Play { get; init; }
    public required InteractiveRunSession Session { get; init; }
    public required IReadOnlyList<CombatantId> Enemies { get; init; }

    public CombatantId Enemy => Enemies[0];
    public InteractiveCombat Now => Play.CombatDriver!.Current!;
    public CombatantState Me => Now.State.GetCombatant(Now.HeroId);
    public CombatantState Body(CombatantId id) => Now.State.GetCombatant(id);
    public CombatantState It => Body(Enemy);
    public int Hand => Now.Hand.Count;
    public int Energy => Me.Resources[StandardCombatIds.EnergyResource].Current;
    public int Health => Me.Health.Current;
    public int Block => LieutenantTests.Block(Me);
    public int HealthOf(CombatantId id) => Body(id).Health.Current;
    public int Stacks(string status) => FightProbe.StacksOf(Me, status);
    public int StacksOn(CombatantId id, string status) => FightProbe.StacksOf(Body(id), status);
    public int Counter(string counter) => Me.GetCounter(new CounterId(counter));
    public int Pile(CardZone zone) => Now.State.CardZonesByCombatant[Now.HeroId].GetCardsInZone(zone).Count;
    public bool InHand(string card) => Now.Hand.Any(c => c.DefinitionId.value == card);
    public string Log => Now.RenderLog();

    // Play the first copy of `card` in hand. `option` answers a question the play asks; `pick` answers a card choice
    // with the first offered copy of that card.
    public void P(string card, CombatantId? at = null, int? option = null, string? pick = null)
    {
        var copy = Now.Hand.FirstOrDefault(c => c.DefinitionId.value == card)
            ?? throw new InvalidOperationException($"{card} is not in hand: {string.Join(", ", Now.Hand.Select(c => c.DefinitionId.value))}");
        Play.CombatDriver!.PlayCard(copy.Id, at ?? Enemy);
        Answer(option, pick);
    }

    public void Answer(int? option = null, string? pick = null)
    {
        var driver = Play.CombatDriver!;
        if (option is { } o)
        {
            Assert.NotNull(driver.PendingOptionChoice);
            driver.SupplyOptionChoice([o]);
        }
        if (pick is { } p)
        {
            Assert.NotNull(driver.PendingCardChoice);
            driver.SupplyCardChoice([driver.PendingCardChoice!.First(c => c.DefinitionId.value == p).Id]);
        }
        Assert.Null(Session.Error);
    }

    public void End()
    {
        Play.CombatDriver!.EndTurn();
        Assert.Null(Session.Error);
    }

    public void Dispose() => Play.Dispose();

    internal static RelicBench Arm(string? relic, string[] deck, int energy = 3, string? character = null,
        (string Enemy, string Intent, int? Health)[]? roster = null, (string Status, int Stacks)[]? hero = null,
        string[]? more = null)
    {
        roster ??= [(Quiet, QuietIntent, null)];
        var probe = FightProbe.RosterAgainstHero("relic_bench", energy, hero ?? [], roster);
        string[] relics = [.. relic is null ? [] : new[] { relic }, .. more ?? []];
        var (play, session, _) = FightProbe.Start(probe, deck, character: character, relics: relics);
        var combat = play.CombatDriver!.Current!;
        var enemies = combat.State.Combatants.Where(c => c.Id != combat.HeroId).Select(c => c.Id).ToList();
        return new RelicBench { Play = play, Session = session, Enemies = enemies };
    }

    internal static string[] Deck(params string[] cards) => cards;
    internal static string[] Times(string card, int n) => Enumerable.Repeat(card, n).ToArray();
}
