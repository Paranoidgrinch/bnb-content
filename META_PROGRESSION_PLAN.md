# Meta-Progression — Konzept (Lieutenant zuerst)

Stand 2026-11-17. Baut auf den Entscheidungen vom 2026-10-26 auf (XP, Booster, Startsets, Muscheln, Depot) und
auf der Abstimmung vom 2026-11-17. Gilt vorerst nur für die Lieutenant; die anderen drei Charaktere bleiben,
wie sie sind, bis sie auf ihr Niveau gebracht werden.

## 0 Entscheidungen

| # | Frage | Entscheidung |
|---|---|---|
| M1 | Startset | 30 ihrer 80 Karten, damit ist ein Lauf gewinnbar; alle fünf Mechaniken (Strength, Authority, Orders, Challenges, Open Guard) von Anfang an vertreten |
| M2 | Booster | 13 Stufen; 12 Booster à 4 Karten + 1 letzter mit 2 Karten und einem Relikt = 50 fehlende Karten. Gelenkt, nicht zufällig: jeder Booster vertieft eine Mechanik |
| M3 | XP | nach Fortschritt, schneller Anfang; alles freigespielt nach etwa 25 Läufen |
| M4 | Depot | ein **persönliches Schließfach / Aktenschrank**: dort liegen Booster, Relikt-Kisten und Muscheln |
| M5 | Trials | ein Menüpunkt „Trials“ für alle Encounter-Trials (gebaut, bnb-godot d13c15f). **Trials stehen außerhalb der Progression**: volle Kartenpools, keine Modifikatoren, keine XP, keine Muscheln |
| M6 | Muscheln | schalten **Modifikatoren** frei (nach Art der Artifacts in Risk of Rain 2); Modifikatoren sind gut, schlecht oder nur anders und ändern die Muscheln, die ein Sieg bringt |
| M7 | Erster Sieg, Serien | Modifikatoren erst nach dem ersten Sieg (der die erste Muschel bringt); jeder weitere Sieg in Folge +1 Muschel mehr (2026-11-17) |
| M8 | Trials | ab Akt II lassen sich die Startbelohnungen zufällig ziehen (gebaut, bnb-godot d3063bb) |

## 1 Startset und Booster (M1–M3)

**Startset (30 Karten):** 12 Common, 12 Uncommon, 6 Rare, dazu 10 ihrer 19 Relikte. Jede Mechanik hat
mindestens eine Karte, die sie aufbaut, und eine, die sie auszahlt. Die genaue Liste nach Karten-Id ist der
erste Bauschritt (§6), mit dem Encounter-Trial als Prüfung: ein Startset, mit dem Akt I nicht zu schaffen ist,
ist falsch.

**Booster-Reihenfolge (Vorschlag):**

| Stufe | Inhalt |
|---|---|
| 2–3 | Orders vertiefen |
| 4–5 | Challenges vertiefen |
| 6 | Open Guard vertiefen |
| 7–10 | die Rares, die zwei Mechaniken verbinden (die Brückenkarten) |
| 11–12 | Authority und Strength in der Breite |
| 13 | die letzten 2 Karten + 1 Relikt |

Ein Booster wird nicht gezogen, sondern ist fest. Was nach Stufe 13 noch fehlt (Relikte), kommt aus
Relikt-Kisten.

**XP pro Lauf:**

| Quelle | XP |
|---|---|
| jeder betretene Raum | 1 |
| Elite besiegt | 3 |
| Boss besiegt | 10 |
| Lauf gewonnen | +15 |

Stufe 2 nach etwa einem Akt-I-Lauf; die Kurve steigt so, dass Stufe 13 nach etwa 25 Läufen erreicht ist. Auch
ein verlorener Akt-I-Lauf bringt sichtbaren Fortschritt.

## 2 Das Schließfach (M4)

Das Hauptmenü bekommt einen Punkt **„Schließfach“** (englisch im Spiel: „Safe Deposit“). Der Look: ein
Bankschließfach oder ein persönlicher Aktenschrank. Schubladen mit Messingschildern, Siegel, Quittungen,
Bank-Briefköpfe auf dem Papierlook, den das Spiel schon hat.

- **Schubladen = Booster:** eine neue Stufe legt eine versiegelte Mappe hinein; man öffnet sie selbst (Siegel
  brechen, vier Karten kommen heraus).
- **Kassette = Relikt-Kisten.**
- **Muschelfach:** die Muscheln, dazu der Kontoauszug (wofür welche Muschel kam).
- **Register:** welche Karten man besitzt, welche noch fehlen (als leere, beschriftete Fächer).
- Die Story trägt das: geerbt, niemand weiß von wem, mit dem Kontoauszug als roter Faden.

## 3 Modifikatoren (M6)

### 3.1 Vergleich mit Risk of Rain 2

In Risk of Rain 2 sind die **Artifacts** Regeländerungen für einen ganzen Lauf. Man schaltet sie einmal frei
und wählt sie dann vor jedem Lauf in der Lobby an oder ab. Beispiele:

- **Glass:** +500 % Schaden, aber nur 10 % Leben (gut und schlecht zugleich).
- **Command:** Gegenstände selbst wählen statt zufällig (gut).
- **Sacrifice:** keine Truhen, dafür lassen Monster Gegenstände fallen (anders).
- **Kin:** pro Stage nur eine Monsterart (anders).
- **Dissonance:** Monster aus allen Gebieten gemischt (anders).
- **Honor:** alle Gegner sind Elites (schlecht).
- **Spite:** Gegner lassen beim Tod Bomben fallen (schlecht).
- **Swarms:** doppelt so viele Gegner mit halbem Leben (anders).

Was zu BnB passt:
- Freischalten einmal, danach frei an- und abwählbar.
- Der Reiz liegt im Ausprobieren, nicht nur in der Schwierigkeit.
- Viele Kombinationen.
- Ein einfacher Schalter pro Modifikator.

Was wir **anders** machen:
- In Risk of Rain 2 ändern Artifacts die Belohnung nicht, der Schwierigkeitsgrad ist ein eigenes System.
- Bei uns hat jeder Modifikator einen **Muschelwert**. Das macht die Modifikatoren zugleich zur zweiten Ebene
  neben der Progression (die frühere Idee mit der Erbschafts-Anfechtung geht darin auf).
- Freigeschaltet wird mit Muscheln statt mit versteckten Codes.

### 3.2 Muschel-Rechnung

- **Anfangs gibt es keine Modifikatoren.** Der Reiter ist zu, bis der erste Lauf gewonnen ist. Der erste Sieg
  bringt die erste Muschel und schaltet den Reiter frei (user, 2026-11-17).
- Ein gewonnener Lauf bringt **1 Muschel** als Grundwert.
- Jeder aktive Modifikator hat einen Wert: schlecht **+1 oder +2**, anders **0**, gut **−1**.
- Grundwert + Modifikatoren ergeben **mindestens 0** (wer sich alles leichter macht, bekommt dafür nichts) und
  **höchstens 6**.
- **Siegesserie:** jeder weitere Sieg in Folge bringt **+1 mehr** — der zweite Sieg in Folge +1, der dritte +2
  und so weiter. Der Bonus kommt nach dem Deckel oben drauf; ein verlorener oder aufgegebener Lauf beendet die
  Serie. Die Serie zählt je Charakter.
- Verlorene Läufe bringen keine Muscheln (aber XP).
- Der Modifikator-Tab zeigt den Ertrag live an, bevor der Lauf beginnt, mit der laufenden Serie
  („Serie: 3 Siege — der nächste Sieg bringt +3“).

Beispiel: dritter Sieg in Folge mit Overtime (+2) → 1 + 2 = 3, dazu Serie +2 = **5 Muscheln**.

Offen zum Ausprobieren: ob die Serie einen Deckel braucht (Vorschlag: erst einmal keinen, nach den ersten
Spieltests ansehen), und ob ein Sieg mit Ertrag 0 (nur gute Modifikatoren) die Serie hält. Vorschlag: ja,
ein Sieg ist ein Sieg.

### 3.3 Der Modifikator-Tab

- Auf dem Charakter-Bildschirm vor dem Lauf gibt es einen Reiter **„Modifikatoren“**.
- Freigeschaltete Modifikatoren sind Karten mit Schalter. Gesperrte sind sichtbar, aber verdeckt: Name, Preis,
  ein Satz Andeutung.
- Darunter steht: „Ein Sieg bringt: N Muscheln“.
- Ein Lauf merkt sich seine Modifikatoren, die History und die Lauf-Logs der Tester zeigen sie mit.

### 3.4 Modifikatoren (15, vom user durchgesehen 2026-11-17)

Werte: Muschelwert / Freischaltpreis in Muscheln.

| Name | Art | Regel | Wert | Preis |
|---|---|---|---|---|
| **Overtime** | schlecht | Gegner beginnen jeden Kampf mit 1 Strength je Akt | +2 | 3 |
| **Statute of Limitations** | schlecht | ab Runde 8 eines Kampfes werden Gegner wütend (+3 Strength pro Runde) | +1 | 2 |
| **Spite Filing** | schlecht | ein Gegner, der fällt, verursacht 5 Schaden am Helden | +1 | 2 |
| **Strict Filing** | schlecht | Kartenbelohnungen bieten 2 statt 3 Karten | +1 | 1 |
| **Inherited Debt** | schlecht | Start mit zwei Flüchen im Deck (alle starten ohnehin mit 0 Gold) | +1 | 1 |
| **Glass Seal** | anders | du verursachst doppelten Schaden, hast aber halbe Max-HP | 0 | 2 |
| **Single Department** | anders | jeder Akt zieht seine normalen Kämpfe aus nur einer Gegnerfamilie | 0 | 1 |
| **Mixed Departments** | anders | normale Kämpfe aus allen Akten bis zum aktuellen, angepasst an den Akt | 0 | 2 |
| **Paper Trail** | anders | doppeltes Gold, doppelte Ladenpreise; Entfernen im Laden kostet nichts extra | 0 | 1 |
| **Clerical Error** | anders | jeden Zug kostet eine zufällige Handkarte 0, eine andere 1 mehr | 0 | 2 |
| **Sealed Envelopes** | anders | Kartenbelohnungen sind verdeckt, dafür 4 statt 3 | 0 | 2 |
| **Requisition Form** | gut | einmal pro Akt: eine Karte deiner Wahl aus deinem Pool | −1 | 3 |
| **Second Opinion** | gut | jede Kartenbelohnung einmal neu ziehen | −1 | 2 |
| **Signing Bonus** | gut | Start mit einem zufälligen Rare-Relikt | −1 | 2 |
| **Bureaucratic Sacrifice** | anders | keine Kartenbelohnungen nach Kämpfen; Gegner lassen mit 25 % eine Karte fallen | 0 | 3 |

Mit allen fünf schlechten zusammen bringt ein Sieg 1 + 6 → gedeckelt 6.

Gestrichen: **Audit Season** (Elite-Regel auf jeder normalen Begegnung) — kaum umsetzbar (user).

**Keiner frei von Anfang an:** der erste Sieg bringt die erste Muschel, und dafür gibt es vier Modifikatoren zu
1 Muschel (Strict Filing, Inherited Debt, Single Department, Paper Trail). Alle 15 kosten zusammen 29 Muscheln.

### 3.5 Später

- **„The gods remember“:** ein besiegter Gott kommt im nächsten Lauf zorniger zurück. Das kann als
  Modifikator kommen (+2), sobald es Boss-Varianten gibt.
- **Vollmachten** (die Sidegrades vom 2026-10-26) sind in den „anders“-Modifikatoren aufgegangen.
- **Zinseszins** auf gesparte Muscheln: zurückgestellt, bis klar ist, ob Muscheln knapp genug sind.

## 4 Was die Progression nicht berührt

- **Trials:** voller Kartenpool, keine Modifikatoren, kein XP, keine Muscheln (M5).
- **Bot-Bench, Golden-Set, Simulator:** voller Pool, damit Balance-Zahlen vergleichbar bleiben.
- **Tägliche Läufe / geteilte Seeds:** voller Pool und keine Modifikatoren, damit alle dasselbe spielen.
  Offen, ob es später einen täglichen Modifikator geben soll.
- **Mehrspieler:** jeder spielt mit seinem eigenen Bestand; Modifikatoren legt der Gastgeber fest.

## 5 Technik

- **Bestand:** `MetaState` bekommt pro Charakter XP, Stufe, die freien Karten, die freien Relikte, die
  ungeöffneten Booster und die Muscheln. Dazu kommen die freigeschalteten und die zuletzt gewählten
  Modifikatoren.
- **Pool-Filter:** der Laufstart reicht die Liste der besessenen Karten und Relikte an Core weiter. Belohnungen,
  Läden und Ereignisse ziehen nur daraus. Ohne Liste gilt der volle Pool, das ist der Weg der Trials, Benches und
  täglichen Läufe.
- **Modifikatoren:** sie sind Lauf-Regeln. Was im Kampf wirkt, wird ein Status am Helden oder an Gegnern beim
  Kampfstart (wie die Elite-Regeln). Was den Lauf betrifft (Belohnungsgröße, Gold, Laden), wird ein Schalter in
  der Lauf-Konfiguration.
- **Speichern:** Bestand und Muscheln gehen in den Spielstand der Meta, nicht in den Laufstand; ein Lauf merkt
  sich nur seine Modifikatoren.
- **Lauf-Logs:** sie tragen die aktiven Modifikatoren und die Stufe, damit Testerdaten auswertbar bleiben.

## 6 Bauschritte (je ein Commit)

1. Startset und Booster-Reihenfolge nach Karten-Id; prüfen mit einem Encounter-Trial-Lauf durch Akt I.
2. Core/Converter: Pool-Filter am Laufstart, Tests, Bench/Golden unverändert auf vollem Pool.
3. `MetaState`: XP, Stufen, Bestand, Muscheln, Modifikator-Auswahl; XP-Abrechnung nach dem Lauf.
4. Schließfach-Bildschirm: Mappen öffnen, Register, Muschelfach.
5. Modifikator-Tab auf dem Charakter-Bildschirm, mit Live-Ertrag.
6. Die 15 Modifikatoren, in Gruppen zu je vier, jeweils mit Probe.
7. Lauf-Logs und History zeigen Modifikatoren und Stufe.

## 7 Stand (2026-11-18)

Gebaut:
- **Muscheln** (bnb-godot `Shells.cs`): Auszahlung, Deckel, Siegesserie je Charakter, erster Sieg öffnet das
  Fach, Niederlage und aufgegebener Lauf beenden die Serie; Kontoauszug in `user://shell-statement.json`.
- **Schließfach** (`SafeDepositPanel.cs`, Hauptmenü „Safe Deposit Box“): Muschelfach, Kontoauszug,
  Modifikator-Register mit Kaufen.
- **Modifikator-Tab** auf dem Blatt „Start a new run“: Schalter für gekaufte Modifikatoren, Ertrag live;
  der Tagesrun läuft ohne Modifikatoren.
- **Wirkung als Relikt** `mod_<id>` (bnb-content `Converter/Relics/ModifierRelics.cs`): ein Lauf startet mit den
  Relikten seiner Modifikatoren. Fertig: Overtime, Inherited Debt, Glass Seal, Signing Bonus. Die übrigen 11
  stehen im Laden als „In preparation“ und lassen sich erst kaufen, wenn ihr Relikt existiert.
- Probe `--smoke-shells`.

Offen: die übrigen 11 Modifikatoren; Strict Filing und Sealed Envelopes brauchen eine Belohnungsregel „weniger
Angebote“ bzw. verdeckte Karten, Second Opinion ein Neu-Ziehen, Single/Mixed Departments die Kartengenerierung.

