ARG DOTNET_RUNTIME_VERSION="10.0.1-alpine3.22"

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade

RUN apk add --no-cache icu-data-full icu-libs tzdata ffmpeg deno
ADD --checksum="sha256:7822619c8f773aefc93cb2803d53ec260cabbba9c40021b98991e43effc87f7b" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2025.12.08/yt-dlp_musllinux /usr/bin/yt-dlp
