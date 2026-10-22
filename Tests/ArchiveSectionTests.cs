using BnbContent.Converter;

namespace BnbContent.Tests;

// Where the archive shelves a card or a relic (user, 2026-10-03): the game files say it, each entry's
// presentation carries it, and a card no pool offers says who puts it in a hand.
public class ArchiveSectionTests
{
    private static readonly RogueDeck.Run.RunBlueprint Game = FightProbe.Game;

    private static string CardShelf(string id) => Game.Presentation.Cards[id].Extra[ArchiveSections.SectionKey];

    [Fact]
    public void Every_card_and_relic_names_its_shelf()
    {
        Assert.All(Game.Cards, c => Assert.True(
            Game.Presentation.Cards.TryGetValue(c.Id, out var look) && look.Extra.ContainsKey(ArchiveSections.SectionKey),
            c.Id));
        Assert.All(Game.Relics, r => Assert.True(
            Game.Presentation.Relics.TryGetValue(r.Id, out var look) && look.Extra.ContainsKey(ArchiveSections.SectionKey),
            r.Id));
    }

    [Fact]
    public void Owned_cards_are_shelved_by_whose_they_are_and_forced_ones_by_who_forces_them()
    {
        Assert.Equal(ArchiveSections.Bureaucrat, CardShelf("paper_cut"));          // a starter
        Assert.Equal(ArchiveSections.Bureaucrat, CardShelf("inkblot_verdict+"));   // an upgrade sits with its card
        Assert.Equal(ArchiveSections.GeneralPool, CardShelf("dawn_summons"));
        Assert.Equal(ArchiveSections.Junk, CardShelf("red_tape"));
        Assert.Equal(ArchiveSections.Junk, CardShelf("missing_signature"));        // only an event leaves it
        Assert.Equal(ArchiveSections.RelicCards, CardShelf("honey_spoon_action"));
        Assert.Equal(ArchiveSections.Fight, CardShelf("petition_for_priority"));
        Assert.Contains("The Queue Commissioner",
            Game.Presentation.Cards["petition_for_priority"].Extra[ArchiveSections.MadeByKey]);
    }

    [Fact]
    public void Relics_are_shelved_by_pool_and_the_bureaucrats_own_apart()
    {
        var shelves = Game.Presentation.Relics.Values.Select(r => r.Extra[ArchiveSections.SectionKey]).ToHashSet();
        Assert.Superset(new HashSet<string> { "Bureaucrat", "Normal", "Shop", "Event", "Elite", "Mimic", "Boss" }, shelves);
        Assert.Equal("Normal", Game.Presentation.Relics["levy_stamp"].Extra[ArchiveSections.SectionKey]);
        Assert.Equal("Bureaucrat", Game.Presentation.Relics["formkeepers_signet"].Extra[ArchiveSections.SectionKey]);
    }

    // The Apprentice's cards and relics on a shelf of their own; his five wand actions with the other actions; and
    // every word of his in the Compendium.
    [Fact]
    public void The_apprentice_has_a_shelf_of_his_own()
    {
        Assert.Equal(ArchiveSections.Apprentice, CardShelf("ap_candle_stub"));
        Assert.Equal(ArchiveSections.Apprentice, CardShelf("ap_last_word+"));
        Assert.Equal(ArchiveSections.Actions, CardShelf(Converter.Apprentice.ApprenticeWand.Cast));
        Assert.Equal(ArchiveSections.Apprentice, Game.Presentation.Relics["ar_brass_dividers"].Extra[ArchiveSections.SectionKey]);
        var words = Game.Presentation.Game!.Extra;
        foreach (var entry in Compendium.ApprenticeEntries)
            Assert.Equal(entry.Name, words[$"compendium:{entry.Id}"]);
    }

    // Her cards and relics on a shelf of their own, and her words in the Compendium.
    [Fact]
    public void The_lieutenant_has_a_shelf_of_her_own()
    {
        Assert.Equal(ArchiveSections.Lieutenant, CardShelf("lt_watch_cut"));
        Assert.Equal(ArchiveSections.Lieutenant, CardShelf("lt_field_pardon+"));
        Assert.Equal(ArchiveSections.Lieutenant, Game.Presentation.Relics["lt_relic_commission"].Extra[ArchiveSections.SectionKey]);
        var words = Game.Presentation.Game!.Extra;
        foreach (var entry in Compendium.LieutenantEntries)
            Assert.Equal(entry.Name, words[$"compendium:{entry.Id}"]);
    }
}
