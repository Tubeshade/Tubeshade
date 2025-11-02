#!/bin/bash
set -e

./build/build.sh

docker build --tag tubeshade-integration-tests ./

dotnet test \
	-p:CollectCoverage=true \
	-p:BuildInParallel=true \
	-p:ContinuousIntegrationBuild=false \
	-p:DebugType=portable \
	-p:CopyLocalLockFileAssemblies=true \
	-m:8 \
	--configuration Release \
	--collect:"XPlat Code Coverage" \
	--no-build
