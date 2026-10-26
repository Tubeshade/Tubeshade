# syntax=docker/dockerfile:1

ARG DOTNET_SDK_VERSION="10.0.403"

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}

ADD --checksum="sha256:dcddc44b6151ccd855a9a3c8fcdb00e897bb872ec9f4908b1b708b16eaaaa114" --chmod="+r-xw" https://github.com/yt-dlp/yt-dlp/releases/download/2026.10.21/yt-dlp_linux.zip /root/yt-dlp.zip
ADD --checksum="sha256:e890975bc2549807bbcc0b584ffafda72cc7c93349dd99c80483dd784f6c4609" --chmod="+r-xw" https://github.com/denoland/deno/releases/download/v2.9.8/deno-x86_64-unknown-linux-gnu.zip /root/deno.zip
ADD --checksum="sha256:6fc9d757578949ba3cad2f561f57dfbc16142dbf8d485fe1bf9f0733f23bf9e3" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/releases/download/2.0.1/bgutil-ytdlp-pot-provider.zip /root/bgutil-ytdlp-pot-provider.zip
ADD --checksum="sha256:bae71b7971fa22376af57edca8d3487724ac9a8ada56d1cccf07a547b4b9b62c" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/archive/refs/tags/2.0.1.tar.gz /root/2.0.1.tar.gz

ENV CI=true
RUN --mount=type=cache,id=tubeshade_apt,target=/var/cache/apt,sharing=locked \
    apt-get update \
    && apt-get install -y --no-install-recommends \
    npm \
    zip \
    lintian \
    gettext \
    tree \
    ffmpeg \
    chrpath \
    && rm -rf /var/lib/apt/lists/*

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade
