# Sorcerer's Apprentice — Umsetzungsplan (dritter spielbarer Charakter)

Kanon: `source-data/design/sorcerers_apprentice_master.md` (Design v0.4, vom Spieler 2026-10-20). Wie bei der
Hexe: **Mechaniken, Reihenfolgen und Sonderregeln sind Kanon, Zahlen sind Balance-Entwurf (BD).** Auftrag
(2026-10-20): „bringe ihn als Charakter inklusive Karten, Relikte, Mechaniken und Zauberstab vollständig ins
Spiel“, in kleinen Commits.

---

## 0 Entscheidungen (getroffen, nicht offen)

| # | Frage | Entscheidung |
|---|---|---|
| A1 | Max-HP | 70, als Zahl im Charakter (`ApprenticeCharacter.MaxHp`), BD wie alles andere |
| A2 | Akt-Verteilung | alle Commons/Uncommons ab Akt I; Rares ab Akt I (Master §1: keine Akt-Sperre wegen Seltenheit); eigene `for:sorcerers_apprentice`-Pools wie bei der Hexe |
| A3 | Spielbar ab Start? | ja, im Neuer-Lauf-Panel wie die Hexe |
| A4 | Fokus/Erschöpfung | eigener Zauberstab-Zustand pro Kämpfer in der Engine (nicht Energie, nicht Zähler), im Snapshot und Hash |
| A5 | Platzwahl beim Einschreiben | ein freier Platz → kein Dialog; mehrere → Auswahl „Platz n“ |
| A6 | Crosswind (zwei gewählte Gegner) | das Ziel des Casts und der nächste lebende Gegner (niedrigste HP), da ein Cast ein Ziel hat |
| A7 | Split Wick (Spielen) | der erste Treffer der nächsten Schadenskarte wird zu 2 × 60 % (statt „größter Treffer“) |
| A8 | Vorschau | Struktur (Reihenfolge, Fizzle, Fokus, Erschöpfung) rechnet die Engine rein; Zahlen zeigt die Oberfläche aus einem Probelauf auf einer Kopie |
| A9 | Erschöpfung mit bedingtem Ground | Legalität rechnet bedingte Grounds gegen den Zustand vor dem Cast; landet der Cast real höher, wird auf das Limit gekappt |
| A10 | General-Pool-Proxys | als Zauberstab-Teil der 50 General-Karten (Kampfwirkung beim Spielen unverändert, für andere Charaktere wirkungslos) |

## 1 Architektur

**Engine (RogueDeck-Core), `Combat/Wand/`:**
- `CardZone.Wand` — Karten im Stab sind in keiner anderen Zone; Platz = `WandState.Slots`.
- `WandState` je Kämpfer: Plätze (4…6), Fokus (0…6), Erschöpfung/Limit (8…12), Zähler je Zug/Kampf,
  gespeicherter Zauber (ein Puffer), Asche-Spur, Ground-Gutschrift, Element-Spur der Runde. Snapshot, Hash,
  Restore.
- `WandRules` (Inhalt liefert sie mit dem Lauf): Zahlen, Element-Status-IDs (Singe/Flow/Grit/Gust), welche
  Status welche Relikt-Regel schalten.
- `WandImprint` an jeder Kartendefinition (`CardData.Wand`): eine der zehn Operationen + Parameter; ein Speak
  besteht aus Atomen (Schaden/Block/Ward Wax/Status/Programm) mit Ausdrücken für die Zahl.
- Compiler (rein): Ausführungsreihenfolge, Bindung der Modifikatoren, Fizzle, Fokuskosten, Erschöpfung,
  Gründe für „nicht erlaubt“.
- Interpreter (`castWand`-Knoten): zahlt, führt Schritt für Schritt aus (jeder Speak über die normalen
  Schaden/Block/Status-Wege, also mit allen Relikt-/Gegnerreaktionen), danach Burn, Flow, Bewegungen.
- Knoten für die fünf Aktionen (Inscribe, Cast, Swap, Withdraw, Burn Out) und für Kartentexte
  (Fokus, Plätze, Limit, freie Tausche …); Ausdrücke `wandValue(...)`.
- Singe/Grit als Treffer-Modifikatoren, Flow im Cast, Gust nach dem Zugziehen.

**Inhalt (bnb-content), `Converter/Apprentice/`:** Charakter, Aktionen, Status, 42 Karten (+ Upgrades),
9 Relikte, 50 Proxys, Pools, Kompendium.

**Godot (bnb-godot):** Stab-Leiste (Plätze, Lücken, Fokus- und Erschöpfungsanzeige, Vorschau, Gründe),
Aktionsknöpfe, Karten mit PLAY/WAND-Text, Smoke.

## 2 Status

| Schritt | Stand |
|---|---|
| Plan | ✔ |
| Engine: Stab-Zustand, Imprint, Regeln, Compiler, Cast, Aktionen, Singe/Grit/Flow/Gust, Haken, temporäre Kopien, Bot | ✔ (RogueDeck-Core, 23 Stab-Tests) |
| Inhalt: Charakter, 42 Karten, 9 Relikte, 50 Proxys, Pools, Archiv, Kompendium | ✔ (Karten-, Relikt-, Proxy-, Pool- und Ganzlauf-Tests) |
| Godot: Stab-Leiste, Aktionsknöpfe mit Fokus-Kosten, Cast mit Zielwahl und Vorschau, PLAY/WAND-Kartentext, Smoke, Golden | ✔ (bnb-godot: `--smoke-wand`; `--smoke-screens --character=sorcerers_apprentice` Akt I–V, 57 Bildschirme, 0 Fehler; Golden mit 6 Apprentice-Läufen; Multiplayer-Smoke mit ihm am dritten Platz, Stab im Einsatz) |
| Balance-Bench (Master §16 Stufe 6) | begonnen: `bnb-godot/tools/bench.sh` (alle Charaktere, gleiche Seeds, `sim-bench`-Zeile aus `roguedeck-bot`); erste Messung siehe §3 |

## 3 Bench, erste Messung (2026-10-20)

`tools/bench.sh --runs 100 --immortal 20`, Würfel-Bot (zufällige Karten, Stab nach `WandBotPolicy`) — eine Untergrenze,
kein Mensch. Gleiche Seeds für alle drei.

| | Bürokrat | Hexe | Apprentice |
|---|---|---|---|
| sterblich: Kämpfe bis zum Tod (Ø) | 5,8 | 8,6 | 8,7 |
| sterblich: Akte geschafft (Ø) | 0,00 | 0,03 | 0,04 |
| Züge pro Kampf (sterblich / unsterblich) | 6,4 / 13,1 | 6,2 / 11,0 | 5,6 / 15,0 |
| Karten pro Zug (unsterblich) | 2,98 | 3,83 | 3,69 |
| eigene Aktionen pro Zug | 0 | 1,0 | 2,0 |

Stab (unsterblich, alle fünf Akte): 1,67 Casts pro Zug, 18 % Züge mit gefülltem, ungecastetem Stab, 0,58 Fokus
am Zugende übrig, 44 % der Plätze belegt (41 % bei 5–6 Plätzen), 7 % der genutzten Karten eingeschrieben statt
gespielt, 0,91 Erschöpfung pro Cast, 36,5 Burn Outs pro Lauf.

Lesart: Beim Würfel-Bot schneidet der Apprentice in Akt I wie die Hexe ab und klar besser als der Bürokrat; in
späten Akten braucht er die meisten Züge pro Kampf. Die Stab-Zahlen beschreiben vor allem die simple Bot-Regel
(einschreiben nur bei leerem Stab, casten wann immer möglich, Burn Out nahe am Limit) — für die Fragen aus §16
(kurz gegen lang, Burn-Ketten, 5–6 Plätze) braucht es als Nächstes einen planenden Spieler (FightPlanner/ActBench
mit Apprentice-Decks) statt des Würfel-Bots.

Gefunden und behoben durch den Bench: Auditor of Returned Lives kam nach dem Closing Audit beliebig oft zurück
(bnb-content 1677f31); verbrauchtes „Cite the Old Survey“ war endlos spielbar (6962eb9).

