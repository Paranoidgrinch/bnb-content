# Nachschärfen nach dem Content-Audit (2026-10-21)

Grundlage: `content_audit.html` (Medium), `content_audit_high.html` (High) und `sts2_vergleich.html` (Vergleich mit
Slay the Spire 2) im Ordner `content_audit` auf dem Windows-Desktop. Reihenfolge nach Wirkung, wie im Vergleich
vorgeschlagen. Zahlen sind Balance-Entwurf wie überall; sie gehören in den Family-Bench.

## Erledigt

### Balance-Ausreißer

| Was | Vorher | Jetzt | Warum |
|---|---|---|---|
| Iron Astrolabe (Normal, Rare) | „das erste Mal pro Zug ziehen“ = jeden Zug 1 Energie | jeden zweiten Zug 1 Energie | Das normale Ziehen war das erste Ziehen; es war die Wirkung des Akt-IV-Bossrelikts |
| Brass Service Bell (Boss, Akt I) | jeder dritte Zug | jeder zweite Zug | Ein Bossrelikt soll nicht seltener läuten als ein Rare aus dem Normal-Pool |
| Crown of the Three Names (Boss, Akt IV) | +1 Energie pro Zug ohne Haken | dazu: jeder Gegner beginnt den Kampf mit 1 Strength | StS2 gibt Energie pro Zug nie ohne Preis |
| Candle Tribunal+ | 9 × 3 (ratifiziert 108) | 7 × 3 (ratifiziert 84) | Ausreißer über dem ganzen Pool |
| Hearth-Zutat (Hexe) | heilt 1 | heilt 2 | Ein Fünftel dessen, was jede andere Familie gibt |
| Hearth Physic (konzentriert) | heilt 4 | heilt 8, entfernt 2 negative Stapel | Schwächster Brew im Spiel |

Die Hearth-Prise (Upgrade-Facette) liest dieselbe Zahl und heilt damit ebenfalls 2.

### Text gegen Code

| Was | Befund | Jetzt |
|---|---|---|
| Binder's Awl | Text: „beim ersten Mischen“; Regel: beim ersten Ziehen des Kampfs (die Engine kennt keinen Misch-Auslöser) | Text sagt, was die Regel tut |
| Hedge Covenant | Text: „die Hälfte des verhinderten Schadens“; Regel: der ganze | doppelt so viel Block wie verhindert, Text passend |
| Kompendium | Beispiele mit alten Zahlen (Certified Kindling 4/4, Form of Ill Intent 3/4) | aktuelle Zahlen |

### Lesbarkeit

- **Form sichtbar:** Jede Karte mit dem Form-Etikett beginnt mit „Form.“ (automatisch in `BnbCard.RulesText`, wie „Exhaust.“ bei Riten). Neuer Kompendium-Eintrag „Form“.
- **Apprentice-Riten:** Copybook, The Borrowed Grimoire, Burning Glass, The Master's Hat, The Examiner's Bell und The Apprentice's Oath sind jetzt Rites. Relikte und Gegner, die Riten lesen, sehen sie.
- **Mimic-Zähne:** Der Relikttext nennt die Stufe (4 / 8 / 12 + 2 Heilung / 16 + 4 Heilung Block).
- **Relikt-Wörter:** Neuer Kompendium-Eintrag „Words on old relics“ (real card, affliction, blessing, guard).

### Einzelkarten

| Karte | Vorher | Jetzt |
|---|---|---|
| Seal Dividend+ | kostet 0, zieht 1 | kostet 1, zieht 2 |
| Pending Matters | 3 / 5 Block | 4 / 6 Block |
| Presumption of Error | 1 Energie; Upgrade kostet 0 | 0 Energie; Upgrade 2 Doubt |
| Wastepaper Bastion | +3 / +4 pro Junk auf der Hand | +2 / +3 pro Junk auf der Hand und im Ablagestapel |
| Dead Letter Office | 1 Paperwork pro Junk-Art im Exhaust | 2 pro Junk-Art in Ablage- und Exhaust-Stapel (Upgrade +2) |
| Final Draft | +1 pro Rote und Poise | +2 pro Rote und Poise |
| Lectern | 3 Rote | 3 Rote und 3 Poise |

### Tests

`SharpeningPassTests` (18), dazu je ein Test in `HeldEnergyTests` (Astrolabe), `HedgeWitchTests` (Hearth),
`ActFourBossRelicTests` (Crown). Jeder ist rot gegen den alten Stand. Suite: 1826 grün. `game.roguedeck.json` und
`ART_SLOTS.md` neu erzeugt. Hedge Covenant hat noch keinen eigenen Test (der Doubt-Treffer lässt sich im Probe-Kampf
schwer beobachten).

**Golden-Set:** nicht neu aufgenommen. Die Änderungen verschieben Läufe mit den betroffenen Karten und Relikten;
`bnb-godot/tools/golden.sh` nach `tools/sync-content.sh` zeigt, welche, und `--record` nimmt sie auf.

## Zweiter Durchgang: O1–O10 (umgesetzt 2026-10-21, vom Spieler freigegeben)

| # | Was | Umgesetzt |
|---|---|---|
| O1 | Allgemeine Karten in Belohnungen | Die Regel des Spielers (Chance pro Slot = allgemeine / alle Karten des Akt-Pools, nach jeder allgemeinen Karte halbiert) als ihr Durchschnitt: die allgemeinen Karten bekommen diesen Anteil jeder Ziehung (`ConversionPools.GeneralSlotShare`). Akt I ≈ 25 %, Akt IV ≈ 32 %, für alle drei Charaktere (vorher 14 % bzw. 45–60 %, die Hexe 60 %). Die Seltenheitskurve bleibt: eine allgemeine Karte ist nicht öfter Rare als jede andere (es gibt keine allgemeinen Commons; ihr Anteil geht an die Uncommons). |
| O2 | Allgemeiner Pool neutraler | Vier Censure-Karten sind neutrale Werkzeuge, auf ihren alten IDs: Second Wind (`sanctioned_charm`, 0 E: 1 Energie, 1 Karte, Exhaust), Blunt Instrument (`blacklisted`, 14 Schaden), Steady Routine (`countermanded_grace`, Rite: 3 Block jeden Zug), Fresh Ledger (`crossed_sigil`, eine Karte exhausten, 2 ziehen). Hexen-Familie und Apprentice-Stabhälfte je neu. Censure behält 5 Karten. |
| O3 | Shop-Relikte | Fünf allgemeine Shop-Relikte sind „Regel gegen Preis“, auf ihren alten IDs: Glass Inkwell (`copper_receipt_roll`, ±25 % Schaden), Iron Thimble (`witchmarket_purse`, −1 je Treffer und je Block), Stale Loaf (`merchant_punchcard`, −2 Energie im ersten Zug, danach +1 jeden Zug), Pawned Wedding Ring (`warranty_tag`, −7 Max HP, 2 Strength), Long-Form Ledger (`indemnity_stamp`, eine Karte mehr zur Wahl, Shop +10 %). |
| O4 | Regelverstärker Bürokrat | Vier seiner sechs Shop-Relikte: Wastebroker's Permit (Archivieren: 3 Schaden an alle), Priority Window Pass (Queue löst auf: 2 Paperwork an alle), Second Opinion (`departmental_purchase_order`, Doubt +1), Assent Ribbon (`archive_voucher_roll`, Ratify: 1 Doubt und 2 Paperwork). Notary's Waiver und Filing-Fee Stamp bleiben Ökonomie. |
| O5 | Einmal-pro-Kampf-Relikte der Hexe | Three-Knot Cord (jeder Ausbruch +1 Hexed), Found Button (jeder verlorene Wurf 4 Block), First Bell (jeder Gegner beim ersten Hex einen Schritt voraus), Black Cat's Collar (einmal pro Zug), Herb-Wife's Rack (jeder Brew mit Hearth heilt 2). |
| O6 | Last Office | 40 Schaden und 15 Block, je 8 und 3 weniger für jede verschiedene negative Status-Art auf dem Ziel, egal wessen. |
| O7 | Namen | Triplicate Injunction, Host's Compact, Slate Sponge, Paper Spill, Book Press, Candle Grease, Forfeit Bond. IDs unverändert. |
| O8 | Doppelungen | Thrice-Spoken Name: bricht der Hex nächste Runde aus, verliert das Ziel danach noch einmal HP in Höhe seines Hexed. Last-Breath Hex (und das Hidden Recipe Grave Hex): beim Tod erben ALLE anderen Gegner die Hälfte (aufgerundet) und den Zähler. |
| O9 | Riten-Upgrades | Mit Facette statt „Kosten −1“: Ash Register+ (zieht 2), Licensed Disposal+ (zieht 2), Continuance+ (bis 20 Block), Hedge Covenant+ (dreifach), dazu Seal Dividend+ aus dem ersten Durchgang. |
| O10 | Apprentice-Text | Fünf längste Stab-Texte gekürzt, Regeln unverändert: Glass Rod, The Fourfold Lesson, The Master's Last Word, Book Press und Warding Primer (Shield Seal), Blotting Sand. |

### Fehler, die beim Umsetzen gefunden und behoben wurden

- **Black Tribunal und Compound Indictment** zählten Hexed, Misfortune und Singe nicht: die Liste der negativen Status (`CardAuthoring.NegativeStatuses`) kannte nur Bürokraten- und allgemeine Status. Compound Indictment verstärkte außerdem nur die ersten fünf Einträge der Liste.

Ein Fehlalarm, zur Vorsicht festgehalten: Energie von 0-Kosten-Karten geht bei vollem Pool **nicht** verloren (seit 2026-09-28, `AuditTests`). Der Probe-Kampf der Tests (`FightProbe.Solo(…, energy: N)`) kappt dagegen am Maximum; ein Test, der Energie misst, gibt dort erst eine aus.

### Tests

`SharpeningPassTests` enthält jetzt 34 Fälle (Belohnungsanteil je Akt und Charakter, Last Office, Black Tribunal, Continuance+, die vier neuen Karten, sieben der neun Shop-Relikte), dazu angepasste Tests für Last-Breath Hex, Black Cat's Collar, Long-Form Ledger und die Belohnungskurve. Suite: 1841 grün.

Ohne eigenen Test: Thrice-Spoken Names zweiter Verlust, Ash Register+ und Licensed Disposal+ (ziehen 2), Hedge Covenant (×2 / ×3), Wastebroker's Permit, First Bell pro Gegner.

### Nicht gemacht

- **Family-Bench** (die Zahlen sind Entwürfe) und **Golden-Set** (verschiebt sich durch Karten, Relikte und den Belohnungsanteil; `golden.sh --record` nach `sync-content.sh`).
- **Design-Kanon** in `source-data/design/*.md` nennt noch die alten Namen und Texte.
- **Bilder:** sieben der neuen Shop-Relikte haben keinen Bild-Brief (`ART_SLOTS.md`: 43 statt 36 ohne Brief).

## Ursprünglich offen (jetzt umgesetzt, siehe oben)

| # | Was | Vorschlag |
|---|---|---|
| O1 | Allgemeine Karten in Belohnungen | „höchstens eine von drei“: Angebote wie bisher ziehen, eine zweite oder dritte allgemeine Karte durch eine Charakterkarte ersetzen. Akt I bleibt bei ~12 %, Akt IV fällt von 45–60 % auf ~30 % |
| O2 | Allgemeiner Pool neutraler | je Charakter ein Haken in einen der vier Stränge (Lien, Citation, Censure, Blood Ink), oder einige Karten durch neutrale Werkzeuge ersetzen (Ziehen, Energie, Retain, Exhaust) |
| O3 | Shop-Relikte | Ökonomie-Relikte durch „Regel gegen Preis“ ersetzen (Vorbild StS2: Bread, Miniature Tent, Ringing Triangle). Bürokrat: 4 von 6, allgemein: 6–8 von 18 |
| O4 | Regelverstärker für den Bürokraten | z. B. „Ratified verdreifacht statt verdoppelt“, „die älteste Queue-Karte löst zweimal aus“, „Paperwork löst auch zu Beginn deines Zugs aus“ |
| O5 | Einmal-pro-Kampf-Relikte der Hexe | Three-Knot Cord, Found Button, First Bell, Black Cat's Collar, Herb-Wife's Rack: pro Zug oder deutlich größer |
| O6 | Last Office | neutral machen; mögliche Lesart „jede Status-Art, die du in diesem Kampf gesetzt hast und die das Ziel nicht trägt“ (braucht eine Zählung pro Kampf) |
| O7 | Namen | Threefold Injunction → Triplicate Injunction · Hearth Compact → Host's Compact · Moss Poultice → Slate Sponge · Kindling (Apprentice) → Paper Spill · Seal Press → Book Press · Sealing Wax (Apprentice) → Candle Grease · Forfeit Seal → Forfeit Bond |
| O8 | Doppelungen gleicher Rolle | Thrice-Spoken Name eigene Wendung (zweiter Ausbruch), Last-Breath Hex auf alle anderen Gegner verteilen. Nach dem StS2-Vergleich niedrige Priorität |
| O9 | Riten-Upgrades des Bürokraten | weitere 5–8 Riten mit Facette statt „Kosten −1“ |
| O10 | Apprentice-Text | Stab-Hälften auf eine Regel kürzen (Vorbild Regent in StS2: zweite Ressource, trotzdem ~10 Wörter) |
