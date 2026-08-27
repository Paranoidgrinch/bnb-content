using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act III — The Road of Permitted Turns, proved in live fights. The act's pressure is source-bound STANDING:
// a Trespass is not a debuff the room puts on you, it is a violation owed to whoever's law you broke, and
// three of them owed to one party become that party's Claim. These tests are mostly about the word "whose".
public class ActThreeStageOneTests
{
    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── the act's licence ─────────────────────────────────────────────────────────────────────────────────

    // A normal Green Docket combat opens with one Safe-Conduct, and it pays for exactly one Trespass. The
    // damage the same intent deals is not its business: a safe conduct is leave to pass, not a shield.
    [Fact]
    public void The_opening_safe_conduct_pays_for_one_trespass_and_stops_no_damage()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo("permit_hare", "check_the_permit"));

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        var before = Hero(play).Health.Current;

        play.CombatDriver!.EndTurn(); // Check the Permit: 10 damage and 1 Trespass

        Assert.Equal(before - 10, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        play.CombatDriver.EndTurn(); // …and the next one lands

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        play.Dispose();
    }

    // Each violation is its own instance carrying its own source, because the threshold is "3 from the SAME
    // source" and merged stacks remember only the last party to file.
    [Fact]
    public void Every_trespass_is_owed_to_the_party_whose_law_was_broken()
    {
        var (play, _, hareId) = FightProbe.Start(
            FightProbe.Solo("permit_hare", "check_the_permit"));

        play.CombatDriver!.EndTurn(); // refused by the opening Safe-Conduct
        play.CombatDriver.EndTurn();
        play.CombatDriver.EndTurn();

        var trespass = Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId))
            .ToList();
        Assert.Equal(2, trespass.Count);
        Assert.All(trespass, s => Assert.Equal(hareId, s.SourceCombatantId));
        play.Dispose();
    }

    // Three owed to one party are spent, and that party gains a Claim — the act's whole engine in one line.
    [Fact]
    public void Three_trespass_owed_to_one_party_become_that_partys_claim()
    {
        var (play, _, hareId) = FightProbe.Start(
            FightProbe.Solo("permit_hare", "check_the_permit"));

        for (var turn = 0; turn < 4; turn++) // one refused, then three that land
            play.CombatDriver!.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, hareId), ActThree.ClaimId));
        // …and the Claim was MADE, not handed over: the announcement is what a later stage listens for.
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, hareId), ActThree.ClaimCreatedId));
        play.Dispose();
    }

    // ── Permit Hare — No Hasty Passage ────────────────────────────────────────────────────────────────────

    // "If the player plays a third card during a player turn: 1 Trespass. Once per player turn." The rule
    // fires on the player's own action, and the violation is still owed to the Hare.
    [Fact]
    public void The_third_card_of_a_turn_is_a_hasty_passage()
    {
        var (play, session, hareId) = FightProbe.Start(
            FightProbe.Solo("permit_hare", "stamp_passage", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 10)]);

        Play(play, session, "paper_cut", hareId);
        Play(play, session, "paper_cut", hareId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));

        Play(play, session, "paper_cut", hareId); // the third — and the Safe-Conduct pays for it

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));

        Play(play, session, "paper_cut", hareId); // a fourth is not a second violation
        Play(play, session, "paper_cut", hareId);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        play.Dispose();
    }

    [Fact]
    public void A_hasty_passage_is_owed_to_the_hare_and_not_to_the_player_who_made_it()
    {
        var (play, session, hareId) = FightProbe.Start(
            FightProbe.Solo("permit_hare", "stamp_passage", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 10)]);

        play.CombatDriver!.EndTurn(); // burn the opening Safe-Conduct on nothing …
        play.CombatDriver.EndTurn();  // … the Hare only blocks, so it is still there
        Play(play, session, "paper_cut", hareId);
        Play(play, session, "paper_cut", hareId);
        Play(play, session, "paper_cut", hareId);

        var trespass = Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId))
            .ToList();
        // The rule answered a CARD PLAY, whose acting source is the player. The violation is the Hare's.
        Assert.All(trespass, s => Assert.Equal(hareId, s.SourceCombatantId));
        play.Dispose();
    }

    // ── Mossbound Clerk — The First Use Became Custom ─────────────────────────────────────────────────────

    // However the combat's first real card was played, that is the procedure. Keep to it and the Clerk has
    // nothing to say.
    [Fact]
    public void Keeping_to_the_first_use_is_no_trespass()
    {
        var (play, session, clerkId) = FightProbe.Start(
            FightProbe.Solo("mossbound_clerk", "moss_seal", energy: 9),
            deck: [.. Enumerable.Repeat("paper_cut", 10)]);

        Play(play, session, "paper_cut", clerkId); // a Deed sets the custom
        play.CombatDriver!.EndTurn();
        Play(play, session, "paper_cut", clerkId); // and a Deed opens the next turn too

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.TrespassId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // nothing was refused either
        play.Dispose();
    }

    // Open a later turn with another kind of card and the moss disagrees.
    [Fact]
    public void Opening_a_turn_against_the_custom_is_a_trespass()
    {
        var (play, session, clerkId) = FightProbe.Start(
            FightProbe.Solo("mossbound_clerk", "moss_seal", energy: 9),
            deck: ["paper_cut", "cower_behind_a_desk", "paper_cut", "cower_behind_a_desk", "paper_cut"]);

        Play(play, session, "paper_cut", clerkId); // Deed — the custom
        play.CombatDriver!.EndTurn();
        Play(play, session, "cower_behind_a_desk", clerkId); // a Working opens the turn

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // refused, once
        play.CombatDriver.EndTurn();
        Play(play, session, "cower_behind_a_desk", clerkId); // and again, with nothing left to refuse it

        var trespass = Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId))
            .ToList();
        Assert.Single(trespass);
        Assert.Equal(clerkId, trespass[0].SourceCombatantId);
        play.Dispose();
    }

    // Only the turn's FIRST real card is asked about. Once the turn has been opened correctly, the rest of it
    // is nobody's business — which is also what keeps the rule to once per turn without a latch.
    [Fact]
    public void Only_the_first_card_of_a_turn_is_asked_about()
    {
        var (play, session, clerkId) = FightProbe.Start(
            FightProbe.Solo("mossbound_clerk", "moss_seal", energy: 9),
            deck: ["paper_cut", "cower_behind_a_desk", "cower_behind_a_desk", "paper_cut", "paper_cut"]);

        Play(play, session, "paper_cut", clerkId); // Deed — the custom, and the turn opened correctly
        Play(play, session, "cower_behind_a_desk", clerkId);
        Play(play, session, "cower_behind_a_desk", clerkId);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // nothing was ever refused
        play.Dispose();
    }

    // ── Cairn of Stray Paths — Every Detour Leaves a Stone ────────────────────────────────────────────────

    // The Cairn remembers what the player actually took from somebody else. It is a support identity, so it
    // is proved in the duo it is fielded in.
    [Fact]
    public void A_trespass_taken_from_another_party_leaves_the_cairn_a_stone()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("stray_paths",
                ("permit_hare", "check_the_permit", null),
                ("cairn_of_stray_paths", "brace_cairn", null)));

        play.CombatDriver!.EndTurn(); // the Hare files; the opening Safe-Conduct refuses it
        var cairn = Enemies(play)[1];
        Assert.Equal(0, FightProbe.StacksOf(cairn, ActThree.DetourStoneId)); // a refusal is not a detour

        play.CombatDriver.EndTurn(); // this one lands

        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[1], ActThree.DetourStoneId));
        play.Dispose();
    }

    // Two stones are spent, and the standing they become belongs to the OTHER party — a violation committed
    // against one becomes precedent supporting somebody else.
    [Fact]
    public void Two_stones_become_another_partys_claim()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Roster("stray_paths",
                ("permit_hare", "check_the_permit", null),
                ("cairn_of_stray_paths", "brace_cairn", null)));

        for (var turn = 0; turn < 3; turn++) // one refused, then two that land
            play.CombatDriver!.EndTurn();

        var hare = Enemies(play)[0];
        var cairn = Enemies(play)[1];
        Assert.Equal(0, FightProbe.StacksOf(cairn, ActThree.DetourStoneId)); // spent
        Assert.Equal(0, FightProbe.StacksOf(cairn, ActThree.ClaimId));       // and not the Cairn's own
        Assert.Equal(1, FightProbe.StacksOf(hare, ActThree.ClaimId));
        play.Dispose();
    }

    // One stone a turn, however many violations the turn holds — the Cairn counts turns, not filings. It takes
    // two Local Laws to break twice inside one player turn, so this is the road's whole roster at once.
    [Fact]
    public void The_cairn_records_one_detour_a_turn()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("stray_paths", energy: 9,
                ("permit_hare", "stamp_passage", null),
                ("mossbound_clerk", "moss_seal", null),
                ("cairn_of_stray_paths", "brace_cairn", null)),
            // Five cards, so the deck IS the opening hand — a bigger one is shuffled and the test would be
            // asking for cards it has not been dealt.
            deck: ["paper_cut", "cower_behind_a_desk", "cower_behind_a_desk", "cower_behind_a_desk",
                   "cower_behind_a_desk"]);

        var hare = Enemies(play)[0].Id;
        Play(play, session, "paper_cut", hare); // a Deed becomes the custom
        Play(play, session, "cower_behind_a_desk", hare);
        Play(play, session, "cower_behind_a_desk", hare); // the third card is a hasty passage — the licence pays
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, FightProbe.StacksOf(Enemies(play)[2], ActThree.DetourStoneId));

        play.CombatDriver!.EndTurn(); // nobody files: all three are defending

        Play(play, session, "cower_behind_a_desk", hare); // a Working against the custom — the Clerk files
        Play(play, session, "cower_behind_a_desk", hare);
        Play(play, session, "cower_behind_a_desk", hare); // …and the third card, so the Hare files too

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.TrespassId)); // two violations …
        Assert.Equal(1, FightProbe.StacksOf(Enemies(play)[2], ActThree.DetourStoneId)); // … one stone
        play.Dispose();
    }
}
