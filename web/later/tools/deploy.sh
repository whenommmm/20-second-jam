#!/usr/bin/env bash
# Deploys the web build to Vercel production (needs the Vercel CLI, logged in). Live at https://later-alpha.vercel.app
set -euo pipefail
cd "$(dirname "$0")/.."
bash tools/package.sh >/dev/null
rm -rf dist/later && mkdir -p dist/later && unzip -q dist/later-web.zip -d dist/later
cd dist/later && vercel deploy --prod --yes
