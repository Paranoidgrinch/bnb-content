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
| Offen | W4 Karten (E7-Vorlage!), W5 Relikte, W6 General-Familien (E4-Tabelle!), Hidden Recipes + Rezeptbuch (G4), Archiv-Rubrik Relikte, Balance |
