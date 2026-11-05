# Act IV rework (user, 2026-11-03/04)

Act IV is rebuilt the way Act III was: every rule one clear sentence, visible in Godot, and an encounter
trial at the end. Paperwork, Weighed and the error bands are gone from the act. Entombed stays, but only
elites and bosses use it, and never more than 1 at a time.

All numbers below are what the player SEES. Authored JSON values are those numbers divided by the
ActTuning factor of the role (act 4: normal 125 % HP / 130 % dmg, multi 70/100, elite 115/120,
boss 130/125). Status-trigger damage is not scaled: write it as seen.

Enemy ids stay as they are (art slots, maps, tests). Display names, intent names and encounter names
change.

## The act's four words

1. **Burdened** (shown as Burdened): every card costs 1 more; paying the surcharge works one stack off.
   Unchanged. The common penalty of the act. Bosses use it sparingly.
2. **The Scale** (replaces Weighed): at the end of your turn the enemy weighs it: Deeds on one pan,
   Workings on the other (Rites do not count). Exactly equal, at least one of each: you gain a
   **Feather** (your next hit deals +5, spent on that hit). Any difference: 1 Burdened.
   The scale is a live status on the enemy: "Scale 2 : 1".
3. **Canopic Jars**: every single normal fight (never a multi-fight) opens with 1–2 jars drawn at random
   from the pool. A jar is its own body (14 HP), does not act, and gives its master a power while it
   stands. A jar falls with its master. The jar's name says its power.
4. **Risen** (Wiedergänger): at its first death it falls (death prevention holds it at 1 HP) and is
   **Downed**: it takes no damage. You keep playing. On its own turn it only rises (40 % HP, all
   debuffs cleansed, its rise buffs) and takes no action; it acts again only after your next turn.
   Status "Rises Again" is visible from the start, "Downed" while it lies. Some rise with a new buff.

### Jar pool (8)

| Son | Jar | Power while it stands |
|---|---|---|
| Imsety | Liver Jar | heals 4 at the end of its turn |
| Imsety | Blood Jar | its attacks deal +3 |
| Hapi | Lung Jar | gains 6 Block every round |
| Hapi | Breath Jar | your first hit on it each turn deals 5 less |
| Duamutef | Stomach Jar | its first attack each turn gives you 1 Burdened |
| Duamutef | Hunger Jar | each Burdened surcharge you pay heals it 3 |
| Qebehsenuef | Gut Jar | a crooked scale gives 2 Burdened instead of 1 |
| Qebehsenuef | Ka Jar | while it stands its master rises once (Risen) |

## Normal enemies (16) — single fights get jars

| id | New name | Rule | Intents (seen) |
|---|---|---|---|
| crocodile_of_the_short_measure | Sobek's Tithe | Its attacks deal +4 for every Burdened you carry. | Weigh Down 14 + 1 Burdened · Jaws 20 · Wallow 18 Block |
| stone_hauler_ushabti | Ushabti Hauler | Every Burdened surcharge you pay is a Stone; its next Stone Blow deals +4 per Stone and spends them. | Haul 18 + 1 Burdened · Stone Blow 22 + stones · Brace 24 Block |
| hungry_grain_thief | Granary Thief | Every hit of its that gets through your Block steals a random card from your hand into its sack; kill it and they come back to your hand. | Snatch 14 + 1 Burdened · Shiv 2×9 · Hide in the Grain 18 Block |
| jar_seal_scarab_swarm | Scarab Swarm | Every hit of its swarm that gets through your Block gives you 1 Burdened. | Swarm 3×5 · Scuttle 14 · Burrow 18 Block |
| cursed_loot_bearer | Tomb Robber | No normal reward: Tomb Gold joins your deck instead. | Gold Sack 16 + 1 Burdened · Pry Bar 22 · Duck 25 Block |
| reed_cord_surveyor | Rope-Stretcher | The Scale. | Lash 16 · Snap Cord 12 + 1 Burdened · Pull Taut 16 Block |
| crooked_rod_bearer | Crooked Cubit | Its scale is crooked: it asks, alternating, for one Deed more or one Working more (shown on its status). | Rod Strike 18 · Bent Measure 13 + 1 Burdened · Brace 17 Block |
| assessor_of_the_forty_two | The Forty-Second Judge | The Scale; three balanced turns in a row and it acquits you and leaves (counts as a kill). | Charge 17 · Question 12 + 1 Burdened · Weigh 18 Block |
| eclipse_scarab | Eclipse Beetle | Every crooked scale moves the eclipse 1; at 3 Black Noon (30 + 2 Burdened) and it starts again. | Scorch 22 · Shadow Shell 24 Block · Black Noon |
| name_erasing_chisel_spirit | The Chiseller | The Scale, and each round it chisels one pan: Deeds or Workings count double. | Chisel 22 · Deface 26 · Stone Dust 24 Block |
| unfinished_mummy | Half-Wrapped Mummy | Risen. | Hook 24 · Unravel 15 + 1 Burdened · Stillness 24 Block |
| natron_bearer | Natron Priest | Risen, and rises with a natron crust: Thorns 3. | Dry Rite 17 · Salt Sting 20 + Doubt 1 · Pack Natron 24 Block |
| fallen_capstone_golem | Pyramidion Golem | Risen; when it rises its next action is always Capstone (30). | Dust Fall 18 + 1 Burdened · Shore Up 25 Block · Capstone 30 |
| silt_buried_farmer_shade | Drowned Farmer | Rises twice, each time with 25 % HP. | Sickle 14 · Mud Pull 18 + 1 Burdened · Raise the Bank 20 Block |
| fourfold_vessel_guardian | Canopic Keeper | Always gets 2 jars; it rises again as long as one of its jars stands. | Body 18 + 1 Burdened · Breath 17 + Panic 1 · Blood 18 + Poison 2 |
| false_door_finder | False-Door Ka | Risen: it slips through the false door and comes back with 40 %, 20 Block and +3 on its attacks. | Knock 17 · Threshold 24 · Stone Door 24 Block |

HP about today's minus 10 % (jars add HP and powers).

**Tomb Gold** (card): costs 0, gain 25 Gold (upgraded 50). No Exhaust (user: looping it in Act IV is
fine). The Tomb Curse is gone.

### Multi-only bodies (no jars in multi-fights)

| id | New name | Rule | Intents |
|---|---|---|---|
| runaway_laborer | Runaway Ushabti | It flees after its third turn; its ally becomes enraged: +4 Strength. Countdown visible. | Swing 13 · Duck Behind 16 Block + 8 Block for its ally |
| jackal_of_the_western_gate | Anubis Jackal | While it lives, an ally that dies rises (Risen, 40 %). | Bite 12 · Drag West 9 + 1 Burdened · Stand Watch 14 Block |
| moon_cycle_ibis | Thoth's Ibis | The Scale; a crooked one gives EVERY enemy 6 Block. | Peck 18 · Ink Mark 12 + 1 Burdened · Wing Shelter 20 Block |

In a multi-fight the Canopic Keeper rises as long as its ally lives (no jars). A Risen body beside the
Jackal rises only once in all.

### Encounter names

Single: The Tithe · Stone for the Dead · Empty Granary · The Swarm · Tomb Gold · Measuring the Field ·
Bent Measure · The Forty-Second Question · Black Noon · Defaced · Half-Wrapped · Salt Rite · The Capstone ·
Silt · Four Jars · The False Door.

Multi: Two Cubits (Rope+Cubit) · Sobek's Share (Sobek+Thief) · Deserters (Runaway+Hauler) ·
The Wrapping Room (Keeper+Mummy) · Robbers' Door (Ka+Robber) · Flood Season (Rope+Farmer) ·
The Last Course (Golem+Runaway) · Salt and Linen (Natron+Mummy) · Anubis Takes Notice (Jackal+Robber) ·
The Door to the West (Jackal+Ka) · The Hall of Two Truths (Ibis+Judge) · Sun and Moon (Beetle+Ibis).

## Elites (11) — their own mechanics, penalty Entombed (at 5 you lose a turn)

| Old body | New | Rule | Intents |
|---|---|---|---|
| rope_master_of_the_corvee | The Taskmaster (~300) | Comes with 2 Ushabti Haulers (~40 each) dragging an obelisk: each round a Hauler lives it moves 1; at 4 it falls on you for 40 and starts again. A killed Hauler is whipped back in after 2 rounds. Obelisk 0/4 and the return countdown visible. | Whip 3×8 · Crack the Whip (Haulers +10 Block) · Stand Over 20 Block |
| colossus_of_the_endless_procession | Colossus of Memnon (~446) | Every third round dawn: it sings, up to 2 of your Burdened become Entombed (the rest stays), plus 10. Countdown visible. | Stride 12 + 1 Burdened · The Foot 22 · Long Shadow 24 Block · Dawn Song |
| keeper_of_the_thirty_six_decans | Coils of Apep (~400) | Each round it coils 1 tighter: 1 card fewer drawn per coil (max 3). 20+ damage to it in one turn loosens 1. | Constrict 18 + 1 coil · Strike 30 · Shed Skin 24 Block, removes its debuffs |
| sphinx_of_the_processional_measure | The Sphinx (~396) | Each round she shows what she wants to hear: Deeds or Workings. Play more of that than of the other: 1 Sign; otherwise 1 Entombed. At 3 Signs she is solved: 2 rounds without Block and +25 % damage taken. | Lion's Paw 34 · Silence 26 + Panic 1 · Stone Riddle 22 Block |
| ammit_devourer_of_the_unworthy | Ammit (~288) | At the end of each of your turns: exactly as many Deeds as Workings is a Feather (kept until spent), anything else 1 Unworthy. At 3 Unworthy she devours: 40, minus 12 per Feather you still hold; the count starts again. | Rend 26 · Weigh the Dead 19 + 1 Burdened · Crouch 20 Block |
| (new body) | The Lioness (~300) | Every time you lose HP she gains 1 Rage (+3 on her attacks). Block one of her attacks completely and she loses all Rage. | Maul 2×9 · Pounce 26 · Prowl 20 Block + 1 Rage |
| surveyor_of_the_errant_cord | Priest of the Four Sons (~285) | All four sons' jars, fixed, elite strength; every 3 rounds he refills a broken one. | Libation 22 · Seal 24 Block · Refill (a jar + 12 Block) |
| scarab_host_of_the_sealed_granary | Brood Mother (~293) | 3 brood jars; every 2 rounds each intact jar hatches a Scarab (10 HP, 3×3). | Black Swarm 5×6 · Spoiled Grain Poison 3 + 1 Burdened · Brood Wall 24 Block |
| mummified_overseer_of_the_linen_house | The Embalmer (~366) | Each round it wraps 1 card in your hand: it costs 0, but playing it gives 1 Entombed. Wraps marked on the card. | Dry Hand 30 · Bind 17 + 1 Burdened · Wrapped 22 Block |
| curse_of_the_opened_tomb | The Restless King (~276) | Rises twice (40 %), each time taking a random jar power from the pool. | Royal Blow 26 · Dust of the Tomb 12 + 1 Entombed · Sealed 20 Block |
| the_tombbreakers_three | The Three Robbers (~120 each) | When one dies the others gain +3 Strength, and one survivor takes the dead one's tool (its signature intent). Kill all three: Tomb Gold joins your deck. | Pry-Bar: Pry 24, Break Seal 14 + 1 Burdened · Lamp: Snuff Panic 1, Knife 2×12 · Curse: Amulet Doubt 1 + 1 Entombed, Idol 25 |

## Bosses (7) — two phases (from 50 %), at most 1 Entombed at once, little Burdened

1. **The Pharaoh** (~800). Each round a command. Fulfilled: his name cracks; 3 cracks break the name: he
   loses his next turn and moves to the next name (3 names = phases; Ward −20 % damage until the third
   breaks). Commands (harder per name), each with its own failure:
   Slay the Traitor (a 25 HP servant; kill it this turn — else it stays and hits for 10 each round) ·
   One Blow (one hit of 15+ — else he strikes back 15) · Silence (at most 2 cards — else 1 Entombed) ·
   Tribute (a 2-cost "Tribute" card in hand; play it — else he heals 30) · Kneel (gain no Block this turn —
   else he gains 25 Block) · Bear My Name ("His Name" curse into the deck for this fight: cost 1, 6 Block;
   no failure). Intents: Golden Sandal 2×14 · Royal Decree 1 Burdened + 20 Block · Smite 30 ·
   All Names Are Mine 40 (third name).
2. **The Weigher** (~780). One scale for the whole fight, pointer −5…+5; each Deed one step to the heart,
   each Working one to the feather; it never resets. End of your turn: exactly 0 = a Feather; heart side
   = the crocodile bites 6 per step; feather side = he gains 8 Block per step. Phase 2: every card moves
   it 2. Intents: Read the Heart 24 · Tip the Scale (pointer 1 step + 16 Block) · Judgement 34.
3. **The Architect** (~820). Builds 2 courses a round; every single hit of 12+ tears one down (15+ in
   phase 2). At 6: Capstone 45 + 1 Entombed, then 3 courses stand. Intents: Stone Sledge 32 ·
   Raise the Ramp 28 Block · Draft More Hands 18 + 2 Burdened · The Angle Is Your Fault 36.
4. **Renenutet** (~760, replaces the Lady). Every turn "Her Bounty" is in your hand (0, vanishes at turn
   end): pay 7 HP, draw 4, gain 1 Energy; she stores 1 Grain. At 7 Grain she harvests: 49, store empties.
   Phase 2: the bounty is pay 7 HP, draw 7, and each is 2 Grain. Intents: Seal the Granary 30 Block ·
   Cobra Strike 28 · Hungry Coils 3×9 · Feed (heals 7 per Grain, spends none).
5. **The First Scribe** (~740). Writes your first 3 cards each turn and reads them back at the end of his
   turn: a Deed hits you for 6, a Working gives him 6 Block; Rites are not written; a Rite erases the last
   entry. Phase 2: the LAST 3 cards. Intents: Reed Through Flesh 30 · Margin of Arrears 18 + 1 Burdened ·
   Seal the Scroll 24 Block.
6. **Selket** (~780, the Mother). Four great canopic jars (~40 each); each round one pours on you in turn:
   2 Burdened · Poison 4 · Panic 1 · 1 Entombed. Broken jars pour nothing. Phase 2 (all jars broken or
   50 %): she wraps herself and is Risen once (40 %, loses debuffs). Intents: Funerary Hook 2×16 ·
   Resin Over the Mouth 24 Block · You Have Already Died 22 + Doubt 1.
7. **Anuket of the Cataract** (~800, the Queen). Nile 1–5, starts at 3. End with 0 Energy: it rises; with
   Energy left: it falls. 1 Drought: she +3 Strength · 2 Low: she loses 12 Block · 3 Ordered: she loses 12
   Block, you +1 Energy next turn · 4 High: you 1 Burdened · 5 Black Flood: her next action is The River
   Takes 40 + 1 Entombed. Phase 2: after each of her turns the level drifts one step away from 3.
   Intents: Harvest 2×18 · Banks Break 32 · Close the Sluice 26 Block.

## Godot

Every counter is a stack on a visible status; jars are bodies whose names say their power; wrapped cards
and the thief's sack are card marks; His Name, Tribute, Her Bounty, Tomb Gold are real cards.

## Order

1. Act vocabulary + normal and multi bodies + jars (tests).
2. Elites (tests). 3. Bosses (tests).
4. RuleTexts, Compendium, BossPhases, LogOnlyStatuses; golden; smoke screens.
5. Encounter trial covers Act IV (EncounterTrial.Acts = [1, 2, 3, 4]); trial smoke.
6. Godot card marks; version bump; commit, push, release to itch.
