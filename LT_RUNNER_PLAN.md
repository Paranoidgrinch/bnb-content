# Lieutenant trial runner

Agreed with the user on 2026-11-06.

## Goal

A bot that plays the encounter trial as the Lieutenant, close to how a strong human plays it. For every
fight it should find the line that loses the least health. Among equal losses, the faster win is better.
After each stage it takes the trial's packages and picks the better cards. Then it plays the next stage with
the grown deck, through every act that has a trial.

The result shows, per encounter, what that fight costs under strong play with a typical deck at that point
in the trial. It complements the user's hand-played trial logs (`tools/trial_report.py`); it does not
replace them.

## What the bot may know

- It knows everything a human sees:
  - the hand, the Energy and the enemy intents and statuses
  - the contents of the draw pile and the discard pile, and therefore the odds of each draw
- It does NOT know:
  - the order of the draw pile
  - any future random roll: enemy intent picks, random targets, jars, Nisaba's sentences and so on

The user's words: "a human can foresee what they draw next within limits, because they know which cards are
in the deck and the odds of drawing them".

## How it plays: determinized search

At each of its turns the hand is known, so the search runs in two parts.

1. **Within the turn: search over play sequences.** The hand and the intents are known, so this part can be
   exact or a wide beam. It covers which card, which target, in which order, and when to end the turn.
2. **Leaf evaluation.** Each end-of-turn state is scored by rollouts over K sampled futures:
   - Each future is a shuffle of the known draw pile plus fresh random rolls.
   - Each future is played on with a cheap policy, for example the existing Greedy, to the end of the fight
     or for H turns.
   - **Score:** expected health lost, then expected rounds, plus a large penalty for a loss.
3. **Choice.** The bot plays the sequence with the best average score.

Choices that arise inside a fight, such as discover, choose-a-card, Inanna's gate or the Pharaoh's commands,
are part of the search like any other action.

K, H and the beam width are flags. Pick defaults that keep Act I at minutes, not hours.

## How a fight is measured

- Each encounter is played on N fresh real seeds (default 20), with the same deck each time.
- **Recorded per encounter:**
  - win rate
  - health lost: mean, median, max, standard deviation
  - rounds: mean and max
- Each fight starts at full health, like the trial with the Infirmary.

## The deck along the trial

The walk is exactly `EncounterTrial`'s for the Lieutenant: the start deck, the stages in order, the packages
between stages, and the earlier acts' catch-up for later acts.

**Package choices take the better option:**
- **Card offers:** pick the highest-rated card, using `Playtest/CardRating.cs` or the best existing rating.
  Skip if every offer rates below the deck average, if the rating supports that.
- **Relic:** the highest-rated relic, or the first if there is no rating.
- **Upgrade:** the card whose upgrade gains the most by rating. Otherwise the most-played attack.
- **Removal:** the weakest card, a starter Strike or Defend first.

**Variance across rewards:** P progressions (default 5) with different reward seeds. Each path plays every
stage on the deck it built.

## Output

- A JSON file and a readable table per run in `~/Desktop/bnb-balance/bots/`.
- **One row per encounter:**
  - act, stage, kind
  - win rate, health lost (mean / median / max), rounds
  - the trial targets (from the encounter-trial memory / `trial_report.py`), and whether each is met
- With `--compare <trial-log.jsonl>`, the user's hand-played numbers sit next to the bot's.

## Scope and order

1. Act I first, measured against the user's Act I trial log in `~/Desktop/bnb-balance/trials/`.
2. Then Acts II–V behind the same flag (`--lt-runner --acts 1,2,…`).
3. **Lieutenant only.**

## Where it lives

- bnb-content `Converter/Playtest/TrialRunner.cs` (plus helpers), run through a flag in `Program.cs`.
- Tests:
  - the search never plays an illegal move
  - the walk equals the trial's
  - the package choices are deterministic for a seed
  - the bot never reads the real draw order: test it, for example by swapping the hidden order and checking
    the decision does not change
