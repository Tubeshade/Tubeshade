#!/bin/bash
set -e

auth=()
if [ -n "$GITHUB_TOKEN" ]; then
	auth=(--header "Authorization: Bearer $GITHUB_TOKEN")
fi

files=$(grep -rlE "uses:" .github --include="*.yml" --include="*.yaml")
actions=$(grep -hoE "uses:[[:space:]]*[^./[:space:]][^@[:space:]]*@[^[:space:]#]+" $files | sed -E 's/uses:[[:space:]]*//' | sort -u)

for action in $actions; do
	name=${action%@*}
	current=${action#*@}
	repository=$(echo "$name" | cut -d / -f 1,2)

	if [[ "$current" =~ ^[0-9a-f]{40}$ ]]; then
		echo "Skipping $name, pinned to commit $current"
		continue
	fi

	latest=$(curl --silent --fail "${auth[@]}" "https://api.github.com/repos/$repository/releases/latest" |
		sed -nE 's/^[[:space:]]*"tag_name":[[:space:]]*"([^"]+)".*/\1/p')
	if [ -z "$latest" ]; then
		echo "Failed to get the latest release of $repository"
		exit 1
	fi

	if [ "$current" == "$latest" ]; then
		continue
	fi

	content="https://raw.githubusercontent.com/$repository/$latest${name#"$repository"}"
	if ! curl --silent --fail --output /dev/null "$content/action.yml" &&
		! curl --silent --fail --output /dev/null "$content/action.yaml"; then
		echo "$name $current -> $latest skipped, action does not exist in $latest"
		continue
	fi

	if [ "${current%%.*}" != "${latest%%.*}" ]; then
		echo "$name $current -> $latest (major)"
	else
		echo "$name $current -> $latest"
	fi

	pattern=$(printf '%s' "$name@$current" | sed 's/[][*^$().+?{}|]/\\&/g')
	sed -i -E "s#(uses:[[:space:]]*)$pattern([[:space:]]|$)#\1$name@$latest\2#" $files
done
