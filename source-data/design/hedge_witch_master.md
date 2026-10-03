# The Hedge Witch — Master Implementation Canon

> **Project:** Bureaucrats & Broomsticks  
> **Character:** The Hedge Witch  
> **Document purpose:** Naming canon, gameplay specification, content inventory, implementation contract, and first-pass balance placeholders.  
> **Status:** Content/design canon after naming audit. Numerical values explicitly marked **BALANCE DRAFT** are placeholders for later simulation/tuning.

---

## 0. Naming canon

The Hedge Witch should sound like a person from an old rural folk tradition, not like a modern fantasy designer naming abilities.

### Naming rules

Prefer:

- short folk phrases: **Tell the Bees**, **Knock on Wood**, **Bite the Hand**
- ordinary old household objects: **Greasy Ladle**, **Iron Trivet**, **Horn Spoon**
- plants and animals: **Mugwort**, **Hawthorn**, **Adder**, **Badger**, **Magpie**
- old household medicine and work: **Poultice**, **Physic**, **Set the Bone**, **Broth**
- superstition and oral tradition: **Evil Eye**, **Bad Penny**, **Seven Magpies**
- compact uncanny phrases: **Hedge-Thing**, **Bane-Root**, **Hex in the Rafters**

Avoid:

- generic abstract fantasy phrasing: “Malediction”, “Arcane”, “Mystic”, “Eternal”, “Empowered”, etc.
- modern technical/administrative vocabulary for Witch content
- object names that imply technology or domestic design later than c. 1900
- long “AI title” constructions where a shorter folk name works
- repeating “Black / Old / Crooked / Witch’s” mechanically without a concrete image behind it
- names that explain the mechanic instead of naming a thing, act, superstition, or saying

### Tone

The Hedge Witch is practical, slightly uncanny, dry rather than theatrical, and competent in a way that makes outsiders unsure where craft ends and magic begins.

Her cards should feel like:

- things found in hedges, kitchens, sheds, churchyards, fields, and animal pens;
- advice from a grandmother that may or may not actually be magic;
- old superstitions that become literal rules in the BnB world;
- small acts with disproportionate consequences.

---

# 1. Character identity

## 1.1 Core fantasy

The Hedge Witch survives and fights by deciding whether a card is more useful **as itself** or **as an ingredient**.

Her central decision is:

> **Do I play this card now, or is it worth more in the pot?**

She is not a “potion character” in the sense of collecting consumable potions. The cauldron is a persistent in-combat transformation system.

## 1.2 Contrast with the Bureaucrat

**Bureaucrat:** builds procedures, delays, records, queues, paperwork, formal disposal, and administrative inevitability.

**Hedge Witch:** sacrifices immediate possibilities, repurposes cards, cooks mixed effects, uses folk remedies, curses, animals, luck, and practical improvisation.

The Bureaucrat asks:

> “What procedure makes this inevitable?”

The Hedge Witch asks:

> “What can I make from what I have?”

---

# 2. The Cauldron

## 2.1 Native character state

The Cauldron is part of the Hedge Witch character state.

It is **not a relic** and cannot be removed.

It has **3 active ingredient slots**.

## 2.2 Cauldron states

### Sheltering
The cauldron is empty.

The Witch may use it as physical cover. Cards may explicitly gain bonuses while the Cauldron is Sheltering.

### Brewing
At least 1 ingredient is in the Cauldron.

The Sheltering bonus is lost.

### Ready
All 3 ingredient slots are filled.

The Character Action **BREW** becomes available.

### Hot
**OPEN IMPLEMENTATION DECISION.**  
Earlier design work proposed a short post-Brew “Hot” state so the Witch cannot Brew and immediately regain full Sheltering in the same turn. This was never fully locked. Implement behind a flag or defer until combat testing.

## 2.3 Adding ingredients

A card in hand may be placed into the Cauldron instead of being played.

When used as an ingredient:

- its normal card effect does not resolve;
- it is not considered Played;
- it is not Discarded;
- it is not Exhausted;
- it is not Archived;
- it leaves the hand and becomes a Cauldron Ingredient;
- it persists in the Cauldron across turns.

### Ingredient insertion cost — BALANCE DRAFT

- first ingredient added each turn: **0 Energy**
- each further ingredient added that turn: **1 Energy**

This makes slow brewing energy-efficient while allowing expensive one-turn rush brewing.

## 2.4 Brew action

**BREW — 1 Energy** (**BALANCE DRAFT**)

BREW is a **Character Action**, not a card.

Therefore:

- it does not count as a card played;
- card-play-count relics do not count it;
- card-type triggers do not trigger from BREW itself;
- effects produced by the Brew are normal game effects and may trigger generic damage/block/status/heal listeners.

After a normal Brew:

- all 3 ingredient cards move to the discard pile;
- Cauldron slots become empty unless a card/relic says otherwise.

## 2.5 Brew target

If a Brew has enemy-targeting output, select one living enemy when BREW is activated.

All targeted Fang / Hex / Fortune output from that Brew uses that target unless the recipe explicitly says otherwise.

Purely defensive/healing Brews require no enemy target.

## 2.6 Standard resolution order

To keep triggers deterministic:

1. **Hex**
2. **Fang**
3. **Husk**
4. **Hearth**
5. **Fortune**

A Hidden Recipe replaces this standard resolution with its own effect package.

---

# 3. Ingredient families

Every Hedge Witch card has exactly one `cauldron_family`.

General Pool cards are also intended to receive Hedge-Witch-only family metadata; see §16.

## 3.1 Fang

**Identity:** bites, claws, beaks, thorns, teeth, aggressive animals, sudden harm.

**Standard Brew contribution — BALANCE DRAFT:**  
**5 damage**

Fang is deliberately straightforward.

## 3.2 Hex

**Identity:** spoken names, knots, ill wishes, household curses, signs, long-running malice.

**Standard Brew contribution — BALANCE DRAFT:**  
**2 Hexed**

Hex is the Witch’s delayed DoT/Burst family.

## 3.3 Husk

**Identity:** bark, shell, hide, scales, hard coverings, thick growth, shelter.

**Standard Brew contribution — BALANCE DRAFT:**  
**4 Block**

Husk is immediate defense and the main bridge to Ward Wax.

## 3.4 Hearth

**Identity:** food, tea, poultices, midwifery, household medicine, practical care.

**Standard Brew contribution — BALANCE DRAFT:**  
**Heal 1 HP lost during this combat**

Hearth is not the generic “utility” family. Its defining function is care and healing.

### Combat-heal limit

Hearth healing cannot raise the Witch above the HP she had at combat start.

Track:

`combat_start_hp`

Hearth healing may restore HP only up to that value.

Cards/relics may explicitly break this rule.

## 3.5 Fortune

**Identity:** charms, omens, unlucky signs, black cats, spilled salt, knucklebones, bad luck.

**Standard Brew contribution — BALANCE DRAFT:**  
**15% Misfortune**

Fortune is probabilistic control, not raw damage.

---

# 4. Hexed — Threefold Hex

## 4.1 Rule

`Hexed X` is a negative enemy Status.

Each Hexed enemy tracks its own `threefold_step`.

At the end of that enemy’s turn:

- if Hexed > 0, advance `threefold_step` by 1;
- when the step reaches 3, the enemy loses HP equal to **3 × its current Hexed stacks**;
- reset `threefold_step` to 0;
- Hexed stacks do **not** decrease.

Additional Hexed does not reset the counter.

This creates:

- latent buildup;
- predictable burst;
- a strong “third night” timing game;
- similar long-term damage budget to a stable non-decaying per-turn DoT while feeling very different.

## 4.2 Display

Recommended enemy UI:

`Hexed 7  ••○`

or

`Hexed 7 — 2/3`

When the next end-of-turn will trigger the burst, clearly telegraph it.

## 4.3 Removal

If Hexed is reduced to 0, reset `threefold_step` to 0.

If only some Hexed stacks are removed, preserve the step.

Transfer effects explicitly state whether they transfer only stacks or both stacks and step.

## 4.4 Damage semantics

Threefold Hex causes **Status HP loss**, not Attack damage.

It bypasses Block unless BnB’s global status rules say otherwise.

It may trigger generic “enemy loses HP because of a Status effect” listeners.

---

# 5. Misfortune

## 5.1 Rule

Misfortune is a negative enemy Status displayed as a percentage.

Immediately before that enemy’s **next scheduled action** resolves, roll once.

### Success
The entire action fails.

- no Attack damage;
- no Block;
- no buff;
- no debuff;
- no summon;
- no other action payload.

The action is still consumed and the enemy proceeds normally afterward.

### Failure
The enemy action resolves normally.

### After the roll
Remove all Misfortune, whether it succeeded or failed, unless a card/relic explicitly says otherwise.

## 5.2 Internal representation

Recommended implementation:

- 1 internal Misfortune stack = **5 percentage points**
- UI displays percentage, not stack count.

This allows existing generic status-stack manipulation to interact in small increments.

## 5.3 Cap

**Normal cap — BALANCE DRAFT:** 60%

100% is reserved for rare, expensive, once-per-combat, hidden-recipe, or similarly exceptional effects.

---

# 6. Dregs

Junk and Curse-like cards should not become normal Fang/Hex/Husk/Hearth/Fortune ingredients.

When the Hedge Witch puts eligible Junk into the Cauldron, it becomes **Dregs**.

Design identity:

> The Witch can make something from rubbish.  
> The Bureaucrat has the formal disposal machinery.

Dregs do **not** permanently remove Junk.

After Brewing, the original Junk card follows the normal ingredient path to discard.

### Default Dregs effect

**NOT YET LOCKED.**

Implementation should support:

- `cauldron_family = DREGS`
- recipes that explicitly require Dregs;
- relics/cards that let Dregs count as a chosen family.

Do not hard-code a default numerical Dregs contribution until balance/content testing decides it.

---

# 7. Concentrated recipes

Three ingredients of the same family override the additive Mixed Brew with a named Concentrated Brew.

All values below are **BALANCE DRAFTS**.

| Ingredients | Final name | Draft effect |
|---|---|---|
| Fang ×3 | **Red Teeth** | Deal 18 damage |
| Hex ×3 | **Third Night** | Apply 7 Hexed |
| Husk ×3 | **Shell-Wax** | Gain 8 Ward Wax |
| Hearth ×3 | **Hearth Physic** | Heal 4 HP lost this combat |
| Fortune ×3 | **Black Cat’s Luck** | Apply 50% Misfortune |

---

# 8. Named mixed recipes

Mixed recipes are intentionally mechanically predictable.

Unless a Hidden Recipe overrides them, their effect is simply the sum of the three ingredient-family contributions.

The name exists for flavor, Recipe Book readability, discovery, and future content hooks.

## 8.1 Two of one family + one other

| Ingredients | Final name |
|---|---|
| Fang + Fang + Hex | **Snake-Curse** |
| Fang + Fang + Husk | **Thornhide** |
| Fang + Fang + Hearth | **Hunter's Stew** |
| Fang + Fang + Fortune | **Hare's Luck** |
| Hex + Hex + Fang | **Witch-Bite** |
| Hex + Hex + Husk | **Black Bark** |
| Hex + Hex + Hearth | **Bitter Charm** |
| Hex + Hex + Fortune | **Crooked Moon** |
| Husk + Husk + Fang | **Hedgehog Broth** |
| Husk + Husk + Hex | **Bark and Bane** |
| Husk + Husk + Hearth | **Bone Broth** |
| Husk + Husk + Fortune | **Lucky Shell** |
| Hearth + Hearth + Fang | **Red Broth** |
| Hearth + Hearth + Hex | **Fever Tea** |
| Hearth + Hearth + Husk | **Thick Stew** |
| Hearth + Hearth + Fortune | **Lucky Supper** |
| Fortune + Fortune + Fang | **Cat's Claw** |
| Fortune + Fortune + Hex | **Ill Star** |
| Fortune + Fortune + Husk | **Charm Against Harm** |
| Fortune + Fortune + Hearth | **Lucky Tea** |

## 8.2 Three different families

| Ingredients | Final name |
|---|---|
| Fang + Hex + Husk | **Briar Hex** |
| Fang + Hex + Hearth | **Hedge Broth** |
| Fang + Hex + Fortune | **Black Adder** |
| Fang + Husk + Hearth | **Hunter's Pot** |
| Fang + Husk + Fortune | **Fox's Chance** |
| Fang + Hearth + Fortune | **Lucky Hunt** |
| Hex + Husk + Hearth | **Countercharm** |
| Hex + Husk + Fortune | **Crossed Charm** |
| Hex + Hearth + Fortune | **Fever Dream** |
| Husk + Hearth + Fortune | **House Blessing** |

---

# 9. Hidden Recipes

## 9.1 Rule

A Hidden Recipe requires three specific ingredient-card identities.

- upgrade state does not matter;
- ingredient order does not matter;
- before first discovery, UI previews only the ordinary family recipe;
- after Brewing the exact combination, show:

`NEW RECIPE DISCOVERED`

- record it permanently in the Recipe Book;
- discovery unlocks **knowledge**, not permanent raw power;
- the Hidden Recipe **replaces** the normal Brew rather than stacking on top of it.

Hidden Recipes should be inferable from names, folklore, flavor, or obvious associations.

They should not be arbitrary wiki combinations.

## 9.2 Initial Hidden Recipe set

Effects remain concept-first; numbers are later tuning.

### 1. Snakebite Remedy
**Adder's Nip + Nettle Tea + Mugwort Poultice**

Effect:
- heal combat HP;
- remove a negative Status stack/effect;
- gain some Block.

Theme: bite + stinging herb + poultice.

### 2. Everpot
**Taste the Broth + Put the Kettle On + Never Wash the Pot**

Effect:
- after Brewing, keep one ingredient;
- next ingredient that would cost Energy costs 0.

Theme: the pot never truly empties.

### 3. Thirteen Years
**Broken Mirror + Black Cat + Spilled Salt**

Effect:
- high Misfortune;
- if the roll fails, part of the Misfortune remains for the next action.

Theme: stacked household bad-luck signs.

### 4. Knock Three Times
**Third Knock + Third Bell + Crooked Finger**

Effect:
- apply Hexed;
- advance the target’s Threefold Hex by one step.

### 5. The Night Answers
**Third Knock + Thrice-Spoken Name + Call the Third Night**

Effect:
- apply Hexed;
- immediately trigger the target’s Threefold Hex;
- begin a fresh cycle afterward.

### 6. Grandam's Cure
**Honey and Onion + Bitter Tea + Hot Broth**

Effect:
- combat healing;
- Draw;
- cleanse.

A practical household cure, not spectacular magic.

### 7. Bone-Mender
**Set the Bone + Slough Off + Midwife's Hands**

Effect:
- substantial combat heal;
- cleanse;
- defensive aftercare such as Ward Wax.

### 8. Biting Hedge
**Thorn Hedge + Hawthorn Wall + Hedge-Thing**

Effect:
- gain substantial Block;
- attackers take retaliatory damage during the next enemy turn.

### 9. Lid Down
**Pot-Lid + Clamp the Lid + Shut the Lid**

Effect:
- gain Block;
- Cauldron counts as Sheltering until next turn even with ingredients inside;
- Cauldron is closed/frozen during that period.

### 10. Adder's Kiss
**Adder's Nip + Adder in the Sleeve + Two Teeth**

Effect:
- a sequence of Fang hits;
- final hit may be larger.

### 11. Crow Breakfast
**Crow's Peck + Murder of Crows + Familiar's Supper**

Effect:
- multiple distributed hits;
- killing an enemy grants a Cauldron-tempo reward.

### 12. Tell the Magpies
**Magpie's Luck + Seven Magpies + Tell the Bees**

Effect:
- spread Misfortune across enemies;
- successful Misfortune grants a small tempo/reward effect.

### 13. Nine Lives
**Black Cat + Ninth Life + The Black Cat Sat Down**

Effect:
- extremely strong next Misfortune;
- some bad luck remains after a successful cancellation.

### 14. Against the Evil Eye
**Evil Eye + Knock on Wood + Horseshoe Over the Door**

Effect:
- remove a negative Status from the Witch;
- convert/reflect that protection into Misfortune on an enemy.

### 15. Sour Supper
**Sour the Milk + Honey and Onion + Proper Supper**

Effect:
- small combat heal for the Witch;
- apply Hexed to the enemy.

### 16. Hedge Physic
**Nettle Tea + Comfrey Poultice + Honey and Onion**

Effect:
- strong combat healing;
- remove multiple small negative Status stacks/effects.

### 17. Winter Coat
**Oakskin + Winter Bark + Birch-Bark Wrap**

Effect:
- large Block;
- convert/preserve part of it as Ward Wax.

### 18. Slow as Stone
**Snail Shell + Snail's Patience + Horn Spoon**

Effect:
- durable Block/Ward Wax over more than one turn.

### 19. Grave Hex
**Old Grudge + Last-Breath Hex + Bane-Root**

Effect:
- apply Hexed;
- if the target dies, transfer most/all Hexed and the Threefold step to another enemy.

### 20. Beggar's Pot
**Stone Soup + Dregs + Dregs**

Effect:
- turn rubbish into a useful defensive/healing Brew;
- does not permanently remove the original Junk cards.

---

# 10. Starter deck

Recommended 10-card starter deck.

All numerical values are **BALANCE DRAFTS**.

| Qty | Card | Cost | Type | Family | Base effect | Upgrade |
|---:|---|---:|---|---|---|---|
| 4 | **Adder's Nip** | 1 | Deed | Fang | Deal 6 damage | Deal 9 damage |
| 4 | **Pot-Lid** | 1 | Working | Husk | Gain 5 Block. If Cauldron is Sheltering, +3 Block | 7 Block; +3 if Sheltering |
| 1 | **Crooked Finger** | 1 | Working | Hex | Apply 4 Hexed | Apply 5 Hexed |
| 1 | **Nettle Tea** | 1 | Working | Hearth | Gain 4 Block. Heal 1 combat HP | Gain 6 Block. Heal 1 |

Fortune is intentionally absent from the starter deck so early survival is not dependent on RNG.

---

# 11. Common cards — 20

Commons teach the core language of the Witch.

They should be readable, useful, and mostly avoid rule-bending.

## Fang — 4

### Bramble Switch
**1E — Deed — Fang**

Deal moderate damage. Deal a little more if the target is Hexed.

**Upgrade:** more base and conditional damage.

**Role:** simple Fang/Hex bridge.

### Two Teeth
**1E — Deed — Fang**

Deal damage twice.

**Upgrade:** stronger hits.

**Role:** basic multi-hit access.

### Crow's Peck
**0E — Deed — Fang**

Deal light damage. Deal more if target is at or below half HP.

**Upgrade:** stronger base/execute damage.

**Role:** 0-cost Fang, finishing utility.

### Briar Sweep
**2E — Deed — Fang**

Deal damage to ALL enemies.

**Upgrade:** more damage.

**Role:** basic Fang AoE.

## Hex — 4

### Evil Eye
**1E — Working — Hex**

Deal light damage. Apply Hexed.

**Upgrade:** more damage and/or Hexed.

### Muttered Name
**1E — Working — Hex**

Apply Hexed to ALL enemies.

**Upgrade:** more Hexed.

### Third Knock
**1E — Working — Hex**

Apply Hexed. If this enemy’s Hex triggers this turn, apply additional Hexed.

**Upgrade:** more Hexed.

**Role:** teaches third-night timing.

### Old Grudge
**2E — Working — Hex**

Apply a large amount of Hexed.

**Upgrade:** more Hexed.

**Role:** simple heavy Hex setup.

## Husk — 4

### Birch-Bark Wrap
**1E — Working — Husk**

Gain solid Block.

**Upgrade:** more Block.

### Snail Shell
**1E — Working — Husk**

Gain Block. If you take no unblocked Attack damage during the enemy turn, gain Ward Wax.

**Upgrade:** more Block/Wax.

### Thorn Hedge
**1E — Working — Husk**

Gain Block. The first time you are attacked this enemy turn, damage the attacker.

**Upgrade:** more Block/retaliation.

### Shed Skin
**1E — Working — Husk**

Gain light Block. Remove 1 stack of a negative Status from yourself.

**Upgrade:** more Block.

## Hearth — 4

### Mugwort Poultice
**1E — Working, Exhaust — Hearth**

Heal combat HP.

**Upgrade:** more combat healing.

### Hot Broth
**1E — Working — Hearth**

Gain Block. If you have lost HP this combat, heal 1 combat HP.

**Upgrade:** more Block.

### Bitter Tea
**1E — Working — Hearth**

Draw 1. Heal 1 combat HP.

**Upgrade:** Draw 2, heal 1.

### Set the Bone
**2E — Working — Hearth**

Gain substantial Block. Heal combat HP.

**Upgrade:** more Block.

## Fortune — 4

### Black Cat
**1E — Deed — Fortune**

Deal light damage. Apply Misfortune.

**Upgrade:** more damage/Misfortune.

### Spilled Salt
**1E — Working — Fortune**

Gain light Block. Apply Misfortune.

**Upgrade:** more Block/Misfortune.

### Crooked Horseshoe
**1E — Working — Fortune**

Apply a meaningful amount of Misfortune.

**Upgrade:** more Misfortune.

### Knock on Wood
**0E — Working — Fortune**

Gain light Block. Gain more if any enemy has Misfortune.

**Upgrade:** more Block.

---

# 12. Uncommon cards — 35

Uncommons introduce build direction, Cauldron handling, timing manipulation, and limited rule bending.

## Fang — 7

### Badger's Temper
Attack. If the target intends to Attack, make a second smaller hit.

### Adder in the Sleeve
Retain. Becomes stronger or cheaper the longer it remains in hand.

### Murder of Crows
Several small hits that may be distributed among enemies.

### Hawthorn Switch
Deal damage. If the target is Hexed, advance its Threefold step by 1.

### Bite the Hand
Heavy damage against enemies with positive Statuses/buffs.

### Familiar's Supper
Attack. If it kills, the next Ingredient this turn that would cost Energy costs 0.

### Hedge-Thing
Attack that becomes stronger while the Cauldron is Sheltering.

## Hex — 7

### Thrice-Spoken Name
Apply Hexed. If the target is already at 2/3, apply more.

### Charm Backwards
Apply substantially more Hexed, but delay the target’s next Hex trigger by one enemy turn.

### Dead Man's Hex
Apply Hexed. If the target dies, transfer part of that Hexed to another enemy.

### Sour the Milk
Apply Hexed plus a small existing negative Status/debuff.

### Nail in the Doorpost
**Rite.** First time each turn you apply Hexed to one enemy, apply a smaller amount to another enemy.

### Third Bell
If the target’s Hex triggers this turn, its burst creates an additional effect, such as spreading some Hexed.

### Knotted Cord
Weak/modest normal effect.

**When Brewed:** counts as two Hex ingredients while occupying one Cauldron slot.

This is an explicit special-ingredient card.

## Husk — 7

### Oakskin
Gain substantial Block. Gain more while the Cauldron is Brewing.

### Clamp the Lid
Gain Block. If the Cauldron is Sheltering, preserve part of the defense into next turn / as Ward Wax.

### Hedgehog Curl
Gain Block. Each time you are hit this enemy turn, retaliate.

### Slough Off
Gain Block and remove a negative Status stack. Successful removal grants a small additional reward.

### Snail's Patience
Gain Block now and again next turn if you played few cards this turn.

### Hawthorn Wall
Gain strong defense. Attackers suffer retaliation or a small negative Status.

### Horn Spoon
Defensive card.

**When Brewed:** its Husk contribution additionally produces a small amount of Ward Wax.

## Hearth — 7

### Comfrey Poultice
Heal combat HP. Heals more when the Witch is badly injured.

### Honey and Onion
Small combat heal plus negative-Status removal.

### Proper Supper
Defensive immediate effect plus delayed combat healing next turn.

### Midwife's Hands
**Rite, Exhaust.** First time per combat the Witch falls below a low-HP threshold, restore some combat HP.

### Tell the Bees
When an enemy dies, gain a small heal/sustain reward, limited per turn/combat.

### Taste the Broth
Return one Ingredient from the Cauldron to hand. Gain a small heal.

### Put the Kettle On
Small heal/Block. Next Ingredient this turn that would cost Energy costs 0.

## Fortune — 7

### Cross Your Fingers
Apply Misfortune. If the roll fails, gain a small defensive consolation afterward.

### Bad Penny
Apply Misfortune. If it fails, part of it remains for the following enemy action.

### Cast the Knucklebones
Choose between a smaller reliable Misfortune application or a larger more variable one.

### Horseshoe Over the Door
**Rite.** First time each turn you apply Misfortune, increase it slightly.

### Magpie's Luck
When Misfortune successfully cancels an enemy action, gain a tempo reward such as Draw/Energy.

### Broken Mirror
Apply strong Misfortune at the cost of creating Junk/Dregs pressure.

### Seven Magpies
Spread Misfortune over multiple enemies; against a lone enemy, concentrate part of the value instead of becoming dead.

---

# 13. Rare cards — 25

Rares are allowed to bend the character rules and define whole runs.

## Fang — 5

### Wolf at the Door
Large Retain attack. Becomes cheaper as it waits in hand.

### Carrion Flight
Long sequence of small hits. If an enemy dies mid-sequence, remaining hits move to another enemy.

### Teeth in the Dark
Attack that scales with the number of Ingredients currently in the Cauldron.

### Turnskin
Transform one Husk Ingredient already in the Cauldron into Fang, then attack.

### The Hare Runs Last
Attack. If the target’s Hex triggers this enemy turn, repeat the attack after the Hex burst.

## Hex — 5

### Call the Third Night
Immediately trigger the target’s current Threefold Hex, then begin a fresh cycle.

### Name Written Backwards
Greatly increase/double the target’s Hexed, but reset its Threefold step to 0/3.

### Hex in the Rafters
**Rite.** First Hex burst each enemy turn spreads Hexed to other enemies.

### Last-Breath Hex
Apply/mark a curse so that when the target dies, its Hexed and current Threefold step transfer to another enemy.

### Bane-Root
**Rite.** After Hex triggers on an enemy, some Hexed grows back / is reapplied.

## Husk — 5

### Shut the Lid
Until next turn, the Cauldron counts as Sheltering even while holding Ingredients.

During this protection, the Cauldron cannot accept Ingredients or Brew.

### Scar-Bark
Gain Block based on unblocked HP lost during the previous enemy turn.

### Full Pot
Gain Block for each Ingredient in the Cauldron; gain a larger bonus if it is Ready.

### Winter Bark
Gain heavy Block. Convert/preserve a limited amount of unused Block as Ward Wax.

### Ironwood
**Rite.** First time each turn an Attack fully breaks your Block, regain some Block/Ward Wax.

## Hearth — 5

### Granny's Physic
Remove several/all negative Statuses from the Witch and heal combat HP based on what was treated.

### Stone Soup
Choose a Dregs/Junk card in hand. For this Brew, it may count as a chosen Ingredient family.

### Keep the Drippings
**Rite.** Combat healing that would be wasted at the combat-heal cap becomes defensive value, preferably Ward Wax.

### Never Wash the Pot
**Rite.** After a Brew, choose one of its Ingredients to remain in the Cauldron.

### For What Ails You
Flexible practical cure. Choose from a set such as combat heal, cleanse, Draw, or reduced next Ingredient cost.

## Fortune — 5

### Loaded Knucklebones
The next Misfortune roll is rolled twice; either success cancels the action.

### Seven Years' Bad Luck
If Misfortune fails, a meaningful portion remains for the next enemy action.

### Ninth Life
**Rite.** After the first successful Misfortune each combat, leave behind a small amount of Misfortune.

### Borrowed Luck
Remove Misfortune from an enemy to cash its probability out into guaranteed value such as Block, Energy, or Draw.

### The Black Cat Sat Down
Expensive/Exhausting signature effect. Make the next action of an enemy effectively 100% Misfortune, with a real price such as HP loss or Dregs/Junk.

---

# 14. Hedge Witch relics — 18

Distribution mirrors the Bureaucrat character-relic structure:

- 3 Common
- 5 Uncommon
- 4 Rare
- 6 Shop

## Common — 3

### Rowan Pin
First time each turn a card enters the Cauldron as an Ingredient, gain a small amount of Block.

### Three-Knot Cord
First time each combat a Threefold Hex triggers, leave/apply a small amount of Hexed afterward.

### Found Button
First time each combat Misfortune fails, gain a small consolation, preferably Block.

## Uncommon — 5

### Greasy Ladle
First BREW each turn draws 1 card.

### Iron Trivet
If you end your turn with a Ready Cauldron, gain Block or Ward Wax.

### Crow's Toe
Your first direct attack each turn against an enemy at Hex step 2/3 gains bonus damage.

### Beeswax Cup
First time each turn you heal combat HP, gain a small amount of Ward Wax.

### Knucklebone Pair
First time each turn Misfortune fails, a small part of its percentage remains instead of vanishing.

## Rare — 4

### False-Bottom Pot
Add one **Reserve Ingredient slot**.

The Reserve slot does not count toward the current 3-ingredient recipe.

After Brewing, the Reserve Ingredient moves into the first normal slot of the new Cauldron.

### First Bell
The first enemy you Hex each combat begins its Threefold cycle one step ahead.

### Black Cat's Collar
The first failed Misfortune roll each combat is rerolled once.

### Grandam's Ember
Once per combat, some Hearth healing may exceed `combat_start_hp` and restore HP lost before the current combat.

## Shop — 6

### Bone Strainer
Dregs may count as any chosen family for Mixed Recipes.

Dregs may **not** create a Concentrated Recipe.

### Apothecary's Scale
First Brew each combat containing three different families costs 0 Energy.

### Yesterday's Jar
After the first Brew each combat, the last Ingredient added remains in the Cauldron.

### Cat's-Eye Coin
Successful Misfortune grants Gold, capped per combat.

### Herb-Wife's Rack
First Brew each combat containing at least one Hearth Ingredient gains additional care/healing value.

### Black Spoon
First time each combat you Brew an already-discovered Hidden Recipe, gain a small additional reward.

This relic never reveals undiscovered recipes.

---

# 15. Card / Cauldron event semantics

These interactions should be consistent in the engine.

## Ingredient placement

Putting a card into the Cauldron:

- **does** count as “leaves hand without being played”;
- **does not** count as Played;
- **does not** trigger card-count relics;
- **does not** trigger Deed/Working/Rite “on play” effects;
- **does not** count as Exhaust;
- **does not** count as Archive.

Therefore, shared effects analogous to `Conservator's Thread` should trigger, while “every Nth card played” effects should not.

## BREW

BREW itself:

- is not a card;
- has no card type;
- does not increment cards played;
- can still produce ordinary Damage, Block, Ward Wax, healing, and Status applications.

## Generic triggers

Brew-generated:

- Block triggers “when you gain Block” listeners;
- Status applications trigger normal negative-status listeners;
- Hex HP loss counts as Status-caused HP loss;
- Misfortune is a normal negative Status for generic manipulation unless explicitly excluded;
- healing counts as healing for relic/card listeners.

## Special ingredient text

Only a minority of cards should have special Ingredient behavior.

Target density:

**~15–20% of Witch cards maximum.**

Examples already in canon:

- **Knotted Cord** — counts as two Hex ingredients in one slot.
- **Horn Spoon** — Husk ingredient adds a Ward Wax rider.

Rare effects may further bend this.

---

# 16. General Pool integration

The Hedge Witch must remain fully compatible with the shared General Pool.

## 16.1 Required metadata

Every General Pool card should receive Hedge-Witch-only metadata:

`cauldron_family: FANG | HEX | HUSK | HEARTH | FORTUNE`

This metadata has no effect for the Bureaucrat.

## 16.2 Mapping principle

Do not map purely by card title.

Prefer its actual gameplay identity:

- direct damage / aggressive execution → Fang
- curses / hostile status engines / delayed harmful effects → Hex
- Block / Ward Wax / protection → Husk
- recovery / cleanse / draw / practical utility → Hearth
- denial / prohibition / Censure / probability-like disruption / odd causality → Fortune

Exact mapping of all 50 General Pool cards is a **required implementation follow-up** and was not previously finalized in design discussion.

Do not silently infer the mapping in code.

## 16.3 Fight cards

Fight-specific cards are **not cookable by default** in v1.

Enable case-by-case only where explicitly authored.

---

# 17. Recipe Book UI

Recommended sections:

## Everyday Brewing
All 35 family recipes.

Visible from the start.

Shows:

- ingredient-family icons;
- recipe name;
- current standard effect;
- concentrated variants.

## Things That Worked Once
Discovered Hidden Recipes.

Shows:

- exact three card names;
- Hidden Recipe name;
- effect;
- discovery marker/date/run if desired.

## Notes in the Margin
Optional clue system for undiscovered Hidden Recipes.

Undiscovered recipes should never reveal exact combinations by default.

Good clue style:

> “For a bite, something that stings and something that soothes.”

Bad clue style:

> “Use Adder's Nip + Nettle Tea + Mugwort Poultice.”

Meta-progression should unlock **knowledge**, not permanent damage bonuses.

---

# 18. Combat UI / VFX

## Cauldron

### Empty / Sheltering
- lid down;
- Witch can visibly shelter behind/beside it.

### 1 Ingredient
- lid open;
- faint steam;
- one ingredient icon.

### 2 Ingredients
- stronger bubbling;
- two visible ingredient icons.

### 3 Ingredients / Ready
- lid rattles;
- BREW action pulses;
- preview shows normal recipe unless a previously discovered Hidden Recipe applies.

### Hidden Recipe undiscovered
Do **not** spoil it in preview.

Show ordinary expected family recipe.

On Brew:
- brief interruption;
- `NEW RECIPE DISCOVERED`;
- resolve hidden effect;
- add to Recipe Book.

## Hexed
Show stacks and Threefold progress together.

## Misfortune
Show direct percentage near enemy intent/action.

Avoid making the player convert internal 5% stacks mentally.

---

# 19. Content distribution

Hedge Witch character content mirrors Bureaucrat reward density:

## Cards — 84 definitions

- 4 Starter definitions
- 20 Common
- 35 Uncommon
- 25 Rare

Starter deck uses 10 cards from the 4 Starter definitions.

## Character relics — 18

- 3 Common
- 5 Uncommon
- 4 Rare
- 6 Shop

---

# 20. Archetype philosophy

Ingredient families are **not** intended to become five isolated deck colors.

Desired cross-family/run identities include:

### Sheltering Witch
Keeps the Cauldron empty longer and exploits Sheltering, Husk, Retain, and low-card-count play.

### Brewer
Uses Cauldron manipulation, Ingredient-cost reductions, retained ingredients, Reserve slots, and frequent Brews.

### Threefold Witch
Builds Hex and manipulates the third-night timing.

### Fortune Witch
Manipulates Misfortune probability, consolation value, and probability cash-out.

### Hedge Doctor
Uses Hearth, Ward Wax bridges, cleansing, and controlled combat healing.

### Beast / Thorn Witch
Uses Fang, animals, multi-hit attacks, retaliation, and aggressive finishers.

### Scavenger
Turns Dregs and awkward cards into recipes without becoming a permanent Junk-removal character.

These are directions, not hard classes.

---

# 21. Naming audit — major changes

The following earlier working names were deliberately replaced because they were generic, overly abstract, duplicated, too “fantasy-generator”, or unnecessarily long.

| Working name | Final name | Reason |
|---|---|---|
| Cauldron Block | **Pot-Lid** | concrete household image |
| Bramble Lash | **Bramble Switch** | older rural object/action |
| Two-Fanged Bite | **Two Teeth** | shorter, less generated |
| Black Cat Crossing | **Black Cat** | removes explanatory phrase |
| Something in the Hedge | **Hedge-Thing** | more folk-horror, less sentence-title |
| Hang the Charm Backwards | **Charm Backwards** | compact |
| Passing Curse | **Dead Man's Hex** | concrete death-transfer image |
| Bell at Midnight | **Third Bell** | ties directly to Threefold motif |
| Old Oak Skin | **Oakskin** | compact |
| Iron Kettle Lid | **Clamp the Lid** | action phrase |
| Shed the Old Skin | **Slough Off** | avoids duplicate with Shed Skin |
| Tortoiseshell Spoon | **Horn Spoon** | older, plainer household material |
| Murder of Crows (Rare duplicate) | **Carrion Flight** | removes duplicate |
| Turn the Hide | **Turnskin** | folk-shapeshifting flavor |
| The Third Night Comes Early | **Call the Third Night** | shorter and active |
| The House Is Cursed | **Hex in the Rafters** | concrete domestic image |
| Last Words at the Gate | **Last-Breath Hex** | clear folk-cursing image |
| A Curse With Roots | **Bane-Root** | compact plant/curse fusion |
| Bark That Remembers | **Scar-Bark** | concrete consequence |
| Cauldron Belly | **Full Pot** | plain folk phrase |
| Never Crack | **Ironwood** | material/plant image |
| Granny's Cure-All | **Granny's Physic** | period-appropriate medicine word |
| Never Waste Broth | **Keep the Drippings** | household action |
| A Remedy for Everything | **For What Ails You** | oral/folk idiom |
| Black Cat's Ninth Life | **Ninth Life** | compact |
| Lucky Button | **Found Button** | less generic |
| Crow's Toe Charm | **Crow's Toe** | shorter |
| False-Bottomed Cauldron | **False-Bottom Pot** | shorter |
| Midnight Cuckoo | **First Bell** | removes clock-like domestic association |
| Stone from the Old Hearth | **Grandam's Ember** | stronger folk-healing image |
| Bottle of Yesterday's Broth | **Yesterday's Jar** | shorter, older object-language |
| Blackened Recipe Spoon | **Black Spoon** | compact |
| Red-Tooth Broth | **Red Teeth** | less generic recipe-name construction |
| Third-Night Draught | **Third Night** | shorter |
| Old Hearth Tonic | **Hearth Physic** | removes generic “tonic” title |
| Viper's Malediction | **Snake-Curse** | removes abstract fantasy diction |
| Witchbite Decoction | **Witch-Bite** | removes “decoction” padding |
| Barkbound Curse | **Black Bark** | shorter and concrete |
| Bitter Countercharm | **Bitter Charm** | compact |
| Crooked Moon Draught | **Crooked Moon** | removes generic suffix |
| Warded Malediction | **Bark and Bane** | removes abstract fantasy language |
| Mender's Broth | **Bone Broth** | concrete household/medical association |
| Ill-Star Draught | **Ill Star** | compact archaic omen |
| Good-Luck Tea | **Lucky Tea** | plainer speech |
| Thorned Curse | **Briar Hex** | more concrete |
| Hedgewitch's Draught | **Hedge Broth** | removes title-like fantasy construction |
| Black Adder's Omen | **Black Adder** | shorter |
| Hunter's Broth | **Hunter's Pot** | household object rather than generic brew suffix |
| Hedge Bet | **Fox's Chance** | removes modern pun |
| Countercharm Decoction | **Countercharm** | removes unnecessary suffix |
| Warded Omen | **Crossed Charm** | more tangible superstition |
| Perpetual Stew | **Everpot** | folk-compound rather than abstract adjective |
| Grandmother's Cold Cure | **Grandam's Cure** | less modern phrasing |
| Whole Again | **Bone-Mender** | concrete craft image |
| Hedge That Bites | **Biting Hedge** | compact |
| Tight-Fitting Lid | **Lid Down** | household command |
| Murderer's Breakfast | **Crow Breakfast** | less melodramatic |
| A Secret Told to Magpies | **Tell the Magpies** | oral folk phrase |
| Nine Lives' Worth | **Nine Lives** | compact |
| Old Wives' Remedy | **Hedge Physic** | avoids generic phrase, strengthens period tone |
| Winter Shelter | **Winter Coat** | compact household metaphor |
| The Curse Outlives You | **Grave Hex** | compact |
| Actual Stone Soup | **Beggar's Pot** | removes meta-joke / modern emphasis |

---

# 22. Implementation data model

Recommended content metadata.

## Card

```yaml
id: string
name: string
rarity: starter|common|uncommon|rare
cost: int
types: [Deed|Working|Rite|Exhaust|Retain|...]
cauldron_family: FANG|HEX|HUSK|HEARTH|FORTUNE
effects: [...]
upgrade:
  effects: [...]
ingredient_override: null | {...}
```

## Cauldron

```yaml
slots:
  - card_ref|null
  - card_ref|null
  - card_ref|null

reserve_slot: card_ref|null   # only enabled by False-Bottom Pot

ingredients_added_this_turn: int
state: SHELTERING|BREWING|READY|HOT
```

## Enemy Hex state

```yaml
hexed_stacks: int
threefold_step: 0|1|2
```

## Enemy Misfortune state

```yaml
misfortune_percent: int
```

or internal:

```yaml
misfortune_stacks: int  # 1 = 5%
```

## Recipe

```yaml
id: string
name: string
kind: MIXED|CONCENTRATED|HIDDEN
family_multiset: [...]
specific_card_ids: [...]   # hidden only
effect_package: [...]
discovered: bool           # hidden only / profile knowledge
```

---

# 23. Balance placeholders that must NOT be treated as final

The following are design anchors only:

- first Ingredient/turn free
- later Ingredients/turn cost 1
- BREW costs 1
- Fang Ingredient = 5 damage
- Hex Ingredient = 2 Hexed
- Husk Ingredient = 4 Block
- Hearth Ingredient = heal 1 combat HP
- Fortune Ingredient = 15% Misfortune
- Fang×3 = 18 damage
- Hex×3 = 7 Hexed
- Husk×3 = 8 Ward Wax
- Hearth×3 = heal 4
- Fortune×3 = 50% Misfortune
- normal Misfortune cap = 60%

Do not freeze these into long-term content contracts until simulation.

The **mechanics and identities** are canon; the numbers are not.

---

# 24. Required follow-up before production lock

1. Map all 50 General Pool cards to Ingredient families.
2. Decide final Dregs default Brew behavior.
3. Decide whether the post-Brew Hot state stays.
4. Put numerical values on all Uncommon/Rare concepts.
5. Simulate starter + Common reward pool in `bnb-runs`.
6. Stress-test:
   - Threefold timing acceleration
   - repeated Misfortune denial
   - Hearth stall-healing
   - Cauldron energy compression
   - ingredient retention loops
   - Dregs exploitation
7. Verify shared relic interactions in RogueDeck-Core event semantics.
8. Build Recipe Book and Cauldron preview in Godot.
9. Add hidden-recipe discovery persistence to profile/meta knowledge.
10. Run a second naming pass only after card art/flavor text exists; names should remain grounded in what is actually depicted.

---

# 25. One-sentence character pitch

> **The Hedge Witch turns cards into ingredients, hides behind the pot when she is not cooking, and wins by deciding when a bite, a curse, a cure, a shell, or a little bad luck is worth more in the hand than in the broth.**
