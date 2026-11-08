# Allgemeiner Kartenpool — Neubau (F2)

Auftrag (2026-11-08): Der allgemeine Pool entstand, als es nur den Bureaucrat gab, und ist auf ihn zugeschnitten.
Er soll von allen Charakteren genutzt werden können, mit Synergien, ohne auf einen bestimmten zu wirken:
Energiemanagement, Draw-Engine, Abwerfen, Gegner-Debuffs und so weiter. „Sei authentisch und kreativ.“

Status: **Entwurf zur Durchsicht.** Gebaut wird erst, wenn die Regeln (§1–§3) und die 60 Karten (§4) freigegeben
sind.

---

## 0 Befund

- **Heute:** 50 allgemeine Karten, keine Commons. Etwa 35 davon stehen auf Lien, Citation, Blood Ink und
  Censure. Diese vier Status setzt nur der Bureaucrat; Hexe, Apprentice und Lieutenant haben keine einzige
  Karte damit.
- **Ward Wax** ist der einzige Status, den alle schon teilen (Hexe 14, Apprentice 6, Lieutenant 3).
- **Jede allgemeine Karte hängt an zwei Tabellen:**
  - eine Zutat der Hexe in `Witch/WitchFamilies.cs`: Fang, Hex, Husk, Hearth oder Fortune
  - eine Stab-Wirkung des Apprentice in `Apprentice/ApprenticeProxies.cs`

  Beide werden für die neuen Karten neu geschrieben.
- **Allgemeine Relikte** sind schon weitgehend neutral. Sie werden in einem eigenen Durchgang gesichtet (§6).

## 1 Entscheidungen (getroffen)

| # | Frage | Entscheidung |
|---|---|---|
| P1 | Lien, Citation, Blood Ink, Censure | **weg.** Ihre Karten verlassen das Spiel. Wo Relikte, Ereignisse oder Gegner sie noch nennen, wird das beim Bau mit umgestellt. |
| P2 | Seltenheiten | **mit Commons.** 60 Karten: 24 Common, 24 Uncommon, 12 Rare, also 4 / 4 / 2 je Achse. |
| P3 | Schwächer zuschlagen | **Doubt** (gibt es schon): die nächsten Angriffe machen 25 % weniger, ein Stapel je Angriff. Wird von einem Bureaucrat- zu einem allgemeinen Status. |
| P4 | Mehr Schaden nehmen | **ein neuer Status**, es gibt noch keinen. Vorschlag §2. |
| P5 | Achsen | die sechs aus §3, je 10 Karten |

## 2 Die allgemeine Sprache

Möglichst wenige neue Wörter. Jedes neue Wort muss allein verständlich sein und bei allen vier Charakteren
etwas bedeuten. Geprüft gegen alle Status, Karten und Relikte im Spiel (2026-11-08).

| Wort | Art | Regel | Stand |
|---|---|---|---|
| **Doubt** | Debuff | Die nächsten Angriffe machen 25 % weniger. Ein Stapel je Angriff. | vorhanden |
| **Liable** | Debuff | Nimmt 25 % mehr Schaden von Angriffen. Ein Stapel fällt am Ende seines Zuges. | **neu**, freigegeben |
| **Ward Wax** | Abwehr | wie bisher | vorhanden |
| **Held Energy** | Energie | Energie, die wartet und kommt, sobald man leer ist | vorhanden (`HeldEnergy.cs`) |
| **Retain** | Hand | Bleibt am Zugende auf der Hand. | vorhanden |
| **Unread:** | Auslöser | Wirkung, wenn ein Karteneffekt die Karte aus der Hand abwirft oder erschöpft. Das normale Abwerfen am Zugende zählt nicht (wie Madness). | **neu**, freigegeben |
| **Pledge** | Energie | Darf ohne genug Energie gespielt werden. Was fehlt, wird zu Fatigue im nächsten Zug. | **neu**, Vorschlag |

**Prüfung gegen den Bestand:**
- **Liable:** Es gibt keinen allgemeinen Status dieser Art. Ähnlich wirken nur Begegnungs-Status (Being
  Served, Solved, Gate: Open, Nanna-Suens abnehmender Mond) und Ratified, das zum Bureaucrat gehört und nur
  Deeds verdoppelt.
- **„Misfiled“ fällt weg:** Das Wort ist schon die Kartenmarke aus Akt II („A Misfiled card is skipped when
  you draw it“), außerdem gibt es die Karte Misfiled Paper.
- **Unread und Pledge** kommen nirgends vor.
- **„Overtime“ fällt weg** (zu modern).

**Was die Charaktere schon mitbringen und der Pool füttert, ohne es zu nennen:**
- Die Hexe wirft ab und braut. Dazu passen Unread-Karten.
- Der Apprentice will viele Karten und Energie für den Stab. Dazu passen Draw und Energie.
- Der Lieutenant will Deed-Treffer und Strength. Dazu passen Liable und Mehrfachtreffer.
- Der Bureaucrat will Gegner, die lange leben und an Status leiden. Dazu passen Doubt, Liable und Retain.

## 3 Die sechs Achsen

Jede Karte tut allein etwas Brauchbares. Die Synergie entsteht aus allgemeinen Größen wie Handgröße,
Energie, Debuffs am Gegner oder Block, nie aus Charakter-Begriffen.

1. **Energie.** Energie jetzt, Held Energy für später, Kosten senken, Pledge. Rares verwandeln Überschuss in
   Wirkung.
2. **Draw-Engine.** Ziehen, Retain, den Nachziehstapel oben ordnen, Karten zurücklegen. Rares lohnen große
   Hände.
3. **Abwerfen und Erschöpfen.** Unread-Karten, Karten, die abwerfen lassen, und Belohnungen dafür, dass Karten
   verschwinden.
4. **Gegner-Debuffs.** Doubt und Liable verteilen, verlängern, verdoppeln. Rares belohnen verschiedene Debuffs
   am selben Gegner.
5. **Verteidigung.** Block-Werkzeuge mit Bedingung, Ward Wax, Block, der bleibt oder sich in etwas verwandelt.
6. **Flexibler Schaden.** Flächenschaden, Bedingungsschaden, Finisher, Mehrfachtreffer (gut mit Strength und
   Liable).

**Rahmen je Achse:** 4 Commons (einfach, sofort spielbar), 4 Uncommons (eine Bedingung oder Wahl),
2 Rares (eine Engine oder ein Finisher).

**Akt-Sperre wie beim Bureaucrat:** Commons ab Akt I, Uncommons zu etwa 2/3 ab Akt I und der Rest ab Akt II,
Rares gestaffelt über Akt II bis IV. **Upgrades** folgen den bisherigen Regeln.

## 4 Die 60 Karten zur Durchsicht

**Benennung im Ton des Spiels:** Kanzlei, Schreibstube und Hausmagie, also Kerzen, Siegel, Besen, Asche,
alte Rechtsformeln und Volksmagie. Nichts Modernes, keine Halbsätze: Ein Name ist ein Ding, ein Akt oder eine
Formel, wie bei Grave Lien, Borrowed Candle, Sealed Mantle oder Dawn Summons. Alle neuen Namen sind gegen
Karten, Relikte und Status im Spiel geprüft und kommen noch nicht vor.

**MTG als Ideengeber,** in eigener Sprache: Ponder, Brainstorm, Looting, Cycling, Madness, Regrowth, Fact or
Fiction, Threshold, Contagion, Fog und Storm. Die Spalte „Notiz“ nennt das Vorbild.

**Bestand:** 12 der 60 sind bestehende, schon neutrale Karten (in der Notiz mit „bestehend“ markiert).
Die übrigen 48 sind neu. Die Zahlen sind ein Balance-Entwurf; nach der Freigabe zieht der Bench sie nach.

**Die letzte Spalte ist die Zutat der Hexe:** Fang, Hex, Husk, Hearth oder Fortune.


### Energie (freigegeben 2026-11-08)

Maßstab: 1 Energy ≈ 7 Block oder 7 bis 8 Schaden, eine Karte auf der Hand ≈ ½ Energy, 1 Fatigue = 1 Energy
weniger im nächsten Zug. Upgrades bringen netto etwa +½ bis +1.

| Karte | Kosten | Seltenheit | Text | Upgrade | Notiz | Hexe |
|---|---|---|---|---|---|---|
| **Stolen Hour** | 0 | Common | Gain 2 Energy. Exhaust. | + Draw 1 card. |  | Hearth |
| **Banked Embers** | 1 | Common | Gain 2 Held Energy. Draw 1 card. | 3 Held Energy |  | Hearth |
| **Tallow Stub** | 0 | Common | Gain 1 Held Energy. Unread: gain 1 Held Energy. | 2 und 2 |  | Hearth |
| **Second Wind** | 0 | Common | Gain 1 Energy. Draw 1 card. Exhaust. | Draw 2 | bestehend (`sanctioned_charm`) | Hearth |
| **Coin Under the Tongue** | 1 | Uncommon | Pledge. Draw 2 cards. The next card you play this turn costs 1 less. | Draw 3 |  | Hearth |
| **False Signature** | 0 | Uncommon | Your next card this turn costs 1 less. The card after it this turn costs 1 more. Exhaust. | 2 less | bestehend; beides gilt nur in diesem Zug | Fortune |
| **Melted Down** | 0 | Uncommon | Exhaust a card from your hand. Gain Energy equal to its cost. | + Draw 1 card. | MTG: Opfern | Fortune |
| **Wick Burnt Short** | 0 | Uncommon | Gain 2 Energy. At the start of your next turn, gain 1 Fatigue. | 3 Energy |  | Hearth |
| **Vigil Lamp** | 0 | Rare | For the rest of this combat, at the end of your turn gain 8 Block for each Energy you have left. Exhaust. | 11 Block |  | Husk |
| **Miser's Coffer** | 1 | Rare | Whenever you end your turn with Energy left, keep up to 2 of it as Held Energy. Exhaust. | kostet 0 |  | Hearth |

### Draw-Engine (freigegeben 2026-11-08)

| Karte | Kosten | Seltenheit | Text | Upgrade | Notiz | Hexe |
|---|---|---|---|---|---|---|
| **Sieve and Shears** | 0 | Common | Look at the top 3 cards of your draw pile. Put any of them on the bottom. Draw 1 card. | Draw 2 | MTG: Ponder | Hearth |
| **Borrowed Candle** | 0 | Common | Draw 3 cards. Put a card from your hand on top of your draw pile. Exhaust. | kein Exhaust | bestehend, war Draw 2; MTG: Brainstorm | Hearth |
| **Lamplit Study** | 1 | Common | Draw 2 cards. Gain 5 Block. | 8 Block |  | Hearth |
| **Dog-Eared Almanac** | 0 | Common | Retain. Draw 1 card. If this was in your hand since last turn, draw 2 more. | draw 3 more |  | Hearth |
| **Night Scrivener** | 1 | Uncommon | Draw 3 cards. Choose a card in your hand: it gains Retain this combat. | Draw 4 |  | Hearth |
| **Fresh Ledger** | 0 | Uncommon | Exhaust another card from your hand. Draw 2 cards. | Draw 3 | bestehend (`crossed_sigil`), war 1 Energy | Hearth |
| **Wheat from Chaff** | 0 | Uncommon | Reveal the top 3 cards of your draw pile. Put either the attacks or the rest into your hand; discard the others. | Reveal 4 | MTG: Fact or Fiction | Fortune |
| **Gathered Gossip** | 0 | Uncommon | Draw 1 card, plus 1 for each enemy. Exhaust. | Draw 2, plus 1 for each enemy |  | Hearth |
| **Errand Rook** | 1 | Rare | At the start of each of your turns, draw 1 more card. Whenever your hand is full, gain 3 Block. Exhaust. | kostet 0 |  | Hearth |
| **Moonlit Counterfeit** | 1 | Rare | Create a Temporary copy of a card in your hand. It costs 0. Exhaust. | kostet 0 | bestehend; das Original bleibt jetzt; MTG: Twincast | Hearth |

### Abwerfen und Erschöpfen (freigegeben 2026-11-08)

| Karte | Kosten | Seltenheit | Text | Upgrade | Notiz | Hexe |
|---|---|---|---|---|---|---|
| **Spiteful Draft** | 1 | Common | Deal 7 damage. Unread: deal 7 damage to a random enemy. | 10 und 10 | MTG: Madness | Fang |
| **Scattered Notes** | 0 | Common | Discard 1 card. Draw 2 cards. | Draw 3 | MTG: Looting | Fortune |
| **Ashen Receipt** | 1 | Common | Gain 7 Block. Unread: draw 1 card. | 10 Block | MTG: Cycling | Husk |
| **Smouldering Correspondence** | 1 | Common | Deal 5 damage to ALL enemies. Unread: deal 5 damage to ALL enemies. | 8 und 8 | MTG: Fiery Temper | Fang |
| **Hearth Kindling** | 0 | Uncommon | Discard any number of cards. Draw that many. Gain 3 Block for each. | 4 Block each | MTG: Careful Consideration | Fortune |
| **Ash Remembers** | 1 | Uncommon | Put a card from your discard pile into your hand. It costs 0 this turn. Exhaust. | kostet 0 | MTG: Regrowth | Hearth |
| **Weight of Paper** | 2 | Uncommon | Deal 6 damage, plus 2 for each card in your discard pile. Count at most 12. | 10 damage, plus 2 for each | MTG: Threshold | Fang |
| **Burnt Offering** | 0 | Uncommon | Exhaust a card from your hand. Gain 8 Block. Draw 1 card. | 11 Block | MTG: Village Rites | Husk |
| **Whispering Gallery** | 1 | Rare | Whenever a card leaves your hand without being played, a random enemy gets 1 Doubt. Exhaust. | kostet 0 | zählt auch das Zugende; MTG: Waste Not | Hex |
| **Pyre of Old Records** | 1 | Rare | Whenever a card is exhausted, deal 7 damage to a random enemy and gain 7 Block. Exhaust. | kostet 0 | MTG: Blood Artist | Fang |

### Gegner-Debuffs (freigegeben 2026-11-08)

Maßstab: 1 Doubt ≈ 0,4 Energy (ein Viertel von einem 12er-Angriff), 1 Liable ≈ 0,6 Energy (ein Zug mit +25 % auf
etwa 20 Schaden).

| Karte | Kosten | Seltenheit | Text | Upgrade | Notiz | Hexe |
|---|---|---|---|---|---|---|
| **Seed of Misgiving** | 1 | Common | Apply 2 Doubt. Gain 4 Block. | 3 Doubt |  | Hex |
| **Named in the Roll** | 1 | Common | Apply 2 Liable. Draw 1 card. | 3 Liable | MTG: Marked for Death | Hex |
| **Overlooking** | 0 | Common | Apply 1 Doubt and 1 Liable. | 2 Doubt and 1 Liable | „overlook“: altes Wort für den bösen Blick | Hex |
| **Curdled Milk** | 1 | Common | Apply 3 Doubt to ALL enemies. | 4 Doubt |  | Hex |
| **Hare's Mark** | 1 | Uncommon | Apply 2 Liable. If the enemy has Doubt, apply 2 more. | 3, and 3 more |  | Hex |
| **Lingering Malison** | 0 | Uncommon | Every enemy with Doubt gets 1 more Doubt; every enemy with Liable gets 1 more Liable. Draw 1 card. | Draw 2 | MTG: Proliferate | Hex |
| **Spreading Rumour** | 1 | Uncommon | Apply 1 Doubt and 1 Liable. Then every other enemy gets the Doubt and Liable this enemy has. | 2 Doubt and 2 Liable | MTG: Contagion | Hex |
| **Ill Wind** | 2 | Uncommon | Apply 1 Liable to ALL enemies, then deal 8 damage to ALL enemies. | 11 damage | MTG: Pestilence | Fang |
| **Compounded Grievance** | 1 | Rare | Double every debuff on an enemy. Exhaust. | kostet 0 | MTG: Doubling Season | Hex |
| **Bill of Attainder** | 1 | Rare | Whenever you apply Doubt or Liable, apply 1 more. Exhaust. | kostet 0 | MTG: Hardened Scales | Hex |

### Verteidigung (freigegeben 2026-11-08)

Maßstab: 1 Energy ≈ 7 Block; 7 Ward Wax ≈ 16 Block über vier Züge ≈ 1,5 Energy.

| Karte | Kosten | Seltenheit | Text | Upgrade | Notiz | Hexe |
|---|---|---|---|---|---|---|
| **Besom Guard** | 1 | Common | Gain 8 Block. Unread: gain 4 Block. | 11 und 6 | MTG: Cycling | Husk |
| **Waxen Surety** | 1 | Common | Gain 7 Ward Wax. | 10 Ward Wax | bestehend | Husk |
| **Salt on the Sill** | 1 | Common | Gain 6 Block. Every enemy that intends to attack gets 1 Doubt. | 9 Block | MTG: Holy Day | Husk |
| **Barred Door** | 2 | Common | Retain. Gain 14 Block. | 18 Block |  | Husk |
| **Tallow Reserve** | 0 | Uncommon | Needs 6 Block. Lose 6 Block. Gain 6 Ward Wax. Exhaust. | 9 Ward Wax | bestehend, war 4 | Husk |
| **Sealed Mantle** | 1 | Uncommon | Gain 8 Block. If enemies attack this turn and nothing gets through, gain 3 Ward Wax. | 12 und 4 | bestehend | Husk |
| **Wax Reliquary** | 1 | Uncommon | Gain 5 Ward Wax. Until your next turn, Ward Wax does not decay faster. | 9 Ward Wax | bestehend | Husk |
| **Rowan over the Door** | 1 | Uncommon | Gain 7 Block. Until your next turn, every enemy that attacks you gets 1 Doubt. | 10 Block |  | Husk |
| **Stay of Proceedings** | 3 | Rare | An enemy's next action does nothing. Exhaust. | kostet 2 | MTG: Fog / Counterspell | Fortune |
| **Standing Stones** | 2 | Rare | Your Block does not expire at the start of your next 2 turns. Exhaust. | kostet 1 | MTG: Ivory Tower | Husk |

### Flexibler Schaden

| Karte | Kosten | Seltenheit | Text | Notiz | Hexe |
|---|---|---|---|---|---|
| **Thrown Inkpot** | 1 | Common | Deal 8 damage. Apply 1 Doubt. |  | Fang |
| **Poppet Pin** | 0 | Common | Deal 4 damage. If you have played 2 or more cards this turn, deal 4 more. |  | Fang |
| **Beadle's Cudgel** | 1 | Common | Deal 9 damage. If the enemy is Liable, draw 1 card. |  | Fang |
| **Hailstones** | 1 | Common | Deal 4 damage to ALL enemies twice. |  | Fang |
| **Volley of Quills** | 1 | Uncommon | Deal 3 damage to ALL enemies, once for each card in your hand. |  | Fang |
| **Blunt Instrument** | 1 | Uncommon | Deal 14 damage. | bestehend (`blacklisted`) | Fang |
| **Tallow Judgment** | 2 | Uncommon | Spend up to 8 Ward Wax. Deal 10 damage, +7 per Wax. | bestehend | Fang |
| **Midnight Toll** | 2 | Uncommon | Deal 5 damage for each card you played this turn. Count at most 6. | MTG: Storm | Fang |
| **Third and Final Writ** | 2 | Rare | Deal 12 damage. If the enemy has Doubt and Liable, deal it twice. Retain. |  | Fang |
| **Dawn Summons** | 2 | Rare | Deal 16 damage. +10 if it is your first card this turn. | bestehend | Fang |

**Synergie über allgemeine Größen, quer durch die Achsen:**
- Handgröße bei Volley of Quills und Errand Rook
- Karten pro Zug bei Poppet Pin und Midnight Toll
- übrige Energie bei Vigil Lamp und Miser's Coffer
- Ablagestapel bei Weight of Paper und Ash Remembers
- Debuff-Kombinationen bei Hare's Mark, Beadle's Cudgel, Third and Final Writ und Compounded Grievance
- Verschwinden aus der Hand bei Unread, Whispering Gallery und Pyre of Old Records

**Was die Engine braucht und noch zu prüfen ist:**
- der Unread-Auslöser
- Pledge
- „Block verfällt nicht“ bei Standing Stones
- eine Gegneraktion aussetzen bei Stay of Proceedings; der Queen-„sits out“-Baustein ist ein Ansatz
- „seit letztem Zug auf der Hand“ bei Dog-Eared Almanac

Das klärt der Bau, bevor eine Karte gebaut wird.

## 5 Was mit den alten Karten passiert

- Die rund 35 Karten mit Lien, Citation, Blood Ink oder Censure fallen weg.
- **Übernommen werden 12 neutrale Karten** (in §4 als „bestehend“ markiert):
  - Second Wind, Borrowed Candle, False Signature, Fresh Ledger, Moonlit Counterfeit
  - Waxen Surety, Tallow Reserve, Sealed Mantle, Wax Reliquary, Tallow Judgment
  - Blunt Instrument, Dawn Summons
- **Ebenfalls weg,** obwohl neutral: Grand Dispensation, Wax Indemnity, Stone Levy. Sie passen in keine Achse
  besser als die neuen Karten.

  Sie zählen zu den 60.
- **Wo alte Karten noch auftauchen**, wird alles mit umgestellt:
  - Golden-Läufe, Tests und Tutorial
  - Hexen-Familien und Apprentice-Proxys
  - `card_texts.json`

## 6 Relikte (eigener Durchgang danach)

- Die allgemeinen Relikte werden gesichtet. Was einen der gestrichenen Status nennt, wird umgeschrieben oder
  gestrichen.
- Wo es passt, bekommen die sechs Achsen je 1–2 Relikte, zum Beispiel „Unread-Wirkungen lösen doppelt aus“
  oder „Liable hält einen Zug länger“.
