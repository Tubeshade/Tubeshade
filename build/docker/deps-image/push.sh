#!/bin/sh
set -e

dotnet_runtime_version=$1

image="ghcr.io/tubeshade/tubeshade-runtime-deps"
version_tag="$image:$dotnet_runtime_version"
latest_tag="$image:latest"

docker build \
	--tag "$version_tag" \
	--tag "$latest_tag" \
	--tag "$image" \
	--build-arg "DOTNET_RUNTIME_VERSION=$dotnet_runtime_version" \
	./build/docker/deps-image/

docker push	"$version_tag"
docker push	"$latest_tag"
