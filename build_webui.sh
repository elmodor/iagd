#!/bin/bash
set -e

pushd WebUI
npm ci
npm run build
popd
