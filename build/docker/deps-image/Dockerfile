# syntax=docker/dockerfile:1

ARG DOTNET_RUNTIME_VERSION="10.0.14-alpine3.24"

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION} AS yt-dlp

ADD --checksum="sha256:66358aac681cf9adc2bea09bdf200d45c6ff59b5084e655f9ea1eb53c9ea3140" https://github.com/yt-dlp/yt-dlp/releases/download/2026.10.21/yt-dlp_musllinux.zip /tmp/yt-dlp.zip
RUN unzip -q /tmp/yt-dlp.zip -d /opt/yt-dlp && \
    chmod -R a+rX,go-w /opt/yt-dlp && \
    chmod a+x /opt/yt-dlp/yt-dlp_musllinux

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade

RUN apk add --no-cache icu-data-full icu-libs tzdata ffmpeg deno
COPY --from=yt-dlp /opt/yt-dlp /opt/yt-dlp
ADD --checksum="sha256:6fc9d757578949ba3cad2f561f57dfbc16142dbf8d485fe1bf9f0733f23bf9e3" --chmod="+r-xw" https://github.com/Brainicism/bgutil-ytdlp-pot-provider/releases/download/2.0.1/bgutil-ytdlp-pot-provider.zip /etc/yt-dlp/plugins/bgutil-ytdlp-pot-provider.zip
