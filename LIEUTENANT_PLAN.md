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
| L14 | Mehrspieler | Nur Treffer und Karten der Lieutenant zählen für ihre Versprechen; Belohnungen gehen an sie. Spielen zwei Lieutenants am selben Tisch, liest jede Regel die erste von beiden (gebaut als `FirstTarget`) — bewusst einfach, bis Mehrspieler das braucht |
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

Kanon-Upgrade → gebautes Upgrade; Facette nach §1 (— = nur ×1,5).

**Starter** (Basis wie Kanon)

| Karte | Kanon + | gebaut + | Facette |
|---|---|---|---|
| Watch Cut | 8 Schaden, +2 Challenge | 9 Schaden, +3 Challenge | — |
| Buckler Up | 7 Block, +2 Open Guard | 8 Block, +3 Open Guard | — |
| First Orders | 7 Block | 6 Block; Erfolg auch 3 Block | O |
| New Stripes | +4 Block | +4 Block | G |

**Commons**

| Karte | Kanon + | gebaut + | Facette |
|---|---|---|---|
| Pike Butt | 5 × 2 | 4 × 3 | B |
| Cut the Strap | 10 Block weg, 8 Schaden | 12 Block weg, 9 Schaden | — |
| Heel Turn | 4 / 4 × 2 | Retain, 4 / 4 × 2 | I |
| Clear the Square | 11 an alle | 12 an alle | — |
| Clenched Jaw | 9 Block | 9 Block | — |
| Buckle | 5 Block | 6 Block | — |
| Roll Call | zieh 2, wirf 1 ab | zieh 2, wirf 1 ab | (Ziehen ist ihre Identität) |
| Gatehouse | 11 Block | 12 Block | — |
| Last Bell | 10 Schaden | 10 Schaden; Erfolg auch 4 Block | O |
| Scabbard | 6 Block | 6 Block | — |
| Marked Man | 7 Schaden | 8 Schaden | — |
| Braced Pike | 10 Block | 11 Block | — |
| Arrest | 9 Schaden | 9 Schaden | — |
| Witness | 6 Block | 6 Block | — |
| Signal | + zieh 1 beim Zahlen | Retain, 3 Block | I (U4: kein Ziehen) |
| Second Rank | 9 Block | 9 Block | — |
| Whetstone | 2 Strength | 1 Strength; nach einer Strength-Zahlung +1 und zieh 1 | C (2 Strength wäre ×2) |
| Armguard | 10 Block, 5 Ward Wax | 12 Block, 4 Ward Wax | — |
| Headlong | nur 1 Open Guard | nur 1 Open Guard | N |
| Split Lip | 10 Schaden | 11 Schaden | — |

**Uncommons** (Akt II: Dispatch, Make an Example, Ultimatum, Remand, Stand Down, Overhand, Heave, Grip, Broken Pike,
Recoil, Poise, Overreach; der Rest Akt I)

| Karte | Kanon + | gebaut + | Facette |
|---|---|---|---|
| Against the Wall | 10 Block | 11 Block | — |
| Double File | zieh 2, wirf 1 ab | zieh 1, 6 Block | — (U4) |
| Mind the Gap | 11 Schaden | 12 Schaden | — |
| Cadence | zieh 2, wirf 1 ab | zieh 2, wirf 1 ab | (Ziehen ist ihre Identität) |
| No Excuses | 11 Schaden | 12 Schaden | — |
| No Surrender | 8 Block | 8 Block | — |
| Gauntlet | 10 Schaden | 11 Schaden | — |
| On Your Feet | 11 Block | 12 Block | — |
| Pursuit | zieh 2, wirf 1 ab | zieh 2, wirf 1 ab | (Ziehen ist ihre Identität) |
| No Running | 12 Schaden | 13 Schaden | — |
| Dare | 9 Block | 9 Block | — |
| Second Blade | 6 × 2 | 7 × 2 | — |
| Testimony | 9 Block | 9 Block | — |
| Whistle | 5 Block | 5 Block | — |
| Dispatch | 9 Block | 9 Block | — |
| Make an Example | 18 Schaden | 20 Schaden | — |
| Under the Colours (Rite) | kostet 0 | Erfolg gibt auch 3 Block | D (U5) |
| Flank | 12 / 9 | 13 / 10 | — |
| Ultimatum | 11 Schaden | 12 Schaden | — |
| Remand | 10 Block | 11 Block | — |
| Stand Down | 6 Block | 6 Block | — |
| Arms Drill | 5 pro Treffer | 6 pro Treffer | — |
| Overhand | 18 Schaden | 20 Schaden | — |
| Heave | 10 pro Treffer | 12 pro Treffer | — |
| Grip | 5 pro Strength, 3 ohne | 6 pro Strength, 3 ohne | — |
| Shield Punch | 8 Schaden, 5 + Strength | 8 Schaden, 6 + Strength | — |
| Scar (Rite) | kostet 0 | sofort 1 Strength | G (U5) |
| Broken Pike | 16 / 9 | 18 / 10 | — |
| Front Rank | 14 Schaden | 16 Schaden | — |
| Recoil | 11 Block | 12 Block | — |
| Ruckus | 4 an alle | 5 an alle | — |
| Poise | 13 Block | 15 Block | — |
| Overreach (Rite) | +6 | +6 | — |
| Bloody Cobbles | 9 pro Treffer | 10 pro Treffer | — |
| Broken Nose (Rite) | kostet 0 | sofort 1 Authority | G (U5) |

Abweichungen vom Kanon beim Bauen: **Dispatch** bietet alles außer Workings aus dem Ablagestapel an (die Kartenwahl
kennt nur ein Ausschluss-Tag; ihre Deeds sind genau das). **Poise**: „bis deine nächste Deed aufgelöst ist, bekommst du
kein Open Guard“ (die Engine hört eine Karte mitten im Programm). **Flank**: der andere Gegner ist zufällig.
**Make an Example / Broken Pike**: die Streuung ist Schaden ohne Strength (Status-Schaden), Block hält ihn auf.

**Rares** (Akt I: Brevet, Insubordination, Parade Ground, Drum Major, Service Record, Splintered Shield, Last Ditch,
Open Visor · Akt II: Black Gauntlet, Redress, Riposte, Countercharge, Forced March, Reprimand · Akt III: Chain of
Command, Succession, Hilt Knot, Pike Square, Rising Star, Watchword · Akt IV: Armsmaster, Full Tilt, Blood Debt,
Watch Surgeon, Field Pardon)

| Karte | Kanon + | gebaut + | Facette |
|---|---|---|---|
| Chain of Command (Rite) | 10 Block, zieh 3 | 10 Block, zieh 3 | — |
| Brevet | 9 Block, zieh 1 | 2 Authority, 8 Block | — (U4) |
| Insubordination | 12 pro Treffer | 15 pro Treffer | — |
| Parade Ground | 12 Block, 6 Ward Wax pro Energie | 15 Block, 5 pro Energie | — |
| Drum Major | 9 Block | 10 Block | — |
| Black Gauntlet | 14 Schaden | 16 Schaden | — |
| Succession (Rite) | 2 Strength | 2 Strength | G |
| Service Record | 10 Basis; Kill: 1 Authority | 12 Basis | — |
| Redress | 13 Block, zieh 1 beim Verlegen | 15 Block | — (U4) |
| Riposte (Rite) | 8 | 8 | — |
| Countercharge | 12 Schaden, bis 3 Strength | 12 Schaden, bis 3 Strength | S |
| Hilt Knot (Rite) | zieh 2 wirf 1, 7 Block | zieh 1, 8 Block | — |
| Forced March | 4 / 4 | 4 / 4 | — |
| Pike Square | 15 Block, 4 + 3 × Strength | 15 Block, 4 + 3 × Strength | — |
| Armsmaster (Rite) | 2 Strength | 2 Strength | G |
| Full Tilt | 11 pro Treffer | 13 pro Treffer | — |
| Blood Debt | 9 Block, bis 24 | 9 Block, bis 24 | — |
| Open Visor | 10 Block, alle Treffer | 11 Block | — (siehe unten) |
| Splintered Shield | danach 4 Block | 16 Block, danach 4 Block | G |
| Last Ditch | 13 Basis, bis 16 | 15 Basis, bis 16 | — |
| Reprimand | 12 Block, 3 Strength | 13 Block, 3 Strength | — |
| Rising Star (Rite) | 2 Authority | 2 Authority | G |
| Watchword | 8 Block, 2 Authority | 8 Block, 2 Authority | — |
| Watch Surgeon (Rite) | heilt 3 | heilt 3 | — |
| Field Pardon | 13 Block, zieh 1 | 15 Block | — (U4) |

Abweichungen beim Bauen:
- **Open Visor**: schon die Basis lässt alle Treffer der nächsten Deed Block ignorieren (der Block wird zurückgegeben,
  der geschluckte Schaden geht durch). Zwei Treffer einer Karte werden nebeneinander gehört; „nur der erste“ ist nicht
  sauber zu trennen. Dafür braucht es den neuen Core-Ausdruck `eventBlockedAmount`.
- **Pike Square**: der Schutz wird beim Ausspielen aus der Strength berechnet; er gilt für den ersten Treffer jeder
  Gegner-Aktion (≈ „jeder Angreifer“), die Vergeltung ist `min(Schutz, 12)`.
- **Watch Surgeon**: kein „you may“ — zahlt und heilt, wenn sie verletzt ist und Authority hat.
- **Redress**: verlegt auf den Gegner, auf den die Karte zielt (statt eine Liste anzubieten).
- **Insubordination**: gibt die älteste Order auf.
- **Chain of Command / Watchword / Succession**: ihr Ziehen „nächster Zug“ geschieht, wenn die nächste Hand gegeben
  wird (wie Magpie's Luck der Hexe).

**Relikte** — Abweichungen vom Kanon §4 beim Bauen:
- **Torn Standard**: kein „you may“ und keine Wahl des Gegners — gelegt nur, wenn keine Challenge offen ist (so
  verdrängt es nie eine), auf den schwächsten Gegner.
- **Full Tang**: trifft den schwächsten Gegner.
- **Lead Knuckles**: die 8 kommen als eigener Schaden ohne Strength auf das Ziel der Deed, wenn ihr Spielen gehört
  wird (die Deed kann da schon getroffen haben).
- **Scarlet Epaulette**: die nächste Deed (nicht nur Einzelziel) schlägt beim Schließen ihrer Aktion noch einmal zu,
  für die Hälfte ihres Treffers ohne Strength, plus einmal Strength. Zählt nicht als Deed-Treffer.
- **Commission / Brandy Flask**: über die Brücke Kampf → Lauf (`CombatResolvedRunEvent.Counters`); die Reserve ist ein
  Lauf-Zähler, der nächste Kampf bekommt sie als Eröffnung.

### 4b Facetten-Durchgang (2026-10-23) — ersetzt die Zeilen oben, wo er sie nennt

Gemessen wie bei Hexe und Apprentice (`UpgradeFacetShareTests`: Text ohne Zahlen, Kosten oder Retain ändern sich):
vorher 14 von 84, jetzt **37 von 84 (44 %)**; dazu sieben Schwellen-Facetten (S), die die Messung nicht sieht.

| Karte | neu + | Facette |
|---|---|---|
| Clear the Square | 11 an alle, nur 1 Open Guard | N |
| Gatehouse | 10 Block; Erfolg auch 1 Karte mehr im nächsten Zug | O |
| Scabbard | Retain, 5 Block | I |
| Marked Man | 7 Schaden; Erfolg auch 1 Strength | O |
| Arrest | 8 Schaden; Erfolg auch 1 Strength | O |
| Witness | 5 Block; die nächsten zwei Treffer zählen doppelt | B |
| Split Lip | 10 Schaden; Kill: zieh 1 und 1 Energie | C |
| Headlong | kein Open Guard mehr | N |
| Against the Wall | 10 Block; Erfolg auch 3 Ward Wax | O |
| Mind the Gap | 11 Schaden; 6 statt 8 Block | S |
| No Excuses | 11 Schaden; Erfolg auch zieh 1 | O |
| No Surrender | 7 Block; ohne Open Guard | N |
| Gauntlet | 10 Schaden; ein Treffer von 10 statt 12 | S |
| On Your Feet | 10 Block; Erfolg auch 1 Strength | O |
| Dare | 8 Block; Erfolg auch 1 Karte mehr im nächsten Zug | O |
| Second Blade | 5 × 3 | B |
| Testimony | 8 Block; Erfolg auch 1 Strength | O |
| Make an Example | 18 Schaden; Streuung ohne Authority | N |
| Flank | 12 / 8, ohne Authority | N |
| Ultimatum | 11 Schaden; 2 Doubt für 1 Authority | S |
| Arms Drill | 5 × 3, ohne Strength | N |
| Overhand | 18 Schaden; ignoriert Block ohne Strength-Bedingung | C |
| Heave | 10 / 10; zweiter Treffer für 1 Strength | S |
| Broken Pike | 16 / 9; Streuung für 1 Strength | S |
| Countercharge | 11 Schaden; bis 2 Strength, je auch 3 Block | K |
| Parade Ground | 12 Block; bis 3 Energie | S |
| Drum Major | 9 Block; die Energie kommt mit einer Karte | D |
| Service Record | 10 Basis; Kill: 1 Authority | C |
| Brevet | Retain, 1 Authority, 8 Block | I |

### 4c Balance-Durchgang (2026-10-23)

Erster Family-Bench (`lieutenant_decks.py` → `LIEUTENANT_DECKS.json`, 10 Decks je Akt, `--family-acts 2,3
--family-relics only --bench-shuffles 2`, 70 HP). Ergebnisse in `~/Desktop/bnb-balance/20261023-lieutenant-family-v*.txt`.

- **Open Guard bleibt bewusst strafend**: der erste gegnerische Treffer einer Aktion macht 25 % mehr je Stack
  (abgerundet, mindestens 1 je Stack), vor Block (Core: `PassiveModifierOperation.AddPercentPerStack`).
- **Zahlen**: die meisten Commons/Uncommons um 1–3 Schaden oder Block hoch, Upgrades mit; Broken Nose 1 Authority +
  2 Strength, Scar 2 Strength, Bloody Cobbles 9, Last Ditch 12, Shield Punch 7.
- **Jede Challenge zahlt 1 Strength mehr** als der Kanon; vier Plus-Specs mit eigener Zusatzbelohnung, damit die
  Facetten-Quote hält. Orders unverändert.
- **Bench-Decks** für Aggro mit mehr Block gebaut (Watch Cuts und First Orders raus, Dare, Clenched Jaw, Armguard rein).

| Stand v5 | Akt II | Akt III |
|---|---|---|
| By the Book | 95 % | 91 % |
| Shield & Steel | 96 % | 73 % (Bosse ins Zug-Limit) |
| Strength Spender | 81 % | 44 % |
| Duelist | 88 % | 36 % |
| Bloodied Veteran | 77 % | 32 % |
| Front Rank / Common Heavy / Bruiser | 79–90 % | 28–29 % |

Ziel wäre Akt II ~89–94 %, Akt III Median ~42–44 %. Die Aggro-Decks verlieren an den Akt-III-Bossen (0–1/10):
gleich schnell wie Paperwork, aber 7–8× so viel verlorenes Leben. Vorerst so gelassen (Entscheidung 2026-10-23).

## 5 Stand

| Schritt | Stand |
|---|---|
| S1 Plan | ✔ |
| S2/S3 Start, Authority, Open Guard, Orders, Challenges | ✔ `LieutenantTests` (11) |
| S4 Commons | ✔ `LieutenantCardTests` (15) |
| S5 Uncommons | ✔ `LieutenantUncommonTests` (24) |
| S6 Rares | ✔ `LieutenantRareTests` (27); Core: `eventBlockedAmount` (RogueDeck-Core a703df1) |
| S7 Relikte | ✔ `LieutenantRelicTests` (19) |
| S8 Pools, Shop, Märkte, Archiv, Kompendium, ART_SLOTS | ✔ `LieutenantPoolTests` (7), Archiv-Test, `LieutenantRunTests` (Bot, 2 Läufe) |
| Facetten-Durchgang | ✔ 37/84 (`UpgradeFacetShareTests`, `LieutenantUpgradeTests` 8) |
| S9 bnb-godot | ✔ `golden.sh` (4 + 2 Lieutenant-Läufe), `bench.sh`, `smoke-mp.sh` (vierter Platz); `--smoke-screens --character=lieutenant`: 56 Bildschirme, 0 Fehler |

## 6 Was beim Bauen über die Engine gelernt wurde

- **Die Ereignisse einer Aktion werden nebeneinander gehört.** Zwei Treffer einer Karte sehen beide „Zahl erreicht“;
  eine Beurteilung neben dem Zähler liest ihn, bevor er wächst. Darum zählen Treffer nur, und beurteilt wird, wenn die
  Aktion schließt (`ActionResolved`). `ClaimOnceThisAction` hilft in Treffer-Triggern nicht (dort ist kein Scope offen).
- **Eine Karte wird mitten im Programm gehört**: jeder kausale Schritt lässt die Ereignisse davor durch, auch ihr
  eigenes `CardPlayed`. Regeln „bis zur nächsten Deed“ enden darum beim Schließen ihrer Aktion.
- **Eine fehlschlagende Bedingung kostet einen Zyklus.** Ein Status, der jede Status-Anwendung hört und 35 Fragen
  stellt, sprengte im Akt IV das Limit von 1024 Zyklen. Fragen als eine balancierte Wenn-sonst-Kette (`N.FirstOf`).
- **Das Spieldokument ist JSON mit Tiefe 64**: lange Ketten balanciert bauen (`N.AnyOf`).
- **Ein Status hört seine eigene Anwendung nicht**; was eine Karte beim Ausstellen tun muss, tut die Karte selbst
  (Zähler zurücksetzen, Hand bepreisen).

## 7 Offen

- **Golden-Set** für bnb-godot noch nicht aufgenommen (nach U2 aufnehmen; Skripte sind vorbereitet, nicht committet).

- **Balance**: Aggro-Decks im Akt III zu schwach gegen Bosse (§4c). Nächster Schritt wäre Sustain für Aggro
  (Block auf Angriffen unter Open Guard, kleine Heilung auf Bloody Cobbles/Scar), nicht weniger Open Guard.
- **Bilder**: keine Platzhalter für ihre Karten, Relikte und Figur (wie beim Apprentice); ART_SLOTS listet sie.
- **Tutorial**: keins für sie (wie beim Apprentice).
