#!/usr/bin/env python3
"""What the encounter trials say (bnb-content EncounterTrial, bnb-godot TrialLog).

Reads the trial notebook the game writes — one JSON line per fight — and prints, per encounter, how often it was
fought and won, the health it cost and the rounds it took, against the agreed cost per fight (user, 2026-10-31):
normal 3/4/5/6, multi 6/8/10/12, elite 10/12/14/16, boss 20/30/40/50 for Acts I-IV.

    tools/trial_report.py [trial-log.jsonl ...]

Without a path it reads the Godot user folder of this machine. A fight lost and retried counts as two attempts;
the cost of a lost fight is everything the Lieutenant had.
"""
import collections
import json
import os
import statistics
import sys

DEFAULT = os.path.expanduser('~/.local/share/godot/app_userdata/Bureaucrats & Broomsticks/trial-log.jsonl')
TARGET = {1: dict(normal=3, multi=6, elite=10, boss=20), 2: dict(normal=4, multi=8, elite=12, boss=30),
          3: dict(normal=5, multi=10, elite=14, boss=40), 4: dict(normal=6, multi=12, elite=16, boss=50)}
ROLE = {'combat': 'normal', 'multi-combat': 'multi', 'elite': 'elite', 'boss': 'boss'}
ORDER = ['normal', 'multi', 'elite', 'boss']


def main(paths):
    rows = []
    for path in paths or [DEFAULT]:
        with open(path, encoding='utf-8') as f:
            rows += [json.loads(line) for line in f if line.strip()]
    if not rows:
        print('no fights written down yet')
        return
    runs = len({r['seed'] for r in rows})
    print(f'{len(rows)} fights from {runs} trial run(s)\n')
    by = collections.defaultdict(list)
    for r in rows:
        by[(r['act'], ROLE.get(r['kind'], r['kind']), r['encounter'])].append(r)
    for act in sorted({k[0] for k in by}):
        print(f'── Act {act} ── lost per fight (target) · won/fought · rounds')
        for role in ORDER:
            keys = sorted(k for k in by if k[0] == act and k[1] == role)
            if not keys:
                continue
            means = []
            for key in keys:
                fights = by[key]
                lost = statistics.mean(r['lost'] for r in fights)
                means.append(lost)
                won = sum(r['won'] for r in fights)
                rounds = statistics.mean(r['rounds'] for r in fights)
                worst = max(r['lost'] for r in fights)
                print(f'  {role:6} {key[2]:46} {lost:5.1f} ({TARGET[act][role]:2})  {won}/{len(fights)}  '
                      f'{rounds:4.1f}r  worst {worst}')
            print(f'  {role:6} {"— mean over encounters":46} {statistics.mean(means):5.1f} ({TARGET[act][role]:2})'
                  f'  spread {min(means):.0f}..{max(means):.0f}\n')


if __name__ == '__main__':
    main(sys.argv[1:])
