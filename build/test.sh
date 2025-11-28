#!/bin/bash
set -e

configuration=${1:-"Release"}

./build/build.sh "$configuration"

docker build --tag tubeshade-integration-tests ./
docker build --tag tubeshade-cover-tests --file ./build/docker/test-image/Dockerfile ./

dotnet tool restore

dotnet test \
	-p:CollectCoverage=true \
	-p:BuildInParallel=true \
	-p:ContinuousIntegrationBuild=false \
	-p:DebugType=portable \
	-p:CopyLocalLockFileAssemblies=true \
	-m:8 \
	--configuration "$configuration" \
	--collect:"XPlat Code Coverage" \
	--logger:"junit;LogFilePath=TestResults/test-result.junit.xml" \
	--settings ./tests/coverlet.runsettings \
	--no-build
