using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Conversion gate for the card + status tables, run against the REAL source data: every bureaucrat
// card must convert (the mapper throws on anything unmapped), ids follow the engine's "+" upgrade
// convention, and the ported rules semantics (exhaust-on-play, unplayable, effect programs) match
// hand-checked spot values from the original JSON.
public class CardAndStatusMappingTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    [Fact]
    public void Every_bureaucrat_card_converts()
    {
        var cards = Data.Cards.Select(CardMapper.Map).ToList();
        Assert.Equal(Data.Cards.Count, cards.Count);
        Assert.Equal(cards.Count, cards.Select(c => c.Id).Distinct().Count());
    }

    [Fact]
    public void Upgraded_ids_follow_the_engine_plus_convention()
    {
        var ids = Data.Cards.Select(CardMapper.Map).Select(c => c.Id).ToHashSet();
        Assert.Contains("paper_cut", ids);
        Assert.Contains("paper_cut+", ids);
        Assert.DoesNotContain(ids, id => id.EndsWith("_plus", StringComparison.Ordinal));

        // Every base card's upgrades_to still resolves after renaming.
        foreach (var card in Data.Cards.Where(c => c.UpgradesTo is not null))
            Assert.Contains(CardMapper.MapCardId(card.UpgradesTo!), ids);
    }

    [Fact]
    public void Spot_check_paper_cut_deals_six_damage_for_one_energy()
    {
        var card = CardMapper.Map(Data.Cards.First(c => c.Id == "paper_cut"));
        var cost = Assert.Single(card.Costs);
        Assert.Equal(StandardCombatIds.EnergyResource, cost.ResourceId);
        Assert.Equal(1, cost.Amount);

        var model = CombatProgramModel.Classify(card.Program)!;
        Assert.Equal("dealDamage", model.Kind);
        Assert.Equal("eventTarget", model.SelectorKey);
        Assert.Equal(6, model.AmountOrDefault.Const);
    }

    [Fact]
    public void Spot_check_damage_per_status_reads_paperwork_stacks_on_the_target()
    {
        var card = CardMapper.Map(Data.Cards.First(c => c.Id == "compounded_penalty"));
        var model = CombatProgramModel.Classify(card.Program)!;
        Assert.Equal("dealDamage", model.Kind);
        var amount = model.AmountOrDefault;
        Assert.Equal("mul", amount.Kind);
        Assert.Equal("statusStacks", amount.LeftOrDefault.Kind);
        Assert.Equal("paperwork", amount.LeftOrDefault.ReadId);
        Assert.Equal("eventTarget", amount.LeftOrDefault.SelectorKey);
        Assert.Equal(4, amount.RightOrDefault.Const);
    }

    [Fact]
    public void Spot_check_junk_curses_are_unplayable_or_exhaust_when_played()
    {
        var redTape = CardMapper.Map(Data.Cards.First(c => c.Id == "red_tape"));
        Assert.Contains(StandardCombatIds.UnplayableTag, redTape.Tags);

        var duplicate = CardMapper.Map(Data.Cards.First(c => c.Id == "duplicate_copy"));
        Assert.Equal(CardZone.ExhaustPile, duplicate.PlayedCardDestinationZone);
    }

    [Fact]
    public void Spot_check_exhaust_by_tag_becomes_a_tag_filtered_zone_walk()
    {
        var card = CardMapper.Map(Data.Cards.First(c => c.Id == "archive_the_evidence"));
        var model = CombatProgramModel.Classify(card.Program)!;
        var walk = model.Kind == "forEachCardInZone"
            ? model
            : model.ChildrenOrEmpty.Single(child => child.Kind == "forEachCardInZone");
        Assert.Equal("junk", walk.ToTag);
        Assert.Equal(1, walk.TakeFirst);
        Assert.Equal(CardZone.ExhaustPile, walk.ChildrenOrEmpty.Single().ToZone);
    }

    [Fact]
    public void All_six_statuses_convert_with_their_ported_semantics()
    {
        var statuses = StatusMapper.Map("statuses", Data.Statuses);
        Assert.Equal(6, statuses.Count);

        var paperwork = statuses.First(s => s.Id == "paperwork");
        Assert.Contains(StandardCombatIds.DamageOverTimeTag.value, paperwork.Tags);
        Assert.Empty(paperwork.Triggers); // no decay

        var poison = statuses.First(s => s.Id == "poison");
        Assert.Contains(StandardCombatIds.DamageOverTimeTag.value, poison.Tags);
        Assert.Single(poison.Triggers); // decays one stack at the bearer's turn end

        var doubt = statuses.First(s => s.Id == "doubt");
        var doubtPassive = Assert.Single(doubt.PassiveModifiers);
        Assert.Equal(PassiveModifierOperation.ScalePercent, doubtPassive.Operation);
        Assert.Equal(75, doubtPassive.Magnitude);
        Assert.Equal(DamageKind.Direct, doubtPassive.RestrictDamageKind);

        var panic = statuses.First(s => s.Id == "panic");
        var panicPassive = Assert.Single(panic.PassiveModifiers);
        Assert.Equal(PassiveModifierPipeline.TurnStartDraw, panicPassive.Pipeline);
        Assert.Equal(-1, panicPassive.Magnitude);
    }
}
