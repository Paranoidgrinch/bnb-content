using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// The roster's locks and lessons (user, 2026-10-23): a new profile plays the Lieutenant alone; each of the others is
// earned in a chain, and every character walks a three-room lesson of its own.
public class CharacterUnlockTests
{
    [Fact]
    public void A_new_profile_plays_the_lieutenant_alone()
    {
        var available = MetaProgression.AvailableCharacters(FightProbe.Game, new MetaState());
        Assert.Equal([Converter.Lieutenant.LieutenantCharacter.Id], available.Select(c => c.Id));
    }

    [Fact]
    public void Each_flag_opens_its_character()
    {
        var meta = new MetaState();
        foreach (var (character, flag, _, _, _) in CharacterUnlocks.Chain)
        {
            meta.SetFlag(flag);
            Assert.Contains(MetaProgression.AvailableCharacters(FightProbe.Game, meta), c => c.Id == character);
        }
        Assert.Equal(4, MetaProgression.AvailableCharacters(FightProbe.Game, meta).Count);
    }

    [Fact]
    public void The_chain_is_written_for_the_frontend()
    {
        var extra = FightProbe.Game.Presentation.Game!.Extra;
        Assert.Equal("any:1", extra["unlock:sorcerers_apprentice"]);
        Assert.Equal("sorcerers_apprentice:2", extra["unlock:hedge_witch"]);
        Assert.Equal("hedge_witch:3", extra["unlock:bureaucrat"]);
        Assert.All(CharacterUnlocks.Chain, c => Assert.False(string.IsNullOrWhiteSpace(extra[$"unlock.hint:{c.Character}"])));
    }

    // Every character's lesson is three rooms, walked to the end, and its own words are in the document.
    [Fact]
    public void Every_character_has_a_three_room_lesson_of_its_own()
    {
        var extra = FightProbe.Game.Presentation.Game!.Extra;
        foreach (var character in FightProbe.Game.Characters)
        {
            var tutorial = FightProbe.Game.ForTutorial(character.Id);
            Assert.Equal(3, tutorial.Map.Nodes.Count);
            var report = Converter.Playtest.RunWalker.Walk(tutorial, seed: 1);
            Assert.True(report.Error is null, $"{character.Id}: {report.Error}");
            Assert.Equal(RunResult.Victory, report.Result);
        }
        foreach (var (moment, _, _) in Tutorial.ApprenticeSteps.Concat(Tutorial.LieutenantSteps))
            Assert.False(string.IsNullOrWhiteSpace(extra[$"tutorial.text:{moment}"]), moment);
    }
}
