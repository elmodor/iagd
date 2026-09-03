#!/bin/bash

IA_VERSION=$(tr -d '\r\n' < version.txt)

git tag "$IA_VERSION"
git push origin "$IA_VERSION"
