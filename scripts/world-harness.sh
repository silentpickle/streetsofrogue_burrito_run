#!/bin/bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
cd "$project_dir"
export BURRITO_HARNESS_PERFORMANCE=0
export BURRITO_HARNESS_PRESSURE=0
if [ "$#" -eq 1 ] && [ "$1" = '--performance' ]; then
    export BURRITO_HARNESS_PERFORMANCE=1
elif [ "$#" -eq 1 ] && [ "$1" = '--pressure' ]; then
    export BURRITO_HARNESS_PRESSURE=1
elif [ "$#" -ne 0 ]; then echo 'Usage: bash scripts/world-harness.sh [--performance|--pressure]' >&2; exit 2; fi
# Refuse to replace a loaded DLL. Failure to enumerate processes is also a refusal.
set +e
processes="$(pgrep -fl '/StreetsOfRogueMac.app/Contents/MacOS/Streets of Rogue' 2>&1)"
process_result=$?
set -e
if [ "$process_result" -ne 1 ]; then
    echo "Close Streets of Rogue before running the harness. Process check: $processes" >&2
    exit 2
fi
bash scripts/build.sh
export BURRITO_HARNESS_ROOT="$project_dir/.runtime/world-harness/$(date '+%Y%m%d-%H%M%S')-$$"
export BURRITO_PROFILE_PATH="$BURRITO_HARNESS_ROOT/profile"
mkdir -p "$BURRITO_PROFILE_PATH/CloudData" "$BURRITO_PROFILE_PATH/BackupData"
# Copy settings/unlocks only; do not load or replace either existing run/save profile.
for name in GameUnlocks.dat GameSettings.dat; do
    if [ -f "$project_dir/.runtime/profile/CloudData/$name" ]; then
        cp -p "$project_dir/.runtime/profile/CloudData/$name" "$BURRITO_PROFILE_PATH/CloudData/$name"
    fi
done
touch "$BURRITO_PROFILE_PATH/.initialized"
if [ -f "$project_dir/.runtime/profile/TechSettings.dat" ]; then
    cp -p "$project_dir/.runtime/profile/TechSettings.dat" "$BURRITO_PROFILE_PATH/TechSettings.dat"
else
    echo 'Existing test-profile TechSettings.dat is required to skip native first-run setup.' >&2
    exit 2
fi
python3 - "$BURRITO_HARNESS_ROOT" <<'PY'
import hashlib
import json
import os
from pathlib import Path
import sys
root = Path(sys.argv[1])
dll = Path('.tools/loader/BepInEx/plugins/BurritoRun.dll')
(root / 'BurritoRun.dll').write_bytes(dll.read_bytes())
(root / 'harness-run.json').write_text(json.dumps(dict(fixtureRevision=1, repeats=2, performance=os.environ['BURRITO_HARNESS_PERFORMANCE'] == '1',
    pressure=os.environ['BURRITO_HARNESS_PRESSURE'] == '1',
    dllSha256=hashlib.sha256(dll.read_bytes()).hexdigest(), profile=str(root / 'profile')), indent=2) + '\n')
PY
echo "Native World Awareness harness: $BURRITO_HARNESS_ROOT"
set +e
python3 - <<'PY'
import os
import signal
import subprocess
import sys
performance = os.environ['BURRITO_HARNESS_PERFORMANCE'] == '1'
pressure = os.environ['BURRITO_HARNESS_PRESSURE'] == '1'
timeout = 900 if performance else 300 if pressure else 180
args = ['bash', 'scripts/launch.sh', '--burrito-test', '--world-harness']
if performance:
    args.append('--world-performance')
if pressure:
    args.append('--world-pressure')
process = subprocess.Popen(args, start_new_session=True)
try:
    sys.exit(process.wait(timeout=timeout))
except (subprocess.TimeoutExpired, KeyboardInterrupt):
    # Only this harness's process group, including its native child; never an existing game.
    os.killpg(process.pid, signal.SIGTERM)
    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        os.killpg(process.pid, signal.SIGKILL)
        process.wait()
    print(f'Harness stopped or exceeded {timeout} wall seconds. Saved evidence remains incomplete.', file=sys.stderr)
    sys.exit(2)
PY
launch_result=$?
set -e
python3 scripts/report-world-harness.py "$BURRITO_HARNESS_ROOT"
if [ "$launch_result" -ne 0 ]; then exit "$launch_result"; fi
