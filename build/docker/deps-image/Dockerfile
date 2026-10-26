# syntax=docker/dockerfile:1

ARG DOTNET_RUNTIME_VERSION="10.0.14-alpine3.24"

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade

RUN apk add --no-cache icu-data-full icu-libs tzdata ffmpeg deno
ADD --checksum="sha256:d39487ff5c8eb1254d6f142bea05f495440d87ff529b2df786dcd3b5f66b23a8" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.10.21/yt-dlp_musllinux /usr/bin/yt-dlp
ADD --checksum="sha256:6fc9d757578949ba3cad2f561f57dfbc16142dbf8d485fe1bf9f0733f23bf9e3" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/releases/download/2.0.1/bgutil-ytdlp-pot-provider.zip /etc/yt-dlp/plugins/bgutil-ytdlp-pot-provider.zip
