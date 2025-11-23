#!/bin/sh
set -e

project=$1
runtime=$2
build=$3
tag=$4

version=$(tr -d '[:space:]' <version)
publish_dir="./source/${project}/bin/Release/net10.0/${runtime}/publish"
archive_name="${project}_${runtime}.zip"

./build/restore.sh "${project}"

echo "Publishing project ${project} for runtime ${runtime}; build ${build}; tag '${tag}'"

dotnet publish \
	./source/"${project}"/"${project}".csproj \
	--runtime "${runtime}" \
	--configuration Release \
	--self-contained \
	--no-restore \
	-p:AssemblyVersion="$version.$build" \
	-p:InformationalVersion="$version$tag$runtime" \
	/warnAsError \
	/nologo

(
	cd "$publish_dir" || exit
	zip -r -9 "$archive_name" .
)

echo "artifact=$publish_dir/$archive_name"

if [ -z "$GITHUB_OUTPUT" ]; then
	exit 0
else
	echo "artifact-name=$archive_name" >>"$GITHUB_OUTPUT"
	echo "artifact=$publish_dir/$archive_name" >>"$GITHUB_OUTPUT"
fi
