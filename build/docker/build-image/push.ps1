param(
    [String]$Version = (Get-Date -Format "yyyy-MM-dd")
)

$image = "ghcr.io/tubeshade/tubeshade-build"
$version_tag = "${image}:$Version"
$latest_tag = "${image}:latest"

docker build --tag $version_tag --tag $latest_tag --tag $image ./build/docker/build-image/

docker push $version_tag
docker push $latest_tag
