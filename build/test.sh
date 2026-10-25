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
	--configuration "$configuration" \
	--no-build \
	--max-parallel-test-modules 8 \
	--results-directory TestResults \
	--results-directory-layout per-module \
	--coverlet \
	--coverlet-output-format cobertura \
	--report-spekt-junit \
	--report-spekt-junit-filename test-result.junit.xml
