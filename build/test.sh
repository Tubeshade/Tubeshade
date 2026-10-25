#!/bin/bash
set -e -o pipefail

configuration=${1:-"Release"}

# The builds below run in parallel, so prefix their output to tell them apart
prefixed() {
	prefix=$1
	shift
	"$@" 2>&1 | sed -u "s/^/[$prefix] /"
}

prefixed build ./build/build.sh "$configuration" &
build=$!
prefixed release docker build --tag tubeshade-integration-tests ./ &
release=$!
prefixed cover docker build --tag tubeshade-cover-tests --file ./build/docker/test-image/Dockerfile ./ &
cover=$!

wait $build
wait $release
wait $cover

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
	--no-build
