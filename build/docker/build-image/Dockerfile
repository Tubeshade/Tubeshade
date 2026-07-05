# syntax=docker/dockerfile:1

ARG DOTNET_SDK_VERSION="10.0.301"

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}

ADD --checksum="sha256:bf8aac79b72287a6d2043074415132558b43743a8f9461a22b0141e90f16ce66" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.06.09/yt-dlp_linux /root/yt-dlp
ADD --checksum="sha256:96c9c73fc05ea57603bc28d8071d41e861bd40bcf2a9f849dd5969ed1a2c8498" --chmod="+r-xw" https://github.com/denoland/deno/releases/download/v2.9.0/deno-x86_64-unknown-linux-gnu.zip /root/deno.zip
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
