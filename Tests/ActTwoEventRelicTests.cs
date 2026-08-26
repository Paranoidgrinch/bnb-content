using BnbContent.Converter;
using BnbContent.Converter.Events;
using RogueDeck.Core.Combat;

namespace BnbContent.Tests;

// The five Act-II Event relics, each taken at a real door and then played with. They are all about ONE card —
// the one you did not get to play, the one you kept back, the one that will not stay filed — so every test
// follows that card through the fight.
public class ActTwoEventRelicTests
{
    private static readonly string[] Deck =
        ["paper_cut", "paper_cut", "paper_cut", "cower_behind_a_desk", "cower_behind_a_desk",
         "cower_behind_a_desk", "permit_a38", "permit_a38"];

    // A card the relic hands back is the one in hand carrying a price nothing else writes — which is also the
    // only thing that distinguishes it from a card the deck simply dealt again.
    private static CardInstance? Discounted(ArchiveProbe probe) =>
        probe.Zone(CardZone.Hand).FirstOrDefault(
            c => c.MarkCounters.TryGetValue(StandardCombatIds.CardCostDeltaCounter, out var d) && d < 0);

    [Fact]
    public void The_library_card_hands_back_what_you_never_got_to_play_and_asks_nothing_for_it()
    {
        // Nothing is played, so everything in the opening hand is a card that was only held.
        using var probe = ArchiveProbe.Enter(Deck, [], [], relics: ["unreturned_library_card"]);
        probe.EndTurn();

        var returned = Discounted(probe);
        Assert.True(returned is not null, "a card left in hand should have come back");
        Assert.Equal(0, probe.CostOf(returned!));

        // Spend it — the price is written on the copy and the play consumes it — and the card is an ordinary
        // card again. Once each fight: nothing else is handed back.
        probe.Play_(returned!);
        probe.EndTurn();
        Assert.Null(Discounted(probe));
    }

    [Fact]
    public void The_shelf_label_makes_the_card_you_put_down_easier_when_it_comes_round_again()
    {
        // A small hand off a small deck, so what is put down comes round again within the fight.
        using var probe = ArchiveProbe.Enter(
            ["paper_cut", "paper_cut", "cower_behind_a_desk", "permit_a38"], [], [],
            relics: ["reversible_shelf_label"], drawnPerTurn: 2);

        CardInstance? labelled = null;
        for (var turn = 0; turn < 4 && labelled is null; turn++)
        {
            probe.EndTurn();
            labelled = Discounted(probe);
        }

        Assert.True(labelled is not null, "the labelled card should come back cheaper");
        Assert.Equal(ArchiveProbe.Printed(labelled!.DefinitionId) - 1, probe.CostOf(labelled));
    }

    [Fact]
    public void The_blank_cameo_keeps_one_card_cheap_and_unmarkable()
    {
        using var probe = ArchiveProbe.Enter(Deck, [], [], relics: ["blank_cameo"]);

        var cameo = probe.Zone(CardZone.Hand)
            .Single(c => c.Marks.Contains(new TagId("blank_cameo_card")));
        Assert.Contains(StandardCombatIds.RetainedCardMark, cameo.Marks);
        Assert.Equal(ArchiveProbe.Printed(cameo.DefinitionId) - 1, probe.CostOf(cameo));

        // The archive writes on it anyway; by the next round the portrait is blank again.
        cameo.AddMark(new TagId(ActTwo.RedactedMark));
        probe.EndTurn();
        Assert.DoesNotContain(new TagId(ActTwo.RedactedMark),
            probe.Zone(CardZone.Hand).First(c => c.Id == cameo.Id).Marks);
    }

    [Fact]
    public void The_vow_bead_pays_for_a_turn_of_exactly_three()
    {
        using var probe = ArchiveProbe.Enter(Deck, [], [], energy: 5, relics: ["vow_bead"]);

        foreach (var card in probe.Zone(CardZone.Hand).Take(3).ToList())
            probe.Play_(card);
        probe.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(probe.Hero, "held_energy"));
    }

    [Fact]
    public void A_turn_of_four_is_not_a_kept_vow()
    {
        using var probe = ArchiveProbe.Enter(Deck, [], [], energy: 5, relics: ["vow_bead"]);

        foreach (var card in probe.Zone(CardZone.Hand).Take(4).ToList())
            probe.Play_(card);
        probe.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(probe.Hero, "held_energy"));
    }

    [Fact]
    public void The_inverted_sealstone_sends_one_card_home_after_its_first_play()
    {
        using var probe = ArchiveProbe.Enter(Deck, [], [], energy: 5, relics: ["inverted_sealstone"]);

        var sealedCard = probe.Zone(CardZone.Hand).Single(c => c.Marks.Contains(new TagId("inverted_seal")));
        probe.Play_(sealedCard);
        Assert.DoesNotContain(probe.Zone(CardZone.Hand), c => c.Id == sealedCard.Id);

        probe.EndTurn();
        Assert.Contains(probe.Zone(CardZone.Hand), c => c.Id == sealedCard.Id);
        // …once. The second play is an ordinary one.
        probe.Play_(probe.Zone(CardZone.Hand).First(c => c.Id == sealedCard.Id));
        probe.EndTurn();
        Assert.DoesNotContain(probe.Zone(CardZone.Hand), c => c.Id == sealedCard.Id);
    }
}
