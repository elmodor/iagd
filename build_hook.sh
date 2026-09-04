#!/bin/bash
set -e

pushd HookDll/Hook/
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --parallel $(nproc)
popd
