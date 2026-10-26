#!/bin/sh
set -e

version=${1:-$(date +%Y-%m-%d)}

image="ghcr.io/tubeshade/tubeshade-build"
version_tag="$image:$version"
latest_tag="$image:latest"

docker build \
	--tag "$version_tag" \
	--tag "$latest_tag" \
	--tag "$image" \
	./build/docker/build-image/

docker push	"$version_tag"
docker push	"$latest_tag"
