using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act II — the Necrology Vaults. A death needs paperwork: kill the Blank Death Certificate while its citation
// is still unanswered and the death does not take.
//
// ⚠⚠ THE CLAUSE STANDS BEFORE THE DEATH, IT DOES NOT UNDO ONE, and the Certificate is why that distinction is
// not academic: its only encounter is a SOLO, and the combat's outcome is decided the moment no enemy is
// living — so a revive-after-down could never have fired in the one fight it exists in.
//
// ⚠ AND WHAT IS MEASURED IS THE MOMENT, NOT THE END OF THE FIGHT. The return is a ONE-SHOT: it puts the body
// back on 35 and is spent doing it, so a probe that keeps swinging kills it for good a few cards later and
// reports that the clause did nothing. Both tests below therefore watch for the body STANDING at 35 — the
// only observable the rule actually makes.
public class ActTwoStageNineTests
{
    private static CombatantState? Body(RunPlayback play, CombatantId id) =>
        play.CombatDriver?.Current is null ? null : play.CombatDriver.Current.State.GetCombatant(id);

    // Beat on the Certificate for a few turns and report whether it was ever seen alive on 35 HP.
    // `answerTheCitation` plays the cited card first each turn, which is what certifies the death.
    private static bool SawTheReturn(bool answerTheCitation)
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("blank_death_certificate", "serve_certificate", energy: 40),
            deck: [.. Enumerable.Repeat("paper_cut", 30)],
            health: 999);

        var returned = false;
        for (var turn = 0; turn < 8 && play.CombatDriver!.Current is not null; turn++)
        {
            if (answerTheCitation && play.CombatDriver.Current!.Hand
                    .FirstOrDefault(c => c.HasMark(new TagId(ActTwo.CertificateReferenceMark))) is { } cited)
            {
                play.CombatDriver.PlayCard(cited.Id, enemyId);
                Assert.Null(session.Error);
            }

            // ⚠ NOT THE CITED CARD when the probe is meant not to answer — playing the whole hand answers the
            // citation by accident, which is how this test first reported the clause as broken.
            foreach (var card in play.CombatDriver.Current?.Hand.ToList() ?? [])
            {
                if (play.CombatDriver.Current is null)
                    break;
                if (!answerTheCitation && card.HasMark(new TagId(ActTwo.CertificateReferenceMark)))
                    continue;
                play.CombatDriver.PlayCard(card.Id, enemyId);
                if (Body(play, enemyId) is { IsAlive: true, Health.Current: 35 })
                    returned = true;
            }
            Assert.Null(session.Error);
            if (play.CombatDriver.Current is null)
                break;
            play.CombatDriver.EndTurn();
        }

        play.Dispose();
        return returned;
    }

    // ⚠ AND IT MUST STILL BE KILLABLE. A prevention that came back every round would make the body immortal
    // and any walker that fights the whole roster loop for ever — which is exactly what a hung suite looks
    // like. The return is ONE-SHOT: what it applies on the way out is the flag that stops the turn-start rule
    // re-arming it, and this is the test that the flag actually lands.
    [Fact]
    public void The_return_happens_once_and_then_the_death_stands()
    {
        var (play, session, enemyId) = FightProbe.Start(
            FightProbe.Solo("blank_death_certificate", "serve_certificate", energy: 40),
            deck: [.. Enumerable.Repeat("paper_cut", 30)],
            health: 9999);

        for (var turn = 0; turn < 20 && play.CombatDriver!.Current is not null; turn++)
        {
            foreach (var card in play.CombatDriver.Current?.Hand.ToList() ?? [])
            {
                if (play.CombatDriver.Current is null)
                    break;
                if (card.HasMark(new TagId(ActTwo.CertificateReferenceMark)))
                    continue;                      // never answer: the clause stays armed
                play.CombatDriver.PlayCard(card.Id, enemyId);
            }
            Assert.Null(session.Error);
            if (play.CombatDriver.Current is null)
                break;
            play.CombatDriver.EndTurn();
        }

        var over = play.CombatDriver?.Current is null;
        var left = over ? 0 : play.CombatDriver!.Current!.State.GetCombatant(enemyId).Health.Current;
        play.Dispose();
        Assert.True(over, $"twenty turns of unanswered citations and it is still standing on {left}");
    }

    // "Otherwise: the Certificate returns once at roughly 35% HP." 35 of its 100.
    [Fact]
    public void An_uncertified_death_does_not_take()
    {
        Assert.True(SawTheReturn(answerTheCitation: false),
            "an unanswered citation should have put the Certificate back on 35");
    }

    // "If the player fulfilled a Reference from this enemy during the same turn: death is certified and final."
    [Fact]
    public void Answering_the_citation_certifies_the_death()
    {
        Assert.False(SawTheReturn(answerTheCitation: true),
            "a citation answered this turn should let the death stand");
    }

    // ── the Spare-Life Jar ────────────────────────────────────────────────────────────────────────────────

    private static CombatantId Find(RunPlayback play, string definitionId) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value == definitionId).Id;

    // Hit `at` card by card and report whether it was ever seen ALIVE on exactly `caughtAt` HP.
    //
    // ⚠ THE MOMENT, NOT THE END OF THE FIGHT — the same trap the Certificate's tests document. A caught body
    // is left on a few HP and the catch is SPENT, so a probe that keeps swinging kills it two cards later and
    // reports that nothing caught it.
    private static bool Caught(
        RunPlayback play, InteractiveRunSession session, CombatantId at, int caughtAt, int turns)
    {
        for (var turn = 0; turn < turns && play.CombatDriver!.Current is not null; turn++)
        {
            foreach (var card in play.CombatDriver.Current?.Hand.ToList() ?? [])
            {
                if (play.CombatDriver.Current is null)
                    break;
                play.CombatDriver.PlayCard(card.Id, at);
                if (play.CombatDriver.Current?.State.Combatants
                        .FirstOrDefault(c => c.Id == at) is { IsAlive: true } body
                    && body.Health.Current == caughtAt)
                    return true;
            }
            Assert.Null(session.Error);
            if (play.CombatDriver.Current is null)
                break;
            play.CombatDriver.EndTurn();
        }
        return false;
    }

    // Beat a body down until it is gone, without watching for a catch.
    private static void Kill(RunPlayback play, InteractiveRunSession session, CombatantId at, int turns)
    {
        for (var turn = 0; turn < turns && play.CombatDriver!.Current is not null; turn++)
        {
            foreach (var card in play.CombatDriver.Current?.Hand.ToList() ?? [])
            {
                if (play.CombatDriver.Current is null)
                    break;
                play.CombatDriver.PlayCard(card.Id, at);
            }
            Assert.Null(session.Error);
            if (play.CombatDriver.Current is null)
                break;
            play.CombatDriver.EndTurn();
        }
    }

    // "Return the stored enemy once at 30% Max HP" — moved to the moment the blow lands, so the Ouroboros
    // (35 HP) simply does not die and is left on 11.
    [Fact]
    public void While_the_jar_stands_the_body_beside_it_has_a_life_in_reserve()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("archives_necrology_duo_01", energy: 40),
            deck: [.. Enumerable.Repeat("paper_cut", 30)],
            health: 9999);

        var ouroboros = Find(play, "dead_letter_ouroboros");
        var caught = Caught(play, session, ouroboros, caughtAt: 11, turns: 10);  // 30 % of its 35

        play.Dispose();
        Assert.True(caught, "the jar should have caught it and left it on 11");
    }

    // ⚠⚠ A PIN, NOT A BLESSING. "If the Jar dies before resolution, Stored Life is lost" is the half of this
    // mechanic the engine cannot yet express: a Downed rule on the Jar cannot reach its allies, because in a
    // Downed program the Source is the downed body and every ally selector wants a living one (see
    // ActTwo.StoredLife). So today the spare life survives the jar, and this test says so OUT LOUD rather
    // than leaving the gap to be discovered in a playtest. The day the engine grows a living-agnostic ally
    // selector, this test fails — and that failure is the reminder to finish the job.
    [Fact]
    public void Breaking_the_jar_first_does_not_yet_lose_what_it_was_holding()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Authored("archives_necrology_duo_01", energy: 40),
            deck: [.. Enumerable.Repeat("paper_cut", 30)],
            health: 9999);

        Kill(play, session, Find(play, "spare_life_jar"), turns: 12);   // the jar goes first
        Assert.NotNull(play.CombatDriver!.Current);                     // the Ouroboros is still standing
        Assert.DoesNotContain(
            play.CombatDriver.Current!.State.Combatants,
            c => c.DefinitionId.value == "spare_life_jar" && c.IsAlive);

        var ouroboros = Find(play, "dead_letter_ouroboros");
        var caught = Caught(play, session, ouroboros, caughtAt: 11, turns: 10);

        play.Dispose();
        Assert.True(caught,
            "PIN: the spare life still outlives the jar — see ActTwo.StoredLife for the missing seam");
    }
}
