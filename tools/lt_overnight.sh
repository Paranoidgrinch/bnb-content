#!/usr/bin/env bash
# The Lieutenant trial bot's overnight training (PlayPolicy, PolicyTraining, DeckModel; LT_RUNNER_PLAN.md).
#
#   start:   setsid nohup tools/lt_overnight.sh >> ~/Desktop/bnb-balance/bots/overnight/run.log 2>&1 < /dev/null &
#   resume:  the same command. Every phase is a step that leaves a marker in overnight/state/ when done; a phase
#            cut short resumes from its checkpoints (policies: lt-policy/ckpt/ per generation and lt-policy/<enc>.json
#            per encounter; deck rounds: the rows in lt-deck-data.jsonl per batch of progressions).
#   smoke:   LT_SMOKE=1 tools/lt_overnight.sh  (two encounters, a few generations, everything in a scratch folder)
#
# The binaries are copied once into overnight/bin, so a rebuild during the night does not change what runs.
set -u
REPO="$(cd "$(dirname "$0")/.." && pwd)"
BAL="${LT_BALANCE:-$HOME/Desktop/bnb-balance}"
BOTS="${LT_BOTS:-$BAL/bots}"
NIGHT="$BOTS/overnight"
SMOKE="${LT_SMOKE:-0}"
JOBS="${LT_JOBS:-8}"
STAMP_DAY="$(date +%Y%m%d)"
[ -f "$NIGHT/day" ] && STAMP_DAY="$(cat "$NIGHT/day")"
mkdir -p "$NIGHT/state"
echo "$STAMP_DAY" > "$NIGHT/day"
POL="$BOTS/lt-policy"
MODEL="$BOTS/lt-deck-model.json"
DATA="$BOTS/lt-deck-data.jsonl"
HUMAN1="$BAL/trials/act1-20261101.jsonl"
HUMAN2="$BAL/trials/act2-20261102.jsonl"
# The final validation must start by this time (epoch seconds) to be done by morning; optional phases are skipped
# when they would push past it.
FINAL_BY="${LT_FINAL_BY:-$(date -d 'tomorrow 05:30' +%s)}"

if [ ! -x "$NIGHT/bin/BnbContent.Converter" ] && [ ! -f "$NIGHT/bin/BnbContent.Converter.dll" ]; then
  mkdir -p "$NIGHT/bin"
  cp -r "$REPO"/Converter/bin/Release/*/. "$NIGHT/bin/"
fi
CONV=(dotnet "$NIGHT/bin/BnbContent.Converter.dll" --data "$REPO/source-data" --out "$NIGHT/game.json" --seed 20260717 --jobs "$JOBS")

say() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }
done_() { [ -f "$NIGHT/state/$1.done" ]; }
mark() { date '+%Y-%m-%d %H:%M:%S' > "$NIGHT/state/$1.done"; }
left_min() { echo $(( (FINAL_BY - $(date +%s)) / 60 )); }

# step <name> <command...>: run once, mark on success
step() {
  local name="$1"; shift
  if done_ "$name"; then say "skip $name (done $(cat "$NIGHT/state/$name.done"))"; return 0; fi
  say "start $name: $*"
  if "$@"; then mark "$name"; say "done $name"; else say "FAILED $name (exit $?)"; return 1; fi
}

if [ "$SMOKE" = 1 ]; then
  ONLY=(--runner-only city_normal_appeal_05,city_elite_enforcement_02)
  T1=(--train-gens 2 --shared-gens 1 --train-pop 6 --train-elite 3 --train-seeds 4 --valid-seeds 6 --train-decks 2 --train-perturn 20)
  T2=("${T1[@]}")
  D1=(--deck-progressions 8 --deck-seeds 1)
  D2=(--deck-progressions 4 --deck-seeds 1)
  FINAL1=(--runner-paths 1 --runner-seeds 2)
  FINAL2=(--runner-paths 1 --runner-seeds 1)
  ACTS_LATE="2"
  ONLY_LATE=(--runner-only archives_returns_01)
else
  ONLY=()
  ONLY_LATE=()
  # Act I properly first: ~150 minutes, pop 12, 8 training and 16 validation seeds per encounter.
  T1=(--train-gens 14 --shared-gens 10 --train-pop 12 --train-elite 4 --train-seeds 8 --valid-seeds 16 --train-decks 4
      --train-perturn 24 --train-minutes 170)
  # Later acts: leaner, by budget.
  T2=(--train-gens 8 --shared-gens 6 --train-pop 10 --train-elite 3 --train-seeds 6 --valid-seeds 10 --train-decks 3
      --train-perturn 20)
  D1=(--deck-progressions 120 --deck-seeds 2)
  D2=(--deck-progressions 48 --deck-seeds 1)
  FINAL1=(--runner-paths 5 --runner-seeds 20)
  FINAL2=(--runner-paths 5 --runner-seeds 4)
  ACTS_LATE="2 3 4"
fi
COMMON=(--policy-dir "$POL" --deck-model "$MODEL" --runner-values "$BOTS/lt-card-values.json")

say "overnight · repo $(git -C "$REPO" rev-parse --short HEAD) · bots $BOTS · final by $(date -d @"$FINAL_BY" '+%H:%M') · smoke $SMOKE"

# 1. Act I: play policies, then a deck round, then the policies again on the model's decks (warm, short)
step a1-train "${CONV[@]}" --lt-train --acts 1 "${ONLY[@]}" "${T1[@]}" "${COMMON[@]}" --compare "$HUMAN1"
step a1-decks "${CONV[@]}" --lt-decks --acts 1 --deck-round 1 "${D1[@]}" "${COMMON[@]}" --deck-data "$DATA" --train-perturn 24

# 2. Acts II-IV, each: play policies on decks the model builds, then a deck round that adds that act's rows
round=2
for act in $ACTS_LATE; do
  case "$act" in 2) mins=70 ;; 3) mins=70 ;; 4) mins=95 ;; *) mins=40 ;; esac
  [ "$SMOKE" = 1 ] && mins=0
  need=$(( mins + 15 + 75 ))   # this act, its deck round, and the final validation still to come
  if ! done_ "a$act-train" && [ "$SMOKE" != 1 ] && [ "$(left_min)" -lt "$need" ]; then
    mins=$(( $(left_min) - 90 ))
    if [ "$mins" -lt 15 ]; then say "skip act $act training: $(left_min) min left before the final validation"; continue; fi
    say "act $act: budget cut to $mins min"
  fi
  step "a$act-train" "${CONV[@]}" --lt-train --acts "$act" "${ONLY_LATE[@]}" "${T2[@]}" --train-minutes "$mins" "${COMMON[@]}" --compare "$HUMAN2"
  if [ "$SMOKE" = 1 ] || [ "$(left_min)" -gt 90 ]; then
    step "a$act-decks" "${CONV[@]}" --lt-decks --acts "$act" --deck-round "$round" "${D2[@]}" "${COMMON[@]}" --deck-data "$DATA" --train-perturn 20
  fi
  round=$(( round + 1 ))
done

# 3. Act I again on the model's decks, warm from its own policies, if there is time (short: 3 generations each)
if [ "$SMOKE" != 1 ] && [ "$(left_min)" -gt 120 ]; then
  step a1-warm "${CONV[@]}" --lt-train --acts 1 "${T1[@]}" --train-gens 3 --train-mingens 1 --shared-gens 2 --train-minutes $(( $(left_min) - 100 > 60 ? 60 : $(left_min) - 100 )) --train-warm "${COMMON[@]}" --compare "$HUMAN1"
fi
# 4. Act V, if there is still time
if [ "$SMOKE" != 1 ] && [ "$(left_min)" -gt 110 ]; then
  step a5-train "${CONV[@]}" --lt-train --acts 5 "${T2[@]}" --train-minutes 25 "${COMMON[@]}"
fi

# 5. The end of the night: Act I at full defaults against the user's log, then Acts II-IV at a fifth of the fights
step final-a1 "${CONV[@]}" --lt-runner --acts 1 "${ONLY[@]}" "${FINAL1[@]}" "${COMMON[@]}" --choice-k 0 --choice-top 4 \
  --compare "$HUMAN1" --runner-out "$BOTS"
step final-a234 "${CONV[@]}" --lt-runner --acts "$(echo $ACTS_LATE | tr ' ' ',')" "${ONLY_LATE[@]}" "${FINAL2[@]}" "${COMMON[@]}" \
  --choice-k 0 --choice-top 4 --compare "$HUMAN2" --runner-out "$BOTS"

# 6. The morning summary
python3 "$REPO/tools/lt_overnight_summary.py" "$BOTS" "$HUMAN1" "$BOTS/lt-runner-act1-20261106-1738.json" \
  > "$BOTS/overnight-summary-$STAMP_DAY.txt" 2>&1
say "summary: $BOTS/overnight-summary-$STAMP_DAY.txt"
say "overnight done"
