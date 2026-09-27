using BnbContent.Converter.Playtest;

namespace BnbContent.Tests;

// The static card rating (BALANCE_PLAN K1): what it counts, what it refuses to count, and that it reads every
// card the game ships.
public class CardRatingTests
{
    private static readonly IReadOnlyDictionary<string, CardRating.Row> Rows =
        CardRating.Rate(FightProbe.Game).ToDictionary(r => r.Card.Id);

    [Fact]
    public void Paper_cut_is_the_yardstick_six_points_for_one_energy()
    {
        var cut = Rows["paper_cut"];
        Assert.Equal(6, cut.Read.Damage);
        Assert.Equal(6, cut.PerEnergy);
    }

    // ⚠ The Seal conversion is compiled into every card that grants Seal. Counted as the card's own effect it
    // would pay Waxing Authority for three stacks spent and a Ratify it only sometimes causes.
    [Fact]
    public void The_seal_conversion_is_the_keyword_not_the_card()
    {
        var wax = Rows["waxing_authority"];   // Deal 5 damage. Apply 1 Seal.
        Assert.Equal(5, wax.Read.Damage);
        Assert.Equal(1, wax.Read.Statuses["seal"]);
        Assert.DoesNotContain("ratified", wax.Read.Statuses.Keys);
        Assert.DoesNotContain("if", wax.Read.Flags);
    }

    // "Deal 14 damage, plus 8 for each different negative Status on the target. Count at most 5."
    [Fact]
    public void A_scaling_card_is_ranked_by_what_it_does_with_no_set_up_and_shows_its_ceiling()
    {
        var tribunal = Rows["black_tribunal"];
        Assert.Equal(14, tribunal.Read.Damage);
        Assert.Equal(7, tribunal.PerEnergy);
        Assert.Equal(27, tribunal.MaxPerEnergy);
        Assert.Contains("scales", tribunal.Read.Flags);
    }

    [Fact]
    public void A_rite_is_flagged_rather_than_scored_as_nothing()
    {
        var rite = Rows["black_ledger"];
        Assert.Contains(rite.Read.Flags, f => f.StartsWith("rule:", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_node_of_every_card_is_read()
    {
        var unread = Rows.Values
            .SelectMany(r => r.Read.Flags.Where(f => f.StartsWith("unread:", StringComparison.Ordinal))
                .Select(f => $"{r.Card.Id} {f}"))
            .ToList();
        Assert.Empty(unread);
    }

    // "Requires at least 1 Seal. … Ratify it immediately. Draw 1 card." Asking about Seal is not the
    // conversion; only "at least 3" is.
    [Fact]
    public void A_card_that_asks_about_seal_for_itself_is_read()
    {
        var privy = Rows["privy_seal"];
        Assert.Contains("if", privy.Read.Flags);
        Assert.True(privy.Read.Draw > 0);
    }
}
