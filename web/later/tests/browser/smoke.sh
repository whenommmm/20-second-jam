#!/usr/bin/env bash
# Runs every in-page check in headless Chrome against a local server. Prints PASS/FAIL per check.
# Usage (from web/later): npm run smoke        Add a mode name to run just that check: npm run smoke -- dodge
set -u
DIR="$(cd "$(dirname "$0")/../.." && pwd)"
CHROME="${CHROME:-/Applications/Google Chrome.app/Contents/MacOS/Google Chrome}"
PORT=$(python3 -I -c 'import socket; s = socket.socket(); s.bind(("127.0.0.1", 0)); print(s.getsockname()[1])')
python3 -I -m http.server "$PORT" --bind 127.0.0.1 --directory "$DIR" >/dev/null 2>&1 &
SERVER=$!
trap 'kill $SERVER 2>/dev/null' EXIT
for _ in $(seq 50); do curl -sf "http://127.0.0.1:$PORT/index.html" >/dev/null && break; sleep 0.1; done

# name  virtual-time budget (ms)  [window size; headless adds 87 px of frame, so 960,627 is a 960×540 page and 844,477 an 844×390 phone]
CHECKS=(
  "title 2000"
  "dodge 22000"
  "noaudio 22000"
  "late 46500"
  "home 42500"
  "lastonly 42500"
  "thread 33000"
  "threadend 32000"
  "doubletap 32500"
  "fallback 56000"
  "dblturnback 44000"
  "restart 64500"
  "ring 18000"
  "widths 2000"
  "touchmargin 3000 1200,627"
  "touchclamp 4600"
  "pendingcard 30500"
  "taptargets 46800 844,477"
  "fsplace 22500"
  "taptext 1200"
  "clicktext 1200"
  "hidden 9500"
  "touch 3000"
  "fullscreen 22000"
  "smallphone 32500 844,477"
)

failed=0
for check in "${CHECKS[@]}"; do
  read -r name budget size <<<"$check"
  if [ $# -gt 0 ] && [ "$1" != "$name" ]; then continue; fi
  title=$("$CHROME" --headless=new --disable-gpu --hide-scrollbars --window-size="${size:-960,627}" \
    --virtual-time-budget="$budget" --dump-dom "http://127.0.0.1:$PORT/index.html#test-$name" 2>/dev/null \
    | grep -o '<title>[^<]*' | head -1 | sed 's/<title>//')
  if [[ "$title" == OK* ]]; then echo "PASS  $name"; else echo "FAIL  $name  →  ${title:-no title}"; failed=1; fi
done
exit $failed
