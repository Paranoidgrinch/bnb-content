# The Lieutenant — Umsetzungsplan (vierter spielbarer Charakter)

Kanon: `source-data/design/lieutenant_master.md` (Design v1.0 vom Spieler, 2026-10-22). Wie bei Hexe und
Apprentice: **Mechaniken, Namen und Sonderregeln sind Kanon, Zahlen sind Balance-Entwurf (BD).** Auftrag
(2026-10-22): „mach dich damit vertraut und bau ihn ins spiel ein. die upgradevorgaben die wir bisher verwendet
haben sollten die aus dem design dokument übertrumpfen“ — in kleinen Commits.

---

## 0 Entscheidungen (getroffen, nicht offen)

| # | Frage | Entscheidung |
|---|---|---|
| L1 | Max-HP | 80 wie im Kanon (§0), als Zahl im Charakter (`LieutenantCharacter.MaxHp`); die anderen haben 70, der Bench entscheidet |
| L2 | Akt-Verteilung | wie Hexe und Apprentice, nicht wie Kanon §1 („keine Akt-Sperre“): Belohnungen sind nach Akt **und** Seltenheitskurve gezogen (vom Spieler bestätigt). Commons Akt I; Uncommons 23 × Akt I / 12 × Akt II; Rares 8 / 6 / 6 / 5 über Akt I–IV. Was Kanon §1 Akt II–IV zuschreibt (Zahlungen, Motoren), kommt später |
| L3 | Upgrades | **die bisherigen Regeln, nicht die des Kanons** — siehe §1 |
| L4 | Authority | Status `lt_authority` am Helden, 0–6 (Gewinn wird an der Kappe abgeschnitten, nie negativ), beginnt jeden Kampf bei 0 |
| L5 | Open Guard | Status `lt_open_guard` am Helden, eigene Art (kein Vulnerable, nicht in `NegativeStatuses`, nicht reinigbar): der erste echte gegnerische Angriffstreffer bekommt +2 je Stapel **vor Block**, dann sind alle Stapel weg; ohne Treffer verfallen sie zu Beginn des nächsten eigenen Zugs. Zusätzlicher HP-Verlust durch Open Guard = min(2 × Stapel, HP-Verlust des Treffers) — bei reinem Block genau die Kanon-Definition (§2.5) |
| L6 | Orders | je Order-Art ein Status am Helden mit Tag `lt_order`; Fortschritt in Kampfzählern. Limit 1 (2 mit Chain of Command). Eine neue Order am Limit **ersetzt die älteste** (der Kanon lässt wählen; eine Wahl bei jedem Ausstellen ist zu viel Klickerei für zwei Plätze). Ersetzt/aufgegeben ≠ gescheitert |
| L7 | Challenges | Status am gewählten Gegner mit Tag `lt_challenge`; höchstens eine im ganzen Kampf, eine neue ersetzt die alte ohne Erfolg/Fehlschlag. „Bevor er handelt“ = vor seiner nächsten Aktion (`ActionStarting`). Karten, die „einen Gegner, der angreifen will“ wählen, nehmen das Kartenziel; will es nicht angreifen, gibt es keine Challenge (der Rest der Karte wirkt) |
| L8 | Deed-Treffer | ein direkter Treffer, dessen Quellkarte das Tag `deed` trägt (`TriggerEventSourceCardHasTag`), egal ob Block ihn schluckt |
| L9 | Strength-Zahlungen | „You may spend N Strength“ = Wahl, nur angeboten, wenn genug **positive** Strength da ist. Armsmaster-Rabatt: erklärt ≥ 2 → tatsächlich 1 weniger. Was „tatsächlich gezahlt“ liest (Sword Knot, Hilt Knot, Full Tang, Whetstone), liest einen Kampfzähler, nicht die erklärte Zahl |
| L10 | Authority-Zahlungen | ebenso als Wahl, nur wenn die effektive Kosten gedeckt sind; Brevet senkt die nächste um 1. Eine Zahlung von 0 ist keine Zahlung (Rising Star, Sword Knot) |
| L11 | Ereignisse | Erfolg, Fehlschlag, Zahlungen werden mit `announceRule` angesagt (`lt_order_fulfilled`, `lt_order_failed`, `lt_challenge_fulfilled`, `lt_challenge_lost`, `lt_strength_paid`, `lt_authority_paid`); Riten und Relikte hören darauf. Jede Verpflichtung endet genau einmal |
| L12 | General-Pool | bleibt unverändert (Kanon §6); die Lieutenant braucht keine Rolle je General-Karte (keine Zutat, kein Stab-Teil) |
| L13 | Spielbar ab Start | ja, im Neuer-Lauf-Panel wie die anderen drei |
| L14 | Mehrspieler | Belohnungen einer Challenge gehen an den Helden, der sie gestellt hat (er trägt den Charakter-Marker); Treffer anderer Helden zählen nicht |
| L15 | Engine | Core nur, wo ein Baustein fehlt; alles Lieutenant-Eigene bleibt Inhalt (`Converter/Lieutenant/`) |

## 1 Upgrade-Regeln (gelten statt der Kanon-Upgrades)

Die Kanon-Upgrades sind fast alle „Hauptzahl +2 bis +3“ (≈ +30 %) und bei Riten „kostet 0“. Es gelten die Regeln
aus F (bnb-godot `PLAYTEST_FEEDBACK_2_PLAN.md`, freigegeben 2026-10-02), W8 (Hexe) und O9 (Nachschärfen):

- **U1** Upgrade ≈ ×1,5 Gesamtwert.
- **U2** Mindestens 40 % (Ziel: die Hälfte) der Upgrades bekommen eine Facette aus dem Katalog: A Breite · B Mehrfach ·
  C Bedingung · D Querverbindung · E Nachwirkung · F Umwandlung · G Sofort · H Kosten −1 · I Retain ·
  K Beides statt Wahl · N Nachteil weg · S Schwelle · T Zielwahl. Neu für die Lieutenant: **O Order-Facette** — der
  Erfolg einer Order/Challenge gibt zusätzlich etwas (wie die Prise der Hexe).
- **U3** Starter: die Basis bleibt, nur das Upgrade wird stärker.
- **U4** Ziehen nur bei Karten, deren Identität Ziehen ist.
- **U5** Riten: Facette statt „kostet 1 weniger“ (O9; die Kanon-Upgrades „Cost 0“ von Under the Colours, Scar,
  Broken Nose fallen weg).
- **U6** 2-Kosten-Karten liefern ≥ 15 % mehr pro Energie als der 1-Kosten-Median.
- Exhaust, Kanon-Leitplanken und Texte bleiben, wo der Kanon sie ausdrücklich festhält (Field Pardon+ kostet weiter 3
  Authority; Forced March+ hat keine Schuld).

Die Upgrade-Tabelle jeder Karte steht in §4, je Block beim Bauen nachgetragen.

## 2 Architektur

**Inhalt (bnb-content), `Converter/Lieutenant/`:**
- `LieutenantCharacter` — Start (80 HP, 10 Karten), Roster, Charakter-Marker.
- `LieutenantKeywords` — Authority, Open Guard, die Zähler, die Ansagen.
- `Obligations` — die Bausteine für Orders und Challenges (ausstellen, ersetzen, Erfolg, Fehlschlag, Fristen).
- `LieutenantCards` (+ `.Common`, `.Uncommon`, `.Rare`) — 84 Karten mit Upgrades.
- `LieutenantRules*` — Status für alles, was die Karte überdauert (Orders, Challenges, Riten, Nachwirkungen).
- `LieutenantRelics` — 18 Relikte (3 / 5 / 4 / 6 Shop).
- Pools (`ConversionPools`, `Eligibility.Lieutenant`), Shop, Händler-Events, Archiv-Abschnitt, Kompendium, ART_SLOTS.

**Godot (bnb-godot):** Charakter erscheint aus dem Roster; Orders, Challenges, Authority und Open Guard sind
Status-Chips mit Text. Golden-Set bekommt Lieutenant-Läufe, `golden.sh` und `bench.sh` kennen sie.

## 3 Schritte (je ein Commit)

| # | Schritt | Test |
|---|---|---|
| S1 | Plan + Kanon | — |
| S2 | Charakter, Authority, Open Guard, Starter, Roster | Start (LT-001), Authority-Kappe, Open Guard erster Treffer / Verfall / Mehrfachtreffer (LT-027/028) |
| S3 | Orders und Challenges + First Orders, New Stripes | LT-003, LT-004, Quelle zählt nicht, Frist |
| S4 | 20 Commons | je Familie eine Regel |
| S5 | 35 Uncommons (zwei Commits) | |
| S6 | 25 Rares (zwei Commits) | Leitplanken aus Kanon §2.7 |
| S7 | 18 Relikte | |
| S8 | Pools, Shop, Archiv, Kompendium, ART_SLOTS, `game.roguedeck.json` | LT-002, Isolation (LT-046), Ganzlauf |
| S9 | bnb-godot: Golden-Läufe, Bench-Zeile | |

## 4 Upgrade-Tabelle

(wird je Block nachgetragen)

## 5 Stand

| Schritt | Stand |
|---|---|
| S1 Plan | ✔ |
