# Act V rework — the six gods

Agreed with the user on 2026-11-05. This replaces every fight in `Converter/ActFive*.cs`.

## What stays

- **The act's shape.** Act V is still the gauntlet: three gods back to back, drawn from six, with nothing in
  between (see `Tests/ActFiveGauntletTests.cs`).
- **The divine rule area.** It stays the act's one shared UI rule (`ActFive.RuleAreas`). Each god's text is
  rewritten to the new rule, and each panel is one or two plain sentences.
- **Act V has no `ActTuning` factor.** Every number below is the value the player sees.

## What the user asked for

- These are the last enemies of the game and they are gods. Each must be special, authentic to its myth,
  hard, straightforward and cool. Nobody should expect to beat them easily.
- Each god gets a long, honorific name.
- One core rule per god. It is visible on the table: chips, counters and bodies, never hidden marks.
- No Entombed and no Burdened in Act V.
- No finale is a pure "die when the clock runs out" timer.

## The six gods

### G1 Nisaba, Lady of the Reed Stylus, Who Writes What Must Come — 950 HP

What she writes happens.

**Core rule.**
- Each sentence stands on the table as its own body (a summon), with 60 HP and a countdown of 2. The
  countdown is shown on the body.
- Break the sentence before the countdown runs out and it is erased. Otherwise it comes true and leaves.
- At most 3 sentences stand at once.

**Sentences.** She picks a random sentence that is not already standing.
- *The Body Shall Bear Forty*: 40 damage, Block does not apply.
- *The Measure Shall Be Withheld*: −2 Energy next turn.
- *The Hand Shall Hold Two*: you draw only 2 cards next turn.
- *The Works Shall Be Broken*: 3 random cards of your draw pile are exhausted for the fight.
- *The Guard Shall Be As Nothing*: your Block is removed, and you gain no Block next turn.
- *The Keeper Shall Be Restored*: she heals 80.

**Intents.**
- *Inscribe*: writes one sentence, +15 Block.
- *Strike of the Reed*: 26 damage.
- *Dry the Clay*: every standing sentence gains +30 HP. Its max HP rises with it.
- *Read Aloud*: 14 damage per standing sentence.
- If 3 sentences already stand, she does not pick *Inscribe*.

**Phase 2, The Lapis Record (65 %).** *Inscribe* writes two sentences, and new sentences have 90 HP.

**Phase 3, The Final Line (30 %).**
- The ordinary sentences are wiped. One sentence remains, with 250 HP and a countdown of 4:
  *"On the Fourth Dawn the Name of the Supplicant Shall Be Erased."*
- While it stands she cannot fall below 1 HP (Indelible).
- If it runs out, the player dies.
- If it is broken, she takes 100 damage ("the reed turns") and Indelible goes.
- Her intents then rotate *Strike of the Reed* and *Read Aloud*, counting the Final Line as one sentence.

### G2 Inanna, Queen of Heaven, Who Descended to the Great Below — 1050 HP

The seven gates of the Descent.

**Core rule.**
- She starts with the 7 regalia of the myth. Each one is its own visible status on her.
- Every 130 HP she loses, she passes a gate. The player chooses freely which of the remaining regalia she
  leaves at that gate.
- Several thresholds crossed by one hit give one choice each.
- The choice must be a real player prompt that names each regalium and its effect.

**The regalia.**
1. *Shugurra, Crown of the Steppe*: +5 Strength.
2. *Rod and Line of Lapis*: +25 Block each of her turns.
3. *Necklace of Small Lapis Beads*: heals 15 each of her turns.
4. *Twin Egg-Shaped Beads*: her first attack each turn hits twice.
5. *Pala, Robe of Ladyship*: she takes 25 % less damage.
6. *Golden Ring*: you gain 1 Weak each turn.
7. *Breastplate "Come, Man, Come"*: Thorns 4.

**Intents.**
- *Lioness of Battle*: 32 damage.
- *Arrows of the Morning Star*: 5×6.
- *Splendor of Eanna*: +35 Block, +2 Strength.
- *Infatuation*: for 2 turns your attacks on her deal 25 % less, and she gains +2 Strength.

**Finale, the seventh gate.** When the last regalium is gone she becomes *Queen of Battle*. Every intent
attacks twice, and she gains +3 Strength each turn.

### G3 Nanshe, Lady of the Just Measure, Who Reads the Dreams of Kings — 900 HP

**Core rule.**
- Every day has a visible portion: 3 Energy and 5 cards.
- *Take From Tomorrow* is a 0-cost card with Retain, given at the start of the fight. Each play gives
  +1 Energy and +1 card now and writes 1 Debt. It can be played any number of times.
- Debt is taken off the next day's portion, 1 Energy and 1 card per point. A day never drops below
  1 Energy and 1 card, and whatever cannot be taken off yet stays as Debt.
- At the end of each Distribution (3 days) the Debt still open is collected: 10 damage per point, Block
  does not apply. Then Debt is reset.
- Chips show the day (I/II/III), the portion and the Debt.

**Intents.** The fixed 3-day pattern is on her tablet before Day I.
- *Day I, Open the Canals*: 22 damage, +20 Block.
- *Day II, The Net of Nanshe*: 3×9, and 1 Weak.
- *Day III, Judgment of the New Year*: 30 damage, +10 per Debt.

**Phase 2, The Lean Years (65 %).**
- The portion becomes 3 Energy and 4 cards.
- Energy and draws made by your own cards also write Debt, 1 per point or card.
- *Judgment* is 40, +10 per Debt.

**Phase 3, The Measure Made Equal (30 %).**
- After each of her turns, whichever side has the higher HP percentage is lowered to the other's.
- The pattern goes on. There is no timer.

### G4 Nanna-Suen, Lord of the Counted Moon, Father of the Sun and the Morning Star — 1000 HP

He is the father of Utu and Inanna.

**Core rule.** A fixed, visible cycle of four phases, one per round, starting at New Moon. Each phase has
exactly one intent and one rule.
- *New Moon, Count the Unseen*:
  - He takes 50 % less damage.
  - +30 Block.
  - Shuffles 2 *Moonless* into your draw pile. *Moonless* is unplayable and Ethereal.
- *Waxing, Raise the Horns*: 2×14 and +3 Strength, permanent.
- *Full Moon, Crown of Ur*:
  - 45 damage.
  - The first card you play that round is Counted.
  - At the next Full Moon a 0-cost temporary copy of it is in your hand (Ethereal).
- *Waning, Pour Away the Light*: 3×8, and he takes double damage.

**Escalation.** Each completed orbit (4 rounds) gives him one permanent *Radiance*: +15 % to everything he
does.

The divine rule area shows the ring and the phase now, plus the next phase.

### G5 Utu-Shamash, Judge of Heaven and Earth, Witness of Every Oath — 950 HP

He sees everything and returns it.

**Core rule 1, the Oath.**
- At the start of every player turn three 0-cost oath cards are in hand: *Swear by the Sun: 20*, *40*
  and *60*. Each has Ethereal, and playing one removes the others.
- The Favor is paid at once: 10, 20 or 30 Block.
- If the turn ends with less damage dealt to him than sworn, he gains +1 *Witness*: permanent +10 % to
  everything he does. A chip shows the sworn value and the damage dealt so far.

**Core rule 2, Retribution.**
- His main attack, *The Saw of Judgment*, deals 15 + 50 % of the damage the player dealt him in the
  player turn just before it.
- The intent number updates live while the player plays.

**Intents.**
- *The Saw of Judgment*: retribution, as above. It is every other intent.
- *Rays That Lay Bare*: removes your Block, then 18 damage.
- *Rise Between the Mountains*: +30 Block, +1 Strength.
- *The Sun-Disk*: 4×7.

**Phase 2, No Shadow (60 %).** Retribution becomes 75 %. A turn without an oath also gives +1 Witness.

**Phase 3, Judge of the Dead (30 %).** The sun goes down into the underworld.
- No more oath cards.
- Each Witness now counts +20 %.
- Retribution becomes 100 %.

### G6 Enlil, Lord of the Wind, Whose Word Cannot Be Unspoken — 1100 HP

**Core rule.**
- A visible ring of 8 decrees. Exactly one decree is in force each round, and the ring turns one step per
  round. The next decree is always announced.
- Every decree binds both sides.
- Nothing removes, shortens or changes a decree.
- The rule area shows the current decree, the next one and the ring.

**Decrees, in ring order.**
1. *No Wall Shall Rise Above Twenty*: Block is capped at 20.
2. *The Fourth Work Shall Be the Last*: at most 4 cards played.
3. *No Word Shall Be Without Price*: no card costs less than 1.
4. *The Blow Returns to the Striker*: whoever deals attack damage takes 25 % of it.
5. *Half Shall Be the Measure of Every Wound*: all damage is halved.
6. *No Wound Shall Close*: no healing.
7. *The First Work Shall Be Done Twice*: your first card each turn is played twice, and his first hit
   lands twice.
8. *The Hand Shall Hold Four*: you draw only 4 cards.

**Intents.**
- *The Command Goes Forth*: 20 damage.
- *Wind Over the Plain*: 4×8.
- *Raise the E-kur*: +40 Block. Under decree 1 this gives only 20.
- *Weight of Kingship*: 35 damage.

**Phase 2, The Noise of Mankind (60 %).** From Atrahasis.
- Every card you play makes 1 *Noise*.
- At the end of your turn with 12 or more Noise he sends *Namtar's Plague*: 25 damage, 2 Weak, 2 Frail.
  Then Noise goes back to 0.

**Phase 3, The Flood (30 %).**
- The ring stops on the decree in force. Noise ends.
- The water rises by 1 each round. It deals water×6 damage to you and to him at the end of his turn, and
  Block does not apply.
- *Atrahasis' Boat* (2 Energy, Retain) is added to your hand once. It halves the flood damage to you this
  round and returns to your hand each turn.

## Encounter trial

- Act V joins `EncounterTrial.Acts` and the single-encounter menu.
- **Deck.** The deck is built the way every trial builds it: the catch-up packages of Acts I–IV.
- **Fights.** Then come the 6 gods in a row, as the bosses, with the Infirmary between them.
- **Single fight.** Each god can be played alone, after the same packages.

## Godot

Everything listed below must be visible in Godot:
- sentence bodies with their countdown
- regalia as statuses, and the gate choice prompt
- the Nanshe day, portion and Debt chips
- the moon phase now and next
- the sworn value against the damage dealt so far, Witness, and the live Saw intent
- decree now and next, Noise and the water level
