using BnbContent.Converter;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Catalogue of Unwise Names (user, 2026-11-03): every turn you name a card in your hand — it costs 1 less and
// Exhausts, or 1 more and stays in hand — and the entry holds until the card is played.
public class CatalogueOfUnwiseNamesTests
{
    private static CombatantCardZones Zones(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);

    private static CardInstance Card(RunPlayback play, CardInstanceId id) =>
        Enum.GetValues<CardZone>().SelectMany(Zones(play).GetCardsInZone).First(c => c.Id == id);

    private static CardZone ZoneOf(RunPlayback play, CardInstanceId id) =>
        Enum.GetValues<CardZone>().First(z => Zones(play).GetCardsInZone(z).Any(c => c.Id == id));

    private static int Energy(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId)
            .Resources[StandardCombatIds.EnergyResource].Current;

    // Names the first card in hand, entered by `option` (0 cheaper, 1 dearer).
    private static (RunPlayback Play, CardInstanceId Named) Fight(int option)
    {
        var play = FightProbe.Start(
            FightProbe.Solo(CatalogueOfUnwiseNames.EnemyId, "enter_the_name_in_black_salt"),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400).Play;
        var named = play.CombatDriver!.PendingCardChoice![0].Id;
        play.CombatDriver.SupplyCardChoice([named]);
        play.CombatDriver.SupplyOptionChoice([option]);
        return (play, named);
    }

    private static CombatantId Catalogue(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId).Id;

    [Fact]
    public void A_cheaper_name_costs_one_less_and_exhausts()
    {
        var (play, named) = Fight(0);
        Assert.True(Card(play, named).HasMark(new TagId(CatalogueOfUnwiseNames.CheaperMark)));
        var energy = Energy(play);
        play.CombatDriver!.PlayCard(named, Catalogue(play));
        Assert.Equal(energy, Energy(play)); // paper_cut costs 1, named cheaper it costs 0
        Assert.Equal(CardZone.ExhaustPile, ZoneOf(play, named));
        play.Dispose();
    }

    // Discarded unplayed, a cheaper card is still cheaper.
    [Fact]
    public void A_cheaper_name_holds_until_the_card_is_played()
    {
        var (play, named) = Fight(0);
        play.CombatDriver!.EndTurn();
        Assert.NotEqual(CardZone.ExhaustPile, ZoneOf(play, named));
        Assert.True(Card(play, named).HasMark(new TagId(CatalogueOfUnwiseNames.CheaperMark)));
        Assert.Equal(-1, Card(play, named).GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
        play.Dispose();
    }

    [Fact]
    public void A_dearer_name_stays_in_hand_until_played_and_then_is_itself_again()
    {
        var (play, named) = Fight(1);
        play.CombatDriver!.EndTurn();
        Assert.Equal(CardZone.Hand, ZoneOf(play, named));
        if (play.CombatDriver.PendingCardChoice is { } offered)
        {
            play.CombatDriver.SupplyCardChoice([offered.First(c => c.Id != named).Id]);
            play.CombatDriver.SupplyOptionChoice([0]);
        }
        var energy = Energy(play);
        play.CombatDriver.PlayCard(named, Catalogue(play));
        Assert.Equal(energy - 2, Energy(play));
        Assert.Equal(CardZone.DiscardPile, ZoneOf(play, named));
        var card = Card(play, named);
        Assert.False(card.HasMark(new TagId(CatalogueOfUnwiseNames.DearerMark)));
        Assert.False(card.HasMark(StandardCombatIds.RetainedCardMark));
        Assert.Equal(0, card.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
        play.Dispose();
    }
}
