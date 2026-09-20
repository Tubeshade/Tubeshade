# syntax=docker/dockerfile:1

ARG DOTNET_SDK_VERSION="10.0.401"

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}

ADD --checksum="sha256:58162f9bfdc27458ea47bfcb311cf47028f17d8154a8bf7d689861d46399230a" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.08.19/yt-dlp_linux /root/yt-dlp
ADD --checksum="sha256:c6527f24f4b16031d3ae4fa9f658d5f11534c8d84ce7dc8502420280919c3490" --chmod="+r-xw" https://github.com/denoland/deno/releases/download/v2.9.7/deno-x86_64-unknown-linux-gnu.zip /root/deno.zip
ADD --checksum="sha256:bce874dfa25896c2798e0f4f8147b7b22e785479eb1e459ab232bf2506c95016" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/releases/download/2.0.0/bgutil-ytdlp-pot-provider.zip /root/bgutil-ytdlp-pot-provider.zip
ADD --checksum="sha256:47038f7e0556a3d689044460f1b6d78381212e1bd04723a27358e1a42279f866" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/archive/refs/tags/2.0.0.tar.gz /root/2.0.0.tar.gz

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
