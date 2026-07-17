# syntax=docker/dockerfile:1

ARG DOTNET_SDK_VERSION="10.0.302"

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}

ADD --checksum="sha256:6bbb3d314cde4febe36e5fa1d55462e29c974f63444e707871834f6d8cc210ae" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.07.04/yt-dlp_linux /root/yt-dlp
ADD --checksum="sha256:8101865641cbede56f08ad19c0a67a87df84bce127fee0d3e3e1f7467717ffa6" --chmod="+r-xw" https://github.com/denoland/deno/releases/download/v2.9.3/deno-x86_64-unknown-linux-gnu.zip /root/deno.zip
ADD --checksum="sha256:b8ceec7f76143da172aaf5ebeec0c2d218e5680c063b931586bca48567069b38" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/releases/download/1.3.1/bgutil-ytdlp-pot-provider.zip /root/bgutil-ytdlp-pot-provider.zip
ADD --checksum="sha256:b5400343482820062372e997cc0c9ace18637f8e774cf70b22c58b89b99e9abe" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/archive/refs/tags/1.3.1.tar.gz /root/1.3.1.tar.gz

ENV CI=true
RUN --mount=type=cache,id=tubeshade_apt,target=/var/cache/apt,sharing=locked \
    apt-get update && apt-get install -y \
    npm \
    zip \
    lintian \
    gettext \
    tree \
    ffmpeg

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade
