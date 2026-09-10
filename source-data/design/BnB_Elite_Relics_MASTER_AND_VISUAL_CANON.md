# Bureaucrats and Broomsticks
## ELITE RELICS — Master and Visual Design Canon

**Status:** content-final, implemented 2026-09-10.
**Extends:** `BnB_Final_Relics_Master_PostAudit.md` (mechanics) and
`BnB_Final_Relics_Master_PostAudit_VISUAL_DESIGN_CANON.md` (imagery).
**Scope:** 38 Elite relics + 1 Mimic relic in 4 grades = **42**, numbered **169–210**.
**Renumbers nothing.** The canon's 1–168 keep their numbers, their pools and their briefs.

> **Why this pool exists.** Until 2026-09-10 every Elite, Boss and Mimic victory in Acts I–IV offered a
> relic pool of 49 entries, of which **47 were ported v2 demo relics** and 2 were canonical by id collision;
> a treasure chest drew from the same bag. The 50 authored Normal relics reached a player only through
> events, programs and shops. Rather than point the elite faucet at the Normal pool and be done, the elite
> layer got a pool of its own: **an elite is a body with a lesson, and the relic is the lesson.**

---

# 1. Acquisition

- **38 Elite relics**, one per elite encounter in Acts I–IV (Act I 10 · II 9 · III 9 · IV 10).
- Defeating an elite awards **its** relic. **Fixed, not random, and no choice screen** — a boss gives a
  forced 1-of-3 because an act's final examination should not be solved twice the same way; an elite gives
  the one relic written for it.
- An Elite relic **never** appears in a shop, a chest, an event or a normal reward. The same wall §1 puts
  around Boss relics.
- The map never repeats an elite encounter within a run, so no relic can be won twice.
- **Act V awards no relics**, elite or otherwise. It has no elites.

## The Mimic — one relic, four grades

- A mimic is rare (5 / 10 / 15 / 20 % of treasures across the acts), so its prize is worth the surprise —
  and rare enough that the same prize twice would be waste.
- There is **one tooth in four grades**, one per act. Beating a later act's mimic hands over the higher grade
  and **removes the one already held**: a player never carries two.
- The removal rides on the relic's own pickup effects, so every site that grants it carries the removal too;
  there is no "and also remove the old one" for a grant site to forget.

## Power band

Above a Rare Normal relic, below a Boss relic. Each is **one clean trigger** — an elite fight is long enough
already, and a relic that has to be tracked turn by turn is a second fight.

## Voice

Each speaks its own act's vocabulary: Act I **Paperwork · Doubt · Seal/Ratified · Archive** · Act II
**Overdue · Misfiled · Referenced · Redacted** · Act III **Safe-Conduct · Trespass · Claim · Wergild** ·
Act IV **Weighed · Burdened · Inscribed · Entombed · Embalmed**. An Act-III relic that named none of its
act's four words would be an Act-I relic found in a hedge.

## No sting

Where an elite's mechanic was a cost the player was forced to pay, its relic turns that same cost into a
resource. None of these punishes the player for having won.

---

# 2. Visual identity — the Elite frame

The Elite frame is **a plainer boss frame**, and that relationship is the whole design: recognisable as the
same family from across a shelf, unmistakably the lesser rank up close.

| | Boss (#100–168) | **Elite (#169–210)** |
|---|---|---|
| Frame | double frame, epic | **one** antique-gold line |
| Ground | dark purple | the same purple, run **much darker — near black** (`#1C0D2C`) |
| Inner window | ivory | **pewter** |
| Crest | boss-family crest | none |
| Gold | antique gold | antique gold, **one step down** (`#A8861D`) so a frame does not compete with the UI accent |

Everything in §10.1–10.3 and §10.5–10.6 of the visual canon applies unchanged: fully analog materials, late
18th–19th century at the latest (Act IV may be far older and Egyptian), silhouette first, one signature twist
per relic, black outline primary, **maximum five visible colours including the frame**, no animation, no
characters unless unavoidable, no text as the primary identity, and the catalogue label beneath the icon.

**The mimic's four grades share one silhouette and grow:** each grade brings more of the chest lid with it.
They must read as **one object at four sizes**, never as four different relics.

⚠ **One consequence of the new UI palette:** the frontend's accent is now antique gold, so **gold no longer
marks a Boss relic by itself**. The dark purple ground and the ivory window carry that distinction — which is
why the Elite frame's purple is darker and its gold is dimmer than the Boss frame's, and why the Elite window
is pewter and not ivory.

---

# 3. Naming rule, learned the hard way

**An elite relic is named after the OBJECT in its brief, never after its elite's mechanic.** Two of these
first shipped named for the rule they were drawn from — `the_proper_line` and `the_errant_cord` — which are
the ids the Ant Queen and the Surveyor of the Errant Cord already carry for their own bodies. The status
registry refused the second registration, and **37 tests across four acts failed with one message**. They are
now *The Head of the Line* and *The Shorter Ferrule*, which name the ant and the ferrule the briefs describe.

A third, *Concordance Thread*, collided with the Grand Cross-Reference's boss relic of that name and is now
*The Line Between the Volumes* — which is the better name anyway: the line between the two books is the
object, and the thread was only how it is drawn.

`EliteRelicTests.No_elite_relic_takes_a_name_the_game_already_uses` is the guard.

---

# 4. Full specification — 42 relics

## ACT I — THE CITY (#169–178)

### 169. The Case That Climbed
- **Pool:** Elite · **Source:** The Appellate Staircase (`city_elite_appeal_01`) · **Family:** Documents, Writs & Tickets
- **Effect:** The first time an enemy falls each fight, gain **1 Energy** and draw **1 card**.
- **Maximum palette:** parchment ivory · charcoal ink · near-black purple frame · antique gold line · muted brass
- **Object / silhouette:** A brass document clip holding a sheaf worn through at the top sheet. Three notches are cut into the clip's spine, one deeper than the other two.
- **Signature cue:** An ascending arrow inked along the paper edge, re-inked twice in different hands — the case has climbed this staircase before.
- **Label beneath icon:** `169. The Case That Climbed`

### 170. Half-Signed Page
- **Pool:** Elite · **Source:** The Living Petition Chorus (`city_elite_appeal_02`) · **Family:** Documents, Writs & Tickets
- **Effect:** The first card you play each fight costs **0**.
- **Maximum palette:** parchment ivory · iron-gall black · near-black purple frame · antique gold line · faded rose
- **Object / silhouette:** A petition leaf whose signature breaks off mid-stroke, the trailing ink drying into a dotted line that never resumes.
- **Signature cue:** Tiny mouths drawn down the margin, every one of them caught mid-word.
- **Label beneath icon:** `170. Half-Signed Page`

### 171. Remittitur Seal
- **Pool:** Elite · **Source:** The Remanded Case (`city_elite_appeal_03`) · **Family:** Stamps & Seals
- **Effect:** The first status an enemy applies to you each fight is sent back: **2 Paperwork** on whoever filed it.
- **Maximum palette:** sealing red · lamp black · near-black purple frame · antique gold line · linen white
- **Object / silhouette:** A wax seal split down the middle, one half red and one half black, held together by a single linen thread.
- **Signature cue:** The impression is a hand pointing backwards over its own shoulder.
- **Label beneath icon:** `171. Remittitur Seal`

### 172. Thrice-Struck Appointment Card
- **Pool:** Elite · **Source:** The Three Appointments (`city_elite_delay_01`) · **Family:** Time & Delay
- **Effect:** On every third turn of a fight, draw **1 extra card**.
- **Maximum palette:** card stock cream · charcoal ink · near-black purple frame · antique gold line · dull pewter
- **Object / silhouette:** A stiff card ruled into three fields, each stamped with a different hour and each struck through with one pen line.
- **Signature cue:** A small bell hangs from a corner on a short chain — its clapper is missing.
- **Label beneath icon:** `172. Thrice-Struck Appointment Card`

### 173. Shutter Key of the Late Hour
- **Pool:** Elite · **Source:** The Reopening-Hours Monolith (`city_elite_delay_02`) · **Family:** Keys, Locks & Access
- **Effect:** End a turn having played no card and the shutter pays for it: **12 Block and 1 Energy** at your next hand.
- **Maximum palette:** black iron · brass · near-black purple frame · antique gold line · soot grey
- **Object / silhouette:** A heavy iron shutter key with a hinged brass plate reading OPEN on one face and CLOSED on the other.
- **Signature cue:** The plate has seized halfway, showing half of each word at once.
- **Label beneath icon:** `173. Shutter Key of the Late Hour`

### 174. Chair of the Ninth Hour
- **Pool:** Elite · **Source:** The Devouring Waiting Room (`city_elite_delay_03`) · **Family:** Time & Delay
- **Effect:** Draw **2 extra cards** in your opening hand, and **1 fewer** on the turn after.
- **Maximum palette:** worn oak brown · faded upholstery green · near-black purple frame · antique gold line · glass grey
- **Object / silhouette:** A waiting-room chair whose seat is hollowed by centuries of sitting, a numeral nine burnt into the backrest.
- **Signature cue:** A small hourglass wedged under one leg to stop it rocking, and left there.
- **Label beneath icon:** `174. Chair of the Ninth Hour`

### 175. Contempt Ledger Nail
- **Pool:** Elite · **Source:** The Iron Warrant Avatar (`city_elite_enforcement_01`) · **Family:** Tools & Implements
- **Effect:** Whenever you gain **Censure**, apply **2 Paperwork** to the weakest enemy.
- **Maximum palette:** black iron · oak brown · near-black purple frame · antique gold line · sealing red
- **Object / silhouette:** An iron nail driven through a folded warrant into a scrap of oak.
- **Signature cue:** The warrant's seal has cracked in a clean ring around the shaft, and nobody has tried to pull it out.
- **Label beneath icon:** `175. Contempt Ledger Nail`

### 176. Inventory Lantern Glass
- **Pool:** Elite · **Source:** The Seizure Procession (`city_elite_enforcement_02`) · **Family:** Tools & Implements
- **Effect:** Every shop will strike **one card from your deck for nothing**.
- **Maximum palette:** smoked glass grey · pewter · near-black purple frame · antique gold line · lamp amber
- **Object / silhouette:** A single pane of smoked lantern glass in a pewter mount.
- **Signature cue:** An inventory number etched **backwards**, so it reads correctly when the light throws it on a wall.
- **Label beneath icon:** `176. Inventory Lantern Glass`

### 177. Gate-Chain Counterweight
- **Pool:** Elite · **Source:** The Portcullis Judicator (`city_elite_enforcement_03`) · **Family:** Measures, Weights & Instruments
- **Effect:** Deal **20 or more damage in one turn** and your first card next turn costs **1 less**.
- **Maximum palette:** lead grey · black iron · near-black purple frame · antique gold line · stone
- **Object / silhouette:** A lead counterweight cast in the shape of a gatehouse, hanging from three chain links.
- **Signature cue:** One link has been forced open and hammered shut again, badly.
- **Label beneath icon:** `177. Gate-Chain Counterweight`

### 178. Sealed Spearhead
- **Pool:** Elite · **Source:** The Final Notice Knight (`city_elite_enforcement_04`) · **Family:** Tools & Implements
- **Effect:** When an enemy falls, deal **6 damage** to every other enemy.
- **Maximum palette:** black iron · sealing red · parchment ivory · near-black purple frame · antique gold line
- **Object / silhouette:** An iron spearhead whose socket is sealed shut with red wax poured over a rolled notice.
- **Signature cue:** The wax has never been broken, and the notice has never been read.
- **Label beneath icon:** `178. Sealed Spearhead`

## ACT II — THE ENDLESS ARCHIVES (#179–187)

### 179. Cracked Bell-Lip
- **Pool:** Elite · **Source:** The After-Hours Return Bell (`archives_elite_after_hours_return_bell`) · **Family:** Tools & Implements
- **Effect:** The first **Overdue** filed against you each fight: **10 Block** and draw **1 card**.
- **Maximum palette:** bell bronze · verdigris · receipt cream · near-black purple frame · antique gold line
- **Object / silhouette:** A curved fragment of bronze bell lip, its strike point worn into a shallow dish.
- **Signature cue:** A paper receipt still wedged in the crack, which has closed around it.
- **Label beneath icon:** `179. Cracked Bell-Lip`

### 180. Roller Pin of the Stacks
- **Pool:** Elite · **Source:** The Rolling Stacks Colossus (`archives_elite_rolling_stacks_colossus`) · **Family:** Books & Archive
- **Effect:** The first card **Misfiled** against you each fight is replaced.
- **Maximum palette:** grey stone · pewter · near-black purple frame · antique gold line · shelf-dust ochre
- **Object / silhouette:** A short stone roller on a pewter axle.
- **Signature cue:** Ground flat along one side, from carrying the same shelf back and forth over the same stretch of floor.
- **Label beneath icon:** `180. Roller Pin of the Stacks`

### 181. Blank Line in the Black Book
- **Pool:** Elite · **Source:** The Catalogue of Unwise Names (`archives_elite_catalogue_of_unwise_names`) · **Family:** Books & Archive
- **Effect:** Every **card reward** shows one more card to choose from.
- **Maximum palette:** lamp black · page cream · near-black purple frame · antique gold line · quill brown
- **Object / silhouette:** A black catalogue open at a page of three ruled lines — two filled with names scratched out to illegibility, the third still empty.
- **Signature cue:** A dry quill laid across the gutter, pointing at the empty line.
- **Label beneath icon:** `181. Blank Line in the Black Book`

### 182. The Unspoken Word
- **Pool:** Elite · **Source:** The Silence Between Two Words (`archives_elite_silence_between_two_words`) · **Family:** Monuments, Stones & Architecture
- **Effect:** End a turn still holding **2 or more cards**: **4 Block** at your next hand.
- **Maximum palette:** white marble · pewter · near-black purple frame · antique gold line · shadow grey
- **Object / silhouette:** Two small marble word-blocks mounted a hand's width apart on a pewter bar.
- **Signature cue:** **The gap is the object.** Nothing between them but polished air; the words are the mounting.
- **Label beneath icon:** `182. The Unspoken Word`

### 183. Strip of Censoring Ink
- **Pool:** Elite · **Source:** The Black-Ink Oracle (`archives_elite_black_ink_oracle`) · **Family:** Books & Archive
- **Effect:** Every fight opens with **8 Block**: the first blow is blacked out.
- **Maximum palette:** lacquer black · plaque cream · near-black purple frame · antique gold line · ink sheen
- **Object / silhouette:** A single strip of lacquered black ink laid across a line of text on a catalogue plaque.
- **Signature cue:** Laid so precisely that only the descenders show beneath it.
- **Label beneath icon:** `183. Strip of Censoring Ink`

### 184. The Line Between the Volumes
- **Pool:** Elite · **Source:** Volumes of Cause and Consequence (`archives_elite_volumes_of_cause_and_consequence`) · **Family:** Books & Archive
- **Effect:** The **second card you play each turn** strikes 4 harder.
- **Maximum palette:** thread red · brass · page cream · near-black purple frame · antique gold line
- **Object / silhouette:** A red thread strung taut between two brass book-clasps.
- **Signature cue:** Small paper tags hang from it at even intervals, each numbered on both sides — with different numbers.
- **Label beneath icon:** `184. The Line Between the Volumes`

### 185. Drawer Within a Drawer
- **Pool:** Elite · **Source:** The Drawer of Infinite Returns (`archives_elite_drawer_of_infinite_returns`) · **Family:** Vessels & Containers
- **Effect:** The first card you **exhaust** each fight goes to your discard pile instead.
- **Maximum palette:** oak brown · brass · near-black purple frame · antique gold line · shadow
- **Object / silhouette:** A small oak drawer front with a second, smaller drawer front carved into its face, and a third begun inside that.
- **Signature cue:** The pull-handle is the same size on all three — which is the wrong size for two of them.
- **Label beneath icon:** `185. Drawer Within a Drawer`

### 186. The Missing Present Hand
- **Pool:** Elite · **Source:** The Presentless Clock (`archives_elite_presentless_clock`) · **Family:** Time & Delay
- **Effect:** Your opening turn is filed to the past: **6 Block and 1 card** at your second hand.
- **Maximum palette:** blued steel · dial cream · near-black purple frame · antique gold line · shadow grey
- **Object / silhouette:** A slender clock hand of blued steel with no counterweight, lying loose beside an empty arbor hole.
- **Signature cue:** The dial behind it is numbered for the past and the future and **entirely blank at the top**.
- **Label beneath icon:** `186. The Missing Present Hand`

### 187. The Third Ending
- **Pool:** Elite · **Source:** The Obituary with Three Endings (`archives_elite_obituary_with_three_endings`) · **Family:** Documents, Writs & Tickets
- **Effect:** Once each fight, the blow that would end you is struck through instead: you stand at **8 HP**.
- **Maximum palette:** newsprint grey · press black · near-black purple frame · antique gold line · wet-ink sheen
- **Object / silhouette:** A single obituary column set in three parallel strips of type.
- **Signature cue:** Two are struck through with a printer's rule; the third has not dried.
- **Label beneath icon:** `187. The Third Ending`
- ⚠ **Balance note.** The design asked for once per **run**; the engine's death prevention is a property of a
  status and therefore per **fight**. This is the strongest relic in the pool and the first place a balance
  pass should look.

## ACT III — THE GREEN DOCKET (#188–196)

### 188. Pre-Approved Antler Tine
- **Pool:** Elite · **Source:** The Stag of Pre-Approved Violence (`green_docket_elite_stag_of_pre_approved_violence`) · **Family:** Ritual & Natural Charms
- **Effect:** The first **Safe-Conduct** you spend each fight is not spent.
- **Maximum palette:** bone antler · green wax · near-black purple frame · antique gold line · cord brown
- **Object / silhouette:** A single antler tine sawn flat at the base and drilled for a cord.
- **Signature cue:** A licence number burnt down the shaft and a bead of green wax fixed to the point.
- **Label beneath icon:** `188. Pre-Approved Antler Tine`

### 189. Mended Thread of the Grandmother
- **Pool:** Elite · **Source:** Grandmother Web (`green_docket_elite_grandmother_web`) · **Family:** Threads, Ribbons & Textiles
- **Effect:** Every fight opens with **1 Safe-Conduct**; spending one gives **3 Block**.
- **Maximum palette:** silk white · household thread brown · near-black purple frame · antique gold line · thorn grey
- **Object / silhouette:** A length of white spider-silk repaired in three places with darker household thread.
- **Signature cue:** The repairs are deliberately visible — and neater than the original.
- **Label beneath icon:** `189. Mended Thread of the Grandmother`

### 190. Toll-Stone from the Wrong Bank
- **Pool:** Elite · **Source:** The Wrong Bridge in Person (`green_docket_elite_the_wrong_bridge_in_person`) · **Family:** Monuments, Stones & Architecture
- **Effect:** The first **Claim** laid on you each fight also gives **6 Block**.
- **Maximum palette:** river stone grey · chisel white · near-black purple frame · antique gold line · water green
- **Object / silhouette:** A flat river stone with a toll mark chiselled into one face.
- **Signature cue:** The same mark is chiselled **upside down** into the other face, and both are worn equally smooth.
- **Label beneath icon:** `190. Toll-Stone from the Wrong Bank`

### 191. Coin Left in the Throat
- **Pool:** Elite · **Source:** The Great Toll Frog (`green_docket_elite_great_toll_frog`) · **Family:** Tokens, Coins & Tallies
- **Effect:** Every **Claim** laid on you leaves **2 Block** behind, and every victory leaves **10 Gold**.
- **Maximum palette:** worn bronze · bile green · near-black purple frame · antique gold line · bone white
- **Object / silhouette:** A swollen bronze coin, its face worn blank on one side by something that held it a long time.
- **Signature cue:** A feather and a small tooth fused to the rim.
- **Label beneath icon:** `191. Coin Left in the Throat`

### 192. The Head of the Line
- **Pool:** Elite · **Source:** The Ant Queen of the Proper Line (`green_docket_elite_ant_queen_of_the_proper_line`) · **Family:** Ritual & Natural Charms
- **Effect:** Play **3 cards in one turn**: draw **2 cards**.
- **Maximum palette:** bark white · resin amber · pewter · near-black purple frame · antique gold line
- **Object / silhouette:** Three white bark strips laid end to end on a pewter tray, each notched at exactly the same interval.
- **Signature cue:** One ant set in resin at the head of the line, **facing the wrong way**.
- **Label beneath icon:** `192. The Head of the Line`

### 193. Bone Tag from the Juniper
- **Pool:** Elite · **Source:** The Juniper Injunction (`green_docket_elite_juniper_injunction`) · **Family:** Ritual & Natural Charms
- **Effect:** The first affliction laid on you each fight is **refused**.
- **Maximum palette:** bone white · juniper green · scorch black · near-black purple frame · antique gold line
- **Object / silhouette:** A thin bone tag pierced and hung on a juniper twig, one edge scorched.
- **Signature cue:** The writing has been scraped off and re-cut twice, and the third hand is the worst of them.
- **Label beneath icon:** `193. Bone Tag from the Juniper`

### 194. Obsolete Boundary Cord
- **Pool:** Elite · **Source:** The Surveyor of Forgotten Paths (`green_docket_elite_surveyor_of_forgotten_paths`) · **Family:** Measures, Weights & Instruments
- **Effect:** Every turn in which something on you **lapsed of its own accord**: **3 Block**.
- **Maximum palette:** tarred cord black · brass · near-black purple frame · antique gold line · map ochre
- **Object / silhouette:** A coil of tarred measuring cord with three brass tags, two of them struck through.
- **Signature cue:** The knots are spaced to a measure no office still recognises.
- **Label beneath icon:** `194. Obsolete Boundary Cord`

### 195. Reed Cut at the Hearing
- **Pool:** Elite · **Source:** The Three Reeds of Appeal (`green_docket_elite_three_reeds_of_appeal`) · **Family:** Ritual & Natural Charms
- **Effect:** The first **Claim you lay** each fight is laid twice.
- **Maximum palette:** reed black-green · linen white · near-black purple frame · antique gold line · water black
- **Object / silhouette:** Three black-water reeds bound with a linen strip — the shortest cut clean, the other two torn.
- **Signature cue:** The binding is knotted only once, and slipping.
- **Label beneath icon:** `195. Reed Cut at the Hearing`

### 196. Thorn Chosen From Three
- **Pool:** Elite · **Source:** The Magistrate of Thorns (`green_docket_elite_magistrate_of_thorns`) · **Family:** Tools & Implements
- **Effect:** At every opening hand, choose one: **10 Block** · **1 card and 1 Energy** · **6 damage to every enemy**.
- **Maximum palette:** blackthorn · pewter · near-black purple frame · antique gold line · dried blood
- **Object / silhouette:** Three long blackthorns mounted in a pewter clasp like a set of pen nibs.
- **Signature cue:** One is noticeably more worn than the other two — and it is not the longest.
- **Label beneath icon:** `196. Thorn Chosen From Three`

## ACT IV — THE LICENSING LABYRINTH (#197–206)

### 197. The Shorter Ferrule
- **Pool:** Elite · **Source:** The Surveyor of the Errant Cord (`labyrinth_elite_surveyor_of_the_errant_cord`) · **Family:** Measures, Weights & Instruments
- **Effect:** The first **Weighed** you take each fight is **1 lighter**.
- **Maximum palette:** cord flax · brass · near-black purple frame · antique gold line · desert dust
- **Object / silhouette:** A surveyor's cord with a brass ferrule at each end, cut to two slightly different measures.
- **Signature cue:** One ferrule has been filed down and **re-stamped with the other's number**.
- **Label beneath icon:** `197. The Shorter Ferrule`

### 198. Broken Granary Seal
- **Pool:** Elite · **Source:** The Scarab Host of the Sealed Granary (`labyrinth_elite_scarab_host_of_the_sealed_granary`) · **Family:** Stamps & Seals
- **Effect:** The first time each fight you strike an enemy that is **behind Block**: **8 more damage**.
- **Maximum palette:** fired clay · scarab black · near-black purple frame · antique gold line · grain ochre
- **Object / silhouette:** A fired-clay granary seal split cleanly in two.
- **Signature cue:** A scarab impression on one half, and on the other the **empty socket it was pressed from**.
- **Label beneath icon:** `198. Broken Granary Seal`

### 199. Cut Corvée Rope
- **Pool:** Elite · **Source:** The Rope-Master of the Corvée (`labyrinth_elite_rope_master_of_the_corvee`) · **Family:** Threads, Ribbons & Textiles
- **Effect:** Every third **Burdened** surcharge you pay gives **1 Energy** back.
- **Maximum palette:** hemp brown · linen white · near-black purple frame · antique gold line · sand
- **Object / silhouette:** A thick hemp rope cut through and whipped at both ends with linen.
- **Signature cue:** Three tally knots remain on the shorter piece; the longer piece is missing entirely.
- **Label beneath icon:** `199. Cut Corvée Rope`

### 200. Glyph Struck From the Name
- **Pool:** Elite · **Source:** The Keeper of the Living Cartouche (`labyrinth_elite_keeper_of_the_living_cartouche`) · **Family:** Stamps & Seals
- **Effect:** The first time the register would **enlarge something against you** each fight, it does not.
- **Maximum palette:** gilded plaster · wax cream · near-black purple frame · antique gold line · chisel grey
- **Object / silhouette:** An oval cartouche of gilded plaster with one glyph chiselled out.
- **Signature cue:** The socket has been filled flush with plain wax, and the wax has not been carved.
- **Label beneath icon:** `200. Glyph Struck From the Name`

### 201. Overseer's Linen Shears
- **Pool:** Elite · **Source:** The Mummified Overseer of the Linen House (`labyrinth_elite_mummified_overseer_of_the_linen_house`) · **Family:** Tools & Implements
- **Effect:** The first thing that **fades** from you each fight: heal **3** and gain **3 Block**.
- **Maximum palette:** bronze · linen white · near-black purple frame · antique gold line · natron grey
- **Object / silhouette:** Long bronze linen shears with **one blade wrapped in its own bandage**.
- **Signature cue:** The pivot is a scarab, and it turns stiffly.
- **Label beneath icon:** `201. Overseer's Linen Shears`

### 202. Weight From the Lighter Pan
- **Pool:** Elite · **Source:** The Treasury of the Two Pans (`labyrinth_elite_treasury_of_the_two_pans`) · **Family:** Measures, Weights & Instruments
- **Effect:** Every victory is weighed out in coin: **25 Gold**.
- **Maximum palette:** lead grey · brass · near-black purple frame · antique gold line · treasury red
- **Object / silhouette:** A small lead weight from a two-pan balance.
- **Signature cue:** Its stamped value scratched out and a **lower one punched beside it in a different hand**.
- **Label beneath icon:** `202. Weight From the Lighter Pan`

### 203. The Riddle's Third Answer
- **Pool:** Elite · **Source:** The Sphinx of the Processional Measure (`labyrinth_elite_sphinx_of_the_processional_measure`) · **Family:** Monuments, Stones & Architecture
- **Effect:** Every fight opens with **1 Ward Wax and 1 Seal**.
- **Maximum palette:** limestone · chisel shadow · near-black purple frame · antique gold line · desert light
- **Object / silhouette:** A limestone tablet with three answers cut into it.
- **Signature cue:** Two are polished smooth by hands; **the third is untouched and its edges are still sharp**.
- **Label beneath icon:** `203. The Riddle's Third Answer`

### 204. The Lamp Thief's Wick
- **Pool:** Elite · **Source:** The Tombbreakers Three (`labyrinth_elite_the_tombbreakers_three`) · **Family:** Tools & Implements
- **Effect:** When an enemy falls, gain **2 Block for every enemy still standing**.
- **Maximum palette:** brass · unburnt wick white · near-black purple frame · antique gold line · tomb black
- **Object / silhouette:** A brass lamp wick-holder with a length of unburnt wick still in it, the reservoir dry and the glass long gone.
- **Signature cue:** Three sets of initials scratched into the base; **one has been crossed out**.
- **Label beneath icon:** `204. The Lamp Thief's Wick`

### 205. Decan Star-Table Chip
- **Pool:** Elite · **Source:** The Keeper of the Thirty-Six Decans (`labyrinth_elite_keeper_of_the_thirty_six_decans`) · **Family:** Measures, Weights & Instruments
- **Effect:** On every sixth turn of a fight, **one affliction leaves you**.
- **Maximum palette:** dark schist · chalk white · near-black purple frame · antique gold line · star silver
- **Object / silhouette:** A broken corner of a star-table in dark schist, six decan glyphs running down it in a column.
- **Signature cue:** The seventh was begun and abandoned — **the chisel mark is still there**.
- **Label beneath icon:** `205. Decan Star-Table Chip`

### 206. Processional Step-Stone
- **Pool:** Elite · **Source:** The Colossus of the Endless Procession (`labyrinth_elite_colossus_of_the_endless_procession`) · **Family:** Monuments, Stones & Architecture
- **Effect:** On every third turn of a fight, your first card costs **0**.
- **Maximum palette:** processional limestone · dust ochre · near-black purple frame · antique gold line · shadow
- **Object / silhouette:** A single flagstone from a processional way, worn into a shallow footprint **exactly at its centre**.
- **Signature cue:** The edges are still square, because nothing ever walked near them.
- **Label beneath icon:** `206. Processional Step-Stone`

## THE MIMIC — ONE RELIC, FOUR GRADES (#207–210)

**Shared identity.** A curved tooth prised from the underside of a chest lid, still set in the wood it grew
through. Every grade is **the same tooth**; what changes is how much of the lid came with it. All four must
read as one object at four sizes — a viewer seeing #210 beside #207 should recognise the tooth first and the
grade second.

**Shared palette (maximum):** tooth ivory · oak brown · brass · near-black purple frame · antique gold line.

**Effect, by grade:** the first blow of every fight glances off the tooth.
**I** 4 Block · **II** 8 Block · **III** 12 Block and heal 2 · **IV** 16 Block and heal 4.
A later grade **removes the one already held**.

### 207. Tooth From the False Lid I
- **Source:** an Act I mimic · **Object:** the tooth, with **a splinter of oak** still gripped in it.
- **Signature cue:** The splinter is torn, not cut — nothing let go willingly.
- **Label beneath icon:** `207. Tooth From the False Lid I`

### 208. Tooth From the False Lid II
- **Source:** an Act II mimic · **Object:** the tooth, with **a brass hinge still screwed to the wood**.
- **Signature cue:** The hinge is bent open past its stop, as though the lid was forced the wrong way.
- **Label beneath icon:** `208. Tooth From the False Lid II`

### 209. Tooth From the False Lid III
- **Source:** an Act III mimic · **Object:** the tooth, with **the lock-plate** attached.
- **Signature cue:** The keyhole is ringed with tooth-marks — something tried to eat its own lock.
- **Label beneath icon:** `209. Tooth From the False Lid III`

### 210. Tooth From the False Lid IV
- **Source:** an Act IV mimic · **Object:** **the whole false lid**, folded shut around the tooth.
- **Signature cue:** It rests like a jaw at rest, not like a box — the silhouette should read as a closed mouth before it reads as furniture.
- **Label beneath icon:** `210. Tooth From the False Lid IV`

---

# 5. Adaptations, stated

Three of these are not what the slate promised, and each is written down where it is built.

1. **Gold cannot be paid inside a fight.** Gold is a run resource and no combat effect reaches it, so
   *Coin Left in the Throat* pays 2 Block per Claim in the fight and 10 Gold per victory outside it, and
   *Weight From the Lighter Pan* is a run program in full.
2. **A card cannot be "entered" permanently at reward time.** The reward layer marks OFFERS, not the cards a
   run ends up holding, so *Blank Line in the Black Book* shows **one more card to choose from** — which is
   what a catalogue with a free line actually offers.
3. **A card cannot be re-resolved across a turn boundary.** The replay node replays a card the current play
   is holding, so *The Missing Present Hand* pays what a turn-one card is worth, one turn later.

And one that is *more* than promised: **The Third Ending** is per fight, not per run (§4, #187).
