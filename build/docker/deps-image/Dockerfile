ARG DOTNET_RUNTIME_VERSION="10.0.5-alpine3.23"

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade

RUN apk add --no-cache icu-data-full icu-libs tzdata ffmpeg deno
ADD --checksum="sha256:d2f449e66131d914f1f5e9a7881cdb5203b091a3cb356d43c4e286e03384f0b7" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.03.17/yt-dlp_musllinux /usr/bin/yt-dlp
