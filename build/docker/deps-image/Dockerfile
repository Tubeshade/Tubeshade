ARG DOTNET_RUNTIME_VERSION="10.0.9-alpine3.23"

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade

RUN apk add --no-cache icu-data-full icu-libs tzdata ffmpeg deno
ADD --checksum="sha256:74b20e9e0d8948cccd81004cb64576293cd41b8f4e7f5b6bdc4a253c3bb9b79a" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.06.09/yt-dlp_musllinux /usr/bin/yt-dlp
