using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — the Hall of Concordances, and the two bodies that were waiting on the ENGINE rather than on a
// reading. Both are written about moments that are CONCLUSIONS another rule reached — a Reference being
// fulfilled, a Delinquency collecting, a Source's signature actually firing — and none of those was an event
// anything could hear until a rule could say so out loud (`node.announceRule`).
public class ActTwoStageTenTests
{
    private static CombatantState Body(RunPlayback play, string definitionId) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.DefinitionId.value == definitionId);

    private static int Residue(RunPlayback play) =>
        play.CombatDriver!.Current!.State
            .GetCombatant(play.CombatDriver.Current!.HeroId)
            .GetCounter(new CounterId("residue"));

    // ★ THE INDEX HEARS A REFERENCE BEING FULFILLED. Its own Redacted source it could always see; this is one
    // of the three it could not, and the only thing that changed is that the citer now says so.
    [Fact]
    public void A_fulfilled_reference_settles_as_residue()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("concordance", 40,
                ("miscellany_index", "index_everything", 200),
                ("orphan_citation", "missing_source", 200)),
            deck: [.. Enumerable.Repeat("paper_cut", 12)],
            health: 999);

        var cited = play.CombatDriver!.Current!.Hand
            .FirstOrDefault(c => c.HasMark(new TagId(ActTwo.CitationReferenceMark)));
        Assert.NotNull(cited);                                  // the citation was served
        Assert.Equal(0, Residue(play));

        play.CombatDriver.PlayCard(cited.Id, Body(play, "orphan_citation").Id);
        Assert.Null(session.Error);

        Assert.Equal(1, Residue(play));                        // …and the Index wrote it down
        play.Dispose();
    }

    // "The FIRST time each round each of the following occurs" — a second fulfilment in the same round does
    // not settle twice. Without the per-source latch the Index would fill on one busy turn.
    [Fact]
    public void One_source_settles_once_a_round()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("concordance", 40,
                ("miscellany_index", "index_everything", 200),
                ("orphan_citation", "missing_source", 200)),
            deck: [.. Enumerable.Repeat("paper_cut", 12)],
            health: 999);

        var citation = Body(play, "orphan_citation").Id;
        foreach (var card in play.CombatDriver!.Current!.Hand.ToList())
        {
            if (play.CombatDriver.Current is null)
                break;
            play.CombatDriver.PlayCard(card.Id, citation);
        }
        Assert.Null(session.Error);

        Assert.Equal(1, Residue(play));
        play.Dispose();
    }

    // ★ THE FOOTNOTE WRITES DOWN WHAT ITS SOURCE DOES. Encounter 34's own words: "Footnote begins linked to
    // Orphan Citation. `Reconstruct the Source` can feed its Notes through the normal Source rule."
    [Fact]
    public void The_footnote_gains_a_note_when_its_source_speaks()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("archives_concordance_duo_01", energy: 40),
            deck: [.. Enumerable.Repeat("paper_cut", 12)],
            health: 999);

        var footnote = Body(play, "detached_footnote");
        Assert.Equal(0, footnote.GetCounter(ActTwo.NotesCounter));

        var cited = play.CombatDriver!.Current!.Hand
            .FirstOrDefault(c => c.HasMark(new TagId(ActTwo.CitationReferenceMark)));
        Assert.NotNull(cited);
        play.CombatDriver.PlayCard(cited.Id, Body(play, "orphan_citation").Id);
        Assert.Null(session.Error);

        Assert.Equal(1, Body(play, "detached_footnote").GetCounter(ActTwo.NotesCounter));
        play.Dispose();
    }

    // "Maximum 2 Notes", and "the first time each ROUND" — so a second fulfilment in the same round writes
    // nothing, and the note only comes once the round has turned.
    [Fact]
    public void One_note_a_round_and_never_more_than_two()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("archives_concordance_duo_01", energy: 40),
            deck: [.. Enumerable.Repeat("paper_cut", 12)],
            health: 999);

        var citation = Body(play, "orphan_citation").Id;

        for (var round = 0; round < 5 && play.CombatDriver!.Current is not null; round++)
        {
            foreach (var card in play.CombatDriver.Current?.Hand.ToList() ?? [])
            {
                if (play.CombatDriver.Current is null)
                    break;
                play.CombatDriver.PlayCard(card.Id, citation);
            }
            Assert.Null(session.Error);
            if (play.CombatDriver.Current is null)
                break;
            Assert.True(
                Body(play, "detached_footnote").GetCounter(ActTwo.NotesCounter) <= ActTwo.NotesFull,
                "the margin holds two notes and no more");
            play.CombatDriver.EndTurn();
        }
        play.Dispose();
    }
}
