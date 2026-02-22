ARG DOTNET_RUNTIME_VERSION="10.0.3-alpine3.22"

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade

RUN apk add --no-cache icu-data-full icu-libs tzdata ffmpeg deno
ADD --checksum="sha256:171ddf9831df5dbe286b63f2bbd1e14d01e0cc7794ee9f74f60ea15a75b420bc" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.02.21/yt-dlp_musllinux /usr/bin/yt-dlp
