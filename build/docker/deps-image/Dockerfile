# syntax=docker/dockerfile:1

ARG DOTNET_RUNTIME_VERSION="10.0.10-alpine3.24"

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade

RUN apk add --no-cache icu-data-full icu-libs tzdata ffmpeg deno
ADD --checksum="sha256:f7439ec2e3ffe69e06ac233f83f0d9687b89105939129bddcbf74e5de0f2b40e" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.07.04/yt-dlp_musllinux /usr/bin/yt-dlp
ADD --checksum="sha256:b8ceec7f76143da172aaf5ebeec0c2d218e5680c063b931586bca48567069b38" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/releases/download/1.3.1/bgutil-ytdlp-pot-provider.zip /etc/yt-dlp/plugins/bgutil-ytdlp-pot-provider.zip
