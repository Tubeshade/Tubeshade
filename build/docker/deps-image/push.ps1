param(
    [String]$Version = (Get-Date -Format "yyyy-MM-dd")
)

$image = "ghcr.io/tubeshade/tubeshade-runtime-deps"
$version_tag = "${image}:$Version"
$latest_tag = "${image}:latest"

docker build --tag $version_tag --tag $latest_tag --tag $image ./build/docker/deps-image/

docker push $version_tag
docker push $latest_tag
