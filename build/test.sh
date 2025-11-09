#!/bin/bash
set -e

./build/build.sh

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
	--configuration Release \
	--collect:"XPlat Code Coverage" \
	--logger:"junit;LogFilePath=TestResults/test-result.junit.xml" \
	--no-build
