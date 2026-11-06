#!/usr/bin/env python3
"""The morning summary of the Lieutenant bot's overnight training (tools/lt_overnight.sh).

usage: lt_overnight_summary.py <bots dir> <act-1 trial log .jsonl> <old act-1 runner .json>

Reads the newest runner results in <bots dir>, the policy validation tables (lt-policy/validation-act*.txt), the
draft model's report and the overnight log, and writes one readable page: Act I per encounter (the new bot, the
old bot, the user's hand log), where the bot still loses to the hand log, the later acts, and what the deck
model learned.
"""
import glob
import json
import os
import sys


def newest(pattern):
    files = sorted(glob.glob(pattern), key=os.path.getmtime)
    return files[-1] if files else None


def human(path):
    out = {}
    if not path or not os.path.exists(path):
        return out
    for line in open(path):
        line = line.strip()
        if not line:
            continue
        r = json.loads(line)
        out.setdefault(r["encounter"], []).append(r["lost"])
    return {k: sum(v) / len(v) for k, v in out.items()}


def encounters(path):
    if not path or not os.path.exists(path):
        return {}
    return {e["Encounter"]: e for e in json.load(open(path))["Encounters"]}


def main():
    bots, log1, old1 = sys.argv[1], sys.argv[2], sys.argv[3]
    print("lieutenant trial bot · overnight summary")
    print("=" * 100)
    night = os.path.join(bots, "overnight")
    state = sorted(glob.glob(os.path.join(night, "state", "*.done")), key=os.path.getmtime)
    if state:
        print("phases done:")
        for s in state:
            print(f"  {os.path.basename(s)[:-5]:<14} {open(s).read().strip()}")
    new1 = newest(os.path.join(bots, "lt-runner-act1-2*.json"))
    if new1 and os.path.abspath(new1) == os.path.abspath(old1):
        new1 = None
    hand = human(log1)
    old = encounters(old1)
    new = encounters(new1)
    print()
    print(f"ACT I · new run: {os.path.basename(new1) if new1 else '(none)'} · old run: {os.path.basename(old1)}")
    print("lost = mean health lost per fight (a loss counts everything); human = the hand-played log (one fight each)")
    print(f"  {'kind':<6} {'encounter':<34} {'new':>6} {'win':>5} {'old':>6} {'human':>6}  verdict")
    worse = []
    better_or_equal = 0
    compared = 0
    for name, e in new.items():
        h = hand.get(name)
        o = old.get(name)
        verdict = ""
        if h is not None:
            compared += 1
            # matches the hand log when within 1 point or 10 %
            if e["LostMean"] <= h + max(1.0, 0.1 * h):
                better_or_equal += 1
                verdict = "matches or beats the user"
            else:
                verdict = f"WORSE than the user by {e['LostMean'] - h:.1f}"
                worse.append((name, e["LostMean"], h))
        old_text = f"{o['LostMean']:6.1f}" if o else "     —"
        h_text = f"{h:6.1f}" if h is not None else "     —"
        print(f"  {e['Kind']:<6} {name:<34} {e['LostMean']:6.1f} {e['WinRate'] * 100:4.0f}% {old_text} {h_text}  {verdict}")
    if new:
        for kind in ["normal", "multi", "elite", "boss"]:
            rows = [e for e in new.values() if e["Kind"] == kind]
            if not rows:
                continue
            mean_new = sum(e["LostMean"] for e in rows) / len(rows)
            olds = [old[e["Encounter"]]["LostMean"] for e in rows if e["Encounter"] in old]
            hs = [hand[e["Encounter"]] for e in rows if e["Encounter"] in hand]
            print(f"  {kind:<6} mean over encounters: new {mean_new:5.1f} · old {sum(olds) / max(1, len(olds)):5.1f} · "
                  f"human {sum(hs) / max(1, len(hs)):5.1f}")
        print(f"\n  the bot matches or beats the hand log on {better_or_equal} of {compared} Act I encounters")
        if worse:
            print("  still worse than the user:")
            for name, n, h in sorted(worse, key=lambda x: x[2] - x[1]):
                print(f"    {name:<34} bot {n:5.1f} · user {h:5.1f}")
    late = newest(os.path.join(bots, "lt-runner-act234-2*.txt")) or newest(os.path.join(bots, "lt-runner-act2-2*.txt"))
    if late:
        print("\nACTS II-IV (a fifth of the fights):", os.path.basename(late))
        for line in open(late):
            if line.strip().startswith(("normal", "multi", "elite", "boss")) or "mean over encounters" in line or line.startswith("── Act"):
                print("  " + line.rstrip())
    for v in sorted(glob.glob(os.path.join(bots, "lt-policy", "validation-act*.txt"))):
        print("\nPOLICY VALIDATION ·", os.path.basename(v), "(the policy alone, held-out seeds)")
        lines = open(v).read().splitlines()
        body = [l for l in lines[3:] if l.strip()]
        try:
            learned = [float(l.split()[5]) for l in body]
            shared = [float(l.split()[4]) for l in body]
            builtin = [float(l.split()[3]) for l in body]
            print(f"  mean score over {len(body)} encounters: learned {sum(learned) / len(learned):.2f} · "
                  f"shared {sum(shared) / len(shared):.2f} · built-in {sum(builtin) / len(builtin):.2f}")
        except (ValueError, IndexError, ZeroDivisionError):
            pass
        for l in lines[:3] + body:
            print("  " + l)
    model = os.path.join(bots, "lt-deck-model.txt")
    if os.path.exists(model):
        print("\nDRAFT MODEL (lt-deck-model.txt, first 60 lines)")
        for l in open(model).read().splitlines()[:60]:
            print("  " + l)
    print("\nfiles: lt-policy/<encounter>.json (weights), lt-policy/validation-act*.txt, lt-deck-model.json/.txt,")
    print("       lt-deck-data.jsonl, lt-card-values.json/.txt, lt-runner-act*-*.txt/.json, overnight/run.log")


if __name__ == "__main__":
    main()
