ARG DOTNET_RUNTIME_VERSION="10.0.1-alpine3.22"

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade

RUN apk add --no-cache icu-data-full icu-libs tzdata ffmpeg deno
ADD --checksum="sha256:ab0027f1dadcdd0d95ca3d4b5978c13040d941255b6823c17bbfca9c2fe040c4" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2025.11.12/yt-dlp_musllinux /usr/bin/yt-dlp
