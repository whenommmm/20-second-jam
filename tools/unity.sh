#!/usr/bin/env bash
# Runs this project in batch-mode Unity, so tests, setup and the build work from the terminal.
#   tools/unity.sh test editmode|playmode [filter]
#   tools/unity.sh run Later.Editor.LaterSetup.RebuildScene
#   tools/unity.sh build
# Batch mode can't open a project the Editor already has open, so close the Editor first.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity}"
cd "$ROOT"
mkdir -p Logs

if [ -f Temp/UnityLockfile ] && lsof Temp/UnityLockfile >/dev/null 2>&1; then
  echo "Close the Unity Editor (this project is open), then run again."
  exit 2
fi

usage() { echo "usage: tools/unity.sh test editmode|playmode [filter] | run <Type.Method> | build" >&2; exit 64; }

# The first compiler error in a log, if there is one.
compile_error() { grep -m1 -E 'error CS[0-9]+' "$1"; }

# Everything runs as WebGL, the jam's only target, so nothing reimports when switching between tests and the build.
batch() { "$UNITY" -batchmode -nographics -projectPath "$ROOT" -buildTarget WebGL "$@"; }

run_method() {
  local method=$1 log=$2
  batch -quit -executeMethod "$method" -logFile "$log"
  local code=$?
  if [ $code -ne 0 ]; then
    compile_error "$log" || grep -m5 -E 'Exception|Error|error' "$log"
    echo "FAIL $method (exit $code, log: $log)"
    exit 1
  fi
  echo "OK $method"
}

case "${1:-}" in
  test)
    case "${2:-}" in editmode) P=EditMode ;; playmode) P=PlayMode ;; *) usage ;; esac
    p=$2
    xml="Logs/$p.xml" log="Logs/$p.log"
    rm -f "$xml"
    filter=()
    [ -n "${3:-}" ] && filter=(-testFilter "$3")
    batch -runTests -testPlatform "$P" -testResults "$xml" -logFile "$log" "${filter[@]+"${filter[@]}"}"
    code=$?
    if [ ! -s "$xml" ]; then
      compile_error "$log" || tail -20 "$log"
      echo "FAIL: no test results (exit $code, log: $log)"
      exit 1
    fi
    python3 -I - "$xml" <<'EOF'
import sys, xml.etree.ElementTree as ET
run = ET.parse(sys.argv[1]).getroot()
total, passed = int(run.get("total", 0)), int(run.get("passed", 0))
failed = [c for c in run.iter("test-case") if c.get("result") not in ("Passed", "Skipped")]
for c in failed:
    msg = c.find("failure/message")
    print(f"FAIL {c.get('fullname')}\n  {(msg.text or '').strip() if msg is not None else c.get('result')}")
if failed or total == 0:
    print(f"{passed}/{total} passed")
    sys.exit(1)
print(f"PASS {passed}/{total}")
EOF
    ;;
  run)
    [ -n "${2:-}" ] || usage
    run_method "$2" "Logs/run.log"
    ;;
  build)
    run_method Later.Editor.LaterBuild.WebGL "Logs/build.log"
    ;;
  *) usage ;;
esac
