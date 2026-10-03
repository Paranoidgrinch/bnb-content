import json, re, collections
pool = {}
fam_of = {}
for line in open('/home/paranoia/Desktop/bnb-balance/20261002-family-pool.txt'):
    if line.startswith('== '): fam = line[3:].split(' (')[0]; continue
    m = re.match(r'\s+A(\d) (\d)E (\w+)\s+(\w+)\s+(\S+)', line)
    if m: pool[m.group(5)] = dict(act=int(m.group(1)), cost=int(m.group(2)), rarity=m.group(3), type=m.group(4)); fam_of[m.group(5)] = fam

STARTER = {'paper_cut':4, 'cower_behind_a_desk':4, 'strong_binder':1, 'permit_a38':1}
GLUE = {2: ['counter_ward','deskward'], 3: ['counter_ward','deskward','borrowed_candle']}
BUDGET = {2: dict(adds=10, removes=3, ups=5, rares=2), 3: dict(adds=13, removes=4, ups=9, rares=3)}
RELICS = {
 'Paperwork': (['formkeepers_signet','chancery_scale'], 'concordance_medallion'),
 'Doubt':     (['concordance_medallion','tarnished_bell'], 'sootglass_lens'),
 'Queue':     (['petitioners_token','deferred_signet'], 'index_bone'),
 'Seal':      (['seal_makers_die','tarnished_bell'], 'sootglass_lens'),
 'Archive/Junk': (['archive_key','archive_censer'], 'ashen_wax_knife'),
 'Ward Wax':  (['riddles_third_answer','bruise_cup'], 'blackthorn_brooch'),
 'Censure':   (['contempt_ledger_nail','threshold_ward'], 'sootglass_lens'),
 'Lien':      (['tarnished_bell','broken_granary_seal'], 'sootglass_lens'),
 'Citation':  (['tarnished_bell','sootglass_lens'], 'index_bone'),
 'Blood Ink': (['tarnished_bell','sootglass_lens'], 'index_bone'),
 'plain':     (['iron_prayer_bead','lead_counterweight'], 'index_bone'),
}
# family: {act: (removed basics, family cards {id: n}, upgraded ids [with repeats = copies upgraded])}
D = {
 'Paperwork': {
  2: ({'paper_cut':2,'cower_behind_a_desk':1},
      {'red_ink_doctrine':1,'summary_judgment':1,'form_of_ill_intent':2,'cursed_addendum':2,'inkblot_verdict':2,'occult_precedent':2},
      ['red_ink_doctrine','summary_judgment','form_of_ill_intent','form_of_ill_intent','inkblot_verdict']),
  3: ({'paper_cut':2,'cower_behind_a_desk':2},
      {'red_ink_doctrine':1,'summary_judgment':1,'black_ledger':1,'form_of_ill_intent':2,'cursed_addendum':2,'inkblot_verdict':2,'occult_precedent':2,'hedge_hospitality':1,'broom_dispatch':1},
      ['red_ink_doctrine','summary_judgment','form_of_ill_intent','form_of_ill_intent','inkblot_verdict','inkblot_verdict','cursed_addendum','cursed_addendum','hedge_hospitality'])},
 'Doubt': {
  2: ({'paper_cut':2,'cower_behind_a_desk':1},
      {'petty_objection':3,'fine_print_hex':2,'rebuttal':1,'hex_circular':1,'presumption_of_error':1,'clerical_discretion':2},
      ['petty_objection','petty_objection','rebuttal','hex_circular','fine_print_hex']),
  3: ({'paper_cut':2,'cower_behind_a_desk':2},
      {'petty_objection':3,'fine_print_hex':2,'rebuttal':1,'hex_circular':1,'clerical_discretion':1,'guestbook_oath':1,'due_recompense':1,'hedge_covenant':1,'presumption_of_error':1,'formal_dissent':1},
      ['petty_objection']*3+['fine_print_hex']*2+['rebuttal','due_recompense','hex_circular','guestbook_oath'])},
 'Queue': {
  2: ({'paper_cut':2,'cower_behind_a_desk':1},
      {'deferred_hex':3,'protective_adjournment':2,'backlog_charge':2,'pending_matters':1,'candle_allowance':1,'skeleton_staff':1},
      ['deferred_hex','deferred_hex','protective_adjournment','protective_adjournment','backlog_charge']),
  3: ({'paper_cut':2,'cower_behind_a_desk':2},
      {'deferred_hex':3,'protective_adjournment':3,'backlog_charge':2,'pending_matters':1,'candle_allowance':1,'skeleton_staff':1,'priority_docket':2},
      ['deferred_hex']*3+['protective_adjournment']*3+['backlog_charge']*2+['pending_matters'])},
 'Seal': {
  2: ({'paper_cut':2,'cower_behind_a_desk':1},
      {'waxing_authority':2,'notarial_press':2,'conditional_approval':2,'threefold_injunction':1,'seal_dividend':1,'candle_tribunal':1,'privy_seal':1},
      ['candle_tribunal','notarial_press','notarial_press','waxing_authority','waxing_authority']),
  3: ({'paper_cut':2,'cower_behind_a_desk':2},
      {'waxing_authority':3,'notarial_press':2,'conditional_approval':2,'threefold_injunction':1,'seal_dividend':1,'candle_tribunal':1,'privy_seal':1,'seal_of_concern':1,'notarys_tithe':1},
      ['candle_tribunal']+['notarial_press']*2+['waxing_authority']*3+['conditional_approval']*2+['threefold_injunction'])},
 'Archive/Junk': {
  2: ({'paper_cut':2,'cower_behind_a_desk':1},
      {'cauldron_copy':2,'secure_misfiling':1,'certified_kindling':2,'cinder_warrant':2,'archive_pyre':1,'funeral_index':1,'clerks_familiar':1},
      ['cauldron_copy','cauldron_copy','cinder_warrant','cinder_warrant','archive_pyre']),
  3: ({'paper_cut':2,'cower_behind_a_desk':2},
      {'cauldron_copy':2,'secure_misfiling':2,'certified_kindling':2,'cinder_warrant':2,'archive_pyre':1,'funeral_index':1,'clerks_familiar':1,'wastepaper_bastion':1,'clutter_concordance':1},
      ['cauldron_copy']*2+['cinder_warrant']*2+['certified_kindling']*2+['archive_pyre','funeral_index','clutter_concordance'])},
 'Ward Wax': {
  # only three Ward Wax cards before Act III and no payoff before Act IV: the rest of the slots stay basics' damage
  2: ({'cower_behind_a_desk':1},
      {'waxen_surety':3,'sealed_mantle':3,'tallow_reserve':2},
      ['waxen_surety','waxen_surety','sealed_mantle','sealed_mantle','tallow_reserve']),
  3: ({'cower_behind_a_desk':4},
      {'waxen_surety':3,'sealed_mantle':3,'wax_reliquary':2,'tallow_reserve':2,'consecrated_testament':1,'wax_indemnity':1,'votive_covenant':1},
      ['waxen_surety']*3+['sealed_mantle']*3+['wax_reliquary']*2+['wax_indemnity'])},
 'Censure': {
  2: ({'cower_behind_a_desk':3},
      {'malediction_review':3,'sanctioned_charm':2,'blacklisted':2,'crossed_sigil':1,'reciprocal_edict':1,'countermanded_grace':1},
      ['malediction_review','malediction_review','sanctioned_charm','blacklisted','blacklisted']),
  3: ({'cower_behind_a_desk':4},
      {'malediction_review':3,'sanctioned_charm':2,'blacklisted':3,'crossed_sigil':2,'reciprocal_edict':1,'countermanded_grace':1,'oath_of_refusal':1},
      ['malediction_review']*3+['sanctioned_charm']*2+['blacklisted']*3+['reciprocal_edict'])},
 'Lien': {
  2: ({'paper_cut':2,'cower_behind_a_desk':1},
      {'grave_lien':2,'foreclosure':3,'mortgage_sigil':2,'forfeit_seal':2,'seizure_writ':1},
      ['grave_lien','grave_lien','foreclosure','foreclosure','seizure_writ']),
  3: ({'paper_cut':2,'cower_behind_a_desk':2},
      {'grave_lien':3,'foreclosure':3,'mortgage_sigil':2,'forfeit_seal':2,'seizure_writ':1,'mortgaged_aegis':1,'debt_ouroboros':1},
      ['grave_lien']*3+['foreclosure']*3+['seizure_writ','mortgaged_aegis','mortgage_sigil'])},
 'Citation': {
  2: ({'cower_behind_a_desk':3},
      {'witchmark_citation':3,'silent_hearing':3,'blood_marginalia':2,'contempt_finding':1,'standing_citation':1},
      ['witchmark_citation','witchmark_citation','silent_hearing','silent_hearing','standing_citation']),
  3: ({'cower_behind_a_desk':4},
      {'witchmark_citation':3,'silent_hearing':3,'blood_marginalia':3,'contempt_finding':2,'standing_citation':1,'exemplary_sentence':1},
      ['witchmark_citation']*3+['silent_hearing']*3+['standing_citation','exemplary_sentence','contempt_finding'])},
 'Blood Ink': {
  # Blood Ink feeds on OTHER statuses shrinking; Blood Marginalia (Citation + Blood Ink) is its Act-I/II partner
  2: ({'paper_cut':2,'cower_behind_a_desk':1},
      {'sanguine_errata':3,'proxy_curse':2,'vein_register':1,'blood_redaction':1,'blood_marginalia':3},
      ['sanguine_errata','sanguine_errata','blood_redaction','blood_marginalia','blood_marginalia']),
  3: ({'paper_cut':2,'cower_behind_a_desk':2},
      {'sanguine_errata':3,'blood_tithe':3,'vital_census':2,'blood_marginalia':2,'vein_register':1,'blood_redaction':1,'proxy_curse':1},
      ['sanguine_errata']*3+['blood_tithe']*3+['vital_census']*2+['blood_redaction'])},
 'plain': {
  2: ({'paper_cut':2,'cower_behind_a_desk':1},
      {'dawn_summons':1,'violence_allowance':1,'counter_ward':1,'deskward':1,'tallow_budget':2,'false_signature':1,'notary_beetle':1,'continuance':0,'marginalia':1,'borrowed_candle':1},
      ['dawn_summons','counter_ward','counter_ward','deskward','deskward']),
  3: ({'paper_cut':2,'cower_behind_a_desk':2},
      {'dawn_summons':1,'blood_testimony':1,'grievance_ledger':1,'counter_ward':2,'deskward':2,'tallow_budget':2,'false_signature':1,'marginalia':1,'borrowed_candle':1,'notary_beetle':1},
      ['dawn_summons','blood_testimony','grievance_ledger','counter_ward','counter_ward','deskward','deskward','tallow_budget','borrowed_candle'])},
}
out=[]; errors=[]
for fam, acts in D.items():
    for act,(rem,cards,ups) in acts.items():
        b=BUDGET[act]; name=f"{fam.lower().replace('/','_').replace(' ','_')}_a{act}"
        cards={k:v for k,v in cards.items() if v}
        cards.pop('black_salt',None)
        short=b['adds']-sum(cards.values())
        if short<0 or (short>0 and fam!='Ward Wax'): errors.append(f"{name}: {sum(cards.values())} adds")
        if sum(rem.values())!=b['removes']-short: errors.append(f"{name}: removes {sum(rem.values())}")
        if len(ups)!=b['ups']: errors.append(f"{name}: {len(ups)} ups")
        for c,n in cards.items():
            if c not in pool: errors.append(f"{name}: {c} not in pool"); continue
            if pool[c]['act']>act: errors.append(f"{name}: {c} is act {pool[c]['act']}")
            cap = 1 if pool[c]['rarity']=='rare' or pool[c]['type']=='rite' else 3
            if n>cap: errors.append(f"{name}: {c} x{n} > {cap}")
        rares=sum(n for c,n in cards.items() if c in pool and pool[c]['rarity']=='rare')
        if rares>b['rares']: errors.append(f"{name}: {rares} rares")
        deck=collections.Counter(STARTER); deck.subtract(rem)
        for g in GLUE[act]: deck[g]+=1
        for c,n in cards.items(): deck[c]+=n
        for u in ups:
            if deck[u]<=0: errors.append(f"{name}: upgrade {u} not in deck"); continue
            deck[u]-=1; deck[u+'+']+=1
        deck={k:v for k,v in deck.items() if v>0}
        r2,r3=RELICS[fam]
        out.append(dict(name=name,family=fam,act=act,cards=deck,relics=r2+([r3] if act==3 else [])))
print("\n".join(errors) or "budget ok")
json.dump(out, open('/home/paranoia/bnb-content/FAMILY_DECKS.json','w'), indent=1)
print(len(out), "decks;", {d['name']:sum(d['cards'].values()) for d in out})
