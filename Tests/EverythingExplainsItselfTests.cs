using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// Every name the player can be shown owes them an explanation.
//
// A status reaches the screen as a chip with its authored NAME on it — "Still in Force", "Gate: Half-Raised",
// "Appointment Due (Second)" — and a name alone is a riddle. So does a card in the hand, a relic in the
// sidebar, a thing on a shop shelf. The frontend puts the text on the hover; where there is none, hovering
// says the name back at you.
//
// Nothing else in the pipeline notices — a status with no description is perfectly valid data, and a card
// whose rules text never reached the presentation manifest still plays correctly — so this is where it gets
// noticed. It found 82 mute statuses, 113 mute relics and 31 mute cards the first time it ran.
public class EverythingExplainsItselfTests
{
    private static readonly RunBlueprint Game =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260828);

    [Fact]
    public void Every_status_says_what_it_does()
    {
        var mute = Game.Statuses
            .Where(status => string.IsNullOrWhiteSpace(status.DescriptionKey))
            .Select(status => $"{status.Id} ('{status.NameKey}')")
            .ToList();

        Assert.True(mute.Count == 0,
            $"{mute.Count} status(es) reach the player with a name and no explanation: "
            + string.Join(", ", mute.Take(20)));
    }

    // …and it must be an explanation, not the name again: a description that only repeats what the chip
    // already says teaches nothing.
    [Fact]
    public void No_status_description_merely_repeats_its_name()
    {
        var echoes = Game.Statuses
            .Where(status => !string.IsNullOrWhiteSpace(status.DescriptionKey)
                && string.Equals(status.DescriptionKey!.Trim(), status.NameKey?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            .Select(status => status.Id)
            .ToList();

        Assert.Empty(echoes);
    }

    // A card explains itself through the presentation manifest, which is where this game keeps its rules text.
    // The cards that used to fall through were the ones a fight HANDS you — a Notice, a Clause, a Fragment —
    // which are exactly the cards nobody has seen before.
    [Fact]
    public void Every_card_the_document_ships_carries_its_rules_text()
    {
        var mute = Game.Cards
            .Where(card => string.IsNullOrWhiteSpace(
                Game.Presentation.Cards.GetValueOrDefault(card.Id)?.FlavorText))
            .Select(card => $"{card.Id} ('{card.NameKey}')")
            .ToList();

        Assert.True(mute.Count == 0,
            $"{mute.Count} card(s) reach the hand with a name and no rules text: "
            + string.Join(", ", mute.Take(20)));
    }

    [Fact]
    public void Every_relic_the_document_ships_says_what_it_does()
    {
        var mute = Game.Relics
            .Where(relic => string.IsNullOrWhiteSpace(
                Game.Presentation.Relics.GetValueOrDefault(relic.Id)?.FlavorText))
            .Select(relic => $"{relic.Id} ('{relic.DisplayName}')")
            .ToList();

        Assert.True(mute.Count == 0,
            $"{mute.Count} relic(s) are worn with a name and no explanation: "
            + string.Join(", ", mute.Take(20)));
    }
}
