#!/bin/sh
set -e

version=${1:-$(date +%Y-%m-%d)}

image="ghcr.io/tubeshade/tubeshade-runtime-deps"
version_tag="$image:$version"
latest_tag="$image:latest"

docker build \
	--tag "$version_tag" \
	--tag "$latest_tag" \
	--tag "$image" \
	./build/docker/deps-image/

docker push	"$version_tag"
docker push	"$latest_tag"
