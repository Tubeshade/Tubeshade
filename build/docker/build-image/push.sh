#!/bin/sh
set -e

dotnet_sdk_version=$1

image="ghcr.io/tubeshade/tubeshade-build"
version_tag="$image:$dotnet_sdk_version"
latest_tag="$image:latest"

docker build \
	--tag "$version_tag" \
	--tag "$latest_tag" \
	--tag "$image" \
	--build-arg "DOTNET_SDK_VERSION=$dotnet_sdk_version" \
	./build/docker/build-image/

docker push	"$version_tag"
docker push	"$latest_tag"
