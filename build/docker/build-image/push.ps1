param(
    [Parameter(Mandatory)]
    [String]$DotnetSdkVersion
)

$image = "ghcr.io/tubeshade/tubeshade-build"
$version_tag = "${image}:$DotnetSdkVersion"
$latest_tag = "${image}:latest"

docker build --tag $version_tag --tag $latest_tag --tag $image --build-arg DOTNET_SDK_VERSION=$DotnetSdkVersion ./build/docker/build-image/

docker push $version_tag
docker push $latest_tag
