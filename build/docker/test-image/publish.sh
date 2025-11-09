#!/bin/sh
set -e

project=$1
runtime=$2
build=$3
configuration="Debug"

version=$(tr -d '[:space:]' <version)

./build/restore.sh "${project}" "$configuration"

echo "Publishing project ${project} for runtime ${runtime}; build ${build}"

dotnet publish \
	./source/"${project}"/"${project}".csproj \
	--runtime "${runtime}" \
	--configuration "$configuration" \
	--self-contained \
	--no-restore \
	-p:AssemblyVersion="$version.${build}" \
	-p:InformationalVersion="$version${runtime}" \
	-p:ContinuousIntegrationBuild=false \
	/nologo
