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
# card -> (rarity, act gate)
POOL = {}
for c in ['wax_drop', 'snuffer', 'ink_dish', 'folded_leaf', 'blotting_sand', 'chalk_circle', 'clay_weight', 'masons_dust',
          'open_casement', 'windblown_leaf', 'penny_whistle', 'wick_trimmer', 'spare_notch', 'pinprick', 'cup_of_water',
          'dust_mote']:
    POOL['ap_' + c] = ('common', 1)
for c in ['split_wick', 'cracked_slate', 'crosswind', 'wrong_way_round', 'rubbing', 'watermark', 'torn_flyleaf', 'footnote',
          'candle_both_ends', 'firebreak', 'kiln_shelf', 'borrowed_stone', 'cold_ash', 'seal_press', 'recitation',
          'steady_hand', 'kindling', 'ember_tongs', 'tracing_paper', 'quill_knife', 'inkhorn']:
    POOL['ap_' + c] = ('uncommon', 1)
for c in ['masters_correction', 'final_examination', 'lessons_ash', 'glass_rod', 'second_hand', 'fourfold_lesson',
          'perfect_copy', 'last_word', 'copybook', 'banked_fire', 'tuning_fork']:
    POOL['ap_' + c] = ('rare', 1)
for c, r, act in [('sealing_wax', 'common', 2), ('drill_book', 'common', 2), ('red_pencil', 'uncommon', 2),
                  ('bookend', 'uncommon', 2), ('pressed_flower', 'uncommon', 2), ('soot_mark', 'uncommon', 2),
                  ('stone_tablet', 'uncommon', 2), ('weathervane', 'uncommon', 2), ('lectern', 'uncommon', 2),
                  ('lead_type', 'uncommon', 2), ('scorch_mark', 'uncommon', 2), ('borrowed_grimoire', 'rare', 2),
                  ('bell_book', 'rare', 2), ('long_recitation', 'rare', 2), ('warding_primer', 'rare', 2),
                  ('examiners_bell', 'rare', 2), ('moss_poultice', 'common', 3), ('wildfire', 'uncommon', 3),
                  ('rain_barrel', 'uncommon', 3), ('standing_stone', 'uncommon', 3), ('burning_glass', 'rare', 3),
                  ('masters_hat', 'rare', 3), ('thunderclap', 'rare', 3), ('millstone', 'rare', 3),
                  ('spring_tide', 'rare', 3), ('slide_rule', 'common', 4), ('compendium', 'uncommon', 4),
                  ('final_draft', 'uncommon', 4), ('magnum_opus', 'rare', 4), ('sorcerers_staff', 'rare', 4),
                  ('unfinished_spell', 'rare', 4), ('apprentices_oath', 'rare', 4)]:
    POOL['ap_' + c] = (r, act)
RELICS = {'ar_sooty_ferrule', 'ar_knotted_wristcord', 'ar_blue_inkstone', 'ar_bent_nib', 'ar_brass_dividers',
          'ar_ash_bookcord', 'ar_feather_catcher', 'ar_chalked_thumbstall', 'ar_bone_spacer', 'ar_pencil_box',
          'ar_wire_spectacles', 'ar_sand_glass', 'ar_pewter_inkwell', 'ar_star_chart', 'ar_tutors_cane', 'ar_spare_wicks',
          'ar_slate_board', 'ar_oil_lamp'}

# The Bureaucrat's budget (FAMILY_DECKS.md), with his glue places given to the build.
BUDGET = {2: dict(adds=12, removes=3, ups=5, rares=2, relics=2),
          3: dict(adds=16, removes=4, ups=9, rares=3, relics=3)}

def n(**cards): return {'ap_' + k: v for k, v in cards.items()}
def u(*ids): return ['ap_' + i for i in ids]
def r(*ids): return ['ar_' + i for i in ids]

# build: {act: (removed starters, build cards {id: n}, upgraded ids [repeats = copies], relics)}  — on the v0.5 pool
D = {
 'Short Hand': {
  2: (n(chalk_line=1, ink_blot=1, candle_stub=1),
      n(penny_whistle=2, pinprick=2, drill_book=2, red_pencil=1, tracing_paper=1, recitation=1, kindling=1,
        dust_mote=1, examiners_bell=1),
      u('candle_stub', 'candle_stub', 'drill_book', 'penny_whistle', 'red_pencil'), r('knotted_wristcord', 'star_chart')),
  3: (n(chalk_line=2, ink_blot=1, candle_stub=1),
      n(penny_whistle=2, pinprick=2, drill_book=2, red_pencil=1, tracing_paper=1, recitation=1, kindling=1,
        dust_mote=1, examiners_bell=1, thunderclap=1, borrowed_grimoire=1, quill_knife=1, soot_mark=1),
      u('candle_stub', 'candle_stub', 'drill_book', 'drill_book', 'penny_whistle', 'penny_whistle', 'red_pencil',
        'pinprick', 'examiners_bell'), r('knotted_wristcord', 'star_chart', 'pencil_box'))},
 'Long Lesson': {
  2: (n(candle_stub=1, chalk_line=2),
      n(spare_notch=2, chalk_circle=1, clay_weight=1, kiln_shelf=1, ink_blot=1, red_pencil=1, bookend=1, dust_mote=1,
        lead_type=1, steady_hand=1, long_recitation=1),
      u('spare_notch', 'kiln_shelf', 'red_pencil', 'bookend', 'candle_stub'), r('blue_inkstone', 'tutors_cane')),
  3: (n(candle_stub=1, chalk_line=3),
      n(spare_notch=2, chalk_circle=1, clay_weight=1, kiln_shelf=1, ink_blot=1, red_pencil=2, bookend=1, lead_type=1,
        steady_hand=1, long_recitation=1, standing_stone=1, copybook=1, millstone=1, dust_mote=1),
      u('spare_notch', 'spare_notch', 'kiln_shelf', 'red_pencil', 'red_pencil', 'bookend', 'candle_stub', 'candle_stub',
        'standing_stone'), r('blue_inkstone', 'tutors_cane', 'sand_glass'))},
 'Ash Wand': {
  2: (n(chalk_line=1, candle_stub=1, ink_blot=1),
      n(wick_trimmer=2, candle_both_ends=2, rubbing=1, borrowed_stone=1, lessons_ash=1, second_hand=1, lectern=1,
        footnote=1, drill_book=1, pinprick=1),
      u('wick_trimmer', 'candle_both_ends', 'borrowed_stone', 'candle_stub', 'lectern'), r('ash_bookcord', 'sooty_ferrule')),
  3: (n(chalk_line=2, candle_stub=1, ink_blot=1),
      n(wick_trimmer=2, candle_both_ends=2, rubbing=1, borrowed_stone=2, lessons_ash=1, second_hand=1, masters_hat=1,
        lectern=1, footnote=1, drill_book=1, pinprick=1, red_pencil=1, moss_poultice=1),
      u('wick_trimmer', 'wick_trimmer', 'candle_both_ends', 'candle_both_ends', 'borrowed_stone', 'candle_stub',
        'candle_stub', 'lectern', 'red_pencil'), r('ash_bookcord', 'sooty_ferrule', 'pewter_inkwell'))},
 'Elemental Tempo': {
  2: (n(chalk_line=2, ink_blot=1),
      n(wax_drop=1, snuffer=2, kindling=1, ember_tongs=2, soot_mark=1, cup_of_water=1, stone_tablet=1,
        fourfold_lesson=1, bell_book=1, split_wick=1),
      u('ember_tongs', 'ember_tongs', 'snuffer', 'kindling', 'candle_stub'), r('sooty_ferrule', 'spare_wicks')),
  3: (n(chalk_line=3, ink_blot=1),
      n(wax_drop=1, snuffer=2, kindling=1, ember_tongs=2, soot_mark=1, cup_of_water=1, stone_tablet=1, wildfire=1,
        rain_barrel=1, fourfold_lesson=1, bell_book=1, burning_glass=1, split_wick=1, firebreak=1),
      u('ember_tongs', 'ember_tongs', 'snuffer', 'snuffer', 'kindling', 'candle_stub', 'candle_stub', 'bell_book',
        'soot_mark'), r('sooty_ferrule', 'spare_wicks', 'feather_catcher'))},
 'Modular Wand': {
  2: (n(candle_stub=1, chalk_line=1, ink_blot=1),
      n(penny_whistle=2, split_wick=1, pinprick=2, chalk_circle=1, seal_press=1, folded_leaf=1, dust_mote=1,
        cracked_slate=1, masters_correction=1, red_pencil=1),
      u('penny_whistle', 'split_wick', 'seal_press', 'candle_stub', 'red_pencil'), r('brass_dividers', 'bone_spacer')),
  3: (n(candle_stub=1, chalk_line=2, ink_blot=1),
      n(penny_whistle=2, split_wick=2, pinprick=2, chalk_circle=1, seal_press=1, folded_leaf=1, cracked_slate=1,
        masters_correction=1, red_pencil=1, warding_primer=1, drill_book=1, examiners_bell=1, moss_poultice=1),
      u('penny_whistle', 'penny_whistle', 'split_wick', 'split_wick', 'seal_press', 'candle_stub', 'pinprick',
        'red_pencil', 'cracked_slate'), r('brass_dividers', 'bone_spacer', 'pewter_inkwell'))},
 'Moving Text': {
  2: (n(chalk_line=2, ink_blot=1),
      n(open_casement=2, torn_flyleaf=2, wrong_way_round=2, perfect_copy=1, windblown_leaf=1, weathervane=1,
        ink_dish=1, drill_book=1, red_pencil=1),
      u('open_casement', 'torn_flyleaf', 'wrong_way_round', 'candle_stub', 'red_pencil'), r('bent_nib', 'knotted_wristcord')),
  3: (n(chalk_line=3, ink_blot=1),
      n(open_casement=2, torn_flyleaf=2, wrong_way_round=2, perfect_copy=1, windblown_leaf=1, weathervane=1,
        ink_dish=1, drill_book=2, red_pencil=1, masters_correction=1, copybook=1, dust_mote=1),
      u('open_casement', 'open_casement', 'torn_flyleaf', 'wrong_way_round', 'candle_stub', 'candle_stub',
        'drill_book', 'drill_book', 'red_pencil'), r('bent_nib', 'knotted_wristcord', 'star_chart'))},
 # Unoptimised: one of each of twelve (sixteen) commons, the starter upgraded — whatever comes.
 'Common-heavy': {
  2: (n(candle_stub=1, chalk_line=1, ink_blot=1),
      {c: 1 for c in u('masons_dust', 'windblown_leaf', 'sealing_wax', 'drill_book', 'wax_drop', 'snuffer', 'pinprick',
                       'dust_mote', 'penny_whistle', 'clay_weight', 'chalk_circle', 'wick_trimmer')},
      u('candle_stub', 'candle_stub', 'chalk_line', 'chalk_line', 'flyleaf'), r('feather_catcher', 'pencil_box')),
  3: (n(candle_stub=1, chalk_line=2, ink_blot=1),
      {c: 1 for c in u('open_casement', 'cup_of_water', 'sealing_wax', 'drill_book', 'wax_drop', 'snuffer', 'pinprick',
                       'dust_mote', 'penny_whistle', 'clay_weight', 'chalk_circle', 'wick_trimmer', 'moss_poultice',
                       'masons_dust', 'windblown_leaf', 'ink_dish')},
      u('candle_stub', 'candle_stub', 'candle_stub', 'chalk_line', 'chalk_line', 'flyleaf', 'open_casement', 'cup_of_water',
        'drill_book'), r('feather_catcher', 'pencil_box', 'spare_wicks'))},
}

out = []; errors = []
for build, acts in D.items():
    for act, (removed, cards, ups, relics) in acts.items():
        b = BUDGET[act]; name = f"ap_{build.lower().replace(' ', '_').replace('-', '_')}_a{act}"
        if sum(cards.values()) != b['adds']: errors.append(f"{name}: {sum(cards.values())} adds, not {b['adds']}")
        if sum(removed.values()) != b['removes']: errors.append(f"{name}: {sum(removed.values())} removed")
        if len(ups) != b['ups']: errors.append(f"{name}: {len(ups)} upgrades")
        if len(relics) != b['relics'] or any(r not in RELICS for r in relics): errors.append(f"{name}: relics {relics}")
        for c, k in cards.items():
            if c in STARTERS: continue
            if c not in POOL: errors.append(f"{name}: {c} unknown"); continue
            if POOL[c][1] > act: errors.append(f"{name}: {c} is gated at act {POOL[c][1]}")
            if k > (1 if POOL[c][0] == 'rare' else 3): errors.append(f"{name}: {c} x{k}")
        if sum(k for c, k in cards.items() if POOL.get(c, ('',))[0] == 'rare') > b['rares']: errors.append(f"{name}: too many rares")
        deck = collections.Counter(STARTER); deck.subtract(removed)
        for c, k in cards.items(): deck[c] += k
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
