#!/bin/sh
set -e

project=$1
configuration=${2:-"Release"}

if [ -z "$project" ]; then
	echo "Restoring solution"
	dotnet restore --locked-mode /p:Configuration="${configuration}"
else
	echo "Restoring project $project"
	dotnet restore ./source/"$project"/"$project".csproj --locked-mode /p:Configuration="${configuration}"
fi
