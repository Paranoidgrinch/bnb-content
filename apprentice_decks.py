# The Sorcerer's Apprentice's build families (master §13 / §16 stage 6) as finished decks for the family bench, on
# the budget FAMILY_DECKS.md sets for the Bureaucrat's families: same deck size, same upgrades, same rare ceiling.
# He has no glue cards of his own (Counter Ward and Deskward are the Bureaucrat's), so his two (three) glue places are
# cards of the build. The Bureaucrat's Paperwork and plain decks ride along as the reference the bench divides by.
#
#   python3 apprentice_decks.py     -> APPRENTICE_DECKS.json
#   dotnet run --project Converter -- --family-decks APPRENTICE_DECKS.json [--family-acts 2]
import json, collections

STARTER = {'ap_candle_stub': 4, 'ap_chalk_line': 4, 'ap_ink_blot': 1, 'ap_flyleaf': 1}
STARTERS = set(STARTER)
COMMONS = {'ap_wax_drop', 'ap_snuffer', 'ap_ink_dish', 'ap_folded_leaf', 'ap_blotting_sand', 'ap_chalk_circle',
           'ap_clay_weight', 'ap_masons_dust', 'ap_open_casement', 'ap_windblown_leaf', 'ap_penny_whistle',
           'ap_wick_trimmer', 'ap_spare_notch', 'ap_pinprick', 'ap_cup_of_water', 'ap_dust_mote'}
UNCOMMONS = {'ap_split_wick', 'ap_cracked_slate', 'ap_crosswind', 'ap_wrong_way_round', 'ap_rubbing', 'ap_watermark',
             'ap_torn_flyleaf', 'ap_footnote', 'ap_candle_both_ends', 'ap_firebreak', 'ap_kiln_shelf',
             'ap_borrowed_stone', 'ap_cold_ash', 'ap_seal_press'}
RARES = {'ap_masters_correction', 'ap_final_examination', 'ap_lessons_ash', 'ap_glass_rod', 'ap_second_hand',
         'ap_fourfold_lesson', 'ap_perfect_copy', 'ap_last_word'}
RELICS = {'ar_sooty_ferrule', 'ar_knotted_wristcord', 'ar_blue_inkstone', 'ar_bent_nib', 'ar_brass_dividers',
          'ar_ash_bookcord', 'ar_feather_catcher', 'ar_chalked_thumbstall', 'ar_bone_spacer'}

# The Bureaucrat's budget (FAMILY_DECKS.md), with his glue places given to the build.
BUDGET = {2: dict(adds=12, removes=3, ups=5, rares=2, relics=2),
          3: dict(adds=16, removes=4, ups=9, rares=3, relics=3)}

# build: {act: (removed starters, build cards {id: n}, upgraded ids [repeats = copies], relics)}
D = {
 'Short Hand': {
  2: ({'ap_chalk_line': 1, 'ap_ink_blot': 1, 'ap_candle_stub': 1},
      {'ap_penny_whistle': 2, 'ap_pinprick': 2, 'ap_dust_mote': 2, 'ap_split_wick': 1, 'ap_torn_flyleaf': 1,
       'ap_footnote': 1, 'ap_wax_drop': 1, 'ap_ink_dish': 1, 'ap_flyleaf': 1},
      ['ap_candle_stub', 'ap_candle_stub', 'ap_penny_whistle', 'ap_split_wick', 'ap_pinprick'],
      ['ar_sooty_ferrule', 'ar_knotted_wristcord']),
  3: ({'ap_chalk_line': 2, 'ap_ink_blot': 1, 'ap_candle_stub': 1},
      {'ap_penny_whistle': 2, 'ap_pinprick': 2, 'ap_dust_mote': 2, 'ap_split_wick': 2, 'ap_torn_flyleaf': 1,
       'ap_footnote': 1, 'ap_wax_drop': 1, 'ap_ink_dish': 1, 'ap_flyleaf': 1, 'ap_cracked_slate': 1,
       'ap_glass_rod': 1, 'ap_last_word': 1},
      ['ap_candle_stub', 'ap_candle_stub', 'ap_penny_whistle', 'ap_penny_whistle', 'ap_split_wick', 'ap_split_wick',
       'ap_pinprick', 'ap_cracked_slate', 'ap_dust_mote'],
      ['ar_sooty_ferrule', 'ar_knotted_wristcord', 'ar_feather_catcher'])},
 'Long Lesson': {
  2: ({'ap_candle_stub': 1, 'ap_chalk_line': 2},
      {'ap_spare_notch': 2, 'ap_chalk_circle': 2, 'ap_clay_weight': 2, 'ap_kiln_shelf': 1, 'ap_ink_blot': 1,
       'ap_dust_mote': 2, 'ap_glass_rod': 1, 'ap_split_wick': 1},
      ['ap_spare_notch', 'ap_kiln_shelf', 'ap_clay_weight', 'ap_ink_blot', 'ap_candle_stub'],
      ['ar_blue_inkstone', 'ar_chalked_thumbstall']),
  3: ({'ap_candle_stub': 1, 'ap_chalk_line': 3},
      {'ap_spare_notch': 2, 'ap_chalk_circle': 2, 'ap_clay_weight': 2, 'ap_kiln_shelf': 1, 'ap_ink_blot': 2,
       'ap_dust_mote': 2, 'ap_glass_rod': 1, 'ap_split_wick': 1, 'ap_cracked_slate': 1, 'ap_final_examination': 1,
       'ap_ink_dish': 1},
      ['ap_spare_notch', 'ap_spare_notch', 'ap_kiln_shelf', 'ap_clay_weight', 'ap_clay_weight', 'ap_ink_blot',
       'ap_candle_stub', 'ap_candle_stub', 'ap_cracked_slate'],
      ['ar_blue_inkstone', 'ar_chalked_thumbstall', 'ar_bone_spacer'])},
 'Ash Wand': {
  2: ({'ap_chalk_line': 1, 'ap_candle_stub': 1, 'ap_ink_blot': 1},
      {'ap_wick_trimmer': 2, 'ap_candle_both_ends': 2, 'ap_rubbing': 1, 'ap_borrowed_stone': 2, 'ap_lessons_ash': 1,
       'ap_second_hand': 1, 'ap_footnote': 1, 'ap_dust_mote': 1, 'ap_pinprick': 1},
      ['ap_wick_trimmer', 'ap_candle_both_ends', 'ap_borrowed_stone', 'ap_candle_stub', 'ap_candle_stub'],
      ['ar_ash_bookcord', 'ar_sooty_ferrule']),
  3: ({'ap_chalk_line': 2, 'ap_candle_stub': 1, 'ap_ink_blot': 1},
      {'ap_wick_trimmer': 2, 'ap_candle_both_ends': 2, 'ap_rubbing': 1, 'ap_borrowed_stone': 2, 'ap_lessons_ash': 1,
       'ap_second_hand': 1, 'ap_footnote': 2, 'ap_dust_mote': 1, 'ap_pinprick': 2, 'ap_cracked_slate': 1,
       'ap_last_word': 1},
      ['ap_wick_trimmer', 'ap_wick_trimmer', 'ap_candle_both_ends', 'ap_candle_both_ends', 'ap_borrowed_stone',
       'ap_candle_stub', 'ap_candle_stub', 'ap_pinprick', 'ap_cracked_slate'],
      ['ar_ash_bookcord', 'ar_sooty_ferrule', 'ar_chalked_thumbstall'])},
 'Elemental Tempo': {
  2: ({'ap_chalk_line': 2, 'ap_ink_blot': 1},
      {'ap_wax_drop': 2, 'ap_snuffer': 2, 'ap_cup_of_water': 1, 'ap_masons_dust': 1, 'ap_firebreak': 1,
       'ap_cold_ash': 1, 'ap_crosswind': 1, 'ap_watermark': 1, 'ap_fourfold_lesson': 1, 'ap_split_wick': 1},
      ['ap_wax_drop', 'ap_snuffer', 'ap_snuffer', 'ap_candle_stub', 'ap_split_wick'],
      ['ar_sooty_ferrule', 'ar_feather_catcher']),
  3: ({'ap_chalk_line': 3, 'ap_ink_blot': 1},
      {'ap_wax_drop': 2, 'ap_snuffer': 2, 'ap_cup_of_water': 2, 'ap_masons_dust': 1, 'ap_firebreak': 1,
       'ap_cold_ash': 1, 'ap_crosswind': 1, 'ap_watermark': 1, 'ap_fourfold_lesson': 1, 'ap_split_wick': 2,
       'ap_seal_press': 1, 'ap_glass_rod': 1},
      ['ap_wax_drop', 'ap_wax_drop', 'ap_snuffer', 'ap_snuffer', 'ap_candle_stub', 'ap_candle_stub', 'ap_split_wick',
       'ap_split_wick', 'ap_fourfold_lesson'],
      ['ar_sooty_ferrule', 'ar_feather_catcher', 'ar_chalked_thumbstall'])},
 'Modular Wand': {
  2: ({'ap_candle_stub': 1, 'ap_chalk_line': 1, 'ap_ink_blot': 1},
      {'ap_penny_whistle': 2, 'ap_split_wick': 1, 'ap_pinprick': 2, 'ap_chalk_circle': 1, 'ap_seal_press': 1,
       'ap_folded_leaf': 1, 'ap_dust_mote': 1, 'ap_cracked_slate': 1, 'ap_masters_correction': 1, 'ap_footnote': 1},
      ['ap_penny_whistle', 'ap_split_wick', 'ap_seal_press', 'ap_candle_stub', 'ap_pinprick'],
      ['ar_brass_dividers', 'ar_bone_spacer']),
  3: ({'ap_candle_stub': 1, 'ap_chalk_line': 2, 'ap_ink_blot': 1},
      {'ap_penny_whistle': 2, 'ap_split_wick': 2, 'ap_pinprick': 2, 'ap_chalk_circle': 1, 'ap_seal_press': 2,
       'ap_folded_leaf': 1, 'ap_dust_mote': 1, 'ap_cracked_slate': 1, 'ap_masters_correction': 1, 'ap_footnote': 1,
       'ap_final_examination': 1, 'ap_spare_notch': 1},
      ['ap_penny_whistle', 'ap_penny_whistle', 'ap_split_wick', 'ap_split_wick', 'ap_seal_press', 'ap_candle_stub',
       'ap_pinprick', 'ap_pinprick', 'ap_cracked_slate'],
      ['ar_brass_dividers', 'ar_bone_spacer', 'ar_sooty_ferrule'])},
 'Moving Text': {
  2: ({'ap_chalk_line': 2, 'ap_ink_blot': 1},
      {'ap_open_casement': 2, 'ap_torn_flyleaf': 2, 'ap_wrong_way_round': 2, 'ap_perfect_copy': 1,
       'ap_windblown_leaf': 1, 'ap_ink_dish': 1, 'ap_dust_mote': 1, 'ap_split_wick': 1, 'ap_folded_leaf': 1},
      ['ap_open_casement', 'ap_torn_flyleaf', 'ap_wrong_way_round', 'ap_candle_stub', 'ap_candle_stub'],
      ['ar_bent_nib', 'ar_knotted_wristcord']),
  3: ({'ap_chalk_line': 3, 'ap_ink_blot': 1},
      {'ap_open_casement': 2, 'ap_torn_flyleaf': 2, 'ap_wrong_way_round': 2, 'ap_perfect_copy': 1,
       'ap_windblown_leaf': 1, 'ap_ink_dish': 1, 'ap_dust_mote': 2, 'ap_split_wick': 2, 'ap_folded_leaf': 1,
       'ap_cracked_slate': 1, 'ap_masters_correction': 1},
      ['ap_open_casement', 'ap_open_casement', 'ap_torn_flyleaf', 'ap_wrong_way_round', 'ap_candle_stub',
       'ap_candle_stub', 'ap_split_wick', 'ap_split_wick', 'ap_cracked_slate'],
      ['ar_bent_nib', 'ar_knotted_wristcord', 'ar_sooty_ferrule'])},
 # Unoptimised: one of each common in list order, the starter upgraded — what a player who takes whatever comes gets.
 'Common-heavy': {
  2: ({'ap_candle_stub': 1, 'ap_chalk_line': 1, 'ap_ink_blot': 1},
      {c: 1 for c in ['ap_wax_drop', 'ap_snuffer', 'ap_ink_dish', 'ap_folded_leaf', 'ap_blotting_sand',
                      'ap_chalk_circle', 'ap_clay_weight', 'ap_masons_dust', 'ap_open_casement', 'ap_windblown_leaf',
                      'ap_penny_whistle', 'ap_wick_trimmer']},
      ['ap_candle_stub', 'ap_candle_stub', 'ap_chalk_line', 'ap_chalk_line', 'ap_flyleaf'],
      ['ar_feather_catcher', 'ar_chalked_thumbstall']),
  3: ({'ap_candle_stub': 1, 'ap_chalk_line': 2, 'ap_ink_blot': 1},
      {c: 1 for c in sorted(COMMONS)},
      ['ap_candle_stub', 'ap_candle_stub', 'ap_candle_stub', 'ap_chalk_line', 'ap_chalk_line', 'ap_flyleaf',
       'ap_wax_drop', 'ap_snuffer', 'ap_pinprick'],
      ['ar_feather_catcher', 'ar_chalked_thumbstall', 'ar_sooty_ferrule'])},
}

out = []; errors = []
for build, acts in D.items():
    for act, (removed, cards, ups, relics) in acts.items():
        b = BUDGET[act]; name = f"ap_{build.lower().replace(' ', '_').replace('-', '_')}_a{act}"
        if sum(cards.values()) != b['adds']: errors.append(f"{name}: {sum(cards.values())} adds, not {b['adds']}")
        if sum(removed.values()) != b['removes']: errors.append(f"{name}: {sum(removed.values())} removed")
        if len(ups) != b['ups']: errors.append(f"{name}: {len(ups)} upgrades")
        if len(relics) != b['relics'] or any(r not in RELICS for r in relics): errors.append(f"{name}: relics {relics}")
        for c, n in cards.items():
            if c not in STARTERS | COMMONS | UNCOMMONS | RARES: errors.append(f"{name}: {c} unknown"); continue
            if n > (1 if c in RARES else 3): errors.append(f"{name}: {c} x{n}")
        if sum(n for c, n in cards.items() if c in RARES) > b['rares']: errors.append(f"{name}: too many rares")
        deck = collections.Counter(STARTER); deck.subtract(removed)
        for c, n in cards.items(): deck[c] += n
        for u in ups:
            if deck[u] <= 0: errors.append(f"{name}: upgrade {u} not in deck"); continue
            deck[u] -= 1; deck[u + '+'] += 1
        deck = {k: v for k, v in sorted(deck.items()) if v > 0}
        if any(v < 0 for v in deck.values()): errors.append(f"{name}: negative count")
        out.append(dict(name=name, family=build, act=act, character='sorcerers_apprentice', cards=deck, relics=relics))

# The reference: the Bureaucrat's Paperwork deck (the bench divides by it) and his plain deck.
for ref in json.load(open('FAMILY_DECKS.json')):
    if ref['family'] in ('Paperwork', 'plain'):
        out.append(dict(ref, character='bureaucrat'))

print("\n".join(errors) or "budget ok")
json.dump(out, open('APPRENTICE_DECKS.json', 'w'), indent=1)
print(len(out), "decks;", {d['name']: sum(d['cards'].values()) for d in out})
