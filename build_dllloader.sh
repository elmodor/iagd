#!/bin/bash
set -e

pushd DllLoader
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --parallel $(nproc)
popd
