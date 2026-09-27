# Content-Fix-Plan

Stand 2026-09-27. Entstanden aus zwei gespielten Läufen (`~/Desktop/bnb-balance/playtest/`) und den Messungen
dahinter. Jeder Punkt hat eine belegte Ursache und ein Tor; er gilt erst als fertig, wenn das Tor steht.

Reihenfolge: F1 → F2 → F3 → F5, F4 parallel als Design-Frage.

## F1 — Relikte nur einmal

**Befund:** Relikt-Belohnungen ziehen aus einem Pool (`PoolRewardSource` → `RunPool.DrawMany`, Core), der nicht
weiß, was der Spieler schon trägt (`ConversionPools.NormalRelicOfRarity`, bnb-content). Doppelte Relikte sind
damit strukturell möglich — aus Truhen, Events, Elites und Shops.

**Fix:** Ein Relikt, das der Lauf schon hat, wird nie angeboten und nie ein zweites Mal hinzugefügt. Im Pool
wird aus den noch nicht besessenen Einträgen gezogen (das Angebot bleibt gleich groß, solange der Pool reicht).

**Tor:** Test — ein Lauf, der jedes Relikt eines Pools bereits trägt, bekommt keines davon angeboten; ein Lauf
mit einem Relikt bekommt es aus einer Quelle mit nur diesem Relikt nicht. Golden-Set neu aufgenommen, mit
Begründung im Commit.

## F2 — Gegner nur in ihrer Stufe

**Befund:** In Akt I–III gibt es **keine** Tiefen-Regel für Kämpfe (`EncounterMinimumDepthPercent` ist leer; nur
Akt IV hat 10 Einträge für Elites). Die Stufen stehen nur als `stage_<name>`-Tags an den Encountern und werden
vom Generator nie gelesen. Die Engine kennt außerdem nur eine Mindest-, keine Höchsttiefe. Belegt: Stufe-8-
Encounter (Enforcement) in Raum 0 und Raum 6. Nicht jeder Encounter hat eine Stufe (Akt I: 5 easy, 16 normal,
8 elite ohne `stage_`-Tag) — erst klären, ob die überhaupt im Pool des Akts stehen.

**Fix:** Core bekommt neben der Mindest- eine Höchsttiefe je Encounter (ein Band). bnb-content setzt für jeden
Kampf mit Stufe k von S das Band seiner Stufe (dieselbe Umrechnung wie die Events: Stufe → Anteil der
Akttiefe). Ist das Band einer Reihe erschöpft, weicht der Generator auf die NÄCHSTE Stufe aus und nie weiter —
eine Karte darf nicht an einem leeren Pool scheitern.

**Tor:** `--lanes` bzw. eine neue Messung über 300 Seeds je Akt: jeder platzierte Kampf steht in seinem Band
(oder der Nachbarstufe, gezählt und ausgewiesen). Test im Core für das Band.

## F3 — Remittitur Seal und alle Status-Auslöser der Relikte

**Befund (bewiesen per Zweig, Seed 7, Antwort 142):** Remittitur Seal legt dem **Helden** zu Beginn jedes Kampfes
2 Paperwork auf. Der Auslöser (`Relics/EliteRelicRules.cs`) reagiert auf das erste `StatusApplied` im Kampf ohne
zu prüfen, dass der Absender ein Gegner und das Ziel der Träger ist — das erste Ereignis ist der eigene
Start-Status des Helden.

**Fix:** Absender = Gegner des Trägers, Ziel = Träger. Danach **alle 17** `StatusApplied`-Auslöser in
`Relics/*.cs` auf dasselbe Muster prüfen (und die Karten-/Rite-Auslöser in `Cards/*Rites.cs`).

**Tor:** Test je korrigiertem Relikt: der eigene Start-Status löst nichts aus; ein Gegner-Status löst genau
einmal aus.

## F4 — Karten-Design: zu wenige Gabelungen (Design-Frage, keine Reparatur)

**Befund (`roguedeck-bot --lanes`, 300 Seeds):** Wege ohne Rast sind **nicht** die Hälfte — in Akt I haben 13 %
aller Wege keine Rast, 2 % haben 3+ Elites und keine Rast. Das eigentliche Problem: **im Schnitt nur 0,1–0,2
Räume mit mehr als einem Ausgang pro Reihe, also rund 3 Gabelungen im ganzen Akt.** „Zwei Elites mitnehmen und
dann abbiegen“ ist fast nie möglich, weil es fast keine Abzweigungen gibt. Eine Tür in den ersten Reihen legt
den Akt fest.

**Nächster Schritt:** Mit dem Spieler entscheiden, wie viele Entscheidungspunkte ein Akt haben soll (z. B. eine
Gabelung alle 2–3 Reihen) und ob die Topologie der Strategic-Generierung (`StrategicMapGeneration.Topology`,
`LaneProfiles`, `ForkQuality`) das hergibt. Messgröße ist `--lanes` („forks per map by row“).

## F5 — Großes Audit aller Karten und Relikte

**Ziel:** Jede Karte und jedes Relikt tut, was sein Text sagt. Automatisch, wiederholbar, mit einer Liste der
Abweichungen zur Durchsicht — nicht 363 Karten von Hand.

- **Karten:** jede Karte einmal in einem ruhigen Testkampf gegen einen passiven Dummy spielen und messen, was
  tatsächlich passiert (Schaden, Block, Status auf wen, gezogene Karten, Energie, erzeugte Karten). Dasselbe aus
  dem Regeltext lesen (die Texte sind formelhaft: „Deal N damage“, „Gain N Block“, „Apply N <Status>“,
  „Draw N“). Abweichung → Liste. Dazu der statische Leser aus K1 als dritte Meinung.
- **Relikte:** jedes Relikt allein in einem Kampf gegen einen passiven Dummy über mehrere Züge, verglichen mit
  demselben Kampf ohne Relikt. Alles, was dem Helden schadet (HP, negative Status), ohne dass der Text es sagt,
  ist ein Befund — genau die Fehlerklasse von Remittitur Seal. Danach je Auslöser-Art ein gezielter Szenario-Test.
- **Tor:** Die Abweichungsliste ist leer oder jeder Eintrag ist begründet (Text angepasst oder Effekt gefixt).

## Kleinere Befunde aus den Läufen (einsortieren, wenn F5 läuft)

- Absichten ohne Zahl: „Ram the Case“ (Civic Battering Ram), „Turn the Glass“ (Inverted Hourglass).
- Contradictory Signpost zeigt vor der ersten Karte eine Absicht, die er nicht ausführt.
- Queue wird im Glossar nicht erklärt.
- Ereignis „versiegelte Seitentür“ verschweigt die Vorladung.
- Verdacht: Grave Coin zahlt für die Kampfregel „Struck Last Round“; Dubious Authority löste einmal nicht aus.
