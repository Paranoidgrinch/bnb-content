using RogueDeck.Run;
using RogueDeck.Sandbox.Run;
using RogueDeck.Core.Combat;

namespace BnbContent.Tests;

// The Filing-Fee Stamp read the corpse through the event-target selector, which only answers the living: every
// corpse was 0 Paperwork deep and the stamp never paid (audit 2026-11-19). A rat buried under 9-Paperwork Forms dies
// 10+ deep, which is 6 + 4 Gold.
public class FilingFeeStampProbe
{
    [Fact]
    public void A_corpse_ten_deep_in_paperwork_pays_ten_gold()
    {
        var (play, session, rat) = FightProbe.Start(
            FightProbe.Solo(EventStory.Rat, EventStory.RatIntent),
            deck: ["form_of_ill_intent+", "form_of_ill_intent+", "form_of_ill_intent+", "form_of_ill_intent+", "form_of_ill_intent+"],
            character: "bureaucrat", relics: ["filing_fee_stamp"]);
        var before = session.Run.GetResource(StandardRunIds.Gold);

        for (var turn = 0; turn < 6 && play.CombatDriver!.Current is not null; turn++)
        {
            foreach (var card in play.CombatDriver.Current?.Hand.ToList() ?? [])
            {
                if (play.CombatDriver.Current is null)
                    break;
                play.CombatDriver.PlayCard(card.Id, rat);
            }
            if (play.CombatDriver.Current is null)
                break;
            play.CombatDriver.EndTurn();
        }
        while (session.IsAwaitingEntities)
            session.PickEntities([]);
        Assert.Null(session.Error);
        // The probe's ring pays no spoils of its own, so every coin is the stamp's.
        Assert.Equal(before + 10, session.Run.GetResource(StandardRunIds.Gold));
    }
}
