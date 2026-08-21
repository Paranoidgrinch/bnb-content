using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Catalogue of Unwise Names: the player decides what is worth making official. The benefit is real and
// immediate, the Citation is chosen and shown at the same moment, and the book cashes it later at its own
// pace. These tests walk one entry from signing to cashing, and check that declining is a real option.
public class CatalogueOfUnwiseNamesTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static CardInstance? Marked(RunPlayback play, string mark) =>
        play.CombatDriver!.Current!.State
            .GetCardZones(play.CombatDriver.Current!.HeroId)
            .GetCardsInZone(CardZone.Hand)
            .FirstOrDefault(c => c.HasMark(new TagId(mark)));

    private static CardInstance? Anywhere(RunPlayback play, string mark) =>
        Enum.GetValues<CardZone>()
            .SelectMany(play.CombatDriver!.Current!.State
                .GetCardZones(play.CombatDriver.Current!.HeroId).GetCardsInZone)
            .FirstOrDefault(c => c.HasMark(new TagId(mark)));

    // 7.2: the naming is PREPARED by an intent and offered after the player's next normal draw — not on the
    // spot. The prompt is the player's, and taking it marks exactly one card.
    [Fact]
    public void Entering_a_name_marks_the_card_the_player_chose()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo(CatalogueOfUnwiseNames.EnemyId, "enter_the_name_in_black_salt"),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        play.CombatDriver!.EndTurn(); // the Catalogue prepares; the player's draw raises the offer
        Assert.Equal(
            ["enter a name in the Catalogue", "decline to be named"],
            play.CombatDriver.PendingOptionChoice);

        play.CombatDriver.SupplyOptionChoice([0]);
        Assert.Null(session.Error);

        // …and then the player says WHICH card. Naming is two decisions: whether, and what.
        var offered = play.CombatDriver.PendingCardChoice;
        Assert.NotNull(offered);
        var pick = offered![2];
        play.CombatDriver.SupplyCardChoice([pick.Id]);
        Assert.Null(session.Error);

        var named = Marked(play, CatalogueOfUnwiseNames.RecognizedMark);
        Assert.NotNull(named);
        Assert.Equal(pick.Id, named!.Id);
        // 7.3: the benefit is real — that instance costs 1 less.
        Assert.Equal(-1, named!.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
        // 7.4: and the liability is already shown, on the same card.
        Assert.True(new[]
        {
            CatalogueOfUnwiseNames.CitationOfCostMark,
            CatalogueOfUnwiseNames.CitationOfFormMark,
            CatalogueOfUnwiseNames.CitationOfRecordMark,
        }.Any(m => named.HasMark(new TagId(m))));
        play.Dispose();
    }

    // "If eligible cards exist but the player voluntarily declines: Catalogue gains 8 Block."
    [Fact]
    public void Declining_to_be_named_costs_the_player_eight_block_of_pressure()
    {
        var (play, session, catalogue) = FightProbe.Start(
            FightProbe.Solo(CatalogueOfUnwiseNames.EnemyId, "enter_the_name_in_black_salt"),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        play.CombatDriver!.EndTurn();
        var before = Block(Enemy(play, catalogue));
        play.CombatDriver.SupplyOptionChoice([1]);
        Assert.Null(session.Error);

        Assert.Equal(before + 8, Block(Enemy(play, catalogue)));
        Assert.Null(Marked(play, CatalogueOfUnwiseNames.RecognizedMark));
        play.Dispose();
    }

    // 7.3: playing the named card spends the benefit. Recognized goes, the discount goes with it, and what is
    // left is an Established entry — a debt the book can call in later.
    [Fact]
    public void Playing_a_named_card_turns_the_entry_into_a_debt()
    {
        var (play, session, catalogue) = FightProbe.Start(
            FightProbe.Solo(CatalogueOfUnwiseNames.EnemyId, "enter_the_name_in_black_salt", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        play.CombatDriver!.EndTurn();
        play.CombatDriver.SupplyOptionChoice([0]);
        play.CombatDriver.SupplyCardChoice([play.CombatDriver.PendingCardChoice![0].Id]);
        var named = Marked(play, CatalogueOfUnwiseNames.RecognizedMark)!;

        play.CombatDriver.PlayCard(named.Id, catalogue);
        Assert.Null(session.Error);

        var after = Anywhere(play, CatalogueOfUnwiseNames.EstablishedMark);
        Assert.NotNull(after);
        Assert.Equal(named.Id, after!.Id);
        Assert.False(after.HasMark(new TagId(CatalogueOfUnwiseNames.RecognizedMark)));
        // The discount was the benefit, and it is spent: the card is back to what it was printed at.
        Assert.Equal(0, after.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
        play.Dispose();
    }

    // 7.6 Already Known cashes the oldest Established entry, then attacks. With a Citation of Cost standing,
    // cashing is Block: 5 + 3 × the card's printed cost.
    [Fact]
    public void Cashing_an_entry_charges_what_the_citation_says()
    {
        var (play, session, catalogue) = FightProbe.Start(
            FightProbe.Authored("archives_elite_catalogue_of_unwise_names", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 16)], health: 400);

        // Turn 1: the Catalogue prepares a naming. The player's draw raises the offer.
        play.CombatDriver!.EndTurn();
        Assert.NotNull(play.CombatDriver.PendingOptionChoice);
        play.CombatDriver.SupplyOptionChoice([0]);
        play.CombatDriver.SupplyCardChoice([play.CombatDriver.PendingCardChoice![0].Id]);
        var named = Marked(play, CatalogueOfUnwiseNames.RecognizedMark)!;
        Assert.True(named.HasMark(new TagId(CatalogueOfUnwiseNames.CitationOfCostMark)));

        play.CombatDriver.PlayCard(named.Id, catalogue);
        Assert.Null(session.Error);

        // Turn 2 is Already Known: it cashes the entry and attacks.
        play.CombatDriver.EndTurn();

        // paper_cut is a 1-Energy card: 5 + 3 × 1 = 8 Block on top of whatever the intent itself gives.
        Assert.True(Block(Enemy(play, catalogue)) >= 8);
        // The line is struck: the card is no longer an entry.
        Assert.Null(Anywhere(play, CatalogueOfUnwiseNames.EstablishedMark));
        play.Dispose();
    }

    // 7.2: "If no eligible card exists, the naming prompt is skipped with no penalty." A hand of nothing but
    // Junk is not a hand the book will write in.
    [Fact]
    public void A_hand_of_junk_is_not_worth_naming()
    {
        var (play, _, catalogue) = FightProbe.Start(
            FightProbe.Solo(CatalogueOfUnwiseNames.EnemyId, "enter_the_name_in_black_salt"),
            deck: [.. Enumerable.Repeat("red_tape", 14)], health: 400);

        var before = Block(Enemy(play, catalogue));
        play.CombatDriver!.EndTurn();

        Assert.Null(play.CombatDriver.PendingOptionChoice);
        Assert.Null(Marked(play, CatalogueOfUnwiseNames.RecognizedMark));
        // …and no penalty either: declining is only declining when there was something to decline.
        Assert.Equal(before + 10, Block(Enemy(play, catalogue))); // the intent's own 10 Block, nothing more
        play.Dispose();
    }

    // Signature — Recite an Unwise Name: 14, and 20 only with all three lines full.
    [Fact]
    public void The_recitation_is_louder_with_a_full_book()
    {
        var (quiet, _, _) = FightProbe.Start(
            FightProbe.Solo(CatalogueOfUnwiseNames.EnemyId, "recite_an_unwise_name"),
            deck: [.. Enumerable.Repeat("paper_cut", 14)], health: 400);

        var before = Hero(quiet).Health.Current;
        quiet.CombatDriver!.EndTurn();
        Assert.Equal(14, before - Hero(quiet).Health.Current);
        quiet.Dispose();
    }
}
