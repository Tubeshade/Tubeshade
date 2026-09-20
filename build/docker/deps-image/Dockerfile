# syntax=docker/dockerfile:1

ARG DOTNET_RUNTIME_VERSION="10.0.12-alpine3.24"

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade

RUN apk add --no-cache icu-data-full icu-libs tzdata ffmpeg deno
ADD --checksum="sha256:f3dec9cfeaf304cec98290fe41c6ad465d4b747d302473559643e7af24929722" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.08.19/yt-dlp_musllinux /usr/bin/yt-dlp
ADD --checksum="sha256:bce874dfa25896c2798e0f4f8147b7b22e785479eb1e459ab232bf2506c95016" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/releases/download/2.0.0/bgutil-ytdlp-pot-provider.zip /etc/yt-dlp/plugins/bgutil-ytdlp-pot-provider.zip
