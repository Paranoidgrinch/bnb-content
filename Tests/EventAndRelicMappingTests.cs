using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// Conversion gate for events and relics against the real source data: everything converts (fail-loud
// mappers), and hand-checked spot values pin the ported semantics.
public class EventAndRelicMappingTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly IReadOnlyList<MappedRelic> Relics = Data.Relics.Select(RelicMapper.Map).ToList();
    private static readonly ConversionPools Pools = ConversionPools.Build(Data, Relics);

    [Fact]
    public void Every_relic_converts()
    {
        Assert.Equal(Data.Relics.Count, Relics.Count);
        Assert.Equal(Relics.Count, Relics.Select(r => r.Relic.Id).Distinct().Count());
    }

    [Fact]
    public void Every_event_converts()
    {
        foreach (var babEvent in Data.Events)
        {
            var script = EventMapper.Map(babEvent, Pools);
            Assert.Equal("start", script.StartSituationId);
            // Every result situation a choice points at exists.
            var situationIds = script.Situations.Values.Select(s => s.Id).ToHashSet();
            foreach (var choice in script.Situations.Values.SelectMany(s => s.Choices))
                if (choice.NextSituationId is { } next)
                    Assert.Contains(next, situationIds);
        }
    }

    [Fact]
    public void Spot_check_combat_start_relic_installs_an_opening_per_combat()
    {
        var stamp = Relics.First(r => r.Relic.Id == "self_inking_stamp"); // apply 2 paperwork to all enemies
        Assert.Empty(stamp.PickupEffects);
        var program = Assert.Single(stamp.Relic.RunPrograms);
        Assert.Equal(typeof(NodeEnteredRunEvent), program.EventType);
    }

    [Fact]
    public void Spot_check_pickup_relics_bundle_their_effects()
    {
        var mug = Relics.First(r => r.Relic.Id == "certified_tea_mug"); // heal 10 on pickup
        var heal = Assert.IsType<HealRunEffect>(Assert.Single(mug.PickupEffects));
        Assert.Equal(10, heal.Amount);

        var manual = Relics.First(r => r.Relic.Id == "pocket_sized_procedure_manual"); // +1 max energy
        var counter = Assert.IsType<IncrementCounterRunEffect>(Assert.Single(manual.PickupEffects));
        Assert.StartsWith(StandardRunIds.ResourceMaxCounterPrefix, counter.Counter.Value);
        Assert.Equal(1, counter.Delta);

        // The relic offer used by every grant site carries the relic AND its pickup bundle.
        var offer = ConversionPools.RelicOffer(mug);
        Assert.Equal(2, offer.Grant.Count);
        Assert.IsType<AddRelicByIdRunEffect>(offer.Grant[0]);
    }

    [Fact]
    public void Spot_check_misfiling_cabinet_transforms_and_adds_the_notice()
    {
        var babEvent = Data.Events.First(e => e.Id == "misfiling_cabinet");
        var script = EventMapper.Map(babEvent, Pools);
        var start = script.Situations.Values.First(s => s.Id == "start");
        var choice = start.Choices.First(c => c.Id == "let_it_misfile_something_important");
        Assert.Collection(choice.Effects,
            e => Assert.IsType<TransformCardsRunEffect>(e),
            e => Assert.Equal("duplicate_copy", Assert.IsType<AddCardToDeckRunEffect>(e).Card.value));
        Assert.NotNull(choice.NextSituationId); // the result text is readable before the event ends
    }

    [Fact]
    public void Event_relic_pool_excludes_boss_and_foreign_class_relics()
    {
        foreach (var relic in Pools.Relics)
        {
            Assert.NotEqual("boss", relic.Source.Rarity);
            if (relic.Source.AllowedClasses is { } allowed)
                Assert.Contains("bureaucrat", allowed);
        }
    }
}
