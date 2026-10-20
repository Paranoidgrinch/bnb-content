# The Sorcerer's Apprentice — Pool Expansion (v0.5)

> **Extends:** `sorcerers_apprentice_master.md` (v0.4). Everything there stays canon except §0 constraint 1 and §1's
> inventory: the user decided on 20 October 2026 that his shelf matches the Bureaucrat's and the Hedge Witch's in size
> and power ("Sorge dafür, dass sein Power-Level und die Menge an Karten dem von Bureaucrat und Witch gleicht, jedoch
> weiterhin eine eigene Identität hat. Das Gleiche gilt für Relikte.").
> **Why:** the character bench (APPRENTICE_PLAN.md §3) found him level with the others in Act I and far behind from Act
> II on: 38 reward cards, all gated at Act I, and nothing that grows over a fight. Act III bosses (≈750 HP) took ~24
> HP a turn from his best decks against ~150 from the Bureaucrat's Paperwork deck.
> **Numbers:** all BALANCE DRAFT, tuned by the family bench afterwards (§5).

---

## 1. Inventory after the expansion

| Shelf | Act I | Act II | Act III | Act IV | Total |
|---|---:|---:|---:|---:|---:|
| Bureaucrat (reference) | 46 | 14 | 12 | 8 | 80 |
| Hedge Witch (reference) | 51 | 18 | 6 | 5 | 80 |
| **Apprentice v0.4** | 38 | 0 | 0 | 0 | 38 |
| **Apprentice v0.5** | 48 (18 C · 21 U · 9 R) | 16 (2 C · 9 U · 5 R) | 9 (1 C · 3 U · 5 R) | 7 (1 C · 2 U · 4 R) | **80** |

Relics: 9 → **18**, the others' shape: 3 Common · 5 Uncommon · 4 Rare in his own pool, 6 Shop.

## 2. What is new in the rules (three things, no more)

1. **Rote** (positive, on the Apprentice, combat-long, stacks): every damage hit a wand Speak deals is **1 larger per
   stack**, before Heighten — so a Heighten multiplies it too. Played cards are not touched.
2. **Poise** (the same for Block): every Block a wand Speak gives is **1 larger per stack**.
3. **"Whenever you cast"**: a rule may listen for a settled cast (`TriggerEvent.WandCast`). And the wand counts the
   **casts and Speaks of the whole combat**, which a card may read.

Identity check: none of this is a damage-over-time status, a recipe or a queue. Singe stays hit ammunition (new cards
read it, double it or spread it; none makes it tick). The growth lives **in the wand** — the more and the longer he
casts, the stronger the program — which is the character's question ("play it now, or make it a reusable part of a
spell?") with a payoff that finally compounds.

## 3. The 42 new cards

Columns as in master §6–§9. `after the cast` = an after-cast program atom (never copied). `burns` = the card exhausts
when its instruction runs.

### 3.1 Act I — 2 Common, 7 Uncommon, 1 Rare

| ID | Name · type · E · rarity | PLAY | WAND | UPGRADE (PLAY / WAND) |
|---|---|---|---|---|
| `ap_slate_pencil` | **Slate Pencil** · Deed · 1 · C | Deal **8** damage. | `SPEAK`: deal **5** damage, **4** more if you have already cast this turn. | **11** / **7**, 4 more |
| `ap_blotter` | **Blotter** · Working · 1 · C | Gain **8** Block. | `SPEAK`: gain **6** Block; in the first slot cast, also gain **1 Grit**. | **11** / **8** |
| `ap_recitation` | **Recitation** · Working, Exhaust · 1 · U | Gain **2 Rote**. Draw 1 card. | `SPEAK`: after the cast, gain **1 Rote**. | **3 Rote** / also gain **4** Block |
| `ap_steady_hand` | **Steady Hand** · Working, Exhaust · 1 · U | Gain **2 Poise** and **5** Block. | `SPEAK`: gain **4** Block; after the cast, gain **1 Poise**. | **3 Poise**, **7** Block / **6** Block |
| `ap_kindling` | **Kindling** · Deed · 1 · U | Deal **4** damage twice; each hit then applies **1 Singe**. | `SPEAK`: deal **3** damage twice. | **5** twice / **4** twice |
| `ap_ember_tongs` | **Ember Tongs** · Deed · 1 · U | Deal **4** damage, **+3** for each Singe on the target (at most +15). | `SPEAK`: deal **2** damage, **+2** for each Singe on the target (at most +10). | **+4** each / **+3** each |
| `ap_tracing_paper` | **Tracing Paper** · Working · 1 · U | Gain **6** Block. Draw 1 card. | `ECHO`: repeat the last damaging Speak at **100%**; then this card burns. | **9** Block / **125%** |
| `ap_quill_knife` | **Quill Knife** · Deed · 0 · U | Deal **4** damage, **4** more if you inscribed a card this turn. | `SPEAK`: deal **3** damage, **+3** for each empty slot (at most +9). | **6**, 6 more / **4**, +3 each (at most +12) |
| `ap_inkhorn` | **Inkhorn** · Working · 1 · U | Gain **2 Focus**. Draw 1 card. | `SPEAK`: after the cast, gain **1 Focus**. | **3 Focus** / same |
| `ap_copybook` | **Copybook** · Working, Exhaust · 2 · R | At the start of each of your turns, gain **1 Rote** and **1 Poise**. | `SPEAK`: after the cast, gain **2 Rote**; then this card burns. | cost **1** / **3 Rote** |

### 3.2 Act II — 2 Common, 9 Uncommon, 5 Rare

| ID | Name · type · E · rarity | PLAY | WAND | UPGRADE (PLAY / WAND) |
|---|---|---|---|---|
| `ap_sealing_wax` | **Sealing Wax** · Working · 1 · C | Gain **6** Block and **3** Ward Wax. | `SPEAK`: gain **3** Ward Wax, **+1** for each Grit you have (at most +3). | **8** + **4** / **4** |
| `ap_drill_book` | **Drill Book** · Deed · 1 · C | Deal **6** damage, **+2** for each cast this combat (at most +10). | `SPEAK`: deal **3** damage, **+1** for each cast this combat (at most +8). | **8** / **5** |
| `ap_red_pencil` | **Red Pencil** · Deed · 1 · U | Deal **9** damage. | `HEIGHTEN`: the next damaging Speak deals **100%** more; **1** more Exhaustion. | **12** / no extra Exhaustion |
| `ap_bookend` | **Bookend** · Working · 1 · U | Gain **8** Block, **4** more if the wand is full. | `SPEAK`: gain **4** Block, **+2** for each card in the wand. | **11** / **6** |
| `ap_pressed_flower` | **Pressed Flower** · Working · 1 · U | Gain **2 Flow**. Draw 1 card. | `KEEP`: the next defensive Speak is stored at **125%** and goes first in your next cast. | **3 Flow** / **150%** |
| `ap_soot_mark` | **Soot Mark** · Deed · 1 · U | Apply **4 Singe**. | `SPEAK`: apply **2 Singe** to ALL enemies. | **6** / **3** |
| `ap_stone_tablet` | **Stone Tablet** · Working · 1 · U | Gain **2 Grit** and **5** Block. | `SPEAK`: gain **2 Grit**. | **3 Grit**, **7** / **3 Grit** |
| `ap_weathervane` | **Weathervane** · Working · 1 · U | Gain **2 Gust**. Draw 1 card. | `ORDER`: the next two instructions trade places before they run; if that changes which Speak a Heighten reaches, **2** less Exhaustion. | **3 Gust** / **3** less |
| `ap_lectern` | **Lectern** · Working, Exhaust · 2 · U | Gain **3 Rote**. | `SPEAK`: after the cast, gain **2 Rote**; then this card burns. | cost **1** / **3 Rote** |
| `ap_lead_type` | **Lead Type** · Working · 1 · U | Gain **7** Block. Draw 1 card. | `SHAPE—Join`: the next two damaging Speaks become one hit of both together, **+6**. | **10** / **+9** |
| `ap_scorch_mark` | **Scorch Mark** · Deed · 2 · U | Deal **8** damage to ALL enemies. | `SPEAK`: deal **4** damage to ALL enemies. | **11** / **6** |
| `ap_borrowed_grimoire` | **The Borrowed Grimoire** · Working, Exhaust · 2 · R | Whenever you cast this combat, gain **1 Rote**. | `SPEAK`: after the cast, gain **3 Rote**; then this card burns. | cost **1** / **4 Rote** |
| `ap_bell_book` | **Bell, Book and Candle** · Deed · 2 · R | Deal **6** damage 3 times; each hit then applies **1 Singe**. | `SPEAK`: deal **4** damage 3 times. | **8** × 3 / **5** × 3 |
| `ap_long_recitation` | **The Long Recitation** · Working · 2 · R | For each card in the wand, deal **4** damage and gain **3** Block. | `SPEAK`: deal **2** damage for each cast this combat (at most 20). | **5** / **4** each; wand **3** each (at most 30) |
| `ap_warding_primer` | **Warding Primer** · Working, Exhaust · 2 · R | Gain **3 Poise** and **8** Block. | `KEEP—Shield Seal`: the next defensive Speak is stored at **150%** and rises before the first enemy hit this enemy turn. | cost **1** / **200%** |
| `ap_examiners_bell` | **The Examiner's Bell** · Working, Exhaust · 1 · R | At the end of each of your turns in which you cast twice or more, gain **2 Rote**. | `SPEAK`: deal **6** damage, **6** more if you have already cast this turn. | cost **0** / **9**, 6 more |

### 3.3 Act III — 1 Common, 3 Uncommon, 5 Rare

| ID | Name · type · E · rarity | PLAY | WAND | UPGRADE (PLAY / WAND) |
|---|---|---|---|---|
| `ap_moss_poultice` | **Moss Poultice** · Working · 1 · C | Gain **9** Block. Remove 1 stack of a negative status from yourself. | `SPEAK`: gain **6** Block; in the first slot cast, also remove 1 negative stack from yourself. | **12** / **8** |
| `ap_wildfire` | **Wildfire** · Deed · 1 · U | The target gains as much Singe again as it has (at most **8**). | `SPEAK`: the target gains as much Singe again as it has (at most **4**). | at most **12** / **6** |
| `ap_rain_barrel` | **Rain Barrel** · Working · 1 · U | Gain **4** Block, **+3** for each Flow you have. | `SPEAK`: gain **4** Block; after the cast, gain **2 Flow**. | **6**, +3 / **3 Flow** |
| `ap_standing_stone` | **Standing Stone** · Working · 2 · U | Gain **14** Block and **2 Grit**. | `SPEAK`: gain **8** Block, **+2** for each Grit you have. | **18** / **10** |
| `ap_burning_glass` | **Burning Glass** · Working, Exhaust · 2 · R | Whenever you cast this combat, apply **1 Singe** to ALL enemies. | `SPEAK`: apply **3 Singe**; then this card burns. | cost **1** / **5** |
| `ap_masters_hat` | **The Master's Hat** · Working, Exhaust · 2 · R | At the start of each of your turns, gain **1 Focus** and **1 Rote**. | `SPEAK`: after the cast, gain **2 Focus**; then this card burns. | cost **1** / **3 Focus** |
| `ap_thunderclap` | **Thunderclap** · Deed · 2 · R | Deal **5** damage to ALL enemies, **+5** for each cast you made this turn. | `SPEAK`: deal **5** damage to ALL enemies, **+5** for each earlier cast this turn. | **7**, +7 / **7**, +7 |
| `ap_millstone` | **The Millstone** · Working · 2 · R | Gain **6** Block for each Grit you have (at least 6), then lose all Grit. | `SPEAK`: gain **3** Block for each cast this combat (at most 30). | **8** each / **4** each (at most 40) |
| `ap_spring_tide` | **Spring Tide** · Working, Exhaust · 1 · R | Gain **4 Flow** and **2 Poise**. | `SPEAK`: after the cast, gain **2 Flow** and **1 Poise**. | **6 Flow** / **3 Flow** |

### 3.4 Act IV — 1 Common, 2 Uncommon, 4 Rare

| ID | Name · type · E · rarity | PLAY | WAND | UPGRADE (PLAY / WAND) |
|---|---|---|---|---|
| `ap_slide_rule` | **Slide Rule** · Working · 1 · C | Gain **8** Block. If the wand holds 4 or more cards, draw 1 card. | `GROUND`: **2** less Exhaustion, or **4** less when 4 or more instructions run. | **11** / **3** or **5** |
| `ap_compendium` | **Compendium** · Working · 1 · U | Gain **1 Rote** and **1 Poise**. Draw 2 cards. | `ECHO`: repeat the last Speak at **75%**. | draw **3** / **100%** |
| `ap_final_draft` | **Final Draft** · Deed · 2 · U | Deal **14** damage, **+1** for each Rote and Poise you have. | `SPEAK`: deal **8** damage. | **18** / **11** |
| `ap_magnum_opus` | **The Magnum Opus** · Working, Exhaust · 3 · R | Double your Rote and Poise. | `SPEAK`: after the cast, gain **1 Rote** for each card in the wand; then this card burns. | cost **2** / same |
| `ap_sorcerers_staff` | **The Sorcerer's Staff** · Working, Exhaust · 2 · R | The wand has one more slot (6 at most) and a **2** higher Exhaustion cap this combat. Gain **2 Focus**. | `SPEAK`: deal **10** damage. | cost **1** / **14** |
| `ap_unfinished_spell` | **The Unfinished Spell** · Deed · 1 · R | Deal **6** damage, **+6** for each card in the wand. | `SPEAK`: deal **3** damage for each Speak resolved this combat (at most 24). | **8**, +8 / **4** each (at most 32) |
| `ap_apprentices_oath` | **The Apprentice's Oath** · Working, Exhaust · 1 · R | Whenever you cast this combat, deal **3** damage to ALL enemies and gain **3** Block. | `SPEAK`: deal **3** damage to ALL enemies and gain **3** Block. | cost **0** / **5** and **5** |

### 3.5 Coverage after the expansion

- **Growth over a fight (new):** Rote — Recitation, Copybook, Lectern, Borrowed Grimoire, Examiner's Bell, Master's
  Hat, Magnum Opus; Poise — Steady Hand, Warding Primer, Spring Tide; casts/Speaks this combat — Drill Book, Long
  Recitation, Millstone, Unfinished Spell; "whenever you cast" — Borrowed Grimoire, Burning Glass, Apprentice's Oath.
- **Several casts a turn (new decision):** Slate Pencil, Examiner's Bell, Thunderclap.
- **Singe, still ammunition:** Kindling, Ember Tongs (reads it), Soot Mark, Wildfire (doubles it), Burning Glass,
  Bell, Book and Candle.
- **Area damage (thin before):** Scorch Mark, Thunderclap, Apprentice's Oath, Burning Glass (Singe on all).
- **Defence that scales:** Poise, Standing Stone, Millstone, Bookend, Sealing Wax, Rain Barrel.

## 4. The nine new relics

| Rarity | ID / Name | Effect (BD) | Role |
|---|---|---|---|
| Common | `ar_pencil_box` — **Pencil Box** | At the start of each combat, gain **1 Rote**. | The plainest step onto the growth curve. |
| Uncommon | `ar_wire_spectacles` — **Wire Spectacles** | At the start of each combat, gain **1 Poise** and **1 Focus**. | A defended, earlier first cast. |
| Uncommon | `ar_sand_glass` — **Sand Glass** | At the start of your turn, if the wand holds 3 or more cards, gain **1 Focus**. | Rewards keeping a built wand rather than rebuilding it. |
| Uncommon | `ar_pewter_inkwell` — **Pewter Inkwell** | Whenever you cast, gain **2** Block. | Casting as defence; several casts a turn. |
| Rare | `ar_star_chart` — **Star Chart** | The first time you cast each turn, gain **1 Rote**. | Steady growth for any wand. |
| Rare | `ar_tutors_cane` — **Tutor's Cane** | The wand starts each combat with **one more slot**. | Long Lesson without a Spare Notch. |
| Shop | `ar_spare_wicks` — **Spare Wicks** | At the start of each combat, apply **2 Singe** to ALL enemies. | Singe from the first hit. |
| Shop | `ar_slate_board` — **Slate Board** | Whenever you inscribe a card, gain **2** Block. | Safe inscription turns. |
| Shop | `ar_oil_lamp` — **Oil Lamp** | At the end of your turn, if you did not cast, gain **1 Focus**. | A quiet turn is not a wasted one. |

## 5. Tuning procedure

1. Build the pool as above (Act gates 1–4, his own `for:sorcerers_apprentice` pools as before).
2. Family bench (`apprentice_decks.py`, the archetypes of master §13 rebuilt on the expanded pool) against the
   Bureaucrat's Paperwork and plain decks, Acts II and III, with relics.
3. Close the gap with the numbers, in this order: the new growth cards, then the v0.4 cards' wand Speaks, then their
   Play values. Target: his best archetype within ~10 % of Paperwork's health kept, his median archetype within ~25 %,
   and no act where his bosses fall below half the reference's wins.
4. The dice-bot character bench (`tools/bench.sh`) must not show him far ahead in Act I either.
