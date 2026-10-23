# The Lieutenant's build families (lieutenant_master.md §5) as finished decks for the family bench, on the budget
# FAMILY_DECKS.md sets for the Bureaucrat's families: same deck size, same upgrades, same rare ceiling, the act gates of
# LIEUTENANT_PLAN L2. The Bureaucrat's Paperwork and plain decks ride along as the reference the bench divides by.
#
#   python3 lieutenant_decks.py     -> LIEUTENANT_DECKS.json
#   dotnet run --project Converter -c Release -- --family-decks LIEUTENANT_DECKS.json --family-acts 2,3 --family-relics only --bench-shuffles 2
import json, collections

STARTER = {'lt_watch_cut': 4, 'lt_buckler_up': 4, 'lt_first_orders': 1, 'lt_new_stripes': 1}
STARTERS = set(STARTER)
POOL = {}
for c in ['pike_butt', 'cut_the_strap', 'heel_turn', 'clear_the_square', 'clenched_jaw', 'buckle', 'roll_call', 'gatehouse',
          'last_bell', 'scabbard', 'marked_man', 'braced_pike', 'arrest', 'witness', 'signal', 'second_rank', 'whetstone',
          'armguard', 'headlong', 'split_lip']:
    POOL['lt_' + c] = ('common', 1)
for c in ['against_the_wall', 'double_file', 'mind_the_gap', 'cadence', 'no_excuses', 'no_surrender', 'gauntlet',
          'on_your_feet', 'pursuit', 'no_running', 'dare', 'second_blade', 'testimony', 'whistle', 'under_the_colours',
          'flank', 'arms_drill', 'shield_punch', 'scar', 'front_rank', 'ruckus', 'bloody_cobbles', 'broken_nose']:
    POOL['lt_' + c] = ('uncommon', 1)
for c in ['dispatch', 'make_an_example', 'ultimatum', 'remand', 'stand_down', 'overhand', 'heave', 'grip', 'broken_pike',
          'recoil', 'poise', 'overreach']:
    POOL['lt_' + c] = ('uncommon', 2)
for act, names in {1: ['brevet', 'insubordination', 'parade_ground', 'drum_major', 'service_record', 'splintered_shield',
                       'last_ditch', 'open_visor'],
                   2: ['black_gauntlet', 'redress', 'riposte', 'countercharge', 'forced_march', 'reprimand'],
                   3: ['chain_of_command', 'succession', 'hilt_knot', 'pike_square', 'rising_star', 'watchword'],
                   4: ['armsmaster', 'full_tilt', 'blood_debt', 'watch_surgeon', 'field_pardon']}.items():
    for c in names:
        POOL['lt_' + c] = ('rare', act)
RELICS = {'lt_relic_' + r for r in ['tin_fife', 'chipped_buckler', 'split_chinstrap', 'sword_knot', 'warrant_cord',
                                    'parade_spur', 'pay_chest', 'dented_gorget', 'torn_standard', 'full_tang',
                                    'scarlet_epaulette', 'commission', 'lead_knuckles', 'iron_shackle', 'brandy_flask',
                                    'badge_chain', 'worn_epaulette', 'spur_buckle']}

BUDGET = {2: dict(adds=12, removes=3, ups=5, rares=2, relics=2),
          3: dict(adds=16, removes=4, ups=9, rares=3, relics=3)}

def n(**cards): return {'lt_' + k: v for k, v in cards.items()}
def u(*ids): return ['lt_' + i for i in ids]
def r(*ids): return ['lt_relic_' + i for i in ids]

D = {
 'Strength Bruiser': {
  2: (n(watch_cut=2, first_orders=1),
      n(whetstone=2, pike_butt=3, buckle=1, arms_drill=2, clenched_jaw=1, armguard=1, shield_punch=1, last_ditch=1),
      u('pike_butt', 'pike_butt', 'whetstone', 'watch_cut', 'arms_drill'), r('chipped_buckler', 'sword_knot')),
  3: (n(watch_cut=3, first_orders=1),
      n(whetstone=2, pike_butt=3, buckle=1, arms_drill=2, clenched_jaw=2, shield_punch=2, last_ditch=1,
        overhand=1, hilt_knot=1, scar=1),
      u('pike_butt', 'pike_butt', 'pike_butt', 'whetstone', 'whetstone', 'watch_cut', 'arms_drill', 'arms_drill',
        'shield_punch'), r('chipped_buckler', 'sword_knot', 'full_tang'))},
 'By the Book': {
  2: (n(watch_cut=1, buckler_up=2),
      n(roll_call=2, gatehouse=2, scabbard=1, signal=2, cadence=1, under_the_colours=1, against_the_wall=1,
        drum_major=1, parade_ground=1),
      u('gatehouse', 'roll_call', 'first_orders', 'under_the_colours', 'against_the_wall'), r('tin_fife', 'pay_chest')),
  3: (n(watch_cut=1, buckler_up=3),
      n(roll_call=2, gatehouse=2, scabbard=1, signal=2, cadence=1, under_the_colours=1, against_the_wall=2,
        drum_major=1, parade_ground=1, chain_of_command=1, no_excuses=1, last_bell=1),
      u('gatehouse', 'gatehouse', 'roll_call', 'first_orders', 'under_the_colours', 'against_the_wall',
        'against_the_wall', 'watch_cut', 'no_excuses'), r('tin_fife', 'pay_chest', 'commission'))},
 'Duelist': {
  2: (n(buckler_up=2, first_orders=1),
      n(marked_man=2, pike_butt=2, braced_pike=2, gauntlet=1, arrest=1, second_blade=1, witness=1, service_record=1,
        black_gauntlet=1),
      u('marked_man', 'pike_butt', 'braced_pike', 'gauntlet', 'watch_cut'), r('chipped_buckler', 'warrant_cord')),
  3: (n(buckler_up=3, first_orders=1),
      n(marked_man=2, pike_butt=2, braced_pike=2, gauntlet=1, arrest=1, second_blade=2, witness=1, service_record=1,
        black_gauntlet=1, overhand=1, riposte=1, on_your_feet=1),
      u('marked_man', 'marked_man', 'pike_butt', 'braced_pike', 'braced_pike', 'gauntlet', 'watch_cut', 'watch_cut',
        'second_blade'), r('chipped_buckler', 'warrant_cord', 'scarlet_epaulette'))},
 'Strength Spender': {
  2: (n(buckler_up=2, first_orders=1),
      n(whetstone=2, buckle=2, heave=2, grip=1, armguard=2, arms_drill=1, countercharge=1, pike_butt=1),
      u('whetstone', 'heave', 'heave', 'armguard', 'watch_cut'), r('sword_knot', 'chipped_buckler')),
  3: (n(buckler_up=3, first_orders=1),
      n(whetstone=3, buckle=2, heave=2, grip=1, armguard=2, arms_drill=1, countercharge=1, pike_butt=1, hilt_knot=1,
        broken_pike=1, forced_march=1),
      u('whetstone', 'whetstone', 'heave', 'heave', 'armguard', 'armguard', 'watch_cut', 'arms_drill', 'grip'),
      r('sword_knot', 'chipped_buckler', 'full_tang'))},
 'Front Rank': {
  2: (n(watch_cut=2, first_orders=1),
      n(headlong=1, heel_turn=2, front_rank=2, split_lip=1, bloody_cobbles=1, open_visor=1, overreach=1, dare=1,
        clenched_jaw=1, armguard=1),
      u('front_rank', 'front_rank', 'heel_turn', 'dare', 'buckler_up'), r('dented_gorget', 'split_chinstrap')),
  3: (n(watch_cut=3, first_orders=1),
      n(headlong=1, heel_turn=2, front_rank=2, split_lip=1, bloody_cobbles=2, open_visor=1, overreach=1,
        clear_the_square=1, last_ditch=1, broken_nose=1, dare=1, clenched_jaw=1, armguard=1),
      u('front_rank', 'front_rank', 'heel_turn', 'heel_turn', 'dare', 'buckler_up', 'buckler_up', 'bloody_cobbles',
        'overreach'), r('dented_gorget', 'split_chinstrap', 'scarlet_epaulette'))},
 'Shield and Steel': {
  2: (n(watch_cut=2, first_orders=1),
      n(gatehouse=2, against_the_wall=1, no_surrender=1, braced_pike=2, shield_punch=2, armguard=1, buckle=1,
        splintered_shield=1, on_your_feet=1),
      u('gatehouse', 'braced_pike', 'shield_punch', 'buckler_up', 'buckler_up'), r('chipped_buckler', 'pay_chest')),
  3: (n(watch_cut=2, first_orders=1, new_stripes=1),
      n(gatehouse=2, against_the_wall=1, no_surrender=1, braced_pike=2, shield_punch=2, armguard=2, buckle=1,
        splintered_shield=1, on_your_feet=1, pike_square=1, parade_ground=1, poise=1),
      u('gatehouse', 'gatehouse', 'braced_pike', 'shield_punch', 'shield_punch', 'buckler_up', 'buckler_up',
        'armguard', 'pike_square'), r('chipped_buckler', 'pay_chest', 'split_chinstrap'))},
 'Bloodied Veteran': {
  2: (n(watch_cut=2, first_orders=1),
      n(clenched_jaw=2, scar=1, dare=2, broken_nose=1, bloody_cobbles=2, last_ditch=1, pike_butt=2, split_lip=1),
      u('bloody_cobbles', 'bloody_cobbles', 'clenched_jaw', 'dare', 'watch_cut'), r('split_chinstrap', 'dented_gorget')),
  3: (n(watch_cut=3, first_orders=1),
      n(clenched_jaw=2, scar=1, dare=2, broken_nose=1, bloody_cobbles=2, last_ditch=1, pike_butt=2, split_lip=1,
        recoil=1, overhand=1, rising_star=1, signal=1),
      u('bloody_cobbles', 'bloody_cobbles', 'clenched_jaw', 'clenched_jaw', 'dare', 'buckler_up', 'buckler_up',
        'pike_butt', 'recoil'), r('split_chinstrap', 'dented_gorget', 'scarlet_epaulette'))},
 # Unoptimised: one of each of twelve (sixteen) commons, the starter upgraded — whatever comes.
 'Common-heavy': {
  2: (n(watch_cut=1, buckler_up=1, first_orders=1),
      {c: 1 for c in u('pike_butt', 'cut_the_strap', 'clenched_jaw', 'buckle', 'gatehouse', 'last_bell', 'marked_man',
                       'braced_pike', 'arrest', 'whetstone', 'armguard', 'split_lip')},
      u('watch_cut', 'watch_cut', 'buckler_up', 'buckler_up', 'new_stripes'), r('tin_fife', 'chipped_buckler')),
  3: (n(watch_cut=1, buckler_up=2, first_orders=1),
      {c: 1 for c in u('pike_butt', 'cut_the_strap', 'clenched_jaw', 'buckle', 'gatehouse', 'last_bell', 'marked_man',
                       'braced_pike', 'arrest', 'whetstone', 'armguard', 'split_lip', 'heel_turn', 'roll_call',
                       'second_rank', 'headlong')},
      u('watch_cut', 'watch_cut', 'watch_cut', 'buckler_up', 'buckler_up', 'new_stripes', 'pike_butt', 'gatehouse',
        'armguard'), r('tin_fife', 'chipped_buckler', 'split_chinstrap'))},
}

out = []; errors = []
for build, acts in D.items():
    for act, (removed, cards, ups, relics) in acts.items():
        b = BUDGET[act]; name = f"lt_{build.lower().replace(' ', '_').replace('-', '_')}_a{act}"
        if sum(cards.values()) != b['adds']: errors.append(f"{name}: {sum(cards.values())} adds, not {b['adds']}")
        if sum(removed.values()) != b['removes']: errors.append(f"{name}: {sum(removed.values())} removed")
        if len(ups) != b['ups']: errors.append(f"{name}: {len(ups)} upgrades")
        if len(relics) != b['relics'] or any(x not in RELICS for x in relics): errors.append(f"{name}: relics {relics}")
        for c, k in cards.items():
            if c in STARTERS: continue
            if c not in POOL: errors.append(f"{name}: {c} unknown"); continue
            if POOL[c][1] > act: errors.append(f"{name}: {c} is gated at act {POOL[c][1]}")
            if k > (1 if POOL[c][0] == 'rare' else 3): errors.append(f"{name}: {c} x{k}")
        if sum(k for c, k in cards.items() if POOL.get(c, ('',))[0] == 'rare') > b['rares']: errors.append(f"{name}: too many rares")
        deck = collections.Counter(STARTER); deck.subtract(removed)
        for c, k in cards.items(): deck[c] += k
        for x in ups:
            if deck[x] <= 0: errors.append(f"{name}: upgrade {x} not in deck"); continue
            deck[x] -= 1; deck[x + '+'] += 1
        if any(v < 0 for v in deck.values()): errors.append(f"{name}: negative count")
        deck = {k: v for k, v in sorted(deck.items()) if v > 0}
        out.append(dict(name=name, family=build, act=act, character='lieutenant', cards=deck, relics=relics))

for ref in json.load(open('FAMILY_DECKS.json')):
    if ref['family'] in ('Paperwork', 'plain'):
        out.append(dict(ref, character='bureaucrat'))

print("\n".join(errors) or "budget ok")
json.dump(out, open('LIEUTENANT_DECKS.json', 'w'), indent=1)
print(len(out), "decks;", {d['name']: sum(d['cards'].values()) for d in out})
