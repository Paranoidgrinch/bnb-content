# Balance-Plan — Diagnostik, die nicht ewig rechnet

Stand 2026-09-27, nach dem Aufräumen (Core `557113c`, bnb-godot `4c85c6f`).

## Das Prinzip

**Balance wird am einzelnen Kampf gemessen, nicht am Lauf.** Ein Lauf ist dreißig Räume Rauschen vor dem
Kampf, um den es geht; er kostet Minuten und ist nie zweimal derselbe. Ein Kampf mit festgelegtem Deck ist
Sekunden, wiederholbar und paarbar.

- **Ein Spieler:** der `FightPlanner` (RogueDeck.Bot) — spielt einen Kampf mit fünf Zügen Vorausschau
  (receding horizon, Beam). Er sieht den Nachziehstapel, also ist sein Ergebnis eine **Untergrenze der
  Kosten**: Was hier teuer ist, ist teuer. Was hier billig ist und Spieler trotzdem tötet, ist eine
  Frage des Könnens oder der Lesbarkeit, nicht der Zahlen.
- **Eine Maßeinheit:** `(Encounter, Deck, Relikte, HP, Mischung, Planner-Einstellung, Inhalts-Hash)` →
  `gewonnen?, HP verloren, Züge, Sekunden`.
- **Alles Größere ist eine Summe:** eine Stufe, ein Akt oder eine Route besteht aus solchen Messungen,
  gewichtet damit, wie oft die Karte (`MapOracle`) den Kampf austeilt.

## Was es nach dem Aufräumen gibt

| Werkzeug | Wofür | Nicht wofür |
|---|---|---|
| `FightPlanner` + `--act1-bench` (bnb-content) | **das Messgerät** | — |
| `--fight` SparringRing (bnb-content) | ein Kampf mit angegebenem Deck; heute mit Greedy → wird auf den Planner umgestellt (B1) | — |
| `MapOracle` / `roguedeck-bot --oracle-only` | welche Encounter eine Karte wie oft austeilt; Sekunden für 1000 Seeds | Schwierigkeit |
| Würfelspieler `RunBot`, `simulate.sh`, `golden.sh` | Abdeckung, Abstürze, Regression | **keine** Balance-Zahlen |
| `--playtest` / `--walk` (RunWalker) | Bugs, die nur ein ganzer Lauf findet | **keine** Balance-Zahlen |
| `bnb-runs` + `--replay-run` | echte Spieler-Läufe, deterministisch nachspielbar | — |

Entfernt am 2026-09-27: Zucht (Politiken, `train.py`), Champion, Exam, Autopsie-Solver, Map-Foresight,
BalanceMap, DamageLedger, `--routes`, `--stop-after-act`, `--legacy` — rund 5300 Zeilen. Die Befunde,
die davon bleiben:

- Der faire Tiefensucher (Deck je Welt gemischt) räumte **exakt so viele** Akt-I-Routen wie der
  hellsichtige (9/67 beide). Hellsicht verzerrt auf Laufebene also kaum. Das rechtfertigt, den
  hellsichtigen Planner als Messgerät zu nehmen.
- Mit einem guten Spieler ist die Kostenkurve je Besuch in Akt I monoton: normal 1,1 < multi 10,3 <
  elite 15,6 < boss 18,0. „Der Boss ist billig“ war ein Artefakt des schwachen Läufers.
- Zucht brachte zwei Nullergebnisse. Lauf-Politik-Fragen werden gemessen, nicht gezüchtet.

## Was wir schon wissen (Akt I, Starterdeck, 70 HP)

`~/Desktop/bnb-balance/20260927-act1-bench-starter.txt`: 640 Kämpfe, 20 Mischungen, Beam 16, **alle
gewonnen**. Mittlere Kosten je Stufe: Queue 4,0 · Counter 3,7 · Form 2,7 · Seal 11,6 · Ordinance 14,2 ·
Delay 8,9 · Appeal 22,1 · Enforcement 14,9. Schlechtester Einzelkampf 57 (Appeal).

Dem stehen die Spieler gegenüber: von 11 aufgezeichneten Läufen enden **8 in Akt I**, die meisten um
Raum 12–14. Der Planner verliert dort nichts. Die Lücke zwischen „Untergrenze“ und „was Spieler zahlen“
ist die interessanteste Zahl, die wir noch nicht haben (→ B4).

## Rechenbudget (gemessen auf dieser Maschine, 12 Kerne, 15 GB)

| Einstellung | Sekunden je Kampf | Quelle |
|---|---|---|
| Beam 16, 300/Zug, Starterdeck | ~62 (parallel, 12 Jobs) | Starter-Bench |
| Beam 6, 100/Zug, 14er-Deck, Stufen 5–8 | ~8 (1,6 – 21 je Encounter) | Karten-Lauf, Basis |
| Beam 6 gegen Beam 16 | ±0,3 HP im Mittel, halbe Zeit | Gedächtnis-Notiz Act-I-Bench |

**Warum der Karten-Lauf starb:** 65 Karten × 16 Encounter × 8 Mischungen = 8320 Kämpfe. Die Kerne waren
nur zu ~40 % ausgelastet (1220 min CPU in 240 min Wand, weil lange Kämpfe am Ende einzeln liefen). Er
wurde nach 4 h abgebrochen, **und weil erst am Ende geschrieben wurde, war alles verloren.**

Größenordnung des ganzen Spiels: ~294 Encounter (Akt I ~109, II ~55, III ~50, IV ~70, V 6). Ein Deck je
Akt, 8 Mischungen, Beam 6 ≈ 2350 Kämpfe ≈ **30–60 min** bei voller Auslastung. Im Sieb (B2) soll das ein
Viertel davon werden.

## Die Regeln gegen Ewig-Rechnen

1. **Kein Lauf ohne Voranschlag.** Vor dem Start stehen `Kämpfe × Sekunden je Kampf ÷ Kerne` da. Die
   Sekunden kommen aus dem Speicher (B1), sonst aus der Tabelle oben. Über **30 min** startet nichts ohne
   `--yes`.
2. **Jede Zeile sofort auf die Platte.** Ein abgebrochener Lauf verliert höchstens die Kämpfe, die gerade
   liefen.
3. **Nie zweimal dasselbe rechnen.** Ergebnisse liegen in einem Speicher. Der Schlüssel enthält den
   Inhalts-Hash **des Encounters und der Deckkarten**, nicht den des ganzen Spiels. Wer einen Gegner
   ändert, rechnet nur dessen Kämpfe neu.
4. **Grob vor fein.** Zuerst das Sieb über alles, dann die Feinmessung nur für das, was auffällt.
5. **Längste Kämpfe zuerst** in die Warteschlange (Zeiten aus dem Speicher). So hängen am Ende nicht
   einzelne Riesen-Kämpfe an einem Kern, während elf warten.
6. **Ein langer Lauf zur Zeit** (Gedächtnis: gleichzeitige Sonden kosteten einmal das 20-Fache).

## Die Schritte

Jeder Schritt hat ein Tor. Er gilt erst als fertig, wenn das Tor mit Zahlen bestanden ist.

**Reihenfolge:** K1 braucht nichts und kostet Sekunden, also zuerst. B0 → B1 → B2. B3 braucht K1 für die
Leiter-Decks, B4 braucht B0, K2 braucht B1 und B3, K3 braucht K1 und K2.

### B0 — Vorbedingung: beide Sitze spielen dasselbe Spiel

Beim Neuaufnehmen des Golden-Sets (2026-09-27) gefunden, **älter als das Aufräumen:** Immortal-Seed 11
wird vom direkten Sitz (Konsole) anders gespielt als vom Replay-Sitz (Godot, `--replay`). Schon in
Akt I, Raum r16c2, vergibt der direkte Sitz eine Karten-Instanznummer mehr (`deskward (card#16)` statt
`card#15`). Ab Akt III laufen die Läufe auseinander (Schaden 7929 statt 5501).

Das betrifft uns doppelt: B4 spielt Spieler-Aufnahmen nach, und die Kartenwert-Paarung (K2) verlässt
sich auf Instanznummern.
**Tor:** `golden.sh` (Godot) und `golden.sh --console` stimmen in 15/15 Läufen überein, dazu ein Test,
der die erste abweichende Stelle festnagelt.

### B1 — Ein Bench für alles, mit Speicher und Voranschlag

`Act1Bench` wird zum allgemeinen `Bench`. Eingabe: eine Liste von Kampf-Aufträgen (Encounter × Deck ×
HP × Mischungen). Ausgabe: JSONL-Zeilen in `~/Desktop/bnb-balance/store/`, eine je Kampf, sofort
geschrieben. Dazu kommen die Regeln 1–5. `--fight` bekommt denselben Planner statt Greedy. Greedy bleibt
nur für die Walks.

**Tor:** (a) Der Starter-Bench, zweimal hintereinander gestartet, rechnet beim zweiten Mal **0** Kämpfe
und druckt dieselbe Tabelle. (b) Nach einem Abbruch mitten im Lauf rechnet der Neustart nur den Rest.
(c) Der Voranschlag liegt höchstens ±30 % neben der Wanduhr.

### B2 — Das Sieb eichen

Akt I mit Starterdeck zweimal messen: fein (Beam 6, 20 Mischungen) gegen grob (Beam 2, 4 Mischungen).
**Tor:** Die Rangfolge der Encounter nach mittleren Kosten korreliert mit Spearman ≥ 0,9, und die
Top-10-Ausreißer sind zu ≥ 8/10 dieselben. Wenn nicht: die nächstgröbere Stufe probieren (Beam 4,
8 Mischungen). Ergebnis ist **eine** Sieb-Einstellung, die hier eingetragen wird.

### B3 — Alle Akte, alle Rollen

Auswahl nach Akt × Rolle (normal / multi / elite / boss) statt nur nach Akt-I-Stufen. Genommen werden
nur Encounter, die die Karte wirklich austeilt, gewichtet mit ihrer Häufigkeit laut `MapOracle` über
viele Seeds. Bericht je Akt: Rolle × Mittel / Median / schlechtester Wert. **Ausreißer** sind Encounter
mit mehr als dem 1,5-Fachen des Rollen-Medians ihres Akts (die Schwelle wird nach dem ersten Lauf
festgelegt).

**Die Decks — eine Leiter, Stufe für Stufe (entschieden 2026-09-27).** Ein Deck wächst so, wie es im
Spiel wächst: **nach jeder Stufe kommt eine Karte dazu.**

- **Akt I:** Starterdeck. Nach Stufe k wird eine Karte aus dem Belohnungs-Pool von Akt I gelegt.
- **Ab Akt II:** Die Startpunkte sind die echten Decks der Demo-Spieler, mit denen sie Akt II betreten
  haben (aus `bnb-runs`): timez, Quintus (zwei Läufe), Derin Büro, Sandale. ⚠ Quintus ×2 und Derin Büro
  spielten **denselben Seed** 165882923, das sind also eher drei als fünf unabhängige Decks. Von dort
  wächst jedes Deck die Stufen des Akts hinauf, eine Karte je Stufe.
- **Welche Karte dazukommt:** die bestbewertete des Stufen-Pools nach der statischen Bewertung (K1).
  Das ist die Leiter eines „vernünftigen Spielers“. Dazu kommt eine Leiter mit zufälliger Wahl (fester
  Seed) als Streuung.
- Jede Stufe wird mit dem Deck gemessen, das man **dort** hätte. Eine teure späte Stufe mit gewachsenem
  Deck ist ein Befund. Eine teure Stufe, die nur mit dem Starterdeck teuer ist, ist keiner.

⚠ Die Aufnahmen stammen von Inhalts-Hash `deebd79`, vor dem Upgrade-Sync. Karten-Ids, die es nicht mehr
gibt, werden gemeldet und nie still weggelassen.

**Tor:** Das ganze Spiel läuft im Sieb unter 30 min, und jeder Akt hat einen Bericht.

### B4 — Spieler gegen Untergrenze

Für jeden Kampf in jeder Aufnahme in `bnb-runs`: bis zum Kampfbeginn nachspielen (braucht B0) und die
tatsächlichen HP-Kosten des Spielers notieren. Dann spielt der Planner **dieselbe Stellung** mit
demselben Deck, denselben Relikten und derselben Mischung. Die Differenz ist gepaart und verrauscht
nicht.

Heraus kommen zwei verschiedene Befunde: Ist die **Untergrenze** hoch, ist der Inhalt teuer. Ist die
**Lücke** groß, ist der Kampf für Menschen schwer (versteckte Information, zu viele Regeln,
Lesbarkeit).
**Tor:** Jeder aufgezeichnete Kampf (heute 11 Aufnahmen, 377 Räume) hat ein Paar. Aufnahmen mit anderem Inhalts-Hash
werden gegen **ihren** Inhalt nachgespielt oder ausdrücklich übersprungen, nie still gemischt.

### K — Die Karten selbst bewerten

Zwei Bewertungen, die sich gegenseitig prüfen: eine **statische**, die in Sekunden aus dem Kartentext
kommt, und eine **gemessene**, die der Planner im Kampf liefert. Wo beide übereinstimmen, gilt die
billige Zahl. Wo sie auseinanderliegen, steckt in der Karte etwas, das Zahlen nicht sagen: eine
Bedingung, eine Skalierung, eine Synergie. Genau dort schaut man hin.

#### K1 — Statisch: Wirkung pro Kosten (Sekunden, kein Planner)

Erst ganz einfach: **Geringe Kosten und hohe Schadens-, Block- und Statuszahlen sind gut.**

- Das Programm jeder Karte wird abgelaufen und in Töpfen addiert: Schaden, Block, Status (Stapel, je
  Status getrennt aufgeführt), gezogene Karten, gewonnene Ressourcen. Mehrfachtreffer multiplizieren,
  Flächenschaden zählt mal der mittleren Gegnerzahl des Akts.
- Dafür wird der am 2026-09-27 gelöschte `CardFeatures` (Core `557113c^`) wiederbelebt, als
  Inhalts-Werkzeug in bnb-content und nicht mehr im Bot. Er las schon Zahlen statt Wörter (B3) und
  benennt seine drei Schätzungen offen (bedingter Zweig 0,5 · Skalierung 3 · Karten in einer Zone 4).
- **Wert = gewichtete Wirkung ÷ Energiekosten.** Eine 0-Kosten-Karte zählt mit 0,5, damit sie nicht
  unendlich wird. Anfangsgewichte: 1 Schaden = 1 Block = 1 Punkt. Status, Ziehen und Ressourcen werden
  in „Punkte einer durchschnittlichen Karte dieses Spiels“ umgerechnet. Die Gewichte stehen als Tabelle
  im Kopf des Berichts, nicht versteckt im Code.
- Die **Basisversion und die Upgrade-Version** jeder Karte stehen nebeneinander. Das Upgrade ist ein
  eigener Wert, und ein Upgrade, das fast nichts bringt, ist selbst ein Befund.
- Ausgabe: eine Tabelle je Akt-Pool, sortiert nach Wert, mit Rang innerhalb der Seltenheit.
  **Jede Karte, deren Programm Knoten enthält, die der Walker nicht lesen kann, wird aufgeführt**
  (`unread=...`), statt stillschweigend mit 0 bewertet zu werden.

**Tor:** alle Karten in unter 10 s. Die Liste der unlesbaren Karten ist vollständig und wird von Hand
durchgesehen, und die Top und der Boden jedes Pools ergeben beim Lesen Sinn.

**✔ Gebaut 2026-09-27:** `--card-rating` (`Converter/Playtest/CardRating.cs`, Tests in `CardRatingTests`).
138 Karten in ~4 s, jeder Knotentyp wird gelesen. Ergebnis in
`~/Desktop/bnb-balance/20260927-card-rating.txt`. Umgesetzt wie geplant, mit drei Präzisierungen:
- **Zwei Werte statt einem:** `safe` (skalierende Teile = 0, das ist der Rang) und `max` (Skalierung
  gefüttert, läuft meist in den Deckel der Karte). Black Tribunal: 7 gegen 27 pro Energie.
- **Die Schlüsselwort-Maschinerie wird erkannt und nicht gezählt:** die Seal-Umwandlung (genau
  „≥ 3 Seal“), die Riten-Synergie (`rite_in_force`) und die Buchhaltungs-Status (archived, junk_filed,
  queue_resolved, ratified). Gezählt werden die acht Schlüsselwörter Paperwork, Doubt, Seal, Censure,
  Lien, Citation, Blood Ink und Ward Wax.
- **44 Karten tragen ihre Wirkung (ganz oder teilweise) in einer Status-Regel** (alle Riten und einige
  Working-Karten). Sie sind mit `rule:<id>` markiert und statisch nicht bewertet. Das ist die größte
  Lücke von K1, und K2 muss sie füllen.

Erste Auffälligkeit für K3: Permit A38 (5 Paperwork für 2 Energie) kommt nur auf 2,5 pro Energie, weil ein
Paperwork-Stapel jede Runde wirkt und mit 1 Punkt zu billig angesetzt ist.

#### K2 — Gemessen: was die Karte im Kampf spart

Die Tausch-Methode ist gebaut: Die Karte ersetzt Paper Cut bzw. Cower an derselben Stelle, und jedes Paar
wird auf gleiche Ziehreihenfolge geprüft. Neu kommt eine **schrittweise Auslese** dazu. Runde 1: alle
Karten, Sieb, 4 Mischungen. Danach fallen Karten raus, deren Wert eindeutig ist (|Mittel| > 3
Standardfehler) oder eindeutig nichts (< 1 HP bei kleinem Fehler). Runde 2 misst nur den Rest fein.
Gemessen wird auf den Leiter-Decks aus B3, also im Deck, in dem die Karte tatsächlich landen würde.

**Tor:** Alle Akt-I-Karten sind unter 60 min bewertet, und jede Zeile stand beim Rechnen schon auf der
Platte.

#### K3 — Die beiden gegeneinander

Rangkorrelation statisch gegen gemessen, je Pool. Dazu die Liste der größten Abweichungen in beide
Richtungen: **statisch gut und gemessen schwach** (Zahlen, die im Kampf nicht ankommen) sowie
**statisch schwach und gemessen stark** (Wirkung, die der Walker nicht sieht). Wenn die Korrelation hoch
ist, reicht K1 für den Alltag, und K2 läuft nur noch für neue oder geänderte Karten. Die gemessenen
Werte eichen danach die Gewichte von K1.

### B6 — Der Bericht

Eine Datei je Akt in `~/Desktop/bnb-balance/`, festes Format: Kopf (Deck, HP, Einstellung, Inhalts-Hash,
Kämpfe, Wanduhr), Tabelle Rolle × Kosten, Ausreißerliste, Spieler-Lücke je Encounter (sobald B4 steht),
Kartenwerte (K1–K3). Erst damit beginnt das eigentliche **Balancing am Inhalt**. Das Gedächtnis sagt dazu:
Den Audit am Inhalt machen wir erst, wenn genug Spieler-Läufe da sind. Die Werkzeuge B0–B4 und K hängen nicht
daran.

## Was ausdrücklich nicht gemacht wird

- Keine Läufer, die ganze Läufe für Balance-Zahlen spielen, und keine Zucht.
- Den Planner nicht „fair“ machen. Die C4-Messung sagt, dass das auf Laufebene nichts ändert, und die
  Untergrenze ist die ehrlichere Aussage.
- Keine Nachtläufe ohne Voranschlag, keine Ausgabe erst am Ende.
