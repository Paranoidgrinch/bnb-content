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
| Balance-Bench (Master §16 Stufe 6) | offen |
