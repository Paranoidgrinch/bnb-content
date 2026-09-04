using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Drawer of Infinite Returns: file a card away and it comes back cheaper the deeper it went. What it
// costs is turns, and the Drawer counts those as Depth Pressure. Refusing to play along is a real option —
// with nothing filed, the drawer is closed and guards itself.
public class DrawerOfInfiniteReturnsTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Pressure(RunPlayback play, CombatantId id) =>
        FightProbe.StacksOf(Enemy(play, id), DrawerOfInfiniteReturns.DepthPressureId);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static CardInstance? Nested(RunPlayback play, CardZone zone) =>
        play.CombatDriver!.Current!.State
            .GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone(zone)
            .FirstOrDefault(c => c.HasMark(new TagId(DrawerOfInfiniteReturns.NestedMark)));

    // The Drawer's offer comes at the OPENING HAND, which is a real prompt now that the interactive driver
    // installs its choosers before the hand is dealt: the probe takes it up (option 0, "file a card away")
    // so every test below starts from a drawer with a card already in it. That the offer can be REFUSED is
    // its own test, from the second hand on.
    private static (RunPlayback, InteractiveRunSession, CombatantId) Fight(
        string intent = "open_another_drawer", params (string, int)[] statuses)
    {
        var started = FightProbe.Start(
            FightProbe.Solo(DrawerOfInfiniteReturns.EnemyId, intent, 9, statuses),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);
        // Two prompts, in order: which way the drawer goes, then which card goes into it.
        if (started.Play.CombatDriver!.PendingOptionChoice is not null)
            started.Play.CombatDriver.SupplyOptionChoice([0]);
        if (started.Play.CombatDriver.PendingCardChoice is { } candidates)
            started.Play.CombatDriver.SupplyCardChoice([candidates[0].Id]);
        Assert.Null(started.Session.Error);
        return (started.Play, started.Session, started.EnemyId);
    }

    private static CardInstanceId AlreadyFiled(RunPlayback play) =>
        Nested(play, CardZone.BanishedPile)!.Id;

    // Empty the drawer by playing what it returns, so the NEXT draw raises the offer interactively.
    private static void EmptyTheDrawer(RunPlayback play, CombatantId drawer)
    {
        play.CombatDriver!.EndTurn(); // the filed card comes back
        var back = Hand(play).First(c => c.HasMark(new TagId(DrawerOfInfiniteReturns.NestedMark)));
        play.CombatDriver.PlayCard(back.Id, drawer);
    }

    // 11.3: one card at a time leaves the normal zones entirely — Banished is the drawer.
    [Fact]
    public void What_is_filed_leaves_the_normal_zones()
    {
        var (play, _, _) = Fight();

        var filed = Nested(play, CardZone.BanishedPile);
        Assert.NotNull(filed);
        Assert.DoesNotContain(Hand(play), c => c.Id == filed!.Id);
        play.Dispose();
    }

    // …and it is voluntary: once the drawer is empty again the offer is a real prompt with a real refusal.
    [Fact]
    public void The_drawer_offers_and_can_be_refused()
    {
        var (play, session, drawer) = Fight();
        EmptyTheDrawer(play, drawer);
        play.CombatDriver!.EndTurn(); // drawer empty: it stands open and asks

        Assert.Equal(["file a card away in the Drawer", "keep your hand"],
            play.CombatDriver.PendingOptionChoice);
        var energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
        play.CombatDriver.SupplyOptionChoice([1]); // keep your hand
        Assert.Null(session.Error);

        Assert.Null(Nested(play, CardZone.BanishedPile));
        Assert.Equal(energy, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);
        play.Dispose();
    }

    // …and taking it costs no Energy and files the card the player named.
    [Fact]
    public void Filing_costs_nothing_and_takes_the_card_you_named()
    {
        var (play, session, drawer) = Fight();
        EmptyTheDrawer(play, drawer);
        play.CombatDriver!.EndTurn();

        play.CombatDriver.SupplyOptionChoice([0]);
        var pick = play.CombatDriver.PendingCardChoice![1].Id;
        var energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
        play.CombatDriver.SupplyCardChoice([pick]);
        Assert.Null(session.Error);

        Assert.Equal(pick, Nested(play, CardZone.BanishedPile)?.Id);
        Assert.Equal(energy, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);
        play.Dispose();
    }

    // 11.2 Closed Drawer: with nothing inside it simply guards itself — 14 Block a turn. A hand of nothing
    // but Junk is never asked, so the drawer stays shut and the player fights through the extra defence.
    [Fact]
    public void A_closed_drawer_guards_itself()
    {
        var (play, _, drawer) = FightProbe.Start(
            FightProbe.Solo(DrawerOfInfiniteReturns.EnemyId, "open_another_drawer", energy: 9),
            deck: [.. Enumerable.Repeat("red_tape", 16)], health: 400);

        Assert.Null(Nested(play, CardZone.BanishedPile));
        play.CombatDriver!.EndTurn();

        Assert.Equal(14, Block(Enemy(play, drawer)));
        play.Dispose();
    }

    // …and a full drawer is not a closed one.
    [Fact]
    public void A_full_drawer_does_not_guard()
    {
        var (play, _, drawer) = Fight();

        play.CombatDriver!.EndTurn();

        Assert.Equal(0, Block(Enemy(play, drawer)));
        play.Dispose();
    }

    // 11.4 Depth 1: it comes back at the start of the next player turn, costing 1 less.
    [Fact]
    public void What_you_filed_comes_back_cheaper()
    {
        var (play, _, _) = Fight();
        var filed = AlreadyFiled(play);

        play.CombatDriver!.EndTurn();

        var back = Hand(play).FirstOrDefault(c => c.Id == filed);
        Assert.NotNull(back);
        Assert.Equal(-1, back!.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
        play.Dispose();
    }

    // 11.5: left in hand at the turn's end it goes deeper, and entering Depth 2 costs a Depth Pressure.
    [Fact]
    public void A_card_left_unplayed_goes_deeper_and_presses()
    {
        var (play, _, drawer) = Fight();
        var filed = AlreadyFiled(play);

        play.CombatDriver!.EndTurn(); // Depth 1 back
        Assert.NotNull(Hand(play).FirstOrDefault(c => c.Id == filed));
        play.CombatDriver.EndTurn();  // left unplayed: down to Depth 2

        Assert.Equal(1, Pressure(play, drawer));
        Assert.Equal(filed, Nested(play, CardZone.BanishedPile)?.Id);
        play.Dispose();
    }

    // 11.6: from the inner drawer it comes back for nothing at all — the whole printed cost taken off.
    [Fact]
    public void From_the_inner_drawer_it_comes_back_free()
    {
        var (play, _, _) = Fight();
        var filed = AlreadyFiled(play);

        // Depth 1 back → unplayed → Depth 2 (waits) → back → unplayed → Depth 3 (waits) → back, free.
        for (var i = 0; i < 5; i++)
            play.CombatDriver!.EndTurn();

        var back = Hand(play).FirstOrDefault(c => c.Id == filed);
        Assert.NotNull(back);
        Assert.Equal(-1, back!.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
        Assert.Equal(0, back.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter)
            + PrintedCost(play, back));
        play.Dispose();
    }

    // 11.5: playing it from Depth 2 or deeper ends the nesting and pays a card back.
    [Fact]
    public void Playing_it_ends_the_nesting_and_pays_a_card()
    {
        var (play, session, drawer) = Fight();
        var filed = AlreadyFiled(play);

        play.CombatDriver!.EndTurn(); // Depth 1 back
        play.CombatDriver.EndTurn();  // unplayed: Depth 2, waiting
        play.CombatDriver.EndTurn();  // Depth 2 back

        var back = Hand(play).First(c => c.Id == filed);
        var handSize = Hand(play).Count;
        play.CombatDriver.PlayCard(back.Id, drawer);
        Assert.Null(session.Error);

        // One card played, one drawn to replace it: the hand is the size it was.
        Assert.Equal(handSize, Hand(play).Count);
        Assert.Null(Nested(play, CardZone.BanishedPile));
        play.Dispose();
    }

    // 11.7 / Signature: at Depth Pressure 3 the next intent is the Drawer slamming shut, whatever it was
    // going to do — and the pressure is spent.
    [Fact]
    public void At_three_pressures_the_drawer_slams_shut()
    {
        var (play, _, drawer) = Fight("index_the_contents",
            (DrawerOfInfiniteReturns.DepthPressureId, 3));

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(24, before - Hero(play).Health.Current); // not Index the Contents' 20 Block
        Assert.Equal(0, Pressure(play, drawer));
        play.Dispose();
    }

    // 11.7: "Maximum 3." Letting the card ride forever never presses past the ceiling.
    [Fact]
    public void The_pressure_never_passes_three()
    {
        var (play, _, drawer) = Fight();

        for (var i = 0; i < 12; i++)
        {
            play.CombatDriver!.EndTurn();
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([1]);
        }

        Assert.True(Pressure(play, drawer) <= 3);
        play.Dispose();
    }

    private static int PrintedCost(RunPlayback play, CardInstance card) =>
        FightProbe.Game.Cards.First(c => c.Id == card.DefinitionId.ToString())
            .Costs.FirstOrDefault(x => x.ResourceId == StandardCombatIds.EnergyResource)?.Amount ?? 0;
}
