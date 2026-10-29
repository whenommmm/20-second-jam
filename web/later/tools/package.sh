#!/usr/bin/env bash
# Builds dist/later-web.zip for itch.io: just the game (no tests, tools or package.json). Upload it as an HTML game.
set -euo pipefail
cd "$(dirname "$0")/.."
rm -rf dist && mkdir -p dist
zip -qr dist/later-web.zip index.html style.css src fonts -x '*.DS_Store'
unzip -l dist/later-web.zip
