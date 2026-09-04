#!/bin/bash
set -e

export MSBUILDDISABLENODEREUSE=1

dotnet test IAGrim-core.Tests.slnx -c Release -r linux-x64

exit 0
