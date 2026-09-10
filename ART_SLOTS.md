# Art slots — every picture the game looks for

Generated: `dotnet run --project Converter -- --art-slots ART_SLOTS.md`. **Do not edit by hand.**

**733 pictures**: 254 cards, 210 relics and 269 bodies (every
enemy, elite and boss). None of them exists yet, and that is a normal state — a card with no file draws
an empty socket with its own code printed in it, a relic with no file draws its code, and a body with
no file draws the stick figure it draws today. Nothing breaks while a slot is empty, so the list below
can be worked down in any order. 25 of the card slots belong to the demo game's leftovers and
are marked "Ported v2 remnants" at the end: **708 pictures** are the
real list.

## How a slot is filled

1. Name the file after the **code** in the table and drop it in:
   `bnb-godot/assets/art/cards/<code>.png` · `bnb-godot/assets/art/relics/<code>.png` ·
   `bnb-godot/assets/art/enemies/<code>.png`
2. Run `bnb-godot/tools/import-art.sh` once afterwards. Godot only reads textures it has imported, so a
   file that was merely copied in is invisible to the game until that runs (the editor does it by itself
   on focus; the headless probes do not).

The code is the entity's id, which is also what the export contract's `Presentation.Art` already says.
**An upgraded card has no slot of its own**: `levy_stamp+` is drawn from `levy_stamp.png`, so an
improvement never changes the picture. Dropping `levy_stamp+.png` in does nothing — the `+` is not part
of any file name in this game.

## What the picture has to be

- **Card art** is drawn into the frame's window at **122 × 110** points (11:10 landscape), cropped to
  fill. Draw it larger — 488 × 440 or 976 × 880 — and let the mipmaps do the reduction; the window is
  the only part of the card the picture may occupy, and the frame is printed over everything else.
- **Relic art** will be drawn (D4) as a **small square** in the right-hand strip, roughly 34 × 34
  points, larger on hover. It has to survive being that small: one object, a clear silhouette, no fine
  text. Square source, 512 × 512 or more.
- **A body** stands in its column in the arena, in a window as wide as that column and **150 points
  tall**, its aspect KEPT (never cropped, never squashed): a portrait ends up about 110 points wide
  with the health bar directly under its feet. Draw it portrait on transparency — 600 × 900 or more —
  and let the mipmaps reduce it. A crowd of four narrows every column to about 110 points, so the
  silhouette has to read at half the room a duel gives it. **Draw the body facing LEFT**, towards the
  player, who stands on the left of the arena: a picture is never mirrored by the game (a flipped body
  wears its sash on the wrong side), so the direction it faces is the direction it was drawn in.
- PNG, RGBA. Transparency is welcome — a card's socket is a dark recess and a relic's square carries
  its pool's frame colour, and both are meant to show through.

The relic briefs below are the design canon's own words
(`source-data/design/BnB_Final_Relics_Master_PostAudit_VISUAL_DESIGN_CANON.md` for #1–168,
`BnB_Elite_Relics_MASTER_AND_VISUAL_CANON.md` for #169–210). Frames, palettes and the label rule live
there in full; only the object line is repeated here, because that is the line that differs per relic.

## Relics — 210 pictures

The catalogue number is the design canon's, and it is not part of the file name: the id is.

### Normal relics — 50

| # | code | title | rarity | the object |
|---:|---|---|---|---|
| 1 | `levy_stamp` | Levy Stamp | common | A squat late-18th-century wooden tax stamp with a broad oval handle and a heavy oval brass stamping plate. The plate bears a simple crown-and-tally emblem rather than a modern currency symbol. |
| 2 | `brass_bookmark` | Brass Bookmark | common | A long, narrow brass bookmark with a hooked top, one pierced hole, and a short cloth tassel. Keep the silhouette tall and blade-like, more like an old reading marker than a modern bookmark strip. |
| 3 | `conservators_thread` | Conservator's Thread | common | A small wooden spool of fine conservation thread with a blunt binding needle laid diagonally across it. The thread loops around a tiny torn-paper patch as if repairing it. |
| 4 | `sun_warmed_waystone` | Sun-Warmed Waystone | common | A simple standing boundary stone with a shallow sun disk carved near the top and a single horizontal tally cut near the base. The upper edge is softened by age, not neatly geometric. |
| 5 | `five_notch_bead` | Five-Notch Bead | common | One large oval bone or hardwood bead threaded on a short cord. Exactly five deep notches are cut around the outer rim and must be clearly countable at icon size. |
| 6 | `formkeepers_signet` | Formkeeper's Signet | common | A heavy old signet ring with a broad octagonal face. The face shows a folded parchment form with three short handwritten lines and a tiny wax dot, not a modern file/document icon. |
| 7 | `rootbound_walking_staff` | Rootbound Walking Staff | uncommon | A crooked wooden walking staff with a simple hooked top. Its lower end has grown into three exposed roots that curl outward rather than ending in a ferrule. |
| 8 | `counterfeit_toll_writ` | Counterfeit Toll Writ | uncommon | A rolled roadside toll writ partly unfurled, with a rough border and a stamped seal that is visibly misaligned with the written authority line. The paper looks copied and suspiciously patched. |
| 9 | `emergency_inkwell` | Emergency Inkwell | uncommon | A squat ceramic or thick-glass inkwell with a simple painted cross on the front and a large goose quill standing in it. The form should resemble a portable 18th–19th-century writing bottle. |
| 10 | `ashen_wax_knife` | Ashen Wax Knife | uncommon | A short wax-and-paper knife with a rounded wooden handle and a broad, slightly asymmetrical blade appropriate for scraping and cutting sealing wax. The blade tip is darkened by soot. |
| 11 | `quiet_readers_cord` | Quiet Reader's Cord | uncommon | A looped reading cord with two modest tassels, tied in a low central knot. It should resemble a cord used to mark or hold pages in a large bound volume. |
| 12 | `archive_key` | Archive Key | uncommon | A large iron archive key with a quatrefoil bow and a long plain shaft. The bit is shaped like two staggered shelf teeth rather than a domestic door key. |
| 13 | `index_volvelle` | Index Volvelle | rare | A circular paper-and-brass volvelle made from two concentric parchment discs fixed by a central rivet. One slim pointer arm rotates over handwritten index marks. |
| 14 | `withheld_hourglass` | Withheld Hourglass | rare | A narrow wooden-framed hourglass with a small cord tied tightly around its waist, visually 'holding' the sand in the upper bulb. The glass itself is plain and period-appropriate. |
| 15 | `road_claim_token` | Road-Claim Token | rare | A thick old travel token with an irregular round edge. Its face shows a winding road ending at a small boundary stake and two tiny milestone marks. |
| 16 | `concordance_medallion` | Concordance Medallion | rare | A hanging medallion containing two mirrored parchment leaves that touch at the center, joined by one small linking bar. The outer ring is plain and bureaucratic rather than jeweled. |
| 17 | `chancery_ribbon` | Chancery Ribbon | rare | A broad old chancery ribbon folded into a formal rosette with two long tails. Tiny lines of script are visible only on the tails, as if the ribbon itself carries authority. |
| 18 | `moss_salve` | Moss Salve | common | A small lidded ceramic salve pot with a rough hand-thrown profile. A short sprig of moss lies across the front and a little ointment is visible under the half-open lid. |
| 19 | `lead_counterweight` | Lead Counterweight | common | A compact bell-shaped lead weight with a thick iron loop. The surface is pitted and carries one old tally line rather than a printed mass label. |
| 20 | `hollow_wax_bead` | Hollow Wax Bead | common | A thick ring-shaped wax bead with a very large central hole and one softened drip at the lower edge. It should look hand-formed, slightly lopsided, and tactile. |
| 21 | `binders_awl` | Binder's Awl | common | A short wooden-handled binder's awl with a narrow steel spike. Beside the spike is a tiny strip of parchment pierced by two stitch holes, grounding it in bookbinding rather than weaponry. |
| 22 | `carved_bone_buckle` | Carved Bone Buckle | common | A rectangular-to-oval belt buckle carved from bone, with softened corners, a central bar, and simple leaf scoring around the rim. It should look handmade and worn. |
| 23 | `petitioners_token` | Petitioner's Token | common | A modest round petition token, slightly thinner than a coin, with a raised open hand holding a tiny rolled petition. Avoid crowns or generic heraldry. |
| 24 | `redaction_knife` | Redaction Knife | uncommon | A broad scraping knife used to lift ink from parchment: short blade, rounded back, plain horn handle. One portion of the blade edge is stained dark from scraped ink. |
| 25 | `alms_basin` | Alms Basin | uncommon | A shallow pewter alms basin with a rolled lip and small foot ring. Three old coins rest inside, but the vessel remains dominant. |
| 26 | `index_bone` | Index Bone | uncommon | A slender carved bone index pointer shaped like an elongated finger bone, with three incised reference ticks along one side. It should resemble an old scholarly pointer, not an anatomical specimen display. |
| 27 | `refusal_rosary` | Refusal Rosary | uncommon | A loop of plain prayer beads with one bead deliberately missing and a small palm-out refusal charm hanging at the join. Keep it old, austere, and handmade. |
| 28 | `archive_censer` | Archive Censer | uncommon | A small hanging censer on three chains with a rounded perforated body. The smoke rises in a squared, shelf-like curl rather than a normal spiral. |
| 29 | `seal_makers_die` | Seal-Maker's Die | uncommon | A stout cylindrical metal die with a wide flat striking head and a reversed floral seal pattern cut into the lower face. It should look like a craftsman's hand tool, not a rubber stamp. |
| 30 | `iron_astrolabe` | Iron Astrolabe | rare | A compact iron astrolabe with a thick outer ring, one pierced star plate, and a single movable rule. Keep the geometry simple enough to draw repeatedly. |
| 31 | `twin_ember_brazier` | Twin-Ember Brazier | rare | A squat portable brazier with two distinct fire bowls sharing one base. Each flame is small and simple; the metal body has only a few punched air holes. |
| 32 | `gilded_tithe_chain` | Gilded Tithe Chain | rare | A short ceremonial chain of heavy links with three small coin-like tithe weights spaced along it. The links are slightly oversized and handmade. |
| 33 | `rebinding_spindle` | Rebinding Spindle | rare | A broad wooden binding spindle tightly wound with book thread, crossed by two slim binding needles. A short loose end curls outward. |
| 34 | `deferred_signet` | Deferred Signet | rare | A heavy signet ring whose oval face contains a tiny suspended hourglass held inside a raised frame. The ring itself is plain and old-fashioned. |
| 35 | `iron_prayer_bead` | Iron Prayer Bead | common | One dense iron prayer bead on a very short cord, oversized compared with ordinary rosary beads. A simple vertical groove and a tiny punched dot decorate it. |
| 36 | `black_salt_charm` | Black Salt Charm | common | A small linen sachet tied with twine, visibly leaking coarse black salt from one corner. A tiny hand-painted ward line sits on the cloth. |
| 37 | `tarnished_bell` | Tarnished Bell | common | A small old hand bell with a thick lip, visible clapper, and uneven patina. One hairline crack runs upward from the rim. |
| 38 | `grave_coin` | Grave Coin | common | A worn burial coin with a chipped rim and a simple closed-eye or skull relief at the center. It should feel ancient and handled rather than decorative. |
| 39 | `bruise_cup` | Bruise Cup | common | A small dented pewter cup with a low stem. A dark violet stain pools along one side of the interior, as if the metal itself bruised. |
| 40 | `votive_candle` | Votive Candle | common | A short thick candle on a plain metal saucer, heavily dripped with wax. The flame is calm and upright, not dramatic. |
| 41 | `blood_price_token` | Blood-Price Token | uncommon | A flat iron or bone token pierced by a single teardrop-shaped cutout. One edge is lightly stained dark red, but the object remains mostly austere. |
| 42 | `blackthorn_brooch` | Blackthorn Brooch | uncommon | A circular thorn wreath used as a brooch, closed by a long straight pin that crosses the center. The thorn points are uneven and natural. |
| 43 | `executioners_measure` | Executioner's Measure | uncommon | A rigid measuring rod with fifteen clearly grouped notches and a broad, slightly blade-like end cap. It should read first as a measure, second as something ominous. |
| 44 | `sootglass_lens` | Sootglass Lens | uncommon | A single round smoked-glass lens in a thick old brass rim with a tiny finger loop. The glass should be almost black but still reflective. |
| 45 | `rubric_tablet` | Rubric Tablet | uncommon | A palm-sized wood or clay tablet with three short rubric lines picked out in faded red and a small heading mark at the top. No printed typography. |
| 46 | `refuse_docket` | Refuse Docket | uncommon | A crumpled docket with one torn corner, folded once as if rejected and shoved aside. A broad diagonal hand-drawn refusal slash crosses the page. |
| 47 | `blood_stamped_bond` | Blood-Stamped Bond | rare | A formal bond folded vertically and tied with cord, bearing one dark red thumb-like seal near the lower edge. The paper remains clean and legalistic. |
| 48 | `thorn_crowned_reliquary` | Thorn-Crowned Reliquary | rare | A small domed reliquary surrounded at its roofline by a literal crown of black thorns. The reliquary body is simple enough that the thorn ring dominates. |
| 49 | `blank_folio` | Blank Folio | rare | A thick closed folio with absolutely no title, crest, clasp, or writing on the cover. One pale page corner protrudes where something was removed. |
| 50 | `chancery_scale` | Chancery Scale | rare | A narrow two-pan chancery balance with a quill-shaped central pointer and shallow document-sized pans. The beam is elegant but not ornate. |

### Shop relics — 24

| # | code | title | rarity | the object |
|---:|---|---|---|---|
| 51 | `pawnbrokers_loupe` | Pawnbroker's Loupe | shop | A folding jeweler's loupe with a large circular lens and a smaller protective cover hinged beneath it. Place one tiny old coin under the lens. |
| 52 | `copper_receipt_roll` | Copper Receipt Roll | shop | A narrow handwritten receipt strip wound around a small copper spindle. Several short transaction lines and one torn edge are visible, all hand-written. |
| 53 | `secondhand_reliquary` | Secondhand Reliquary | shop | A worn little reliquary repaired with one mismatched metal patch and tied with an old handwritten price tag. The form should look valuable but obviously used. |
| 54 | `bounty_hook` | Bounty Hook | shop | A strong curved copper hook with a small waxed bounty tag tied near the eye. The hook is blunt enough to look like gear rather than a weapon. |
| 55 | `witchmarket_purse` | Witchmarket Purse | shop | A patched drawstring purse with one visible copper coin, a dried herb sprig, and a small witch-knot stitched into the cloth. Keep the magic folkloric, not flashy. |
| 56 | `bent_auction_gavel` | Bent Auction Gavel | shop | An old wooden auction gavel whose handle visibly bows sideways, banded once with copper. The head is simple and heavy. |
| 57 | `wastebrokers_permit` | Wastebroker's Permit | shop | A dirty folded permit with frayed corners, a salvager's hook emblem, and several hand-written tally marks. One edge is reinforced with copper staples or binding tabs. |
| 58 | `filing_fee_stamp` | Filing-Fee Stamp | shop | A square wooden fee stamp with a copper band around the base and a tiny old coin wedged beneath one corner of the stamping plate. The face bears only a simple fee tally. |
| 59 | `scriveners_shears` | Scrivener's Shears | shop | Small ornate paper shears with copper finger loops and one blade engraved with a single crossed-out line. They should resemble historical desk scissors, not modern office scissors. |
| 60 | `apprentices_whetstone` | Apprentice's Whetstone | shop | A plain rectangular sharpening stone sitting in a shallow wooden tray, with one deep worn groove where blades have repeatedly passed. No industrial holder. |
| 61 | `backroom_kettle` | Backroom Kettle | shop | A squat copper kettle with a high swinging handle, short spout, and heavily soot-darkened underside. The lid knob resembles an old coin. |
| 62 | `crooked_display_case` | Crooked Display Case | shop | A small glass-front cabinet on short feet, built from dark wood with copper corner brackets. One shelf is visibly slanted and the whole case leans slightly. |
| 63 | `turnover_bell` | Turnover Bell | shop | A small period hand/service bell on a low copper stand, with a circular arrow engraved around the base to suggest changing stock. It should feel like a tavern or shop bell, not a modern reception bell. |
| 64 | `debtors_signet` | Debtor's Signet | shop | A broad copper signet ring with a split coin motif crossed by a small chain link on the face. One side of the ring is visibly worn thin. |
| 65 | `notarys_waiver` | Notary's Waiver | shop | A folded notarial parchment with four small removable seal tabs hanging along the lower edge. The handwriting is dense but entirely manual. |
| 66 | `priority_window_pass` | Priority Window Pass | shop | A stiff hand-lettered pass with an arched service-window cutout punched near the top and a small ribbon hole. The shape should resemble an old permit card, not a modern ticket. |
| 67 | `twin_lock_chest_key` | Twin-Lock Chest Key | shop | A large copper-and-iron key with a double-loop bow and two distinct sets of teeth on the bit. The shaft is slightly too long, emphasizing its special access function. |
| 68 | `appraisers_chalk` | Appraiser's Chalk | shop | Two short sticks of white appraisal chalk tied together with thin copper wire. One stick bears a small star cut into its end. |
| 69 | `guest_favor_token` | Guest-Favor Token | shop | A broad copper guest token with a laurel half-wreath on one side and a tiny split relief: coin on one half, paired cards/pages on the other. |
| 70 | `merchant_punchcard` | Merchant Punchcard | shop | A thick 19th-century-style merchant tally card with three large holes manually punched in one edge and handwritten purchase marks beside them. It must look hand-punched, not machine-readable. |
| 71 | `warranty_tag` | Warranty Tag | shop | A sturdy parchment tag tied with coarse string, bearing a small copper eyelet and one wax bead. The writing is minimal and hand-scripted. |
| 72 | `indemnity_stamp` | Indemnity Stamp | shop | A round copper-bound hand stamp whose seal face shows a split coin with one half arcing back toward the other. The motion is expressed with an old engraved curve, not a modern arrow icon. |
| 73 | `archive_voucher_roll` | Archive Voucher Roll | shop | A narrow roll of small hand-written archive vouchers on a copper spindle, each exposed voucher separated by a deep tear notch. A short string keeps the roll from unwinding. |
| 74 | `departmental_purchase_order` | Departmental Purchase Order | shop | A bound handwritten purchase order with three broad ruled sections for Deed, Working, and Rite, plus a single wax seal at the bottom. Use hand-drawn columns, never spreadsheet-like cells. |

### Event relics — 25

| # | code | title | rarity | the object |
|---:|---|---|---|---|
| 75 | `originality_stamp` | Originality Stamp | event | A distinctive star-edged hand stamp with a feather-and-spark engraving. Beside it lie two slightly offset violet seal impressions, one darker than the other. |
| 76 | `unclaimed_property_tag` | Unclaimed Property Tag | event | A blank old property tag with no owner name, tied to an empty loop of cord as though removed from an object. The pale-violet frame supplies the uncanny event identity. |
| 77 | `uncalled_ticket` | Uncalled Ticket | event | A thick numbered waiting ticket with scalloped edges and a tiny clock-hand emblem, but no validation tear or call mark. The number is hand-written, not machine printed. |
| 78 | `threshold_ward` | Threshold Ward | event | A small old doorway and threshold stone seen front-on, with a restrained violet ward sigil painted across the threshold. The door is half open into darkness. |
| 79 | `crossed_out_map` | Crossed-Out Map | event | A folded old road map covered in faint hand-drawn paths, with one enormous violet X crossing the official route and a thinner alternate path sneaking around it. |
| 80 | `inherited_bone_folder` | Inherited Bone Folder | event | A polished bone paper-folder, long and leaf-shaped, with a worn family initial near the handle and one old repair band around the middle. |
| 81 | `unreturned_library_card` | Unreturned Library Card | event | A hand-written library borrowing card with several old due-date entries, the latest one crossed repeatedly but never closed. The lower edge is softened from years of use. |
| 82 | `reversible_shelf_label` | Reversible Shelf Label | event | Two small wooden or ivory shelf labels connected back-to-back by a simple hinge, with opposing arrows or category marks on the two faces. |
| 83 | `blank_cameo` | Blank Cameo | event | An ornate oval cameo brooch frame containing nothing but a smooth blank pale-violet field. The clasp is visible at the back edge. |
| 84 | `vow_bead` | Vow Bead | event | A single pale bead threaded between two tight knots on a short cord. The cord continues only a little beyond each knot, making the object feel deliberately constrained. |
| 85 | `inverted_sealstone` | Inverted Sealstone | event | A carved sealstone mounted upside down on a short pedestal so the engraved seal face points upward instead of downward. A faint violet impression appears above it like a reversed echo. |
| 86 | `mootcap` | Mootcap | event | A broad mushroom cap worn like a tiny cap, with no visible stem except a short central nub. A restrained ring of spores drifts around the edge. |
| 87 | `dissenting_spore` | Dissenting Spore | event | One round spore head on a thin stalk releasing a halo of tiny dots, except for one larger spore visibly drifting in the opposite direction. |
| 88 | `antway_marker` | Antway Marker | event | A small boundary marker stone with a single-file ant trail entering on one side and leaving on the other. One ant is carved larger at the center. |
| 89 | `complaint_leaf` | Complaint Leaf | event | A dry broad leaf covered with tiny scratch-like lines resembling cramped petitions along the veins. One edge is folded as if repeatedly handled. |
| 90 | `guest_right_brooch` | Guest-Right Brooch | event | An oval brooch shaped like an open doorway, with a tiny cup and loaf relief facing each other across the opening. The pin runs horizontally behind it. |
| 91 | `cup_of_the_lowest_mark` | Cup of the Lowest Mark | event | A simple old measuring cup or goblet with several faint etched level lines, but one unusually low line is darkened and violet. |
| 92 | `red_linen_knot` | Red Linen Knot | event | A thick strip of red funerary linen tied into one compact square knot with two broad frayed tails. Keep the knot simple and physical. |
| 93 | `blank_cartouche` | Blank Cartouche | event | A stone or ivory cartouche frame with a completely scraped-clean center. Fine chisel dust sits at the bottom edge. |
| 94 | `jar_of_borrowed_breath` | Jar of Borrowed Breath | event | A small corked glass jar containing a pale violet spiral of breath or vapor that curls upward but never escapes. The jar itself is plain and old. |
| 95 | `broken_royal_weight` | Broken Royal Weight | event | A crowned ceremonial weight split by a large diagonal crack, with the two halves still touching at the base. The royal emblem is visibly bisected. |
| 96 | `petition_chisel` | Petition Chisel | event | A short stoneworker's chisel with a wooden striking end and several tiny petition lines incised along the flat blade. It looks used for carving words, not masonry alone. |
| 97 | `tablet_of_the_missing_name` | Tablet of the Missing Name | event | A thick stone tablet with a broad rectangular name panel deliberately scraped away, leaving rough borders around a smooth blank center. |
| 98 | `funerary_linen_coil` | Funerary Linen Coil | event | A compact roll of funerary linen with one long frayed strip unwinding downward. Faint violet funerary marks appear only on the outer wrap. |
| 99 | `mercy_counterweight` | Mercy Counterweight | event | A small hanging counterweight engraved with a feather on one face and a single shallow crack on the other. A tiny loop allows it to hang from a scale. |

### Boss relics — 69

| # | code | title | rarity | the object |
|---:|---|---|---|---|
| 100 | `unfinished_docket` | Unfinished Docket | boss | A half-bound docket with several written pages at the top and a conspicuously blank lower half. One loose sheet protrudes from the unfinished binding. |
| 101 | `red_ribboned_matter` | Red-Ribboned Matter | boss | A thick bundle of official papers wrapped in an exaggerated deep-red ribbon tied across both axes. A small gold seal sits at the knot. |
| 102 | `backlog_counterseal` | Backlog Counterseal | boss | A heavy round counterseal with three stepped concentric levels, each carrying a small backlog notch. It should look weighty, layered, and official. |
| 103 | `brass_service_bell` | Brass Service Bell | boss | An ornate old brass service bell with a domed body, visible clapper, and a ring of small queue numerals engraved around the base. |
| 104 | `priority_sash` | Priority Sash | boss | A broad ceremonial sash folded into a loop, with one large numbered priority medallion pinned at the lower crossing. Fringed ends hang unevenly. |
| 105 | `ivory_number_disc` | Ivory Number Disc | boss | A thick ivory service disc with a large hand-carved number at center and three tiny queue dots around the rim. The edge is slightly worn and chipped. |
| 106 | `access_seal_shard` | Access Seal-Shard | boss | A jagged fragment of a once-large royal seal, with a clear keyhole cut into the surviving face. Gold veins mark where it broke from the whole. |
| 107 | `testimony_seal_shard` | Testimony Seal-Shard | boss | A second jagged seal fragment, differently shaped, engraved with a quill crossing a small eye. Keep the broken edges asymmetrical. |
| 108 | `execution_seal_shard` | Execution Seal-Shard | boss | A narrow spear-shaped seal shard engraved with a downward sword. One edge is sharper and darker than the others. |
| 109 | `stamped_expedition_writ` | Stamped Expedition Writ | boss | A rolled expedition writ partly unfurled to reveal a winding route and three small milestone marks, finished with a large gold civic seal. |
| 110 | `civic_entry_warrant` | Civic Entry Warrant | boss | A stiff formal warrant framed by a simple city-gate arch motif. The central text area is sparse and the lower corner carries a royal/civic seal. |
| 111 | `inspectors_brass_charter` | Inspector's Brass Charter | boss | A rigid brass-edged charter plate with a central eye-over-balance engraving and four riveted corners. It should look inspectorial and durable rather than like a loose paper. |
| 112 | `continuance_fragment` | Continuance Fragment | boss | A torn charter fragment whose decorative border line continues cleanly off the broken edge, as if the law extends beyond the surviving piece. |
| 113 | `right_of_redress` | Right of Redress | boss | A formal writ with a circular return-arrow ornament curling around the seal at the bottom. The arrow should look engraved and historical, not UI-like. |
| 114 | `margin_of_appeal` | Margin of Appeal | boss | A legal page with an absurdly wide decorated margin taking nearly half the sheet, while the actual text is compressed into the remaining space. A small appeal ribbon hangs below. |
| 115 | `errata_ribbon` | Errata Ribbon | boss | A long narrow ribbon marked by a sequence of tiny stitched corrections, crosses, and replacement strokes. One end is folded back over itself. |
| 116 | `index_of_contradictions` | Index of Contradictions | boss | A compact index volume with three tabs on each side pointing in opposing directions. Two small index arrows on the cover contradict each other. |
| 117 | `registry_tab` | Registry Tab | boss | One oversized brass-and-ivory registry tab detached from a larger ledger, shaped like a broad stepped label with a short insertion tongue. |
| 118 | `custody_shackle` | Custody Shackle | boss | A single old iron cuff attached by a short chain to a tiny book-shaped lock plate. The cuff is open only a finger's width. |
| 119 | `master_release_key` | Master Release Key | boss | A large ornate master key whose bow resembles an open book and whose long bit has a single oversized release tooth. |
| 120 | `release_tag` | Release Tag | boss | A thick parchment tag with a broken chain ring still attached to its eyelet. The lower edge bears one broad gold release stripe. |
| 121 | `misdated_pocket_watch` | Misdated Pocket Watch | boss | An old pocket watch with two conflicting date rings and hands that disagree. One tiny calendar wheel peeks through the face. |
| 122 | `borrowed_minute` | Borrowed Minute | boss | A tiny removable minute-hand token attached to a short watch chain and one circular pivot piece, as though literally borrowed from a clock. |
| 123 | `deferred_appointment_book` | Deferred Appointment Book | boss | A small appointment book clasped shut by an hourglass-shaped metal latch. Several page tabs protrude, but none is open. |
| 124 | `identity_writ` | Identity Writ | boss | A formal identity parchment with an empty oval portrait medallion near the top and a tied seal at the bottom. The portrait space is deliberately blank. |
| 125 | `settled_ledger` | Settled Ledger | boss | A thick ledger closed with a horizontal balance-shaped clasp and a neat final ribbon marker tucked fully inside. No loose pages or unresolved tags remain. |
| 126 | `closure_writ` | Closure Writ | boss | A completed writ rolled inward from both top and bottom and tied at the center with a dark cord, leaving almost no text exposed. |
| 127 | `premise_slip` | Premise Slip | boss | A very small single slip of paper with one heavily decorated first line and a folded lower corner. It should look deliberately preliminary and incomplete. |
| 128 | `concordance_thread` | Concordance Thread | boss | One taut thread connecting two tiny brass page tabs, each facing the other. The thread forms a shallow arc and nothing else distracts from it. |
| 129 | `conclusion_leaf` | Conclusion Leaf | boss | A single leaf-shaped parchment slip with a strong final flourish at its pointed lower end. A tiny closing dot sits above the point. |
| 130 | `boundary_tally` | Boundary Tally | boss | A carved boundary stake with alternating road marks on one side and root-like cuts on the other. Three deep tally notches run down the center. |
| 131 | `counter_petition_twine` | Counter-Petition Twine | boss | Two cords twisted in opposite directions around a miniature rolled petition, ending in a firm central knot. The cords visibly oppose each other. |
| 132 | `signed_settlement` | Signed Settlement | boss | Two narrow settlement papers overlap and are joined by one shared signature seal at their crossing point. Both ends are neatly trimmed and calm. |
| 133 | `countersealed_ring_of_passage` | Countersealed Ring of Passage | boss | A thick old signet ring whose face is a deep arched doorway with a small path entering it. Leaf engraving runs around the band. |
| 134 | `countersealed_ring_of_restraint` | Countersealed Ring of Restraint | boss | A matching ring family silhouette, but the face contains one heavy chain link trapped inside an oval border. The band has tighter thorn-like engravings. |
| 135 | `countersealed_ring_of_keeping` | Countersealed Ring of Keeping | boss | The third matching ring uses a square lock and keyhole on its face, with two tiny leaf curls holding the lock in place. |
| 136 | `honey_spoon` | Honey Spoon | boss | A simple old silver spoon with a long narrow handle and one visible drop of honey hanging from the bowl. A tiny domestic floral engraving sits near the grip. |
| 137 | `better_chair_cushion` | Better Chair Cushion | boss | A thick tufted chair cushion with four corner tassels and one neatly stitched clause mark in the center. It should feel domestic and old-fashioned. |
| 138 | `last_slice_tin` | Last-Slice Tin | boss | A round lidded tin whose lid has a pie-slice-shaped gap revealing the final piece inside. Small old floral marks ring the rim. |
| 139 | `surveyed_milestone` | Surveyed Milestone | boss | A standing milestone carved with three clear horizontal threshold lines at descending heights and a small route mark above them. |
| 140 | `survey_cairn` | Survey Cairn | boss | A carefully balanced cairn of four stones with a small sealed tablet visibly buried beneath the lowest stone. The stack is stable and symmetrical. |
| 141 | `loadstone_cairn` | Loadstone Cairn | boss | A rough cairn built around one dark magnetic lodestone at the center, with two tiny iron fragments visibly pulled toward it. The stack is more irregular than Survey Cairn. |
| 142 | `royal_grace_cup` | Royal Grace Cup | boss | An ornate fae court chalice with three small reliefs around the bowl: a flame/spark, a parchment leaf, and a shield. Keep the shape elegant and old, not jeweled excess. |
| 143 | `hollow_court_token` | Hollow-Court Token | boss | A royal token with a large hollow center and exactly three deep notches around its rim. A tiny crown sits at the top of the inner ring. |
| 144 | `silver_name_tally` | Silver Name-Tally | boss | A narrow silver tally stick with three groups of carved name-runes and a small loop at the top. The lower tip is pointed like an old counting tag. |
| 145 | `crown_of_the_three_names` | Crown of the Three Names | boss | A dramatic royal crown with three tall front plaques, each shaped like a tiny cartouche and each carrying a different abstract name-mark. Avoid modern lettering or Latin words. |
| 146 | `edict_of_the_open_audience` | Edict of the Open Audience | boss | A large formal scroll held open by two side rods, with the lower seal visibly broken and the text fully exposed. The scroll feels ceremonial rather than administrative-modern. |
| 147 | `eternal_cartouche` | Eternal Cartouche | boss | A thick unbroken cartouche loop of stone and gold with no beginning or end, enclosing a deep-purple empty field. The base is minimal. |
| 148 | `feather_of_perfect_measure` | Feather of Perfect Measure | boss | A long pale feather resting exactly level across a tiny gold balance line, with no visible pan. The quill tip aligns precisely with the center mark. |
| 149 | `acquittal_scarab` | Acquittal Scarab | boss | A funerary scarab with its wing cases open and a tiny broken chain falling beneath it. The body remains compact and symmetrical. |
| 150 | `balance_of_the_two_pans` | Balance of the Two Pans | boss | An ornate funerary balance with two shallow pans, one engraved with a small blade and the other with a small scroll. The central post uses a feather finial. |
| 151 | `impossible_capstone` | Impossible Capstone | boss | A heavy pyramid capstone drawn with subtly impossible perspective: one edge appears to meet two incompatible faces. Keep the geometry simple enough to redraw by hand. |
| 152 | `pyramidion_of_repetition` | Pyramidion of Repetition | boss | A small pyramidion with a faint second outline echoing behind it, offset just enough to imply repetition. The front face has one repeating chevron motif. |
| 153 | `crooked_plumb_line` | Crooked Plumb Line | boss | A plumb bob hanging from a visibly bent or kinked cord, yet the heavy bob still points straight down. A short crossbar anchors the top. |
| 154 | `black_granary_key` | Black Granary Key | boss | A large blackened key whose bow is shaped like a grain sheaf and whose bit resembles a simple granary door latch. It should look heavy and agricultural. |
| 155 | `granary_reserve_seal` | Granary Reserve Seal | boss | A round reserve seal showing a tightly bound sheaf of grain surrounded by a thick raised rim. A tiny fullness line arcs beneath the sheaf. |
| 156 | `ration_seal` | Ration Seal | boss | A divided ration seal with one simple loaf at center and four wedge-like measure marks around the edge. The form is flatter and more utilitarian than the Reserve Seal. |
| 157 | `palimpsest_reed` | Palimpsest Reed | boss | A reed pen whose current dark stroke is shadowed by one faint older stroke beneath it, as if writing over erased text. The reed itself is slim and traditional. |
| 158 | `erasure_tablet` | Erasure Tablet | boss | A stone writing tablet with a large scraped-smooth center, faint former lines barely visible at the edges, and a small pile of scraping dust below. |
| 159 | `correction_reed` | Correction Reed | boss | A reed pen with a tiny hooked scraping blade at the opposite end, making it a two-ended writing/correction tool. One corrected line sits beside it. |
| 160 | `canopic_cabinet` | Canopic Cabinet | boss | A compact four-compartment wooden cabinet with four simple canopic-jar silhouette plaques on the doors. The cabinet stands on short block feet and has one side ring. |
| 161 | `resin_shroud` | Resin Shroud | boss | A folded funerary shroud draped into a broad triangular silhouette, with three glossy black resin bands crossing the linen. The edges are frayed and old. |
| 162 | `basin_of_black_natron` | Basin of Black Natron | boss | A shallow stone ritual basin filled with coarse black natron crystals, with one small wooden scoop resting against the rim. The basin is low and wide. |
| 163 | `triune_office_seal` | Triune Office Seal | boss | A large triangular or three-lobed royal seal divided into three equal office fields, each carrying a distinct tiny abstract emblem. The three fields meet at one central boss mark. |
| 164 | `staff_of_the_kings_mouth` | Staff of the King's Mouth | boss | A tall ceremonial staff topped by a stylized open mouth beneath a small royal seal, with a ribbon of speech-like parchment curling from one side. Keep it solemn, not comic. |
| 165 | `vacant_throne_decree` | Vacant-Throne Decree | boss | A large decree scroll unfurled in front of a clearly empty throne silhouette cut into the lower field. The royal seal hangs above the empty seat rather than on a person. |
| 166 | `sluice_gate_of_the_two_lands` | Sluice Gate of the Two Lands | boss | A miniature double sluice gate with two adjacent shutters: one visibly raised, one lowered. Reed motifs flank the structure and a small water line runs beneath. |
| 167 | `flood_reckoning_crown` | Flood-Reckoning Crown | boss | A royal crown whose band carries four stepped water-level marks and reed finials. One side rises slightly higher, hinting at changing flood state. |
| 168 | `black_flood_vessel` | Black Flood Vessel | boss | A broad black ceramic flood jar with a carved wave band and a single dark stream spilling over one side. The jar body is almost silhouette-black against the ivory icon field. |

### Elite relics — 38

| # | code | title | rarity | the object |
|---:|---|---|---|---|
| 169 | `case_that_climbed` | The Case That Climbed | elite | A brass document clip holding a sheaf worn through at the top sheet. Three notches are cut into the clip's spine, one deeper than the other two. |
| 170 | `half_signed_page` | Half-Signed Page | elite | A petition leaf whose signature breaks off mid-stroke, the trailing ink drying into a dotted line that never resumes. |
| 171 | `remittitur_seal` | Remittitur Seal | elite | A wax seal split down the middle, one half red and one half black, held together by a single linen thread. |
| 172 | `thrice_struck_appointment` | Thrice-Struck Appointment Card | elite | A stiff card ruled into three fields, each stamped with a different hour and each struck through with one pen line. |
| 173 | `shutter_key_late_hour` | Shutter Key of the Late Hour | elite | A heavy iron shutter key with a hinged brass plate reading OPEN on one face and CLOSED on the other. |
| 174 | `chair_of_the_ninth_hour` | Chair of the Ninth Hour | elite | A waiting-room chair whose seat is hollowed by centuries of sitting, a numeral nine burnt into the backrest. |
| 175 | `contempt_ledger_nail` | Contempt Ledger Nail | elite | An iron nail driven through a folded warrant into a scrap of oak. |
| 176 | `inventory_lantern_glass` | Inventory Lantern Glass | elite | A single pane of smoked lantern glass in a pewter mount. |
| 177 | `gate_chain_counterweight` | Gate-Chain Counterweight | elite | A lead counterweight cast in the shape of a gatehouse, hanging from three chain links. |
| 178 | `sealed_spearhead` | Sealed Spearhead | elite | An iron spearhead whose socket is sealed shut with red wax poured over a rolled notice. |
| 179 | `cracked_bell_lip` | Cracked Bell-Lip | elite | A curved fragment of bronze bell lip, its strike point worn into a shallow dish. |
| 180 | `roller_pin_of_the_stacks` | Roller Pin of the Stacks | elite | A short stone roller on a pewter axle. |
| 181 | `blank_line_black_book` | Blank Line in the Black Book | elite | A black catalogue open at a page of three ruled lines — two filled with names scratched out to illegibility, the third still empty. |
| 182 | `the_unspoken_word` | The Unspoken Word | elite | Two small marble word-blocks mounted a hand's width apart on a pewter bar. |
| 183 | `strip_of_censoring_ink` | Strip of Censoring Ink | elite | A single strip of lacquered black ink laid across a line of text on a catalogue plaque. |
| 184 | `line_between_the_volumes` | The Line Between the Volumes | elite | A red thread strung taut between two brass book-clasps. |
| 185 | `drawer_within_a_drawer` | Drawer Within a Drawer | elite | A small oak drawer front with a second, smaller drawer front carved into its face, and a third begun inside that. |
| 186 | `missing_present_hand` | The Missing Present Hand | elite | A slender clock hand of blued steel with no counterweight, lying loose beside an empty arbor hole. |
| 187 | `the_third_ending` | The Third Ending | elite | A single obituary column set in three parallel strips of type. |
| 188 | `pre_approved_antler_tine` | Pre-Approved Antler Tine | elite | A single antler tine sawn flat at the base and drilled for a cord. |
| 189 | `mended_thread` | Mended Thread of the Grandmother | elite | A length of white spider-silk repaired in three places with darker household thread. |
| 190 | `toll_stone_wrong_bank` | Toll-Stone from the Wrong Bank | elite | A flat river stone with a toll mark chiselled into one face. |
| 191 | `coin_left_in_the_throat` | Coin Left in the Throat | elite | A swollen bronze coin, its face worn blank on one side by something that held it a long time. |
| 192 | `head_of_the_line` | The Head of the Line | elite | Three white bark strips laid end to end on a pewter tray, each notched at exactly the same interval. |
| 193 | `bone_tag_from_the_juniper` | Bone Tag from the Juniper | elite | A thin bone tag pierced and hung on a juniper twig, one edge scorched. |
| 194 | `obsolete_boundary_cord` | Obsolete Boundary Cord | elite | A coil of tarred measuring cord with three brass tags, two of them struck through. |
| 195 | `reed_cut_at_the_hearing` | Reed Cut at the Hearing | elite | Three black-water reeds bound with a linen strip — the shortest cut clean, the other two torn. |
| 196 | `thorn_chosen_from_three` | Thorn Chosen From Three | elite | Three long blackthorns mounted in a pewter clasp like a set of pen nibs. |
| 197 | `the_shorter_ferrule` | The Shorter Ferrule | elite | A surveyor's cord with a brass ferrule at each end, cut to two slightly different measures. |
| 198 | `broken_granary_seal` | Broken Granary Seal | elite | A fired-clay granary seal split cleanly in two. |
| 199 | `cut_corvee_rope` | Cut Corvée Rope | elite | A thick hemp rope cut through and whipped at both ends with linen. |
| 200 | `glyph_struck_from_the_name` | Glyph Struck From the Name | elite | An oval cartouche of gilded plaster with one glyph chiselled out. |
| 201 | `overseers_linen_shears` | Overseer's Linen Shears | elite | Long bronze linen shears with **one blade wrapped in its own bandage**. |
| 202 | `weight_from_the_lighter_pan` | Weight From the Lighter Pan | elite | A small lead weight from a two-pan balance. |
| 203 | `riddles_third_answer` | The Riddle's Third Answer | elite | A limestone tablet with three answers cut into it. |
| 204 | `lamp_thiefs_wick` | The Lamp Thief's Wick | elite | A brass lamp wick-holder with a length of unburnt wick still in it, the reservoir dry and the glass long gone. |
| 205 | `decan_star_table_chip` | Decan Star-Table Chip | elite | A broken corner of a star-table in dark schist, six decan glyphs running down it in a column. |
| 206 | `processional_step_stone` | Processional Step-Stone | elite | A single flagstone from a processional way, worn into a shallow footprint **exactly at its centre**. |

### Mimic relics — 4

| # | code | title | rarity | the object |
|---:|---|---|---|---|
| 207 | `tooth_from_the_false_lid_1` | Tooth From the False Lid I | mimic | The first blow of every fight glances off the tooth. It comes with a splinter of the oak it grew through. |
| 208 | `tooth_from_the_false_lid_2` | Tooth From the False Lid II | mimic | The first blow of every fight glances off the tooth. It comes with a brass hinge, still screwed to it. |
| 209 | `tooth_from_the_false_lid_3` | Tooth From the False Lid III | mimic | The first blow of every fight glances off the tooth. It comes with the lock-plate, its keyhole ringed with tooth-marks. |
| 210 | `tooth_from_the_false_lid_4` | Tooth From the False Lid IV | mimic | The first blow of every fight glances off the tooth. It comes with the whole false lid, folded shut around it like a jaw at rest. |

## Bodies — 269 pictures

Every enemy, elite and boss in the game. No visual canon was written for the bodies, so
the row carries what the fight itself says: how much it can take, how many rooms use it,
and one of those rooms by name. The role is what the picture has to BE — a body that is
ever met in a boss room is drawn as a boss, even if it also turns up as filler.

### Boss bodies — 35

| code | title | HP | rooms | met in |
|---|---|---:|---:|---|
| `architect_of_the_impossible_pyramid` | The Architect of the Impossible Pyramid | 640 | 1 | The Architect of the Impossible Pyramid |
| `auditor_of_returned_lives` | The Auditor of Returned Lives | 288 | 1 | The Auditor of Returned Lives |
| `captain_of_the_inner_stair` | Captain of the Inner Stair | 124 | 1 | The Vizier of the King's Mouth |
| `curator_of_misplaced_hours` | The Curator of Misplaced Hours | 278 | 1 | The Curator of Misplaced Hours |
| `deputy_undersecretary` | The Deputy Undersecretary | 130 | 1 | The Deputy Undersecretary |
| `enlil_voice_of_the_unalterable_decree` | Enlil, Voice of the Unalterable Decree | 700 | 1 | The Unalterable Decree |
| `first_scribe_of_the_house_of_life` | The First Scribe of the House of Life | 580 | 1 | The First Scribe of the House of Life |
| `gcr_authority` | The Authority | 72 | 1 | The Grand Cross-Reference |
| `gcr_conclusion` | The Conclusion | 76 | 1 | The Grand Cross-Reference |
| `gcr_premise` | The Premise | 68 | 1 | The Grand Cross-Reference |
| `grand_cross_reference` | The Grand Cross-Reference | 96 | 1 | The Grand Cross-Reference |
| `grandmother_clause` | Grandmother Clause | 350 | 1 | Grandmother Clause |
| `inanna_mistress_of_the_eanna_ledger` | Inanna, Mistress of the Eanna Ledger | 760 | 1 | The Ledger of Eanna |
| `keeper_of_tallies` | Keeper of Tallies | 116 | 1 | The Vizier of the King's Mouth |
| `lady_of_the_black_granaries` | The Lady of the Black Granaries | 600 | 1 | The Lady of the Black Granaries |
| `living_charter` | The Living Charter | 134 | 1 | The Living Charter |
| `lord_sealkeeper` | The Lord Sealkeeper | 136 | 1 | The Lord Sealkeeper |
| `mother_of_natron_and_resin` | The Mother of Natron and Resin | 610 | 1 | The Mother of Natron and Resin |
| `municipal_dragon` | The Municipal Dragon | 142 | 1 | The Municipal Dragon |
| `nanna_sin_lord_of_the_counted_moon` | Nanna-Sin, Lord of the Counted Moon | 700 | 1 | The Counted Moon |
| `nanshe_keeper_of_the_just_ration` | Nanshe, Keeper of the Just Ration | 600 | 1 | The Just Ration |
| `nisaba_keeper_of_the_first_tablet` | Nisaba, Keeper of the First Tablet | 620 | 1 | The First Tablet |
| `notary_of_old_growth` | The Notary of Old Growth | 360 | 1 | The Notary of Old Growth |
| `ombudsman_of_root_and_road` | The Ombudsman of Root and Road | 342 | 1 | The Ombudsman of Root and Road |
| `pharaoh_of_the_sealed_name` | The Pharaoh of the Sealed Name | 630 | 1 | The Pharaoh of the Sealed Name |
| `queen_of_the_flood_reckoning` | The Queen of the Flood Reckoning | 620 | 1 | The Queen of the Flood Reckoning |
| `queen_under_the_hill` | The Queen Under the Hill | 392 | 1 | The Queen Under the Hill |
| `queue_commissioner` | The Queue Commissioner | 126 | 1 | The Queue Commissioner |
| `royal_seal_bearer` | Royal Seal Bearer | 110 | 1 | The Vizier of the King's Mouth |
| `the_answering_hill` | The Answering Hill | 374 | 1 | The Answering Hill |
| `utu_witness_of_every_oath` | Utu, Witness of Every Oath | 660 | 1 | Every Oath Ever Sworn |
| `vizier_of_the_kings_mouth` | The Vizier of the King's Mouth | 590 | 1 | The Vizier of the King's Mouth |
| `warden_of_sealed_volumes` | The Warden of Sealed Volumes | 270 | 1 | The Warden of Sealed Volumes |
| `weigher_of_the_unspoken_heart` | The Weigher of the Unspoken Heart | 610 | 1 | The Weigher of the Unspoken Heart |
| `whispering_catalogue_boss` | The Whispering Catalogue | 258 | 1 | The Whispering Catalogue |

### Elite bodies — 66

| code | title | HP | rooms | met in |
|---|---|---:|---:|---|
| `after_hours_return_bell` | After-Hours Return Bell | 118 | 1 | After-Hours Return Bell |
| `ant_queen_of_the_proper_line` | Ant Queen of the Proper Line | 160 | 1 | Ant Queen of the Proper Line |
| `archivists_hound` | Archivist's Hound | 76 | 1 | The Archivist's Hound |
| `bailiff_of_warrants` | Bailiff of Warrants | 50 | 1 | The Bailiff Twins |
| `bailiff_of_writs` | Bailiff of Writs | 52 | 1 | The Bailiff Twins |
| `black_ink_oracle` | Black-Ink Oracle | 150 | 1 | Black-Ink Oracle |
| `catalogue_of_unwise_names` | The Catalogue of Unwise Names | 136 | 1 | The Catalogue of Unwise Names |
| `colossus_of_the_endless_procession` | Colossus of the Endless Procession | 388 | 1 | Colossus of the Endless Procession |
| `counter_of_certification` | Counter of Certification | 42 | 1 | The Three Counters |
| `counter_of_delay` | Counter of Delay | 38 | 1 | The Three Counters |
| `counter_of_denial` | Counter of Denial | 40 | 1 | The Three Counters |
| `curse_bearer` | Curse-Bearer | 108 | 1 | The Tombbreakers Three |
| `devouring_waiting_room` | Devouring Waiting Room | 68 | 1 | The Waiting Room Eats the Day |
| `drawer_of_infinite_returns` | The Drawer of Infinite Returns | 154 | 1 | The Drawer of Infinite Returns |
| `escalation_writ` | Escalation Writ | 30 | 1 | The Case Returns Higher |
| `final_appointment` | Final Appointment | 34 | 1 | The Three Appointments |
| `final_notice_knight` | Final Notice Knight | 62 | 1 | The Final Notice Arrives Armed |
| `first_appointment` | First Appointment | 24 | 1 | The Three Appointments |
| `first_line_bearer` | First Line-Bearer | 27 | 1 | Ant Queen of the Proper Line |
| `grandmother_web` | Grandmother Web | 154 | 1 | Grandmother Web |
| `great_toll_frog` | Great Toll Frog | 176 | 1 | Great Toll Frog |
| `hearing_reed` | Hearing Reed | 78 | 1 | Three Reeds of Appeal |
| `ink_witch_auditor` | Ink Witch Auditor | 72 | 1 | The Ink Witch Auditor |
| `inventory_lantern` | Inventory Lantern | 24 | 1 | The Seizure Procession |
| `iron_warrant_avatar` | Iron Warrant Avatar | 94 | 1 | The Warrant Becomes Iron |
| `juniper_injunction` | Juniper Injunction | 188 | 1 | Juniper Injunction |
| `keeper_of_the_living_cartouche` | Keeper of the Living Cartouche | 300 | 1 | Keeper of the Living Cartouche |
| `keeper_of_the_thirty_six_decans` | Keeper of the Thirty-Six Decans | 365 | 1 | Keeper of the Thirty-Six Decans |
| `lamp_thief` | Lamp Thief | 100 | 1 | The Tombbreakers Three |
| `licensed_chimera` | Licensed Chimera | 90 | 1 | The Licensed Chimera |
| `living_petition_chorus` | Living Petition Chorus | 90 | 1 | The Petition Reads Itself Aloud |
| `lock_cart` | Lock Cart | 32 | 1 | The Seizure Procession |
| `lower_appellate_step` | Lower Appellate Step | 24 | 1 | The Appeal Climbs the Stairs |
| `magistrate_of_thorns` | Magistrate of Thorns | 220 | 1 | Magistrate of Thorns |
| `middle_appellate_step` | Middle Appellate Step | 30 | 1 | The Appeal Climbs the Stairs |
| `minute_moth_cloud` | Minute-Moth Cloud | 24 | 1 | The Waiting Room Eats the Day |
| `mummified_overseer_of_the_linen_house` | Mummified Overseer of the Linen House | 318 | 1 | Mummified Overseer of the Linen House |
| `obituary_with_three_endings` | The Obituary with Three Endings | 128 | 1 | The Obituary with Three Endings |
| `portcullis_judicator` | Portcullis Judicator | 98 | 1 | The Gatehouse Drops the Portcullis |
| `presentless_clock` | Presentless Clock | 158 | 1 | Presentless Clock |
| `pry_bar_veteran` | Pry-Bar Veteran | 112 | 1 | The Tombbreakers Three |
| `red_tape_golem` | Red Tape Golem | 96 | 1 | The Red Tape Golem |
| `refusal_reed` | Refusal Reed | 90 | 1 | Three Reeds of Appeal |
| `remand_reed` | Remand Reed | 84 | 1 | Three Reeds of Appeal |
| `remanded_case_phantom` | Remanded Case Phantom | 60 | 1 | The Case Returns Higher |
| `reopening_hours_monolith` | Reopening-Hours Monolith | 92 | 1 | The Office That Reopens Itself |
| `rolling_stacks_colossus` | The Rolling Stacks Colossus | 132 | 1 | The Rolling Stacks Colossus |
| `rope_master_of_the_corvee` | Rope-Master of the Corvée | 275 | 1 | Rope-Master of the Corvée |
| `scarab_host_of_the_sealed_granary` | Scarab Host of the Sealed Granary | 255 | 1 | Scarab Host of the Sealed Granary |
| `sealed_spear` | Sealed Spear | 30 | 1 | The Final Notice Arrives Armed |
| `second_appointment` | Second Appointment | 28 | 1 | The Three Appointments |
| `second_line_bearer` | Second Line-Bearer | 27 | 1 | Ant Queen of the Proper Line |
| `seizure_marshal` | Seizure Marshal | 40 | 1 | The Seizure Procession |
| `senior_clerk` | The Senior Clerk | 82 | 1 | The Senior Clerk |
| `silence_between_two_words` | The Silence Between Two Words | 140 | 1 | The Silence Between Two Words |
| `sphinx_of_the_processional_measure` | Sphinx of the Processional Measure | 344 | 1 | Sphinx of the Processional Measure |
| `stag_of_pre_approved_violence` | The Stag of Pre-Approved Violence | 138 | 1 | The Stag of Pre-Approved Violence |
| `stampede_of_stamps` | Stampede of Stamps | 66 | 1 | The Stampede of Stamps |
| `surveyor_of_forgotten_paths` | Surveyor of Forgotten Paths | 198 | 1 | Surveyor of Forgotten Paths |
| `surveyor_of_the_errant_cord` | Surveyor of the Errant Cord | 248 | 1 | Surveyor of the Errant Cord |
| `the_wrong_bridge_in_person` | The Wrong Bridge in Person | 200 | 1 | The Wrong Bridge in Person |
| `third_line_bearer` | Third Line-Bearer | 27 | 1 | Ant Queen of the Proper Line |
| `treasury_of_the_two_pans` | The Treasury of the Two Pans | 330 | 1 | The Treasury of the Two Pans |
| `upper_appellate_step` | Upper Appellate Step | 36 | 1 | The Appeal Climbs the Stairs |
| `volume_of_causes` | Volume of Causes | 76 | 1 | Volumes of Cause and Consequence |
| `volume_of_consequences` | Volume of Consequences | 84 | 1 | Volumes of Cause and Consequence |

### Mimic bodies — 4

| code | title | HP | rooms | met in |
|---|---|---:|---:|---|
| `brass_maw_of_returns` | Brass Maw of Returns | 50 | 3 | Brass Maw of Returns |
| `cairn_of_stray_paths` | Cairn of Stray Paths | 58 | 5 | Every Detour Leaves a Stone |
| `cursed_loot_bearer` | Cursed Loot Bearer | 156 | 3 | Every Object Requires a Form |
| `receipt_mimic` | Receipt Mimic | 52 | 1 | Suspicious Receipt Chest |

### Standard bodies — 164

| code | title | HP | rooms | met in |
|---|---|---:|---:|---|
| `a_very_official_line` | A Very Official Line | 29 | 3 | A Very Official Line |
| `appeals_clerk` | Appeals Clerk | 38 | 1 | Your Complaint Is Accepted |
| `appointment_leech` | Appointment Leech | 31 | 1 | A Drained Appointment |
| `blackthorn_bride` | The Blackthorn Bride | 101 | 3 | The Bride at the Threshold |
| `blank_death_certificate` | Blank Death Certificate | 100 | 1 | Blank Death Certificate |
| `blank_line_leech` | Blank-Line Leech | 45 | 1 | Required Field Left Blank |
| `blue_slip_sprite` | Blue Slip Sprite | 23 | 1 | Blue Slip, Wrong Door |
| `bracken_moot` | The Bracken Moot | 98 | 2 | Boundary Hearing |
| `bylaw_gargoyle` | Bylaw Gargoyle | 48 | 1 | The Bylaw Looks Down |
| `bylaw_hydra` | Bylaw Hydra | 44 | 1 | The Bylaw Grows Heads |
| `chain_of_office_specter` | Chain of Office Specter | 40 | 1 | Chain of Office |
| `charter_shell_snail` | Charter-Shell Snail | 90 | 2 | Shell Charter |
| `checkout_codex` | Checkout Codex | 89 | 2 | Checkout Codex |
| `choir_of_unspoken_words` | Choir of Unspoken Words | 64 | 1 | Choir of Unspoken Words |
| `civic_battering_ram` | Civic Battering Ram | 69 | 1 | The Civic Ram Advances |
| `civic_bell_ringer` | Civic Bell-Ringer | 34 | 1 | The Bell and the Seal |
| `clerkling_apprentice` | Clerkling Apprentice | 28 | 2 | First Day at the Counter |
| `closing_bell_specter` | Closing Bell Specter | 38 | 1 | The Bell Will Not Stop |
| `cobblestone_bailiff` | Cobblestone Bailiff | 38 | 2 | Cobblestone Enforcement |
| `cobra_of_the_entry_mark` | Cobra of the Entry Mark | 94 | 3 | The Entry Mark |
| `compliance_chain_guard` | Compliance Chain Guard | 42 | 2 | Chains of Compliance |
| `contradictory_signpost` | Contradictory Signpost | 49 | 1 | Mandatory in Every Direction |
| `contrary_magpie` | Contrary Magpie | 73 | 2 | Contrary Permit |
| `cornerstone_oath_stone` | Cornerstone Oath-Stone | 137 | 2 | Oath in the Foundation |
| `corridor_in_the_wrong_edition` | Corridor in the Wrong Edition | 60 | 1 | Corridor in the Wrong Edition |
| `counter_bell_toad` | Counter Bell Toad | 30 | 1 | The Bell Calls Someone Else |
| `counterclaim_imp` | Counterclaim Imp | 45 | 2 | Counterclaim in Red Ink |
| `crabwise_shelf` | Crabwise Shelf | 56 | 2 | Crabwise Shelf |
| `crocodile_beneath_the_balance` | Crocodile Beneath the Balance | 196 | 2 | Jaws Beneath the Scale |
| `crocodile_of_the_short_measure` | Crocodile of the Short Measure | 100 | 2 | The Short Measure |
| `crooked_rod_bearer` | Crooked Rod Bearer | 88 | 2 | The Crooked Standard |
| `crossroads_cup` | Crossroads Cup | 82 | 4 | A Cup at the Crossroads |
| `dead_letter_ouroboros` | Dead-Letter Ouroboros | 47 | 3 | Dead-Letter Ouroboros |
| `detached_footnote` | Detached Footnote | 86 | 2 | Orphan Citation, See Footnote |
| `ditch_lamprey_of_appeals` | Ditch Lamprey of Appeals | 98 | 2 | Upstream Appeal |
| `donkey_of_the_third_tally` | Donkey of the Third Tally | 119 | 1 | The Donkey Was Counted Three Times |
| `drowned_field_scribe` | Drowned Field Scribe | 106 | 2 | Silted Record |
| `duplicate_copy_mite` | Duplicate Copy Mites | 37 | 2 | Certified Pest Control |
| `eclipse_scarab` | Eclipse Scarab | 174 | 1 | Black Noon |
| `elsewhere_path` | Elsewhere Path | 113 | 2 | The Hawthorn Destination |
| `embossed_seal` | Embossed Seal | 24 | 1 | The Certificate Is Alive |
| `empty_handed_envoy` | Empty-Handed Envoy | 102 | 1 | Nothing Was Presented, Yet the Fee Remains |
| `errant_boundary_stone` | Errant Boundary Stone | 75 | 5 | The Errant Line |
| `errata_doppelganger` | Errata Doppelgänger | 85 | 2 | Errata Doppelgänger |
| `eternal_reed_scribe` | Eternal Reed Scribe | 177 | 2 | The Eternal Shift |
| `exception_imp` | Exception Imp | 40 | 2 | Exception to the Exception |
| `expunged_name` | Expunged Name | 72 | 1 | Expunged Name |
| `fading_number_token` | Fading Number Token | 43 | 2 | Your Number Fades |
| `fallen_capstone_golem` | Fallen Capstone Golem | 145 | 2 | The Capstone Is Already Above You |
| `false_door_finder` | False-Door Finder | 150 | 2 | This Entrance Is Legally Valid |
| `false_seal_forger` | False-Seal Forger | 124 | 3 | Counterfeit Venom |
| `fanged_alphabet` | Fanged Alphabet | 58 | 1 | Fanged Alphabet |
| `fatal_comma` | Fatal Comma | 77 | 2 | Fatal Comma |
| `feather_bearer` | Feather-Bearer | 184 | 2 | Feather's True Measure |
| `filing_beetle` | Filing Beetle | 40 | 2 | Courier at the Filing Cabinet |
| `flood_mark_reader` | Flood-Mark Reader | 105 | 2 | Read the Floodmark |
| `folded_affidavit_bat` | Folded Affidavit Bat | 30 | 1 | Folded Statement Overhead |
| `footfall_root` | Footfall Root | 124 | 2 | Footsteps Become Precedent |
| `foreign_tribute_shade` | Foreign Tribute Shade | 115 | 2 | Correct Tribute, Wrong Procedure |
| `form_rat_a` | Form Rat | 11 | 1 | Forms in the Gutter |
| `form_rat_b` | Form Rat | 11 | 1 | Forms in the Gutter |
| `form_rat_c` | Form Rat | 11 | 1 | Forms in the Gutter |
| `form_rat_swarm` | Form Rat Swarm | 22 | 1 | Paper Vermin |
| `fourfold_vessel_guardian` | Fourfold Vessel Guardian | 170 | 3 | The Fourfold Office |
| `foxglove_witness` | Foxglove Witness | 75 | 2 | Roadside Testimony |
| `gatehouse_enforcer` | Gatehouse Enforcer | 50 | 1 | The Gatehouse Enforces Procedure |
| `golden_ushabti_captain` | Golden Ushabti Captain | 194 | 2 | The Eternal Shift |
| `handworn_tally_coin` | Handworn Tally Coin | 113 | 2 | Every Name Has Value |
| `hawthorn_tenant` | The Hawthorn Tenant | 83 | 3 | Thorn Lease |
| `hieroglyphic_complaint_wall` | Hieroglyphic Complaint Wall | 150 | 2 | Undismissed Complaint |
| `hourglass_with_two_bottoms` | Hourglass With Two Bottoms | 96 | 2 | Hourglass With Two Bottoms |
| `hungry_grain_thief` | Hungry Grain Thief | 96 | 2 | Granary Theft |
| `ink_spattered_scribe` | Ink-Spattered Scribe | 30 | 3 | Ink and Wax |
| `inverted_hourglass` | Inverted Hourglass | 51 | 2 | The Sand Runs Upward |
| `jar_seal_scarab_swarm` | Jar-Seal Scarab Swarm | 94 | 1 | The Jar Seal Breaks |
| `jurisdiction_snail` | Jurisdiction Snail | 30 | 1 | Not This Jurisdiction |
| `jurisdictional_clerkling` | Jurisdictional Clerkling | 19 | 1 | A Matter for Another Desk |
| `keeper_of_buried_names` | Keeper of Buried Names | 132 | 4 | Names Kept Below Ground |
| `kneeling_petitioners` | Kneeling Petitioners | 120 | 3 | Processional Seal |
| `lantern_inspector` | Lantern Inspector | 33 | 1 | Lamp and Ledger |
| `levy_constable` | Levy Constable | 44 | 1 | The Levy Comes Due |
| `line_cutter_weasel` | Line-Cutter Weasel | 20 | 1 | The Line Moves Without You |
| `linen_wrapped_embalmer` | Linen-Wrapped Embalmer | 150 | 3 | Instructions for Wrapping |
| `living_certificate` | Living Certificate | 28 | 1 | The Certificate Is Alive |
| `mandated_mushroom_circle` | Mandated Mushroom Circle | 94 | 2 | Hare Before the Quorum |
| `margin_note_gnawer` | Margin-Note Gnawer | 33 | 1 | Correction in the Margin |
| `minor_tax_familiar` | Minor Tax Familiar | 29 | 1 | Minor Tax Assessment |
| `minute_moth` | Minute Moth | 36 | 1 | Minutes Become Moths |
| `miscellany_index` | Miscellany Index | 107 | 2 | Miscellany Index |
| `misfiled_page` | Misfiled Page | 27 | 1 | The Page That Followed You |
| `mnemonic_chain` | Mnemonic Chain | 93 | 2 | Mnemonic Chain |
| `moon_cycle_ibis` | Moon-Cycle Ibis | 155 | 1 | Fixed-Day Moon |
| `mossbound_clerk` | Mossbound Clerk | 70 | 2 | The First Use Became Custom |
| `motion_to_reconsider` | Motion to Reconsider | 40 | 1 | Motion to Reconsider |
| `municipal_gargoyle` | Municipal Gargoyle | 48 | 1 | Stone Ordinance |
| `mute_margin` | Mute Margin | 70 | 1 | Mute Margin |
| `name_eating_baboon` | Name-Eating Baboon | 90 | 2 | Chewed Credentials |
| `name_erasing_chisel_spirit` | Name-Erasing Chisel Spirit | 166 | 2 | Erase the Favor |
| `natron_bearer` | Natron Bearer | 144 | 1 | Dry What Would Decay |
| `number_ticket_wisp` | Number-Ticket Wisp | 25 | 1 | Now Serving the Wrong Number |
| `oath_candle` | Oath Candle | 39 | 1 | Witness at the Sealed Threshold |
| `oathbound_gate` | Oathbound Gate | 224 | 2 | Oathbound Gate |
| `object_listed_as_other` | Object Listed as "Other" | 54 | 2 | Object Listed as "Other" |
| `objection_sprite` | Objection Sprite | 34 | 1 | An Objection Is Raised |
| `old_statute_ghost` | Old Statute Ghost | 54 | 2 | The Statute Was Never Repealed |
| `ordinance_tablet` | Ordinance Tablet | 46 | 1 | Subsection Carved in Stone |
| `orphan_citation` | Orphan Citation | 62 | 2 | Orphan Citation |
| `overdue_page` | Overdue Page | 15 | 2 | Overdue Delivery |
| `palette_bearing_apprentice` | Palette-Bearing Apprentice | 119 | 3 | Fresh Pigment |
| `palimpsest_husk` | Palimpsest Husk | 75 | 3 | Palimpsest Husk |
| `permit_beggar` | Permit Beggar | 27 | 2 | A Question of Permits |
| `permit_hare` | Permit Hare | 66 | 5 | The Hare Checks the Road |
| `pigeon_courier` | Pigeon Courier | 19 | 4 | Courier at the Filing Cabinet |
| `precedent_lichen` | Precedent Lichen | 98 | 2 | Two Authorities Agree |
| `procedural_advocate` | Procedural Advocate | 42 | 1 | The Procedure Argues Back |
| `public_hours_clerk` | Public-Hours Clerk | 34 | 1 | Public Hours End at Once |
| `queue_crier_homunculus` | Queue-Crier Homunculus | 31 | 2 | The Line Has Started Moving |
| `queue_imp` | Queue Imp | 22 | 3 | The Wrong Counter |
| `receipt_eyed_clerk` | Receipt-Eyed Clerk | 35 | 1 | Proof of Arrival |
| `reckoning_hedge` | Reckoning Hedge | 77 | 6 | Counter-Survey |
| `red_tape_serpent` | Red Tape Serpent | 42 | 1 | Red Tape Crossing |
| `reed_cord_surveyor` | Reed-Cord Surveyor | 85 | 2 | The Surveyor Measures the Road |
| `registry_moth` | Registry Moth | 21 | 2 | Dust in the Registry |
| `roadside_witchling` | Roadside Witchling | 92 | 2 | The Witch at the Milestone |
| `rope_gang_wraith` | Rope-Gang Wraith | 120 | 1 | Keep the Work Rhythm |
| `royal_genealogy_wall` | Royal Genealogy Wall | 188 | 2 | Dynastic Favor Claim |
| `runaway_laborer` | Runaway Laborer | 102 | 1 | Break the Gang |
| `seal_bearer_toad` | Seal-Bearer Toad | 28 | 3 | Ink and Wax |
| `seal_witness` | Seal Witness | 34 | 1 | Witnessed and Filed |
| `sealed_door_ward` | Sealed Door Ward | 56 | 2 | The Sealed Door |
| `second_person_entry` | Second-Person Entry | 60 | 4 | Second-Person Entry |
| `self_correcting_record` | Self-Correcting Record | 53 | 2 | The Record Corrects You |
| `silt_buried_farmer_shade` | Silt-Buried Farmer Shade | 109 | 1 | Rising Field |
| `sleeping_stump_auditor` | Sleeping Stump Auditor | 117 | 2 | The Old Measure |
| `spare_life_jar` | Spare-Life Jar | 83 | 2 | Dead-Letter Revival |
| `stamp_goblin` | Stamp Goblin | 22 | 3 | Stamping Errand |
| `star_table_scribe` | Star-Table Scribe | 160 | 2 | The Fixed Decan Measure |
| `stationary_queue_marker` | Stationary Queue Marker | 40 | 1 | The Queue Has Not Advanced |
| `stone_hauler_ushabti` | Stone-Hauler Ushabti | 128 | 2 | The Stones Grow Heavier |
| `streamside_oath_fish` | Streamside Oath-Fish | 84 | 2 | Oaths in Running Water |
| `street_law_writ` | Street-Law Writ | 38 | 1 | The Street Cites Precedent |
| `street_ordinance_wisp` | Street Ordinance Wisp | 22 | 2 | Flickering Ordinance |
| `sun_seal_bearer` | Sun-Seal Bearer | 134 | 3 | The Authorized Impression |
| `sustaining_gavel` | Sustaining Gavel | 44 | 1 | Sustained Counterclaim |
| `the_sedge_bench` | The Sedge Bench | 111 | 2 | Charter Review |
| `threshold_seizure_ward` | Threshold Seizure Ward | 61 | 2 | Seized at the Threshold |
| `tollhouse_sprite` | Tollhouse Sprite | 24 | 1 | The Tollhouse Dispute |
| `triplicate_examiner` | Triplicate Examiner | 41 | 2 | The Evidence Exists in Triplicate |
| `two_bank_toll_ford` | Two-Bank Toll Ford | 97 | 4 | Both Banks Demand Payment |
| `unclaimed_reading_table` | Unclaimed Reading Table | 66 | 2 | Unclaimed Reading Table |
| `uncounted_pilgrim` | Uncounted Pilgrim | 92 | 2 | No Number in the Register |
| `unfinished_mummy` | Unfinished Mummy | 160 | 3 | Hooks Still Attached |
| `unoccurred_tuesday` | Unoccurred Tuesday | 87 | 2 | Unoccurred Tuesday |
| `unsigned_form_ghost` | Unsigned Form Ghost | 43 | 1 | Unsigned in Triplicate |
| `untranslated_trail_marker` | The Untranslated Trail Marker | 121 | 2 | Three Readings on One Stone |
| `vacant_portrait` | Vacant Portrait | 81 | 2 | Vacant Portrait |
| `velvet_rope_mimic` | Velvet Rope Mimic | 22 | 1 | Held Behind the Velvet Rope |
| `volume_q_null` | Volume Q-Null | 52 | 2 | Volume Q-Null |
| `waiting_bench_gremlin` | Waiting-Bench Gremlin | 36 | 1 | A Bench Older Than Patience |
| `waiting_room_gnawer` | Waiting-Room Gnawer | 19 | 1 | Something Under the Bench |
| `warrant_bailiff` | Warrant Bailiff | 58 | 3 | Warrant Served in Person |
| `wax_notary` | Wax Notary | 48 | 2 | The Wax Is Still Warm |
| `waxen_bailiff` | Waxen Bailiff | 42 | 1 | The Waxen Bailiff |
| `wrong_window_scribe` | Wrong-Window Scribe | 25 | 2 | The Wrong Window |

## Cards — 254 pictures

No visual canon was ever written for the cards, so the row carries the card's own rules
text instead of a brief. Act = the act that unlocks it; the starters, the Junk and the
cards a boss or a door hands over are never offered and have no act.

### Starters and Junk — 8

| code | title | type | rarity | what it does |
|---|---|---|---|---|
| `cower_behind_a_desk` | Cower Behind a Desk | working | starter | Gain 5 Block. |
| `duplicate_copy` | Duplicate Copy | junk | junk | Exhaust. No other effect. |
| `misfiled_paper` | Misfiled Paper | junk | junk | Draw 1 card. Exhaust. |
| `paper_cut` | Paper Cut | deed | starter | Deal 6 damage. |
| `permit_a38` | Permit A38 | working | starter | Apply 5 Paperwork. |
| `red_tape` | Red Tape | junk | junk | Unplayable. |
| `strong_binder` | Strong Binder | working | starter | Gain 7 Block. Apply 1 Doubt. |
| `unsigned_form` | Unsigned Form | junk | junk | Exhaust. Add a fresh Unsigned Form to your discard pile. |

### Bureaucrat — Act I — 46

| code | title | type | rarity | what it does |
|---|---|---|---|---|
| `ash_register` | Ash Register | rite | uncommon | The first time each turn you Archive a card, draw 1 card. |
| `backlog_charge` | Backlog Charge | deed | uncommon | Deal 6 damage, plus 3 damage for each card currently in your Queue. Count at most 3 Queued cards. |
| `black_ledger` | Black Ledger | rite | uncommon | At the start of your turn, if any enemy has at least 8 Paperwork, draw 1 card. |
| `blank_warrant` | Blank Warrant | deed | rare | Deal 18 damage. If the target has no Paperwork, Doubt, or Seal, deal 5 additional damage. |
| `candle_allowance` | Candle Allowance | working | uncommon | Queue: Gain 1 Energy and draw 1 card. Exhaust. |
| `candle_tribunal` | Candle Tribunal | deed | rare | Deal 5 damage 3 times. If the target is Ratified, repeat this attack. |
| `cauldron_copy` | Cauldron Copy | deed | common | Deal 9 damage. Add 1 Duplicate Copy to your discard pile. |
| `certified_kindling` | Certified Kindling | working | common | Archive a card from your hand. Gain 4 Block. If it was Junk, gain 4 additional Block. |
| `cinder_warrant` | Cinder Warrant | deed | uncommon | Deal 7 damage. Archive a Junk card from your hand; if you do, repeat this attack. |
| `clerical_discretion` | Clerical Discretion | working | uncommon | Gain 5 Block. Choose one: apply 1 Doubt; or apply 1 Seal. |
| `clerks_familiar` | Clerk's Familiar | rite | uncommon | The first time each turn you create a Junk card, gain 4 Block. |
| `conditional_approval` | Conditional Approval | deed | uncommon | Deal 6 damage. If the target does not intend to Attack, apply 2 Seal; otherwise apply 1 Seal. |
| `continuance` | Continuance | rite | rare | At the end of your turn, retain up to 8 Block. |
| `counter_ward` | Counter Ward | working | uncommon | Gain 6 Block. Your next card this turn costs 1 less Energy. |
| `cursed_addendum` | Cursed Addendum | deed | common | Deal 6 damage. Apply 2 Paperwork. |
| `deferred_hex` | Deferred Hex | deed | common | Queue: Deal 13 damage. |
| `deskward` | Deskward | working | common | Gain 8 Block. Add 1 Red Tape to your discard pile. |
| `dubious_authority` | Dubious Authority | rite | uncommon | Whenever Doubt is consumed after an enemy attacks, apply 2 Paperwork to that enemy. |
| `fine_print_hex` | Fine-Print Hex | deed | common | Deal 7 damage. If the target has Doubt, apply 1 Seal. |
| `form_of_ill_intent` | Form of Ill Intent | working | common | Apply 3 Paperwork. If the target intends to Attack, also apply 1 Doubt. |
| `formal_dissent` | Formal Dissent | working | uncommon | Remove 1 Doubt from an enemy. Gain 1 Energy. Exhaust. |
| `hex_circular` | Hex Circular | deed | uncommon | Deal 7 damage to ALL enemies. Apply 1 Doubt to ALL enemies. |
| `inkblot_verdict` | Inkblot Verdict | deed | common | Deal 8 damage. If the target has Paperwork, deal 2 additional damage. |
| `licensed_disposal` | Licensed Disposal | rite | rare | The first Junk card you draw each turn is automatically Archived; then draw 1 card. |
| `night_docket` | Night Docket | working | uncommon | Resolve your oldest Queued card immediately. Add 1 Red Tape to your discard pile. Exhaust. |
| `notarial_press` | Notarial Press | working | common | Apply 2 Seal. If this Ratifies the target, gain 5 Block. |
| `notarys_tithe` | Notary's Tithe | working | uncommon | Remove 1 Seal from an enemy. Draw 2 cards. Exhaust. |
| `occult_precedent` | Occult Precedent | working | common | Gain 7 Block. If any enemy has Paperwork, gain 2 additional Block. |
| `pending_matters` | Pending Matters | rite | uncommon | The first time each turn a Queued card resolves, gain 3 Block. |
| `petty_objection` | Petty Objection | working | common | Gain 5 Block. Apply 1 Doubt. |
| `presumption_of_error` | Presumption of Error | working | uncommon | Apply 1 Doubt. The next time that enemy consumes Doubt by attacking, apply 1 Doubt to it after the Attack resolves. Exhaust. |
| `privy_seal` | Privy Seal | working | rare | Requires at least 1 Seal. Remove all Seals from an enemy and Ratify it immediately. Draw 1 card. Exhaust. |
| `protective_adjournment` | Protective Adjournment | working | common | Queue: Gain 11 Block. |
| `rebuttal` | Rebuttal | deed | rare | Deal 9 damage. Gain 4 Block per Doubt already on the target, maximum 12 Block. Then apply 1 Doubt. |
| `red_ink_doctrine` | Red Ink Doctrine | rite | rare | After an enemy takes HP loss from its Paperwork, if it survives, apply 2 Paperwork to it. |
| `seal_dividend` | Seal Dividend | rite | uncommon | The first time each turn you Ratify an enemy, draw 1 card. |
| `seal_of_concern` | Seal of Concern | working | common | Apply 1 Seal and 1 Doubt. |
| `secure_misfiling` | Secure Misfiling | working | common | Add 1 Misfiled Paper to your discard pile. Draw 1 card. |
| `skeleton_staff` | Skeleton Staff | working | rare | Queue a card from your hand for free. Add 1 Red Tape to your discard pile. |
| `stay_of_execution` | Stay of Execution | working | rare | Choose an enemy with Paperwork. Its Paperwork does not trigger at the end of its next turn. Gain 2 Block per current Paperwork on that enemy, maximum 20 Block. |
| `summary_judgment` | Summary Judgment | deed | rare | Deal 16 damage. If the target has at least 6 Paperwork, trigger its Paperwork immediately, then remove 3 Paperwork. |
| `tallow_budget` | Tallow Budget | working | uncommon | Gain 1 Energy. Add 1 Red Tape to your hand. Exhaust. |
| `threefold_injunction` | Threefold Injunction | deed | uncommon | Deal 3 damage 3 times. If the target is Ratified, each hit also applies 1 Paperwork. |
| `violence_allowance` | Violence Allowance | rite | rare | The first Deed you play each turn costs 1 less Energy. |
| `wastepaper_bastion` | Wastepaper Bastion | working | uncommon | Gain 4 Block, plus 2 Block for each Junk card in your hand. |
| `waxing_authority` | Waxing Authority | deed | common | Deal 5 damage. Apply 1 Seal. |

### Bureaucrat — Act II — 14

| code | title | type | rarity | what it does |
|---|---|---|---|---|
| `archive_pyre` | Archive Pyre | deed | rare | Archive all Junk cards in your hand. Deal 9 damage to ALL enemies, plus 5 damage for each Junk Archived this way. |
| `binding_fee` | Binding Fee | working | uncommon | Archive a non-Junk card from your hand. Apply Paperwork equal to 3 plus its base Energy cost. |
| `broom_dispatch` | Broom Dispatch | working | common | Apply 2 Paperwork to ALL enemies. |
| `clutter_concordance` | Clutter Concordance | deed | uncommon | Deal 5 damage, plus 2 damage for each different Junk type currently present across your discard and Exhaust piles. |
| `cross_filing` | Cross-Filing | working | common | Apply 4 Paperwork to an enemy. If another enemy is present, move 2 of it to them. |
| `dead_letter_office` | Dead Letter Office | working | uncommon | For each different Junk type in your Exhaust pile, apply 1 Paperwork to ALL enemies. Exhaust. |
| `errata_furnace` | Errata Furnace | working | common | Archive a Junk card from your hand. Apply 4 Paperwork to a random enemy. |
| `funeral_index` | Funeral Index | deed | rare | Deal 5 damage for each card you have Archived this combat. Count at most 8 cards. Exhaust. |
| `ghost_register` | Ghost Register | rite | rare | The first card you Archive each turn is recorded. At the start of your next turn, a Temporary copy of it is added to your hand; it costs 0 and Exhausts when played. |
| `marginalia` | Marginalia | working | uncommon | Choose a card from your Exhaust pile. Create a Temporary copy in your hand; it Exhausts when played. Marginalia Exhausts. |
| `null_catalogue` | Null Catalogue | working | rare | Choose up to 2 cards in your discard pile. Archive them. Draw 1 card for each card Archived this way. Exhaust. |
| `palimpsest_order` | Palimpsest Order | working | uncommon | Archive a card from your hand. Return a non-Junk card from your discard pile to your hand. Exhaust. |
| `redaction_veil` | Redaction Veil | working | uncommon | Remove up to 4 Paperwork from an enemy. Gain 3 Block for each Paperwork removed. |
| `smudged_index` | Smudged Index | working | uncommon | Archive a card from your draw pile. Gain 4 Block. |

### Bureaucrat — Act III — 12

| code | title | type | rarity | what it does |
|---|---|---|---|---|
| `blood_testimony` | Blood Testimony | deed | rare | Deal 9 damage to ALL enemies. Enemies that attacked during the previous enemy turn take 9 additional damage. |
| `customary_due` | Customary Due | working | uncommon | Create a Temporary copy of a card in your discard pile and Queue it. The copy Exhausts after resolving. Customary Due Exhausts. |
| `due_recompense` | Due Recompense | deed | rare | Deal 14 damage, plus 5 damage for each Doubt on the target. Count at most 6 Doubt. Then remove all Doubt from the target. |
| `grievance_ledger` | Grievance Ledger | deed | rare | Deal 10 damage, plus 6 damage for each time this enemy has attacked during this combat. Count at most 4 attacks. |
| `guest_right` | Guest Right | rite | rare | Once per turn, when an enemy with at least 3 Doubt would deal unblocked damage, remove 3 Doubt and reduce that remaining damage to 0. |
| `guestbook_oath` | Guestbook Oath | rite | uncommon | At the end of your turn, if you have any Block, apply 1 Doubt to every enemy that intends to Attack. |
| `hearth_compact` | Hearth Compact | rite | rare | Whenever an enemy with Doubt attacks and deals no unblocked damage, the Doubt stack that would normally be consumed is retained. |
| `hedge_covenant` | Hedge Covenant | rite | rare | Whenever Doubt reduces Attack damage, after that Attack has fully resolved, gain Block equal to half the prevented damage, rounded up. |
| `hedge_hospitality` | Hedge Hospitality | working | uncommon | Gain 7 Block. Until your next turn, the first enemy that deals unblocked damage to you gains 4 Paperwork. |
| `priority_docket` | Priority Docket | working | common | Choose another card in your hand and Queue it, paying 1 less Energy (minimum 0). |
| `restitution_writ` | Restitution Writ | working | uncommon | Apply Paperwork equal to half the unblocked damage you took during the previous enemy turn, rounded down. Maximum 6 Paperwork. Exhaust. |
| `witness_knot` | Witness Knot | working | uncommon | Apply 1 Doubt to an enemy. If it attacks before your next turn, apply 2 Paperwork to all other enemies. |

### Bureaucrat — Act IV — 8

| code | title | type | rarity | what it does |
|---|---|---|---|---|
| `cartouche_reckoning` | Cartouche Reckoning | deed | rare | Deal 18 damage. Then, up to 3 times: if the target has at least 10 Paperwork, remove 10 Paperwork and repeat this attack. |
| `final_attestation` | Final Attestation | deed | common | Deal 8 damage. If the target is Ratified, gain 1 Energy. |
| `fivefold_compliance` | Fivefold Compliance | deed | rare | Deal 12 damage, then repeat once for each fulfilled clause: the target has at least 10 Paperwork; at least 3 Doubt; is Ratified; you hold 2 different Junk types in your Exhaust pile; you have a Queued card. |
| `hieratic_measure` | Hieratic Measure | rite | uncommon | Whenever you Ratify an enemy, immediately trigger its current Paperwork once, then remove 3 Paperwork from it. |
| `monumental_writ` | Monumental Writ | deed | rare | Queue: Deal 24 damage, plus 12 for each other card still in your Queue when this resolves. Count at most 3. |
| `processional_calendar` | Processional Calendar | rite | uncommon | At the end of your turn, if you have at least 2 Queued cards, resolve your oldest Queued card. |
| `stone_levy` | Stone Levy | deed | rare | Remove up to 20 of your Block. Deal 10 damage plus 2 damage for each Block removed. |
| `temple_tally` | Temple Tally | rite | uncommon | Whenever an enemy reaches a new multiple of 5 Paperwork for the first time this combat, apply 1 Seal to it for each new multiple crossed. |

### General — Act I — 19

| code | title | type | rarity | what it does |
|---|---|---|---|---|
| `blood_marginalia` | Blood Marginalia | working | uncommon | Apply 3 Citation and 2 Blood Ink. |
| `borrowed_candle` | Borrowed Candle | working | uncommon | Draw 2 cards. Put one card from your hand on top of your draw pile. Exhaust. |
| `contempt_finding` | Contempt Finding | working | uncommon | Remove all Citation from an enemy. Gain 2 Block per Citation removed. |
| `dawn_summons` | Dawn Summons | deed | rare | Deal 16 damage. If this is the first card you play this turn, deal 10 additional damage. |
| `false_signature` | False Signature | working | uncommon | Your next card this turn costs 1 less Energy. After it is played, the next card you play this combat costs 1 more. Exhaust. |
| `foreclosure` | Foreclosure | deed | uncommon | Deal 6 damage. Then immediately resolve up to 5 Lien on the target. |
| `forfeit_seal` | Forfeit Seal | deed | uncommon | Deal 7 damage. If the target still has Block after this attack, apply 4 Lien. |
| `grave_lien` | Grave Lien | deed | uncommon | Deal 7 damage. Apply 5 Lien. |
| `malediction_review` | Malediction Review | working | uncommon | Gain 6 Block. Choose one: gain 2 Censure; or apply 2 Censure to an enemy. |
| `mortgage_sigil` | Mortgage Sigil | working | uncommon | Apply 3 Lien. The next time the target gains Block before the end of its next turn, apply 3 additional Lien. |
| `notary_beetle` | Notary Beetle | rite | uncommon | The first time each turn you apply a negative Status to an enemy that does not already have that Status, apply 1 additional stack of it. |
| `reciprocal_edict` | Reciprocal Edict | rite | rare | The first time each turn your Censure prevents a negative Status applied by an enemy, apply 2 Censure to that enemy. The first time each turn Censure prevents a positive Status on an enemy, gain 1 Censure. |
| `sanctioned_charm` | Sanctioned Charm | working | uncommon | Gain 5 Block. Until your next turn, the first time your Censure prevents a negative Status, the Censure used to prevent it is not consumed. |
| `sealed_mantle` | Sealed Mantle | working | uncommon | Gain 8 Block. If at least one enemy attacks during this enemy turn and you take no unblocked Attack damage, gain 2 Ward Wax. |
| `silent_hearing` | Silent Hearing | working | uncommon | Apply 2 Citation. Until your next turn, if the target performs a damaging action, gain 7 Block. |
| `tallow_reserve` | Tallow Reserve | working | uncommon | Requires at least 6 Block. Lose 6 Block. Gain 3 Ward Wax. Exhaust. |
| `usurers_moon` | Usurer's Moon | rite | rare | Whenever Lien removes Block from an enemy, apply 1 Citation for every 3 Block removed, maximum 3 Citation per Lien resolution. |
| `waxen_surety` | Waxen Surety | working | uncommon | Gain 4 Ward Wax. |
| `witchmark_citation` | Witchmark Citation | working | uncommon | Apply 3 Citation. If the target currently intends a non-damaging action, draw 1 card. |

### General — Act II — 10

| code | title | type | rarity | what it does |
|---|---|---|---|---|
| `blacklisted` | Blacklisted | working | uncommon | Apply 2 Censure. For each different positive Status already on the target, apply 1 additional Censure, maximum +3. |
| `blood_redaction` | Blood Redaction | working | rare | Remove up to 6 stacks of a negative Status from an enemy. Apply the same number of Blood Ink. Exhaust. |
| `countermanded_grace` | Countermanded Grace | rite | uncommon | The first time each turn Censure prevents any Status stack, gain 2 Ward Wax. This may trigger from Censure on you or on an enemy. |
| `crossed_sigil` | Crossed Sigil | working | uncommon | Remove 1 stack of a negative Status from yourself. Then apply 1 Censure to an enemy. If you had no negative Status to remove, gain 1 Censure instead. |
| `moonlit_counterfeit` | Moonlit Counterfeit | working | rare | Create a Temporary copy of a card in your hand; your next card this turn is free. Exhaust the original. Moonlit Counterfeit Exhausts. |
| `proxy_curse` | Proxy Curse | working | uncommon | Remove up to 3 stacks of a negative Status from yourself. Apply 1 Blood Ink to an enemy per stack removed. |
| `sanguine_errata` | Sanguine Errata | working | uncommon | Apply 2 Blood Ink. Then remove 1 stack of another negative Status from the target. |
| `seizure_writ` | Seizure Writ | deed | rare | Deal 12 damage. Then remove all remaining Block from the target. For every 3 Block removed, apply 1 Lien, maximum 6 Lien. |
| `standing_citation` | Standing Citation | rite | rare | The first time each turn Citation triggers on each enemy, that trigger does not remove a Citation stack. |
| `vein_register` | Vein Register | rite | uncommon | The first time each turn another Status on an enemy loses a stack, apply 1 Blood Ink to it. |

### General — Act III — 10

| code | title | type | rarity | what it does |
|---|---|---|---|---|
| `blood_tithe` | Blood Tithe | deed | uncommon | Deal 8 damage. If the target has Blood Ink, it loses HP equal to twice its Blood Ink, then loses 1 Blood Ink. |
| `consecrated_testament` | Consecrated Testament | rite | uncommon | The first 3 times each turn an enemy loses HP because of a Status effect, gain 1 Ward Wax. |
| `debt_ouroboros` | Debt Ouroboros | rite | rare | Whenever Lien resolves, apply Lien equal to half the amount consumed, rounded down, maximum 4. |
| `exemplary_sentence` | Exemplary Sentence | deed | rare | Remove up to 5 Citation from an enemy. For each removed, ALL enemies lose 4 HP. Then deal 12 damage to it. |
| `mortgaged_aegis` | Mortgaged Aegis | working | uncommon | Gain 18 Block. At the start of your next turn, gain 8 Lien. |
| `oath_of_refusal` | Oath of Refusal | rite | rare | The first 2 times each turn Censure prevents one or more Status stacks, record 1 Refusal. At the start of your next turn, draw 1 card per Refusal, maximum 2, and gain 1 Energy. Then clear them. |
| `vital_census` | Vital Census | deed | uncommon | Deal 8 damage to ALL enemies. Every enemy with Blood Ink loses HP equal to its Blood Ink, then loses 1. |
| `votive_covenant` | Votive Covenant | rite | rare | If you take no unblocked Attack damage during an enemy turn, Ward Wax does not decay. If you do, it loses 3 stacks instead of 2. |
| `wax_indemnity` | Wax Indemnity | working | rare | Until your next turn, damage that gets through is answered by your Ward Wax: up to 4 Wax is spent, healing 3 HP each. |
| `wax_reliquary` | Wax Reliquary | working | uncommon | Gain 4 Ward Wax. Until your next turn, Ward Wax cannot suffer its additional decay. |

### General — Act IV — 11

| code | title | type | rarity | what it does |
|---|---|---|---|---|
| `absolute_interdict` | Absolute Interdict | rite | rare | The first time each turn Censure on a combatant would prevent Status stacks, 1 Censure prevents the entire application instead, however many stacks it carried. |
| `black_tribunal` | Black Tribunal | deed | uncommon | Deal 14 damage, plus 8 damage for each different negative Status on the target. Count at most 5. |
| `candle_cathedral` | Candle Cathedral | rite | uncommon | Whenever Ward Wax grants Block, gain additional Block equal to half your Ward Wax, rounded up. Ward Wax no longer suffers its additional decay. |
| `compound_indictment` | Compound Indictment | working | rare | Requires at least 3 different negative Statuses on the target. Add 2 stacks to each negative Status it carries, up to 5 of them. Exhaust. |
| `crown_repossession` | Crown Repossession | deed | rare | Deal 22 damage. Remove all remaining Block from the target; it loses HP equal to the Block removed, maximum 40. Apply 6 Lien. |
| `grand_citation` | Grand Citation | deed | uncommon | Deal 14 damage to ALL enemies. Each enemy with Citation additionally loses HP equal to 3 times its Citation, then loses 1 Citation. |
| `grand_dispensation` | Grand Dispensation | working | rare | Choose 2 different options: deal 24 damage to an enemy; gain 24 Block; draw 3 cards; gain 2 Energy. Exhaust. |
| `hemal_audit` | Hemal Audit | deed | rare | Deal 18 damage. Then trigger Blood Ink repeatedly, up to 6 times or until no Blood Ink remains. |
| `last_office` | Last Office | working | rare | For each of Paperwork, Doubt, Seal, Lien and Citation the chosen enemy does not carry, deal 8 damage to it and gain 3 Block. Exhaust. |
| `sovereign_prohibition` | Sovereign Prohibition | working | uncommon | Gain 3 Censure. Apply 3 Censure to ALL enemies. |
| `tallow_judgment` | Tallow Judgment | deed | rare | Consume up to 8 Ward Wax. Deal 10 damage plus 7 damage per Ward Wax consumed. |

### Given in play — 91

Handed over by a boss, a door or an event rather than offered; never in a reward pool.

| code | title | what it does |
|---|---|---|
| `acknowledge_service` | Acknowledge Service | Sign for the notice: gain 2 Paperwork. The Knight's enforcement deals 10 instead of 19 and 1 Paperwork. Leave it in hand to refuse. |
| `ask_for_expedited_service` | Ask for Expedited Service | This Service Window opens the Commissioner by 15 % instead of 25 %, but afterwards you stand at Position 1 instead of going back into the queue. |
| `authorized_entry` | Authorized Entry | Remove up to 12 Block from the Municipal Dragon. Costs 1 Authorization; one authority per turn. |
| `authorized_expedition` | Authorized Expedition | Gain 1 Energy. Costs 1 Authorization; one authority per turn. |
| `better_chair_cushion_action` | The Better Chair | Gain 14 Block. Then end the turn holding a real card, or the cushion costs you 6 HP. |
| `black_flood_vessel_action` | Empty the Vessel | Discard your hand, draw 7, and gain 2 Energy. Once a combat. |
| `borrow_one_minute` | Borrow One Minute | Once per turn: push one filed hour back by 1 turn, to a maximum countdown of 3. Free while you hold a Free Adjustment. |
| `borrowers_claim` | Borrower's Claim | Retain. Exhaust. Put another card from your hand on the bottom of your draw pile, then draw 1. If it is still in your hand at the end of your turn, file 1 Paperwork. |
| `break_the_burden_seal` | Break the Burden Seal | Break open the burden chamber: 1 Burdened comes off you, or 1 Paperwork if you carry no burden. The colony packs the breach — 12 Block when its turn comes. |
| `break_the_granary_labor_seal` | Break the Labor Seal | An over-ration can no longer burden you. One Burdened comes off. |
| `break_the_granary_ration_seal` | Break the Ration Seal | From now on every exact ration costs her 10 blood and 10 cover. She loses 10 cover now. |
| `break_the_granary_record_seal` | Break the Record Seal | A failed ration can no longer be written up. Up to 2 Paperwork comes off. |
| `break_the_granary_reserve_seal` | Break the Reserve Seal | She can no longer take Grain, and no longer eats. You heal 5. |
| `break_the_pest_seal` | Break the Pest Seal | Break open the pest chamber: the colony loses 12 HP, and what was living in there gets on you — 2 Poison. |
| `break_the_ration_seal` | Break the Ration Seal | Break open the ration chamber: 1 Energy on your next turn. The colony eats too — it gains 1 Strength. |
| `break_the_seal_of_access` | Break the Great Seal of Access | Shatter the Great Seal of Access and take its Fragment. |
| `break_the_seal_of_execution` | Break the Great Seal of Execution | Shatter the Great Seal of Execution and take its Fragment. |
| `break_the_seal_of_testimony` | Break the Great Seal of Testimony | Shatter the Great Seal of Testimony and take its Fragment. |
| `cite_the_old_survey` | Cite the Old Survey | Spend 1 Old Right, once a turn. OLD BOUNDARY — Current and Former Survey are swapped for the rest of this turn. OLD RIGHT OF PASSAGE — the Surveyor's next attempt to cash a Claim comes to nothing, and the Claim remains. OLD MEASURE — remove up to 8 of the Surveyor's Block. |
| `claim_an_exception` | Claim an Exception | The Articles do not touch you for the rest of this turn. |
| `clause_evidentiary` | Evidentiary Clause | Sign: draw 2 cards. Liability: 1 Doubt and 1 Paperwork when the record is read. Refuse: the Petition gains 1 Strength. |
| `clause_extension` | Extension Clause | Sign: gain 1 Energy. Liability: 1 Fatigue when the record is read. Refuse: the Petition gains 8 Block. |
| `clause_protective` | Protective Clause | Sign: gain 10 Block. Liability: 2 Paperwork when the record is read. Refuse: the Petition gains 1 Strength. |
| `correction_reed_action` | A Small Correction | Send a card away and take one back out of your discard pile; it costs 1 less this turn. With nothing to take back, draw 1. Once a turn. |
| `counter_petition` | Counter-Petition | Once a turn, spend 1 Safe-Conduct to argue one of the Ombudsman's complaints under the other Ground — Road becomes Root, or Root becomes Road. It creates nothing, moves nothing, and nobody's standing changes hands. |
| `counter_petition_twine_action` | Counter-Petition | Discard a card, draw a card, and gain 1 Energy. Once a turn. |
| `dedicate_a_work` | Dedicate a Work | Give Eanna a card from your hand for the rest of this fight. A card she has claimed settles 4 Temple Due, an ordinary one 1, and rubbish nothing — Eanna wants value. Dedicated cards come back when she is dead. |
| `draw_against_the_treasury` | Draw Against the Treasury | Spend a Treasury Credit: take up to 12 Block off the treasury. Once a turn. |
| `draw_ahead` | Draw Ahead | Draw 1 card out of a later day. The quantity moves; the cards are still yours. |
| `edict_of_the_open_audience_action` | Open the Audience | Every card in your hand costs 0 for the rest of this turn. Once a combat. |
| `erasure_tablet_action` | Erase the Line | Every enemy's next action is erased: it deals no damage, and they guard for 20 instead. Once a combat. |
| `far_boundary` | Far Boundary | Accept the surveyor's further figure as this turn's exact measure. Meeting it costs it 10 HP and strips its cover. |
| `file_an_objection` | File an Objection | The Dragon's next attack deals 5 less. Costs 1 Authorization; one authority per turn. |
| `file_the_request` | File the Request | Resolve the Request for Additional Review. |
| `fine_print` | Fine Print | Unplayable. While it is in your hand, the first card you play each turn costs 1 more. |
| `fragment_of_access` | Fragment of Access | Remove up to 12 Block from the Lord Sealkeeper. |
| `fragment_of_execution` | Fragment of Execution | The Sealkeeper's next attack deals 8 less. |
| `fragment_of_testimony` | Fragment of Testimony | Remove 2 stacks of Paperwork from yourself. |
| `hold_the_moon` | Hold the Moon | The phase does not advance: a second night of this same phase, for you and for him. What you counted comes back at once — and so does what he counted. Once an orbit. |
| `honey_spoon_action` | A Little Honey | Gain 2 Energy. Then end the turn with at least 1 Energy, or the spoon costs you 6 HP. |
| `issue_a_citation` | Issue a Citation | Remove 1 Code Violation from the Dragon. Costs 1 Authorization; one authority per turn. |
| `last_slice_tin_action` | Take Another Slice | Draw 2. Then play no more than four real cards this turn, or the tin costs you 6 HP. |
| `make_amends` | Make Amends | Choose one: PAY IN COIN — spend 1 Energy. OFFER A CARD — discard a card from your hand. Either settles 1 Wergild, oldest demand first. |
| `missing_signature` | Missing Signature | Exhaust. If it is still in your hand at the end of your turn, file 1 Paperwork. |
| `near_boundary` | Near Boundary | Accept the surveyor's nearer figure as this turn's exact measure. Meeting it lets it brace. |
| `notice_of_delay` | Notice of Delay | Retain. Exhaust. If it is still in your hand at the end of your turn, gain 1 Fatigue. |
| `offer_the_surplus` | Offer the Surplus | Spend 1 Energy to settle 1 Temple Due. |
| `petition_for_priority` | Petition for Priority | Move one place toward the Counter. Gain 1 Paperwork. Only one administrative choice per turn. |
| `redacted_leaf` | Redacted Leaf | Unplayable. Retain. At the start of your turn one card in your hand is Redacted, and the Leaf is spent. |
| `return_receipt` | Return Receipt | Choose one: FILE THE RECEIPT — remove 1 Overdue; the Bell loses 5 HP. CONTEST THE FEE — remove 1 Late Fee; mark 1 draw-pile card Misfiled. |
| `revise_body_shall_bear` | Revise: Thirty-Six | Spend a Reed Mark to edit this sentence one step. 36 HP, ignoring Block. Revised: 24 · 12 · nothing · and at four, Nisaba bears 18 herself. |
| `revise_guard_counted_nothing` | Revise: The Guard Counts Nothing | Spend a Reed Mark to edit this sentence one step. Every Block you gain next turn is worth 18 less. Revised: 12 · 6 · none · and at four, 6 more. |
| `revise_hand_shall_hold_two` | Revise: The Hand Holds Two | Spend a Reed Mark to edit this sentence one step. You draw 3 fewer cards next turn. Revised: 2 · 1 · none · and at four, you draw one more. |
| `revise_measures_withheld` | Revise: Three Measures | Spend a Reed Mark to edit this sentence one step. 3 Energy off your next turn. Revised: 2 · 1 · none · and at four, one card of that turn costs 1 less. |
| `revise_the_last_line` | Revise: The Last Line | Spend a Reed Mark to edit the last sentence one step. At four it reads THE NAME OF THE SUPPLICANT SHALL REMAIN and she can be killed; at five it reads THE NAME OF THE KEEPER SHALL BE ERASED, and it is read at once. |
| `revise_three_wounds` | Revise: Three Wounds | Spend a Reed Mark to edit this sentence one step. 3 sheets of Red Tape into your draw pile. Revised: 2 · 1 · none · and at four, one sheet is struck out. |
| `revise_two_works_broken` | Revise: Two Works | Spend a Reed Mark to edit this sentence one step. 2 cards exhausted out of your draw pile. Revised: 1 · none · and at four, one comes back from the exhaust pile. |
| `right_of_audience` | Right of Audience | Once a turn, spend Favour. ONE — strike one of the Queen's Claims off. TWO — her law is suspended for the rest of this turn. THREE — her guard is struck away and her granted name is prepared, which takes 8 off her final order. |
| `royal_grace_cup_action` | Royal Grace | Choose one: 1 Energy, a card, or 10 Block. Every enemy guards for 6. Once a turn. |
| `scrape_the_first_entry` | Scrape the First Entry | Blank this entry on the scroll. One entry a turn, and the correction is written up: 1 Paperwork. |
| `scrape_the_second_entry` | Scrape the Second Entry | Blank this entry on the scroll. One entry a turn, and the correction is written up: 1 Paperwork. |
| `scrape_the_third_entry` | Scrape the Third Entry | Blank this entry on the scroll. One entry a turn, and the correction is written up: 1 Paperwork. |
| `settle_the_burden` | Settle the Burden | Spend a Treasury Credit: 1 Burdened is settled and comes off you. Once a turn. |
| `silence_the_inner_stair` | Silence the Inner Stair | The Inner Stair office says nothing until the Vizier's next action is over. One office only — the sheets are gone at the end of the turn either way. |
| `silence_the_royal_seal` | Silence the Royal Seal | The Royal Seal office says nothing until the Vizier's next action is over. One office only — the sheets are gone at the end of the turn either way. |
| `silence_the_tally` | Silence the Tally | The Tally office says nothing until the Vizier's next action is over. One office only — the sheets are gone at the end of the turn either way. |
| `silver_name_tally_action` | Speak the Silver Name | Once a combat: one enemy's guard is gone, you gain 10 Block against what it was about to do, and the next card you play this turn is refunded. |
| `sluice_gate_of_the_two_lands_action` | Work the Two Lands | Open the gate — lose 12 Block for 1 Energy — or close it — spend 1 Energy for 12 Block. You must be able to pay in full. Once a turn. |
| `spend_a_counterseal` | Spend a Counterseal | Spend 1 Counterseal to prise one Notarial Seal back out of the wood. The ring returns to ordinary rotation; the Weight of Precedent already earned stays where it is. |
| `strike_down_the_article` | Strike Down the Article | The Article is struck; the next prepared Article takes its place. The Charter gains 8 Block. |
| `summons_to_appear` | Summons to Appear | Retain. Exhaust. If it is still in your hand at the end of your turn, take 5 damage. |
| `take_ahead` | Take Ahead | Take 1 Energy out of a later day. It arrives the moment you run out. Tomorrow is simply smaller. |
| `the_alternating_stone` | The Alternating Stone | Accept this blueprint: the first two cards you play must be of different kinds. Met, the Monument falls a step; missed, 2 Paperwork. |
| `the_answer_of_burden` | The Answer of Burden | Answer the sphinx by carrying it: 2 Burdened. One Answer Mark. |
| `the_answer_of_burial` | The Answer of Burial | Answer the sphinx by being packed down and filed: 1 Entombed and 1 Paperwork. One Answer Mark. |
| `the_answer_of_measure` | The Answer of Measure | Answer the sphinx by walking to a figure: 2 Weighed. One Answer Mark. |
| `the_equal_courses` | The Equal Courses | Accept this blueprint: play exactly 2 Deeds this turn. Met, the Monument falls a step; missed, you are Burdened. |
| `the_measured_course` | The Measured Course | Accept the fallback course: spend exactly what the Architect has measured for this turn. Met, the Monument falls a step; missed, it climbs one. |
| `the_measured_foundation` | The Measured Foundation | Accept this blueprint: spend exactly 2 Energy this turn. Met, the Monument falls a step; missed, it climbs one and you are Entombed. |
| `unfinished_citation` | Unfinished Citation | Retain. Exhaust. Clear a Reference from a card in your hand. If it is still in your hand at the end of your turn, file 1 Paperwork. |
| `uphold_the_article` | Uphold the Article | The Article stands. Gain 6 Block. |
| `vacant_throne_decree_action` | The Throne Stands Empty | Gain 3 Energy, draw 3, and gain 20 Block. Once a combat. |
| `wash_the_burdened_vessel` | Wash the Burdened Vessel | Pay 1 Energy: the jar holding Burdened is emptied and cannot come back. You are Embalmed 1 for handling it. One jar a turn. |
| `wash_the_doubt_vessel` | Wash the Doubt Vessel | Pay 1 Energy: the jar holding Doubt is emptied and cannot come back. You are Embalmed 1 for handling it. One jar a turn. |
| `wash_the_entombed_vessel` | Wash the Entombed Vessel | Pay 1 Energy: the jar holding Entombed is emptied and cannot come back. You are Embalmed 1 for handling it. One jar a turn. |
| `wash_the_inscribed_vessel` | Wash the Inscribed Vessel | Pay 1 Energy: the jar holding Inscribed is emptied and cannot come back. You are Embalmed 1 for handling it. One jar a turn. |
| `wash_the_paperwork_vessel` | Wash the Paperwork Vessel | Pay 1 Energy: the jar holding Paperwork is emptied and cannot come back. You are Embalmed 1 for handling it. One jar a turn. |
| `wash_the_weighed_vessel` | Wash the Weighed Vessel | Pay 1 Energy: the jar holding Weighed is emptied and cannot come back. You are Embalmed 1 for handling it. One jar a turn. |
| `work_the_sluice` | Work the Sluice | Spend 1 Sluice Authority: at the end of this turn, after the river answers your Energy, it moves one step back toward the Ordered Flood. Once a turn. |
| `wrong_form` | Wrong Form | Exhaust. Discard another card. |
| `yield_your_place` | Yield Your Place | Move one place away from the Counter. Gain 6 Block. Only one administrative choice per turn. |

### Ported v2 remnants — 25

⚠ **Paint these last, or not at all.** They are the demo game's cards, still shipped only because ported events name them; they leave when those events are replaced.

| code | title | what it does |
|---|---|---|
| `administrative_notice` | Administrative Notice | Apply 3 Paperwork. |
| `approved_for_disposal` | Approved for Disposal | Deal 12 damage. Deal 2 damage for each Paperwork on the target. |
| `archive_the_evidence` | Archive the Evidence | Exhaust 1 Junk card from your hand. Gain 8 Block. |
| `come_back_tomorrow` | Come Back Tomorrow | Gain 18 Block. Add Red Tape to your hand. |
| `compliance_review` | Compliance Review | Deal 5 damage. Apply 2 Paperwork. |
| `compounded_penalty` | Compounded Penalty | Deal 4 damage for each Paperwork on the target. |
| `counter_signature` | Counter Signature | Deal 3 damage. Draw 1. Exhaust. |
| `cross_reference` | Cross-Reference | Draw 2. Add a Duplicate Copy to your discard pile. |
| `expedited_stamp` | Expedited Stamp | Apply 2 Paperwork. Add a Duplicate Copy to your discard pile. Exhaust. |
| `final_reminder` | Final Reminder | Apply 4 Paperwork. Apply 2 Doubt. |
| `form_12_b` | Form 12-B | Apply 1 Paperwork. Exhaust. |
| `internal_memo` | Internal Memo | Draw 1. Add a Misfiled Paper to your discard pile. Exhaust. |
| `invalidated` | Invalidated | Deal 3 damage for each Paperwork on the target. |
| `missing_attachment` | Missing Attachment | Apply 2 Paperwork. Draw 1. |
| `overfilled_inbox` | Overfilled Inbox | Apply 3 Paperwork to all enemies. Add an Unsigned Form to your hand. |
| `permit_denied` | Permit Denied | Apply 5 Paperwork. |
| `please_take_a_number` | Please Take a Number | Apply 2 Doubt. Apply 1 Paperwork. |
| `processing_delay` | Processing Delay | Gain 8 Block. Apply 2 Paperwork. |
| `provisional_approval` | Provisional Approval | Gain 1 Energy. Add Red Tape to your hand. Exhaust. |
| `queue_management` | Queue Management | Gain 6 Block. Apply 1 Doubt. |
| `rubber_stamp` | Rubber Stamp | Deal 7 damage. Apply 1 Paperwork. |
| `shredder_drawer` | Shredder Drawer | Exhaust all Junk cards from your hand. Deal 9 damage. |
| `stamp_barrage` | Stamp Barrage | Deal 3 damage to all enemies. Apply 1 Paperwork to all enemies. |
| `temporary_authorization` | Temporary Authorization | Gain 4 Block. Exhaust. |
| `under_consideration` | Under Consideration | Apply 3 Doubt. Draw 2. |

