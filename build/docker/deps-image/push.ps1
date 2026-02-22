param(
    [Parameter(Mandatory)]
    [String]$DotnetRuntimeVersion = "10.0.3-alpine3.22"
)

$image = "ghcr.io/tubeshade/tubeshade-runtime-deps"
$version_tag = "${image}:$DotnetRuntimeVersion"
$latest_tag = "${image}:latest"

docker build --tag $version_tag --tag $latest_tag --tag $image --build-arg DOTNET_RUNTIME_VERSION=$DotnetRuntimeVersion ./build/docker/deps-image/

docker push $version_tag
docker push $latest_tag
