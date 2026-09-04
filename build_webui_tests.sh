#!/bin/bash
set -e

pushd WebUI
npx playwright install chromium
npm test
popd
