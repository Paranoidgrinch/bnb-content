# Hedge Witch — Umsetzungsplan (zweiter spielbarer Charakter)

Kanon: `source-data/design/hedge_witch_master.md` (vom Spieler, 2026-10-03). **Mechaniken und Identitäten sind
Kanon, Zahlen nicht** (Master §23). Dieser Plan sagt, WIE es in die drei Repos kommt, in welcher Reihenfolge und
woran jeder Schritt gemessen wird. Status: **freigegeben 2026-10-03** („alles klar, leg los“) — die Vorschläge in §0 gelten; E4 und E7 kommen als
Vorlage zur Durchsicht, bevor sie gebaut werden.

Arbeitsweise wie bisher: ein Commit pro Schritt, Push pro fertigem Block, Engine bleibt Werkzeug (kein
Hexen-Inhalt im Core), jede neue Regel mit einem Test, der die RICHTUNG misst (rot ohne die Regel).

---

## 0 Entscheidungen, die vor dem Bauen fallen müssen

Aus Master §24 offen, plus was das Dokument nicht sagt:

| # | Frage | Mein Vorschlag |
|---|---|---|
| E1 | Max-HP der Hexe | 70 wie der Bureaucrat; Feintuning im Bench |
| E2 | Akt-Verteilung der 80 Belohnungskarten (der Bureaucrat hat Akt-I- bis -IV-Karten) | alle Commons + ~2/3 der Uncommons ab Akt I, Rest/Rares gestaffelt wie beim Bureaucrat (Vorschlag als Tabelle in W4) |
| E3 | Hexe von Anfang an spielbar oder freizuspielen? | von Anfang an (Charakterwahl im Neuer-Lauf-Panel) |
| E4 | Zuordnung der 50 General-Pool-Karten zu Familien (§16) | ich lege eine Tabelle zur Durchsicht vor (W6), nichts wird still im Code geraten |
| E5 | Dregs-Standardwirkung (§6) | zunächst KEINE Wirkung (füllt nur einen Platz), nur Rezepte/Karten/Relikte lesen Dregs; Zahl später im Bench |
| E6 | „Hot“-Zustand nach dem Brauen (§2.2) | als Schalter bauen, AUS; im Bench beide Varianten messen |
| E7 | Zahlen der 35 Uncommons + 25 Rares (§24.4) | wie beim F-Pass: Regeln + 12–15 Beispielkarten zur Durchsicht, dann der Rest |
| E8 | Gilt das Heilungs-Limit (`combat_start_hp`) auch für allgemeine Heilung (Relikte, Rast)? | nein — nur Hearth (so steht es in §3.4) |
| E9 | Bleiben Bureaucrat-Tutorial und -Golden-Set unverändert? | ja; die Hexe bekommt eigene Golden-Läufe, Tutorial später |

---

## 1 Was die Engine heute kann und was fehlt (geprüft 2026-10-03)

**Vorhanden:** Charakter-Roster mit eigenem Start (`RunCharacter`, `CreateInitialRun(characterId)`) · eine
angehängte Sonderzone als Vorbild (`CardZone.QueuePile`, inkl. Snapshot/Save/Planer-Shape) · Status mit Zählern,
Triggern, Passiven · Ward Wax, Block, Heilung, Status-HP-Verlust · `CardMovedToZone`-Trigger (deckt „verlässt die
Hand ohne gespielt zu werden“ für Conservator's Thread ab) · Optionsfragen, Kartenwahl, Replay-Skript · Meta-Profil.

**Fehlt (neue Core-Arbeit, allgemein gebaut):**
1. **Der Lauf kennt seinen Charakter nicht.** `RunState` hat keine Charakter-ID → Kartenbelohnungen, Shop und
   Relikt-Pools können nicht nach Charakter filtern. Heute sieht jeder Lauf alle Pools.
2. **Kesselzone** (`CardZone.Cauldron`, angehängt wie QueuePile) mit Kapazität aus dem Inhalt (3, +1 Reserve).
3. **„Zur Seite legen“ als eigene Spieleraktion** (Karte aus der Hand in eine Zone, KEIN Ausspielen, keine
   Spielzähler) mit Kosten-Staffel pro Zug (erste 0, jede weitere 1) — eigener Replay-Eintrag.
4. **Charakter-Aktionen**, die keine Karte sind (BREW): Kosten, Verfügbarkeits-Bedingung, optionales Ziel,
   Programm; zählt nicht als gespielte Karte; eigener Replay-Eintrag.
5. **Misfortune-Bausteine:** Auslöser „bevor ein Gegner handelt“, deterministischer Zufallswurf (Kampf-RNG,
   replay-fest), Knoten „diese Aktion schlägt fehl“ (verbraucht, Nutzlast entfällt).
6. **Ausdrücke über die Kesselzone:** Karten mit Tag X zählen, „liegt genau Karte A/B/C drin“, Zonenkarten
   abwerfen/einzeln behalten (Never Wash the Pot, Yesterday's Jar).
7. **Bot/Planer/Walker** kennen die neuen Aktionen nicht → ohne sie gibt es keinen Bench, keinen Ganzlauf-Test
   und kein Golden-Set für die Hexe.

Inhaltlich (bnb-content) baubar mit vorhandenen Mitteln: Hexed/Threefold (Status + Schritt-Zähler), Sheltering
(Kessel leer), `combat_start_hp` (Zähler beim Kampfstart), Familien (Karten-Tags `fam_fang` …), alle Rezepte als
Programm über die Zone.

---

## 2 Phasen

### Phase C — Core (RogueDeck-Core)

| Schritt | Inhalt | Tor |
|---|---|---|
| C1 | `RunState.CharacterId` (im Save); Pool-Einträge (Karten, Relikte, Shop) tragen optional eine Charakter-Eignung, Belohnung/Shop filtern danach | Test: Bureaucrat-Lauf sieht nie eine Hexenkarte und umgekehrt; alter Save lädt als Bureaucrat |
| C2 | `CardZone.Cauldron` + Kapazität; Snapshot, Hasher, Planer-`Shape`, Save | Save mitten im Kampf mit 2 Zutaten → kommt mit denselben 2 zurück (die Queue-Lehre vom 2026-09-18) |
| C3 | Aktion „in Zone legen“ mit Kosten-Staffel/Zug, Validator (Zone voll, Karte nicht kochbar = Tag) | zählt nicht als gespielt; Conservator's Thread reagiert, „jede 5. Karte“ nicht |
| C4 | Charakter-Aktionen (BREW): Kosten, Bedingung, Ziel, Programm, Replay-Eintrag | Replay/Resume-Test: Brauen mitten im Zug, speichern, laden → gleicher Kampf |
| C5 | Misfortune-Bausteine (Vor-Aktions-Auslöser, Zufallswurf, Aktion schlägt fehl) | 1000 Würfe bei 50 % ≈ 50 %; Replay würfelt identisch; fehlgeschlagene Aktion = kein Schaden/Block/Buff |
| C6 | Zonen-Ausdrücke + Knoten (zählen nach Tag, exakte Karten-Kombination, abwerfen, eine behalten) | Einzeltests |
| C7 | FightPlanner, RunBot, Greedy/RunWalker kennen „in den Kessel“ und BREW | Planer findet einen Brauzug, der besser ist als Ausspielen (konstruierter Fall) |

### Phase W — Inhalt (bnb-content)

| Schritt | Inhalt | Tor |
|---|---|---|
| W1 | Charakter `hedge_witch`: Start (E1), Startdeck (4 Definitionen, 10 Karten), `Eligibility.HedgeWitch`, eigene Kartenpools | Lauf als Hexe startet, Belohnungen nur Hexe + General |
| W2 | Schlüsselwörter: **Hexed** (Threefold, Schritt-Zähler, Burst = 3 × Stapel, Status-HP-Verlust), **Misfortune** (1 Stapel = 5 %, Deckel 60 %, Wurf vor der nächsten Aktion, danach weg), **Sheltering**, `combat_start_hp`, **Dregs** (E5), Hot-Schalter (E6) | je Regel ein Test, der ohne sie rot ist |
| W3 | Der Kessel: BREW-Programm — 5 Concentrated, 30 Mixed (Summe, benannt fürs UI), 20 Hidden (exakte Karten, ersetzen den Sud), Reihenfolge Hex→Fang→Husk→Hearth→Fortune, Ziel nur bei Gegner-Ausgabe | alle 35 Familien-Kombinationen + 20 Hidden je ein Test |
| W4 | Karten: Starter + 20 Commons mit Zahlen; 35 Uncommons + 25 Rares erst als Vorschlag (E7), dann gebaut; Upgrades nach der Upgrade-Regel (≈ ×1,5, Facette erlaubt) | Audit (`--audit`), Regeltests wie GeneralCardTests |
| W5 | 18 Hexen-Relikte (3/5/4/6) | Relikt-Tests, Archiv-Rubrik |
| W6 | Familien-Tags für die 50 General-Pool-Karten nach freigegebener Tabelle (E4); Kampfkarten nicht kochbar | Test: jede General-Karte hat genau eine Familie |
| W7 | Archiv-Rubriken „Hedge Witch“ (Karten, Relikte), Compendium (Kessel, BREW, Familien, Hexed, Misfortune, Sheltering, Dregs, Rezepte), ART_SLOTS (Platzhalter-Kunst) | ArchiveSectionTests erweitert |

### Phase G — Godot (bnb-godot)

| Schritt | Inhalt | Tor |
|---|---|---|
| G1 | Charakterwahl im Neuer-Lauf-Panel; Verlauf, Fehlerbericht, bnb-runs-Upload tragen den Charakter | Smoke: Lauf als Hexe startet |
| G2 | Kessel-Widget: 3 Plätze (+Reserve), Zustände Sheltering/Brewing/Ready (Deckel, Dampf, Rütteln), Karte per Ziehen/Rechtsklick hinein, Kosten 0/1 sichtbar, BREW-Knopf mit Zielwahl, Vorschau (Rezeptname + Wirkung; unentdeckte Hidden Recipes werden NICHT verraten) | Screenshot-Probe in allen Zuständen |
| G3 | Statusanzeige: `Hexed 7 ••○` mit Ankündigung des nächsten Bursts; Misfortune als % neben der Absicht | Probe |
| G4 | Rezeptbuch (Everyday Brewing / Things That Worked Once / Notes in the Margin), Entdeckung dauerhaft im Profil (wie das Archiv-Fundbuch), „NEW RECIPE DISCOVERED“ | Entdeckung übersteht Neustart (Datei, nicht nur Speicher) |
| G5 | Archiv + Export zeigen die Hexe; Rezepte als eigener Reiter oder im Rezeptbuch | Export-Probe |
| G6 | Golden-Set: + Hexen-Läufe (unsterblich + sterblich) | `golden.sh` grün |

### Phase B — Balance

| Schritt | Inhalt |
|---|---|
| B1 | Akt-I-Bench mit dem Hexen-Starter (wie 2026-09-27 für den Bureaucrat), Vergleich Kosten je Stufe |
| B2 | Familien-Bench für die Hexen-Archetypen (Sheltering, Brewer, Threefold, Fortune, Hedge Doctor, Beast, Scavenger) in Akt II/III, Vergleich mit den Bureaucrat-Decks |
| B3 | Stresstests aus Master §24.6: Threefold-Beschleunigung, Dauer-Misfortune, Hearth-Hinhalteheilung, Energie-Kompression im Kessel, Zutaten-Schleifen, Dregs-Ausnutzung |
| B4 | Zahlen-Tuning auf Basis von B1–B3 (Vorschlag → Freigabe → Bau) |

---

## 3 Reihenfolge und Abhängigkeiten

C1 → W1 → G1 ergibt zuerst einen **spielbaren Hexen-Lauf mit nur Karten** (noch ohne Kessel) — früh testbar.
Dann C2–C4 → W3 → G2 (der Kessel), C5 → W2 (Misfortune), C6 (Rezept-Bausteine), C7 (Bot) → B1. W4 (Kartenzahlen)
kann ab E7-Freigabe parallel laufen. G3–G6 nach W2/W3. B2–B4 am Schluss.

Grobe Größe: Core ~7 Schritte, Inhalt ~7 (davon W4 der größte, 84 Karten + Upgrades), Godot ~6, Balance 4.
Jeder Schritt einzeln committet; gepusht wird pro fertiger Phase.

---

## 4 Risiken

- **Replay/Save.** Jede neue Aktion ist ein neuer Replay-Eintrag und jede neue Zone muss in Snapshot, Save und
  Planer-Shape. Die Queue hat zweimal gezeigt, was sonst passiert (2026-09-18 Zone fehlte im Save, 2026-10-03
  zwei offene Fragen). Deshalb hat jeder Core-Schritt einen Save/Resume-Test.
- **Misfortune und der hellsehende Planer.** Der Bench-Planer sieht die Zukunft; ein Wurf ist für ihn keine
  Wette. Für Misfortune-Decks misst der Bench daher den Erwartungswert über viele Seeds, nicht eine Zeile.
- **Mitlauf-Kosten im Bench.** Kessel-Züge vervielfachen die Zugmöglichkeiten; der Planer braucht ggf. eine
  Kessel-Heuristik (Zutaten nur am Zugende erwägen), sonst wird der Bench sehr langsam.
- **84 Karten Kunst.** Bis Bilder da sind, Platzhalter wie heute.

---

## Status

| Schritt | Stand |
|---|---|
| C1 Charakter im Lauf, exklusive Inhalte | ✔ Core 6779e8f (`RunCharacter.Exclusive`, `RunState.CharacterId`, `CharacterContent` an Belohnung/Shop/Verwandeln/Bündel) |
| C2 Kesselzone | ✔ Core 4da4be6 (`CardZone.SetAsidePile`, Snapshot/Save/Hash/Planer) |
| C3/C4 Zutat + BREW als Charakter-Aktionen | ✔ Core f851d39 (`CardDefinition.IsAction`, `PlayCondition`, `RunStart.CombatActions`, `UseAction`, Replay-Eintrag; `CombatCardSpec.ExcludeTag`) |
| C5 Misfortune-Bausteine | ✔ Core 93c32e4 (`TriggerEvent.ActionStarting`, `ActionFailsStatus`, `RandomBelowExpression`) |
| C6 Rezept-Bausteine | ✔ vorhanden (Zonen-Zählung nach Tag; jede Hexenkarte trägt Familien- und eigenen Karten-Tag) |
| C7 Planer/Bot | ✔ Core 3c48218 (Planer probiert jede Zutat-Wahl; RunBot nutzt Aktionen) |
| + Charakter-Status pro Kampf | ✔ Core 40d0b03 (`RunStart.CombatStatuses`) |
| Core gepusht | ✔ 2026-10-03, Golden-Set unverändert (Bureaucrat byte-gleich) |
| + Angebote pro Charakter (`for:<id>`) | ✔ Core 13c0696 — jeder Charakter zieht nur seine eigenen, eigens gewichteten Pool-Einträge |
| W1 Charakter + Starter | ✔ `Witch/WitchCharacter.cs`, `WitchCards.cs` (Adder's Nip, Pot-Lid, Crooked Finger, Nettle Tea); Roster: Bureaucrat mit exklusiven Karten/Relikten, Hexe mit ihren; 70 HP |
| W2 Schlüsselwörter | ✔ `WitchKeywords.cs`: Hexed (Threefold, kausal gezählt), Misfortune (5 %/Stapel, Deckel 60 %), Kessel-Status (HP bei Kampfbeginn, freie erste Zutat), Hearth-Heilgrenze; Dregs = ohne Familie, keine Wirkung (E5); Hot aus (E6) |
| W3 Kessel | ✔ `WitchActions.cs`: „Into the Pot“ (0/1 ⚡, Kampf-/Relikt-Karten `uncookable`), „Brew“ (1 ⚡, 3 nötig): 5 Concentrated + Mixed-Summe; `WitchRecipes.cs`: Namen + Wirkung aller 35 Familien-Rezepte in der Darstellung. Hidden Recipes folgen mit W4 |
| G1 Charakterwahl | ✔ war schon da (Titelbildschirm, Roster) |
| G2 Kessel im Kampf | ✔ `SessionCauldron.cs`: Kessel-Leiste (Plätze, Zustand, Rezeptvorschau), Knöpfe mit aktuellem Preis; `--smoke-cauldron --character=hedge_witch` |
| G3 Hexed/Misfortune-Anzeige | ✔ `Hexed 7 ••○`, Misfortune in % |
| Tests | ✔ `HedgeWitchTests` (9), Core-Tests je Baustein; Bureaucrat-Golden unverändert |
| W6 General-Familien | ✔ `WitchFamilies.cs` (Tabelle E4), beim Zusammenbau auf Karte + Upgrade geschrieben; Test: 50 Karten, je genau eine Familie, Grave Lien kocht als Fang |
| W4 Karten | ✔ 20 Commons (`WitchCards.cs`), 35 Uncommons (`WitchCards.Uncommon.cs`, 23 × Akt I / 12 × Akt II), 25 Rares (`WitchCards.Rare.cs`, 8/6/6/5 über Akt I–IV), jede mit Upgrade; Regeln, die die Karte überdauern, als Status in `WitchRules*.cs`; Hexed-Burst und Misfortune-Wurf rufen Haken (`OnBurst`, `AfterRoll`), der Kessel kennt Knotted Cord (zählt doppelt), Horn Spoon (Ward Wax), Shut the Lid, Never Wash the Pot; Hearth-Heilung kennt Keep the Drippings. Junk: Mirror Shard, 5 Soup Stones. `WitchCardTests*` (63 Tests, je Regel rot ohne sie) |
| + Core-Fix | ✔ Core 49f2e6d: eine angekündigte Gegneraktion wartet, bis jedes von der Ankündigung gestartete Programm fertig ist (vorher kam ein zweistufiger Effekt — Crossed Fingers' Block — erst nach dem Schlag) |
| W4-Abweichungen (bewusst, vom Spieler FREIGEGEBEN 2026-10-04) | Murder of Crows/Carrion Flight treffen zufällige Gegner statt verteilter Wahl · Tell the Bees und Midwife's Hands sind Rites · Stone Soup: „Familie nennen → erste Junk-Karte der Hand wird Soup Stone dieser Familie und kommt in den Kessel“ · Familiar's Supper gibt die freie Zutat als Status (bleibt bis zur nächsten Zutat, nicht nur diesen Zug) · Call the Third Night löst die Burst-Haken (Third Bell, Bane-Root …) nicht aus · Adder in the Sleeve zählt Warten pro Hexe, nicht pro Kopie |
| Pools der Hexe | ✔ bnb-content 6eba6bf: Kartenbelohnungen (je Charakter eigene `for:<id>`-Einträge, eigene Kurven), Verwandeln, Charakter-Regal (10 + 10, eigener Würfel), Licensed Vendor + Akt-III-Märkte (`RunExpr.CharacterIs`, Core 7599950); Bureaucrat-Ziehungen byte-gleich (Test). Core 661cd04: Tutorial/Probe behalten ihren Charakter (`AsLoneFighter`) — sonst hätte das echte Tutorial keine Kartenbelohnung mehr gezeigt |
| W5 Relikte | ✔ d1ba464: 18 Relikte (3/5/4/6), Regeln in `WitchRules.Relics.cs`, Relikt-Belohnungen/Regale/Marktstände je Charakter; Black Spoon (f12da0c) belohnt das erste Hidden Recipe pro Kampf (Abweichung: Entdeckung ist Profilwissen) |
| Hidden Recipes | ✔ f12da0c: 20 Rezepte (ersetzen den Sud, Reihenfolge/Upgrade egal, Dregs-Slot), `hidden_recipe_brewed` für das Rezeptbuch, Hinweistexte ohne Kartennamen |
| G4 Rezeptbuch | ✔ bnb-godot 7bc04b7: Everyday Brewing / Things That Worked Once / Notes in the Margin, „NEW RECIPE DISCOVERED“, `user://recipes.json`, Vorschau nennt gefundene Rezepte |
| W7 Kompendium | ✔ 9e3c7c7 (Kessel, Into the Pot, Brew, Familien, Konzentriert, Dregs, Sheltering, Hidden Recipes, Heilgrenze, Hexed, Misfortune) |
| E6 Hot | ✔ 9e3c7c7 gebaut und AUS (`--witch-hot` = Standregel im Start) |
| G6 Golden | ✔ bnb-godot ede9825: 4 unsterbliche + 2 sterbliche Hexen-Läufe, Bureaucrat-Zeilen unverändert |
| §18 Kessel-Optik | ✔ ede9825 ohne Kunst: Familienfarben, Dampf, Deckel, pulsierendes BREW |
| Offen | Kunst (ART_SLOTS), Tutorial der Hexe (E9: später), Balance B1–B4 (Abweichungen inkl. Black Spoon freigegeben 2026-10-04) |

---

## Vorlage E4 — Familien der 50 General-Pool-Karten (FREIGEGEBEN 2026-10-04)

Nach Master §16.2: direkter Schaden → **Fang** · Flüche, feindliche Status-Motoren, verzögerter Schaden → **Hex** ·
Block, Ward Wax, Schutz → **Husk** · Erholung, Reinigung, Ziehen, praktischer Nutzen → **Hearth** · Verweigerung,
Censure, seltsame Kausalität → **Fortune**. Gilt nur für die Hexe; für den Bureaucrat ohne Wirkung.

| Familie | Karten |
|---|---|
| **Fang** (14) | grave_lien¹ · foreclosure · forfeit_seal · dawn_summons · seizure_writ · blood_tithe · vital_census · exemplary_sentence · black_tribunal · grand_citation · crown_repossession · tallow_judgment · hemal_audit · last_office² |
| **Hex** (12) | witchmark_citation · blood_marginalia · mortgage_sigil · silent_hearing · notary_beetle · usurers_moon · sanguine_errata · vein_register · blood_redaction · standing_citation · debt_ouroboros · compound_indictment |
| **Husk** (10) | waxen_surety · contempt_finding · tallow_reserve · sealed_mantle · wax_reliquary · consecrated_testament · mortgaged_aegis · votive_covenant · candle_cathedral · wax_indemnity³ |
| **Hearth** (5) | borrowed_candle · false_signature · proxy_curse · moonlit_counterfeit · grand_dispensation |
| **Fortune** (9) | malediction_review · sanctioned_charm · reciprocal_edict · blacklisted · countermanded_grace · crossed_sigil · oath_of_refusal · sovereign_prohibition · absolute_interdict |

¹ 13 Schaden + 9 Lien: Schaden zuerst, darum Fang (Alternative: Hex). ² „Je fehlendem Status 8 Schaden“: Schaden,
darum Fang (Alternative: Fortune, „seltsame Kausalität“). ³ heilt über Wachs, aber Wachs ist Schutz: Husk
(Alternative: Hearth). Hearth bleibt mit 5 die kleinste Familie — die Hexe bringt ihre eigenen Hearth-Karten mit.

---

## Vorlage E7 — Zahlen für die Hexenkarten (FREIGEGEBEN 2026-10-04)

Der Kanon gibt Zahlen nur für die vier Starter; Commons, Uncommons und Rares sind Konzepte. Vorschlag, wie beim
Bureaucrat-Kartenpass: erst diese Regeln und 14 Beispielkarten, nach deiner Freigabe der Rest.

**Regeln (Wert pro Energie, angelehnt an die Bureaucrat-Pools nach dem Keyword-Pass):**
- R1 Fang: 1 E ≈ 7–8 Schaden (Starter 6), Mehrfachtreffer etwas weniger in Summe, AoE ≈ 60 % pro Ziel.
- R2 Hex: 1 E ≈ 4–5 Hexed (= 12–15 HP alle drei Gegnerzüge, bleibt liegen). Timing-Karten (Schritt vorziehen,
  sofort auslösen) zahlen ihren Effekt mit weniger Stapeln.
- R3 Husk: 1 E ≈ 7–8 Block; Ward Wax 1 E ≈ 5 (×1,25 wie der General-Pool jetzt).
- R4 Hearth: 1 E ≈ 3 HP Kampf-Heilung + kleiner Zusatz (Block oder Ziehen); Heilung bleibt unter der
  Kampfbeginn-Grenze, darum großzügiger als reine Heilung beim Bureaucrat.
- R5 Fortune: 1 E ≈ 25–30 % Misfortune (5–6 Stapel); ein Wurf, der scheitert, ist verloren — darum gibt
  Fortune oft einen kleinen sicheren Teil (Block, Schaden) dazu.
- R6 Sheltering-Bonus ≈ +40 % auf den Grundwert, solange der Kessel leer ist.
- R7 Upgrades ≈ ×1,5 Gesamtwert, gern als neue Facette (wie beim Bureaucrat-Pass).
- R8 Spezial-Zutaten (Knotted Cord, Horn Spoon …) zahlen ihre Kesselwirkung mit einem schwächeren Kartentext.

**Beispiele:**

| Karte | Seltenheit | Vorschlag | Upgrade |
|---|---|---|---|
| Bramble Switch (Fang) | Common 1 E | 7 Schaden, +3 wenn das Ziel Hexed ist | 9 Schaden, +5 |
| Two Teeth (Fang) | Common 1 E | 4 Schaden ×2 | 6 Schaden ×2 |
| Crow's Peck (Fang) | Common 0 E | 3 Schaden, 6 wenn Ziel ≤ 50 % HP | 4 / 9 |
| Evil Eye (Hex) | Common 1 E | 3 Schaden, 3 Hexed | 4 Schaden, 5 Hexed |
| Old Grudge (Hex) | Common 2 E | 10 Hexed | 15 Hexed |
| Birch-Bark Wrap (Husk) | Common 1 E | 8 Block | 12 Block |
| Snail Shell (Husk) | Common 1 E | 6 Block; kein ungeblockter Treffer → 3 Ward Wax | 8 Block, 4 Ward Wax |
| Mugwort Poultice (Hearth) | Common 1 E, Exhaust | 6 HP Kampf-Heilung | 9 HP |
| Black Cat (Fortune) | Common 1 E | 4 Schaden, 25 % Misfortune | 6 Schaden, 35 % |
| Hawthorn Switch (Fang) | Uncommon 1 E | 8 Schaden; ist das Ziel Hexed: Threefold-Schritt +1 | 11 Schaden |
| Knotted Cord (Hex) | Uncommon 1 E | 2 Hexed; **im Kessel: zählt als zwei Hex** | 3 Hexed, zieh 1 |
| Clamp the Lid (Husk) | Uncommon 1 E | 7 Block; Sheltering: 4 davon bleiben als Ward Wax | 10 Block, 5 Ward Wax |
| Call the Third Night (Hex) | Rare 2 E | Threefold des Ziels sofort auslösen, danach neuer Zyklus | kostet 1 |
| Loaded Knucklebones (Fortune) | Rare 1 E | der nächste Misfortune-Wurf wird zweimal geworfen, einer reicht; 15 % Misfortune | 30 % |
