using BnbContent.Converter.Lieutenant;
using RogueDeck.Core.Combat;
using static BnbContent.Tests.LieutenantTests;

namespace BnbContent.Tests;

// A Lieutenant fight held for a test: one enemy, or a biter and a quiet tablet side by side.
internal static class LieutenantDuel
{
    internal static int Standing(CombatantState c) => c.Health.Current + Block(c);
    internal static string[] Five(params string[] cards) =>
        [.. cards, .. Enumerable.Repeat("lt_buckler_up", Math.Max(0, 5 - cards.Length))];

    internal sealed class Duel : IDisposable
    {
        public required RogueDeck.Sandbox.Composition.RunPlayback Play { get; init; }
        public required RogueDeck.Sandbox.Run.InteractiveRunSession Session { get; init; }
        public required CombatantId Enemy { get; init; }
        public CombatantId? Other { get; init; }
        public void P(string card, int? choose = null, CombatantId? at = null) =>
            LieutenantTests.Play(Play, Session, card, at ?? Enemy, choose);
        public void End() => EndTurn(Play, Session);
        public CombatantState Me => Hero(Play);
        public CombatantState It => Body(Play, Enemy);
        public int Hand => Now(Play).Hand.Count;
        public int Energy => Me.Resources[StandardCombatIds.EnergyResource].Current;
        public void Dispose() => Play.Dispose();
    }

    internal static Duel One(string[] deck, string enemy = Quiet, string intent = QuietIntent, int? health = null,
        int energy = 9, params (string, int)[] hero)
    {
        var (play, session, id) = Fight(deck, energy, health, enemy: enemy, intent: intent, hero: hero);
        return new Duel { Play = play, Session = session, Enemy = id };
    }

    // Two bodies: a biter (the Enemy) and a quiet tablet (the Other).
    internal static Duel Two(string[] deck, params (string, int)[] hero)
    {
        var probe = FightProbe.RosterAgainstHero("lt_two", 9, hero, (Biter, BiteIntent, null), (Quiet, QuietIntent, null));
        var (play, session, _) = FightProbe.Start(probe, deck, character: LieutenantCharacter.Id);
        var all = Now(play).State.Combatants.Where(c => c.Id != Now(play).HeroId).ToList();
        return new Duel
        {
            Play = play, Session = session,
            Enemy = all.First(c => c.DefinitionId.value == Biter).Id,
            Other = all.First(c => c.DefinitionId.value == Quiet).Id,
        };
    }

    internal static int Authority(Duel d) => Stacks(d.Me, LieutenantKeywords.Authority);
    internal static int Strength(Duel d) => Stacks(d.Me, LieutenantKeywords.Strength);

}
