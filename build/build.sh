#!/bin/bash
set -e

configuration=${1:-"Release"}

./build/restore.sh "" "$configuration"
dotnet build --configuration "$configuration" --no-restore /warnAsError /nologo /clp:NoSummary
