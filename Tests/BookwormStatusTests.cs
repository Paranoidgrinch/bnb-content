using RogueDeck.Core.Combat;

namespace BnbContent.Tests;

// Bookworm X — the reworked Act-I anti-Paperwork status — proven in LIVE fights, not just in the mapping:
// "immediately before that enemy's Paperwork resolves, remove up to X Paperwork and the same number of
// Bookworm stacks". Everything runs through the real host path, so a wiring gap or a trigger-ordering
// regression fails here.
public class BookwormStatusTests
{
    // 5 Paperwork + 2 Bookworm → 3 Paperwork + 0 Bookworm, and the tick that follows deals 3, not 5.
    [Fact]
    public void Bookworm_erases_paperwork_before_it_ticks()
    {
        var beetle = AfterItsFirstTurn(paperwork: 5, bookworm: 2);

        Assert.Equal(3, FightProbe.StacksOf(beetle, "paperwork"));
        Assert.Equal(0, FightProbe.StacksOf(beetle, "bookworm"));
        Assert.Equal(37, beetle.Health.Current); // 40 − 3 ticked, not − 5
    }

    // More Bookworm than Paperwork: only as much is spent as there was Paperwork, the rest remains.
    [Fact]
    public void Surplus_bookworm_remains_after_erasing_what_paperwork_there_was()
    {
        var beetle = AfterItsFirstTurn(paperwork: 1, bookworm: 3);

        Assert.Equal(0, FightProbe.StacksOf(beetle, "paperwork"));
        Assert.Equal(2, FightProbe.StacksOf(beetle, "bookworm"));
        Assert.Equal(40, beetle.Health.Current); // nothing left to tick
    }

    // No Paperwork at all: Bookworm is not spent (it waits for the filing that is surely coming).
    [Fact]
    public void Bookworm_is_not_spent_without_paperwork()
    {
        var beetle = AfterItsFirstTurn(paperwork: 0, bookworm: 2);

        Assert.Equal(2, FightProbe.StacksOf(beetle, "bookworm"));
        Assert.Equal(40, beetle.Health.Current);
    }

    // The Filing Beetle opens the fight with the given statuses; the hero ends its turn, so the beetle's turn
    // starts: Bookworm first, then the Paperwork tick.
    private static CombatantState AfterItsFirstTurn(int paperwork, int bookworm)
    {
        var probe = FightProbe.Solo("filing_beetle", "mandible_stamp",
            ("paperwork", paperwork), ("bookworm", bookworm));
        var (play, session, enemyId) = FightProbe.Start(probe);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        return play.CombatDriver.Current!.State.GetCombatant(enemyId);
    }
}
