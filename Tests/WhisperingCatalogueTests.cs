using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-II boss that describes what the player is becoming. It files a Turn Record at the end of every
// player turn, says out loud what you will do again, and turns a habit confirmed three times into a standing
// rule about you. These tests walk that ladder: record → prediction → Authority → Established Entry, then the
// two ways out — contradicting it into suspension, and the phase change that makes it predict you twice.
public class WhisperingCatalogueTests
{
    private const string Deed = "paper_cut";        // an action, so the Opening is filed as a Deed
    private const string Working = "strong_binder"; // a form, so the Opening is filed as a Working

    private static CombatantState Book(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static bool Has(RunPlayback play, CombatantId id, string status) =>
        Book(play, id).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    private static int Stacks(RunPlayback play, CombatantId id, string status) =>
        FightProbe.StacksOf(Book(play, id), status);

    private static readonly string[] Entries =
    [
        WhisperingCatalogue.EntryBusyId, WhisperingCatalogue.EntrySparseId,
        WhisperingCatalogue.EntryOpeningId, WhisperingCatalogue.EntryComplianceId,
        WhisperingCatalogue.EntryDamagedId,
    ];

    // How many cards in hand the Catalogue currently has its finger on.
    private static int Cited(RunPlayback play) =>
        play.CombatDriver!.Current!.Hand.Count(c =>
            c.Marks.Contains(new TagId(WhisperingCatalogue.CatalogueReferenceMark)));

    private static int Guard(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static string? SpokenPrediction(RunPlayback play, CombatantId id) =>
        WhisperingCatalogue.Predictions.FirstOrDefault(p => Has(play, id, p));

    // The Catalogue at its authored 258 HP, with a deck the test can spend deliberately: Deeds and Workings in
    // equal number, so the same hand can open either way and reach a Busy or a Sparse tempo at will.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Fight(
        string intent = "open_a_new_entry", int? bossHealth = null)
    {
        var probe = bossHealth is { } hp
            ? FightProbe.Roster("whispering_catalogue", energy: 9,
                (WhisperingCatalogue.EnemyId, intent, hp))
            : FightProbe.Solo(WhisperingCatalogue.EnemyId, intent, 9);

        return FightProbe.Start(probe,
            deck: [.. Enumerable.Repeat(Deed, 12), .. Enumerable.Repeat(Working, 12)],
            health: 600);
    }

    // Play `count` cards, opening with the given type. Returns how many were actually played — a hand that
    // cannot supply the requested opening is a test bug, not a boss bug, so it is asserted on.
    private static void Spend(RunPlayback play, CombatantId at, string openWith, int count)
    {
        var combat = play.CombatDriver!.Current!;
        var opener = combat.Hand.FirstOrDefault(c => c.DefinitionId.value == openWith);
        Assert.True(opener is not null, $"the probe hand held no {openWith}");
        play.CombatDriver.PlayCard(opener!.Id, at);

        for (var i = 1; i < count; i++)
        {
            var next = play.CombatDriver.Current!.Hand.FirstOrDefault();
            if (next is null)
                break;
            play.CombatDriver.PlayCard(next.Id, at);
        }
    }

    // The Busy Entry counts cards, not kinds — so the card that trips it is whatever is left in hand.
    private static void PlayAny(RunPlayback play, CombatantId at)
    {
        var next = play.CombatDriver!.Current!.Hand.FirstOrDefault();
        Assert.True(next is not null, "the probe hand was empty");
        play.CombatDriver.PlayCard(next!.Id, at);
    }

    private static void EndTurn(RunPlayback play)
    {
        play.CombatDriver!.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([0]);
    }

    // §5.2/5.3: nothing is predicted before there is a record, and the turn after there is one the Catalogue
    // says something out loud.
    [Fact]
    public void It_says_nothing_until_it_has_watched_a_whole_turn()
    {
        var (play, _, book) = Fight();

        Assert.Null(SpokenPrediction(play, book));

        Spend(play, book, Deed, 3);
        EndTurn(play);

        Assert.NotNull(SpokenPrediction(play, book));
    }

    // §5.3: the prediction is derived from the RECORD — open three turns the same way and what it says back
    // is that opening. This is the whole thesis: it describes what you are becoming.
    [Fact]
    public void What_it_predicts_is_the_habit_it_watched()
    {
        var (play, _, book) = Fight();

        Spend(play, book, Deed, 3);
        EndTurn(play);

        // The first beat speaks the Opening family, and the record's opening was a Deed.
        Assert.Equal(WhisperingCatalogue.PredictViolenceId, SpokenPrediction(play, book));
    }

    // §5.4: confirming the prediction is what gives it Authority over you.
    [Fact]
    public void Confirming_what_it_said_gives_it_authority()
    {
        var (play, _, book) = Fight();

        Spend(play, book, Deed, 3);
        EndTurn(play);
        Assert.Equal(WhisperingCatalogue.PredictViolenceId, SpokenPrediction(play, book));

        // Open with a Deed again, exactly as described.
        Spend(play, book, Deed, 2);
        EndTurn(play);

        Assert.Equal(1, Stacks(play, book, WhisperingCatalogue.AuthorityId));
        Assert.Equal(0, Stacks(play, book, WhisperingCatalogue.ContradictionId));
    }

    // §5.4: contradicting it costs it a Contradiction and, with no Block to strip, 4 HP outright. The intent
    // under test is a pure attack precisely so the Catalogue carries no guard — "Open a New Entry" would hand
    // it 12 fresh Block on the way past and hide the payment inside its own turn.
    [Fact]
    public void Contradicting_it_costs_it_a_contradiction_and_four_health()
    {
        var (play, _, book) = Fight("index_the_deviation");

        Spend(play, book, Deed, 3);
        EndTurn(play);
        Assert.Equal(WhisperingCatalogue.PredictViolenceId, SpokenPrediction(play, book));

        var health = Book(play, book).Health.Current;

        // Open with a Working instead. The record said Deed — and a Working deals no damage, so the only
        // thing that can move its health this turn is having been wrong.
        Spend(play, book, Working, 1);
        EndTurn(play);

        Assert.Equal(1, Stacks(play, book, WhisperingCatalogue.ContradictionId));
        Assert.Equal(0, Stacks(play, book, WhisperingCatalogue.AuthorityId));
        Assert.Equal(health - 4, Book(play, book).Health.Current);
    }

    // The other half of the same rule: with Block on the table the contradiction is paid out of the guard
    // instead, and the body is untouched.
    [Fact]
    public void With_a_guard_up_the_contradiction_is_paid_out_of_the_block()
    {
        var (play, _, book) = Fight("open_a_new_entry");

        Spend(play, book, Deed, 3);
        EndTurn(play);
        Assert.Equal(WhisperingCatalogue.PredictViolenceId, SpokenPrediction(play, book));

        // Its own intent has been handing it 12 Block a turn, so there is a guard to strip.
        var guard = Guard(Book(play, book));
        Assert.True(guard > 0, "the probe never gave the Catalogue any Block to lose");
        var health = Book(play, book).Health.Current;

        Spend(play, book, Working, 1);

        // Measured before ENDING the turn: the Catalogue's own next action would top the guard back up and
        // hide the payment. The prediction resolves on the player's turn end, so the turn is ended and the
        // health is what proves it went to the guard and not to the body.
        EndTurn(play);

        Assert.Equal(1, Stacks(play, book, WhisperingCatalogue.ContradictionId));
        Assert.Equal(health, Book(play, book).Health.Current);
    }

    // §5.5: three confirmations and the habit stops being an observation and becomes a rule about you.
    [Fact]
    public void Three_confirmations_turn_the_habit_into_a_standing_entry()
    {
        var (play, _, book) = Fight();

        Spend(play, book, Deed, 3);
        EndTurn(play);

        for (var turn = 0; turn < 3; turn++)
        {
            // Whatever it says, do it: a Busy Deed opening satisfies haste and every Opening prediction the
            // record can produce from these turns.
            Spend(play, book, Deed, 3);
            EndTurn(play);
        }

        // Exactly one Entry ever stands, and it is the habit that was confirmed last: three busy turns make
        // the tempo itself the rule.
        Assert.Single(Entries, e => Has(play, book, e));
        Assert.True(Has(play, book, WhisperingCatalogue.EntryBusyId));
        // Spending the Authority is what establishing costs it.
        Assert.Equal(0, Stacks(play, book, WhisperingCatalogue.AuthorityId));
    }

    // §5.6: an Established Entry is not a label, it is a standing tax on the habit. Established Tempo — Busy
    // cites a card in hand the moment the player reaches a third card, every turn, for the rest of the fight.
    [Fact]
    public void The_standing_entry_taxes_the_very_habit_it_was_made_of()
    {
        var (play, _, book) = Fight("index_the_deviation");

        Spend(play, book, Deed, 3);
        EndTurn(play);
        for (var turn = 0; turn < 3; turn++)
        {
            Spend(play, book, Deed, 3);
            EndTurn(play);
        }
        Assert.True(Has(play, book, WhisperingCatalogue.EntryBusyId));

        Assert.Equal(0, Cited(play));

        // Two cards is not the habit; the third is.
        Spend(play, book, Deed, 2);
        Assert.Equal(0, Cited(play));

        PlayAny(play, book);
        Assert.Equal(1, Cited(play));
    }

    // §5.6/§5.7: and being unreadable turns the tax off. Three contradictions suspend the Entry, and a busy
    // turn under the suspension is no longer cited.
    [Fact]
    public void A_suspended_entry_stops_taxing()
    {
        var (play, _, book) = Fight("index_the_deviation");

        Spend(play, book, Deed, 3);
        EndTurn(play);
        for (var turn = 0; turn < 3; turn++)
        {
            Spend(play, book, Deed, 3);
            EndTurn(play);
        }
        Assert.True(Has(play, book, WhisperingCatalogue.EntryBusyId));

        // Now be wrong three times running. Whatever it says, do the other thing.
        for (var turn = 0; turn < 3; turn++)
        {
            var said = SpokenPrediction(play, book);
            if (said == WhisperingCatalogue.PredictSparinglyId || said == WhisperingCatalogue.PredictProcedureId)
                Spend(play, book, Deed, 3);
            else
                Spend(play, book, Working, 1);
            EndTurn(play);
        }

        Assert.True(Has(play, book, WhisperingCatalogue.EntrySuspendedId));
        Assert.True(Has(play, book, WhisperingCatalogue.EntryBusyId), "the Entry was removed rather than suspended");

        var citedBefore = Cited(play);
        Spend(play, book, Deed, 3);
        Assert.Equal(citedBefore, Cited(play));
    }

    // §5.8 "Open a New Entry": the citation is issued on the player's NEXT hand, not into a hand that is
    // about to be discarded — the beat every Act-II citation uses.
    [Fact]
    public void Opening_a_new_entry_cites_the_next_hand()
    {
        var (play, _, book) = Fight("open_a_new_entry");

        // Its very first action is Open a New Entry, and nothing is cited during the player's current turn.
        Assert.Equal(0, Cited(play));
        Spend(play, book, Deed, 1);
        Assert.Equal(0, Cited(play));

        EndTurn(play);

        Assert.True(Cited(play) > 0, "the Catalogue never cited anything on the next hand");
    }

    // §5.7: the deliberate anti-pattern counterplay. Be unreadable three times and the Entry it built out of
    // you is suspended — and it pays for the correction.
    [Fact]
    public void Three_contradictions_suspend_the_entry_it_built()
    {
        var (play, _, book) = Fight();

        Spend(play, book, Deed, 3);
        EndTurn(play);

        // Alternate against whatever it expects: a Sparse Working turn contradicts a Busy or Deed reading,
        // and a Busy Deed turn contradicts the Sparse one that follows.
        for (var turn = 0; turn < 3; turn++)
        {
            var said = SpokenPrediction(play, book);
            if (said == WhisperingCatalogue.PredictSparinglyId || said == WhisperingCatalogue.PredictProcedureId)
                Spend(play, book, Deed, 3);
            else
                Spend(play, book, Working, 1);
            EndTurn(play);
        }

        // Three contradictions reset the count and buy the suspension.
        Assert.True(Has(play, book, WhisperingCatalogue.EntrySuspendedId));
        Assert.Equal(0, Stacks(play, book, WhisperingCatalogue.ContradictionId));
    }

    // §5.8 Transition: at 129 HP or less it stops describing you one entry at a time. The action it spends on
    // speaking in full is not an attack — it clears its guard, takes 14 fresh Block and one Authority.
    [Fact]
    public void At_half_its_body_it_speaks_in_full_instead_of_attacking()
    {
        var (play, _, book) = Fight("compare_with_prior_conduct", bossHealth: 120);

        Assert.False(Has(play, book, WhisperingCatalogue.CompleteDescriptionId));

        var health = Book(play, book).Health.Current;
        EndTurn(play);

        Assert.True(Has(play, book, WhisperingCatalogue.CompleteDescriptionId),
            "the Catalogue never entered its complete description");
        Assert.Equal(1, Stacks(play, book, WhisperingCatalogue.AuthorityId));
        Assert.Equal(health, Book(play, book).Health.Current);
    }

    // §Phase II: the complete description predicts you TWICE a turn where the record can carry two readings.
    [Fact]
    public void The_complete_description_speaks_twice()
    {
        var (play, _, book) = Fight("compare_with_prior_conduct", bossHealth: 120);

        // One turn to file a record, and the transition action along the way.
        Spend(play, book, Deed, 3);
        EndTurn(play);
        Assert.True(Has(play, book, WhisperingCatalogue.CompleteDescriptionId));

        Spend(play, book, Deed, 3);
        EndTurn(play);

        // Two DIFFERENT readings of the same record: the tempo it always supports, and the Deed opening the
        // player has now shown it twice.
        var spoken = WhisperingCatalogue.Predictions.Where(p => Has(play, book, p)).ToList();
        Assert.Equal(2, spoken.Count);
    }

    // Final Signature §Final Entry: at 64 HP the stakes invert. Contradicting the record now tears 8 HP out of
    // it a time, which is the player's last and best tool.
    [Fact]
    public void The_final_entry_makes_contradiction_the_weapon()
    {
        var (play, _, book) = Fight("compare_with_prior_conduct", bossHealth: 60);

        Spend(play, book, Deed, 3);
        EndTurn(play);
        Assert.True(Has(play, book, WhisperingCatalogue.FinalEntryId),
            "the Catalogue never prepared its Final Entry");

        var health = Book(play, book).Health.Current;

        // Falsify the record rather than obey it.
        Spend(play, book, Working, 1);
        EndTurn(play);

        Assert.True(Book(play, book).Health.Current <= health - 8,
            "contradicting the Final Entry cost the Catalogue nothing");
    }
}
