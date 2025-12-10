#!/bin/bash
set -e

# need to translate relative of input archive path to absolute path in container mount
archive_path=${1//"./"/"/root/"}

mkdir -p ./artifacts

docker run --rm \
	--mount type=bind,source=./version,target=/root/version,readonly \
	--mount type=bind,source=./global.json,target=/root/global.json,readonly \
	--mount type=bind,source=./build/debian,target=/root/build/debian,readonly \
	--mount type=bind,source="$1",target="$archive_path",readonly \
	--mount type=bind,source=./artifacts,target=/artifacts \
	--workdir /root \
	ghcr.io/tubeshade/tubeshade-build:10.0.101 \
	sh -c "./build/debian/debian.sh \"$1\" \"$2\" && mv ./tubeshade.deb /artifacts/tubeshade.deb"
