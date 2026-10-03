# Familien-Decks (PLAYTEST_FEEDBACK_2 F2) — zur Durchsicht

Gemessen wird, was eine Schlüsselwort-Familie **in einem um sie gebauten Deck** in späteren Akten leistet (Spieler,
2026-10-02), nicht die einzelne Karte im Akt-I-Starter. Quelle der Wahrheit: `FAMILY_DECKS.json`, erzeugt und auf das
Budget geprüft von `family_decks.py` (dort ändern, dann `python3 family_decks.py`). Bench:
`dotnet run --project Converter -- --family-decks FAMILY_DECKS.json [--family-acts 2] [--bench-only paperwork_a2]`.

## Budget (für jedes Deck gleich)

| | Akt II (Pool bis Akt II) | Akt III (Pool bis Akt III) |
|---|---|---|
| Starter (4 Paper Cut, 4 Cower, Strong Binder, Permit A38) | −3 Basiskarten | −4 Basiskarten |
| Familienkarten | +10 | +13 |
| Kleber (für alle gleich) | Counter Ward, Deskward | + Borrowed Candle |
| Deckgröße | 19 | 22 |
| Upgrades | 5 | 9 |
| Rares unter den Familienkarten | ≤ 2 | ≤ 3 |
| Kopien | ≤ 3, Rares und Rites 1 | ≤ 3, Rares und Rites 1 |
| Relikte (Durchgang „mit Relikten“) | 2 | 3 |

- „Rein“: nur Karten der eigenen Familie. Ausnahme Blood Ink: Blood Marginalia (Citation + Blood Ink) ist sein einziger
  Partner vor Akt III — Blood Ink lebt davon, dass ANDERE Status schrumpfen.
- Familien ohne eigenen Schaden (Censure, Citation, Ward Wax) streichen Cower statt Paper Cut — so würde ein Spieler bauen.
- Ward Wax hat vor Akt III nur 3 Karten (Akt II: 8 Familienkarten, die fehlenden Plätze bleiben Basiskarten) und **keinen
  Zahltag vor Akt IV** (Tallow Judgment). Das ist bereits ein Befund.
- Relikte: familieneigene, wo es sie gibt (Archive 8, Paperwork 6, Queue 3, Seal 2, Censure 1, Ward Wax 1); sonst die
  passendsten allgemeinen (Tarnished Bell, Sootglass Lens, Index Bone …). Keine Boss-Relikte.
- Gemessen: 10 gleichmäßig verteilte Normalkämpfe + alle 9 Elites + alle 5 Bosse des Akts, je 4 Shuffles, 70 HP,
  Planer 5 Züge voraus. Die drei Kampfarten zählen gleich.

## Decks

**paperwork_a2** (Paperwork, Akt 2, 19 Karten)  
Familie: cursed_addendum ×2, form_of_ill_intent+ ×2, inkblot_verdict, inkblot_verdict+, occult_precedent ×2, red_ink_doctrine+, summary_judgment+  
Basis + Kleber: counter_ward, cower_behind_a_desk ×3, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: formkeepers_signet, chancery_scale

**paperwork_a3** (Paperwork, Akt 3, 22 Karten)  
Familie: black_ledger, broom_dispatch, cursed_addendum+ ×2, form_of_ill_intent+ ×2, hedge_hospitality+, inkblot_verdict+ ×2, occult_precedent ×2, red_ink_doctrine+, summary_judgment+  
Basis + Kleber: borrowed_candle, counter_ward, cower_behind_a_desk ×2, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: formkeepers_signet, chancery_scale, concordance_medallion

**doubt_a2** (Doubt, Akt 2, 19 Karten)  
Familie: clerical_discretion ×2, fine_print_hex, fine_print_hex+, hex_circular+, petty_objection, petty_objection+ ×2, presumption_of_error, rebuttal+  
Basis + Kleber: counter_ward, cower_behind_a_desk ×3, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: concordance_medallion, tarnished_bell

**doubt_a3** (Doubt, Akt 3, 22 Karten)  
Familie: clerical_discretion, due_recompense+, fine_print_hex+ ×2, formal_dissent, guestbook_oath+, hedge_covenant, hex_circular+, petty_objection+ ×3, presumption_of_error, rebuttal+  
Basis + Kleber: borrowed_candle, counter_ward, cower_behind_a_desk ×2, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: concordance_medallion, tarnished_bell, sootglass_lens

**queue_a2** (Queue, Akt 2, 19 Karten)  
Familie: backlog_charge, backlog_charge+, candle_allowance, deferred_hex, deferred_hex+ ×2, pending_matters, protective_adjournment+ ×2, skeleton_staff  
Basis + Kleber: counter_ward, cower_behind_a_desk ×3, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: petitioners_token, deferred_signet

**queue_a3** (Queue, Akt 3, 22 Karten)  
Familie: backlog_charge+ ×2, candle_allowance, deferred_hex+ ×3, pending_matters+, priority_docket ×2, protective_adjournment+ ×3, skeleton_staff  
Basis + Kleber: borrowed_candle, counter_ward, cower_behind_a_desk ×2, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: petitioners_token, deferred_signet, index_bone

**seal_a2** (Seal, Akt 2, 19 Karten)  
Familie: candle_tribunal+, conditional_approval ×2, notarial_press+ ×2, privy_seal, seal_dividend, threefold_injunction, waxing_authority+ ×2  
Basis + Kleber: counter_ward, cower_behind_a_desk ×3, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: seal_makers_die, tarnished_bell

**seal_a3** (Seal, Akt 3, 22 Karten)  
Familie: candle_tribunal+, conditional_approval+ ×2, notarial_press+ ×2, notarys_tithe, privy_seal, seal_dividend, seal_of_concern, threefold_injunction+, waxing_authority+ ×3  
Basis + Kleber: borrowed_candle, counter_ward, cower_behind_a_desk ×2, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: seal_makers_die, tarnished_bell, sootglass_lens

**archive_junk_a2** (Archive/Junk, Akt 2, 19 Karten)  
Familie: archive_pyre+, cauldron_copy+ ×2, certified_kindling ×2, cinder_warrant+ ×2, clerks_familiar, funeral_index, secure_misfiling  
Basis + Kleber: counter_ward, cower_behind_a_desk ×3, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: archive_key, archive_censer

**archive_junk_a3** (Archive/Junk, Akt 3, 22 Karten)  
Familie: archive_pyre+, cauldron_copy+ ×2, certified_kindling+ ×2, cinder_warrant+ ×2, clerks_familiar, clutter_concordance+, funeral_index+, secure_misfiling ×2, wastepaper_bastion  
Basis + Kleber: borrowed_candle, counter_ward, cower_behind_a_desk ×2, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: archive_key, archive_censer, ashen_wax_knife

**ward_wax_a2** (Ward Wax, Akt 2, 19 Karten)  
Familie: sealed_mantle, sealed_mantle+ ×2, tallow_reserve, tallow_reserve+, waxen_surety, waxen_surety+ ×2  
Basis + Kleber: counter_ward, cower_behind_a_desk ×3, deskward, paper_cut ×4, permit_a38, strong_binder  
Relikte: riddles_third_answer, bruise_cup

**ward_wax_a3** (Ward Wax, Akt 3, 22 Karten)  
Familie: consecrated_testament, sealed_mantle+ ×3, tallow_reserve ×2, votive_covenant, wax_indemnity+, wax_reliquary+ ×2, waxen_surety+ ×3  
Basis + Kleber: borrowed_candle, counter_ward, deskward, paper_cut ×4, permit_a38, strong_binder  
Relikte: riddles_third_answer, bruise_cup, blackthorn_brooch

**censure_a2** (Censure, Akt 2, 19 Karten)  
Familie: blacklisted+ ×2, countermanded_grace, crossed_sigil, malediction_review, malediction_review+ ×2, reciprocal_edict, sanctioned_charm, sanctioned_charm+  
Basis + Kleber: counter_ward, cower_behind_a_desk, deskward, paper_cut ×4, permit_a38, strong_binder  
Relikte: contempt_ledger_nail, threshold_ward

**censure_a3** (Censure, Akt 3, 22 Karten)  
Familie: blacklisted+ ×3, countermanded_grace, crossed_sigil ×2, malediction_review+ ×3, oath_of_refusal, reciprocal_edict+, sanctioned_charm+ ×2  
Basis + Kleber: borrowed_candle, counter_ward, deskward, paper_cut ×4, permit_a38, strong_binder  
Relikte: contempt_ledger_nail, threshold_ward, sootglass_lens

**lien_a2** (Lien, Akt 2, 19 Karten)  
Familie: foreclosure, foreclosure+ ×2, forfeit_seal ×2, grave_lien+ ×2, mortgage_sigil ×2, seizure_writ+  
Basis + Kleber: counter_ward, cower_behind_a_desk ×3, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: tarnished_bell, broken_granary_seal

**lien_a3** (Lien, Akt 3, 22 Karten)  
Familie: debt_ouroboros, foreclosure+ ×3, forfeit_seal ×2, grave_lien+ ×3, mortgage_sigil, mortgage_sigil+, mortgaged_aegis+, seizure_writ+  
Basis + Kleber: borrowed_candle, counter_ward, cower_behind_a_desk ×2, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: tarnished_bell, broken_granary_seal, sootglass_lens

**citation_a2** (Citation, Akt 2, 19 Karten)  
Familie: blood_marginalia ×2, contempt_finding, silent_hearing, silent_hearing+ ×2, standing_citation+, witchmark_citation, witchmark_citation+ ×2  
Basis + Kleber: counter_ward, cower_behind_a_desk, deskward, paper_cut ×4, permit_a38, strong_binder  
Relikte: tarnished_bell, sootglass_lens

**citation_a3** (Citation, Akt 3, 22 Karten)  
Familie: blood_marginalia ×3, contempt_finding, contempt_finding+, exemplary_sentence+, silent_hearing+ ×3, standing_citation+, witchmark_citation+ ×3  
Basis + Kleber: borrowed_candle, counter_ward, deskward, paper_cut ×4, permit_a38, strong_binder  
Relikte: tarnished_bell, sootglass_lens, index_bone

**blood_ink_a2** (Blood Ink, Akt 2, 19 Karten)  
Familie: blood_marginalia, blood_marginalia+ ×2, blood_redaction+, proxy_curse ×2, sanguine_errata, sanguine_errata+ ×2, vein_register  
Basis + Kleber: counter_ward, cower_behind_a_desk ×3, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: tarnished_bell, sootglass_lens

**blood_ink_a3** (Blood Ink, Akt 3, 22 Karten)  
Familie: blood_marginalia ×2, blood_redaction+, blood_tithe+ ×3, proxy_curse, sanguine_errata+ ×3, vein_register, vital_census+ ×2  
Basis + Kleber: borrowed_candle, counter_ward, cower_behind_a_desk ×2, deskward, paper_cut ×2, permit_a38, strong_binder  
Relikte: tarnished_bell, sootglass_lens, index_bone

**plain_a2** (plain, Akt 2, 19 Karten)  
Familie: borrowed_candle, dawn_summons+, false_signature, marginalia, notary_beetle, tallow_budget ×2, violence_allowance  
Basis + Kleber: counter_ward+ ×2, cower_behind_a_desk ×3, deskward+ ×2, paper_cut ×2, permit_a38, strong_binder  
Relikte: iron_prayer_bead, lead_counterweight

**plain_a3** (plain, Akt 3, 22 Karten)  
Familie: blood_testimony+, dawn_summons+, false_signature, grievance_ledger+, marginalia, notary_beetle, tallow_budget, tallow_budget+  
Basis + Kleber: borrowed_candle, borrowed_candle+, counter_ward, counter_ward+ ×2, cower_behind_a_desk ×2, deskward, deskward+ ×2, paper_cut ×2, permit_a38, strong_binder  
Relikte: iron_prayer_bead, lead_counterweight, index_bone
