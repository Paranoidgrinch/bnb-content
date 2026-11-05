namespace BnbContent.Converter;

// THE RULES OF ELITES AND BOSSES, IN THE PLAYER'S WORDS (playtest 3, B7: "elite regeln sind nicht immer richtig klar
// und oft hinter wording versteckt (zb portcullis judicator)"). A rule badge on an enemy shows its name; its hover
// shows this text. Each line says what triggers the rule and what it does, with the numbers, and leaves the fiction
// to the enemy's name and art. A new name is given where the old one only repeated the enemy's.
//
// The texts are kept here, together, so the whole rulebook can be read and corrected in one place; they replace the
// statuses' own descriptions when the document is assembled (BlueprintAssembler). The mechanics are unchanged.
public static class RuleTexts
{
    public static readonly IReadOnlyDictionary<string, (string? Name, string Text)> All =
        new Dictionary<string, (string? Name, string Text)>(StringComparer.Ordinal)
        {
            // ── Act I elites ────────────────────────────────────────────────────────────────────────────────
            ["iron_warrant"] = (null,
                "Each turn it gives an order about your cards: obey and it takes 2 damage, disobey and it gains 1 Strength."),
            ["seizure_marshal"] = ("Profits from Seizures",
                "Gains 1 Strength for every card the Lock Cart locks away."),
            ["inventory_lantern"] = ("Wax Seal",
                "Each turn it seals a card in your hand: play it this turn, or it is locked away and the Marshal gains 1 Strength."),
            ["lock_cart"] = ("Locks Sealed Cards",
                "A sealed card still in your hand at turn end is locked away for this fight (2 at most)."),
            ["the_gatehouse"] = (null,
                "Deal 12+ damage in one turn and the gate rises a step: it takes more damage but hits harder. A weaker turn lowers it."),
            ["gate_shut"] = (null, "Gate down: it guards and hits lightly."),
            ["gate_half_raised"] = (null, "Gate half up: it hits harder."),
            ["gate_open"] = (null, "Gate open: it takes 20% more damage but hits hardest."),
            ["held_open"] = (null, "You forced the gate this turn: it will not fall at the end of the round."),
            ["the_waiting_room"] = (null,
                "Play exactly the shown number of cards: next turn it has no Block and takes double damage. Otherwise a Patron sits down."),
            ["office_hours"] = (null,
                "Closed for 2 turns, open for 1. Damage dealt while it is closed is saved and hits all at once when it opens."),
            ["closing_time"] = (null, "Counts down each turn; at 0 the Bell rings for 25. A turn you play 4+ cards adds 1 back."),

            // ── Act I bosses and the appellate stairs ──────────────────────────────────────────────────────
            ["the_deputy"] = (null,
                "Each turn it gives you a task (2 turns to do it). Unfinished tasks become Backlog; at 4 it takes over and gains Block."),
            ["appointment_due_first"] = (null, "Counts down each turn. At 0 it hits you for 7."),
            ["appointment_due_second"] = (null, "Counts down each turn. At 0 it hits you for 10."),
            ["appointment_due_final"] = (null, "Counts down each turn. At 0 it hits you for 15."),
            ["step_lower"] = (null, "Bottom step of the case."),
            ["step_middle"] = (null, "Middle step of the case."),
            ["step_upper"] = (null, "Top step: if the case stays up here, the ruling falls."),
            ["holds_the_case"] = (null, "Play a Deed each turn: the case goes down a step. Otherwise it climbs; at the top the ruling falls. It hits 2 harder."),
            ["the_commissioner"] = (null,
                "You move up the queue each turn (twice if you play 4+ cards). At the Counter it loses its Block and takes more damage."),
            ["the_sealkeeper"] = (null,
                "3 Seals give it 6 Block each turn. Break that Block and wound it in one turn: a Seal breaks, and its next phase begins."),
            ["the_municipal_dragon"] = (null,
                "Every second turn it demands 15 Gold: pay and it hoards a Permit (it gets stronger), refuse and you take 8 damage."),
            ["hoarded_permit"] = (null, "3 Block a turn, and 2 more damage on the Dragon's Assessment."),
            ["the_living_charter"] = (null,
                "One Article changes the rules for both sides. Every second turn you may keep it or strike it down for the next one."),
            ["article_of_continuance"] = (null, "Both sides keep half their Block when their turn comes round."),

            // ── Act II standard bodies (content cut 2026-10-29: every law readable on its enemy) ───────────
            ["brass_maw_delinquency"] = (null, "At 2 Overdue owed to it, it collects them: 7 damage, and its next attack deals 8 more (max +16)."),
            ["return_parcel"] = (null, "Its next attack deals 5 more per Parcel, then the Parcels are spent."),
            ["ouroboros_delinquency"] = (null, "At 2 Overdue owed to it, it collects them: 7 damage and 6 Block. Then it files 1 new Overdue at once."),
            ["mnemonic_chain_collect"] = (null, "Leave the remembered card unplayed when it comes back: 1 Overdue, owed to the Chain."),
            ["remembered_volume_rule"] = (null, "The first card you play at the Chain is remembered. Back in hand it costs 1 more; play it and the Chain takes 8."),
            ["second_person_reference"] = (null, "Each turn it cites one card in your hand. Play it, or owe the Entry 1 Overdue."),
            ["orphan_citation_reference"] = (null, "Each turn it cites a card. Play it or a card of the same cost and type, or owe 1 Overdue."),
            ["learned_letter"] = (null, "Play two cards of the same cost in a row and it learns that cost."),
            ["fanged_alphabet_reference"] = (null, "Each turn it cites a card of the cost it learned. Play it, or owe it 1 Overdue."),
            ["blank_certificate_reference"] = (null, "Each turn it cites a card. Play it, or owe the Certificate 1 Overdue."),
            ["certification_required"] = (null, "Unless you played its cited card this turn, its first death leaves it at 35 HP instead."),
            ["certification_clause"] = (null, "Its death clause comes back at the start of each of its turns, until it has been used once."),
            ["stored_life"] = (null, "While the Jar stands, the body beside it survives its first death at 50% HP. Break the Jar first."),
            ["archive_regulations"] = (null, "A Misfiled card is skipped when you draw it: it goes away and you draw another."),
            ["wrong_edition_rule"] = (null, "One card in your hand is the wrong edition: it works, then it is Misfiled."),

            // ── Act II elites ──────────────────────────────────────────────────────────────────────────────
            ["colossus_distance"] = (null, "Drops by 1 each turn; at 0 it crushes you for 20. Deal 12+ damage in one turn to push it back 1."),
            ["the_black_catalogue"] = (null, "Each turn name a card in your hand: it costs 1 less and Exhausts, or 1 more and stays in hand, until played."),
            ["catalogue_rules"] = (null, "Each turn name a card in your hand: it costs 1 less and Exhausts, or 1 more and stays in hand, until played."),
            ["the_black_ink_oracle"] = (null,
                "Every second turn it hides the cost of a card in your hand. Guess it: right, draw 1; wrong, take 5 damage."),
            ["riddle_rules"] = (null,
                "Every second turn it hides the cost of a card in your hand. Guess it: right, draw 1; wrong, take 5 damage."),
            ["the_volume_of_causes"] = (null, "Damage dealt to one Volume becomes Block on the other."),
            ["the_volume_of_consequences"] = (null, "Damage dealt to one Volume becomes Block on the other."),
            ["the_drawer"] = (null, "Each turn it demands a card. Play it: the drawer opens and takes double damage. Keep it: you take 6 damage."),
            ["drawer_open"] = (null, "It holds a fed card: double damage until the end of your next turn. Shut, it gains 10 Block."),
            ["the_obituary"] = (null, "It must be killed three times: each death returns it as a new life with new attacks."),

            // ── Act II bosses ──────────────────────────────────────────────────────────────────────────────
            ["the_whispering_catalogue"] = (null,
                "It mirrors you: each Deed you play this turn is 3 damage to you, each Working 4 Block for it. Play nothing: 10 damage."),
            ["the_warden_of_sealed_volumes"] = (null,
                "Each turn it seals a card from your hand (3 at most). Deal it 20 damage in one turn to free them all."),
            ["warden_custody"] = (null, "Each card it holds adds 3 damage to its attacks."),
            ["the_curator_of_misplaced_hours"] = (null, "Each card you play adds 2 damage to its next attack."),
            ["kept_hours"] = (null, "Its next attack deals 2 more damage for each."),
            ["the_auditor_of_returned_lives"] = (null, "Every second turn is an Audit: each card left in your hand at turn end costs you 3 HP."),
            ["audit_due"] = (null, "Each card still in your hand at the end of this turn costs you 3 HP."),
            ["the_premise"] = (null, "Two volumes are linked and share their Block. The Isolated one does nothing and takes double damage."),
            ["the_authority"] = (null, "Two volumes are linked and share their Block. The Isolated one does nothing and takes double damage."),
            ["the_conclusion"] = (null, "Two volumes are linked and share their Block. The Isolated one does nothing and takes double damage."),
            ["isolated_in_the_margin"] = ("Isolated", "Does nothing and takes double damage. Moves to the next volume when its turns run out."),

            // ── Act III elites and bosses (reworked 2026-11-03: every rule a law, every broken law a Claim) ──
            ["green_docket_body"] = (null, "Break an enemy's law and it gains a Claim: +3 damage on its attacks, at most 3."),
            ["pre_approved_violence"] = (null, "Odd rounds: violence approved, your Deeds and its attacks deal 50% more. Even rounds: every Deed breaks its law."),
            ["grandmother_web_threads"] = (null, "Every card you play spins a thread (6 at most); every Working cuts 2. On its turn each thread hits you for 3."),
            ["debt_before_passage"] = (null, "Your first hit each turn breaks its law. At half health it turns: 8 damage to itself per Claim, then your first hits deal double."),
            ["nothing_crosses_for_free"] = (null, "Each turn it swallows 5 of your Gold; +1 damage per 10 swallowed. Deal it 25 in one turn and it spits it all back."),
            ["the_proper_line"] = (null, "Hitting a Bearer while one ahead stands breaks her law. While any Bearer stands she takes half damage. Each fallen Bearer rises once."),
            ["line_bearer_first"] = (null, "First Line-Bearer of the procession. Hitting it while a Bearer ahead of it stands breaks the Queen's law."),
            ["line_bearer_second"] = (null, "Second Line-Bearer of the procession. Hitting it while a Bearer ahead of it stands breaks the Queen's law."),
            ["line_bearer_third"] = (null, "Third Line-Bearer of the procession. Hitting it while a Bearer ahead of it stands breaks the Queen's law."),
            ["the_survey_map"] = (null, "Two laws trade places each round. Breaking the Current one breaks its law; breaking the Former gives you 1 Energy next turn."),
            ["the_magistrate_of_thorns"] = (null, "Keep the Judgment you chose and it loses 8 HP. Break it and it gains a Claim and 10 Block."),
            ["grounds_of_complaint"] = (null, "Odd rounds: no two cards of one cost in a row. Even rounds: no more than 3 cards. From half health, both."),
            ["three_rings_of_law"] = (null, "One of three laws is read each round. A law you break becomes a ring: it always stands and adds 3 to its attacks."),
            ["the_courtesies"] = (null, "Each turn she gives you a gift: play it and she loses 6 HP; keep it and you break her law. Later they cost 1."),
            ["the_landscape_has_standing"] = (null, "What you played most last turn is forbidden: each one breaks its law. At 2/3 and 1/3 health it hits you 5 per Claim."),
            ["the_hill_court"] = (null, "Your first card each turn must be a Working, or you break her law. Keep it 3 turns running and she sits out a turn."),

            // ── Act IV standard bodies (reworked 2026-11-04: the Scale, the jars, Risen; each rule on its body) ──
            ["weighed"] = (null,
                "The measure: spend exactly this much Energy this turn. How far off you end up is what the labyrinth answers."),
            // ── Act IV elites ──────────────────────────────────────────────────────────────────────────────
            ["labyrinth_body"] = (null, "Uses Act IV law: Burdened, the Scale, the jars and Risen."),
            ["raising_the_obelisk"] = (null,
                "While a Hauler stands the obelisk moves 1 each round; at 4 it falls on you for 40. Haulers return 2 rounds after falling."),
            ["the_dawn_song"] = (null, "Every third round is dawn: it sings, and up to 2 of your Burdened become Entombed."),
            ["the_coils"] = (null,
                "Each round it coils tighter: 1 card fewer drawn per Coil (at most 3). Deal it 20 in one turn to loosen one."),
            ["the_riddle"] = (null,
                "Each round she names Deeds or Workings: play more of those than the other for a Sign, else 1 Entombed. At 3 she is solved."),
            ["the_devourer_waits"] = (null,
                "Your turn ends: Deeds equal to Workings earn a Feather, else 1 Unworthy. At 3 she devours: 40, minus 12 per Feather eaten."),
            ["sekhmets_rage"] = (null,
                "Every time you lose HP she gains 1 Rage. Block one of her attacks completely and she loses all Rage."),
            ["the_four_sons"] = (null,
                "A jar of each son of Horus stands beside him, each a power. Every third round he refills a broken one."),
            ["the_brood"] = (null,
                "Three brood jars stand beside her. Every second round each intact jar hatches a Scarab that bites you 3 times for 3."),
            ["the_linen_house"] = (null,
                "Each turn it wraps a card in your hand. A wrapped card costs 0, but playing it gives you 1 Entombed."),
            ["tombbreaker"] = (null,
                "When one dies, the others gain 3 Strength and one takes up its tool. Kill all three and Tomb Gold joins your deck."),

            // ── Act IV bosses (reworked 2026-11-04) ─────────────────────────────────────────────────────────
            ["the_sealed_name"] = (null,
                "Each round he gives a command. Obey 3 and a name breaks: he loses a turn. Break all 3 names and his Ward falls."),
            ["cartouche_ward"] = (null,
                "While one of his names stands he takes 20 % less damage."),
            ["the_great_scale"] = (null,
                "Deeds tip it to the heart, Workings to the feather; it never resets. Level at turn end: a Feather; tipped, he bites or guards."),
            ["the_impossible_pyramid"] = (null,
                "He builds 2 courses a round. Any single hit of 12 or more tears one down. At 6 his next action is the Capstone."),
            ["her_bounty"] = (null,
                "Her Bounty is in your hand each turn: 7 HP for cards, and Grain for her. At 7 Grain she harvests you for 49."),
            ["the_house_of_life"] = (null,
                "He writes your first 3 Deeds and Workings, reads them back: Deeds hit you for 6, Workings give him 6 Block. Rites erase one."),
            ["selkets_vessels"] = (null,
                "Her 4 vessels pour on you in turn, one each turn of hers; broken ones pour nothing. From half, or all broken, she rises once."),
            ["the_nilometer"] = (null,
                "End your turn with 0 Energy and the Nile rises a step; with Energy left it falls. Each of the 5 levels does something."),

            // ── Act V gods ─────────────────────────────────────────────────────────────────────────────────
            ["oaths_beneath_the_sun"] = (null,
                "Each turn swear an Oath and get paid at once. Breaking it is allowed, but makes his attacks stronger."),
            ["beneath_the_morning_sun"] = (null, "Each Oath lasts one turn; he asks again every morning."),
            ["the_word_of_enlil"] = (null,
                "Each decree is shown a full turn before it starts, runs its count and ends. It binds him too."),
            ["one_word"] = (null, "One decree at a time. The next one is already shown."),
            ["next_the_fourth_work_shall_be_the_last"] = (null,
                "Next decree: you can play at most 4 cards a turn. Starts at the end of your next turn."),
        };

    // Every status the rulebook names, with its name and text replaced.
    public static List<RogueDeck.Scenario.Authoring.StatusData> Apply(IReadOnlyList<RogueDeck.Scenario.Authoring.StatusData> statuses) =>
        [.. statuses.Select(s => All.TryGetValue(s.Id, out var rule)
            ? s with { NameKey = rule.Name ?? s.NameKey, DescriptionKey = rule.Text }
            : s)];
}
