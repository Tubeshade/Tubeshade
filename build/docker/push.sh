#!/bin/sh
set -e

build_number=$1
version=$(tr -d '[:space:]' <version)
tag=${2:-$version}

image="ghcr.io/tubeshade/tubeshade"
version_tag="$image:$tag"
latest_tag="$image:latest"

docker build \
	--tag "$version_tag" \
	--tag "$latest_tag" \
	--tag "$image" \
	--build-arg "BUILD_NUMBER=$build_number" \
	./

docker push	"$version_tag"

if [ "$tag" = "$version" ]; then
	docker push	"latest_tag"
fi
