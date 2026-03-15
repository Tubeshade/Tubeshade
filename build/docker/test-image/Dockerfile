ARG DOTNET_CHANNEL="10.0"
ARG DOTNET_SDK_VERSION="${DOTNET_CHANNEL}.201"
ARG DOTNET_RUNTIME_VERSION="${DOTNET_CHANNEL}.5"
ARG DOTNET_RUNTIME="linux-x64"
ARG ALPINE_VERSION="3.23"
ARG BUILD_NUMBER="1"

FROM ghcr.io/tubeshade/tubeshade-build:${DOTNET_SDK_VERSION} AS build
ARG BUILD_NUMBER
ARG DOTNET_CHANNEL
ARG DOTNET_RUNTIME

WORKDIR /tubeshade
COPY ./ ./

ADD --checksum="sha256:c2b0189f581fe4a2ddd41954f1bcb7d327db04b07ed0dea97e4f1b3e09b5dd8e" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2026.03.17/yt-dlp_linux /usr/bin/yt-dlp

RUN --mount=type=cache,target=/root/.nuget/packages \
    ./build/docker/test-image/publish.sh "Tubeshade.Server" $DOTNET_RUNTIME $BUILD_NUMBER

VOLUME /reports
EXPOSE 8080

ENV DOTNET_CHANNEL=$DOTNET_CHANNEL \
    DOTNET_RUNTIME=$DOTNET_RUNTIME \
    DOTNET_gcServer=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8 \
    Ytdlp__YtdlpPath=/usr/bin/yt-dlp \
    Ytdlp__FfmpegPath=/usr/bin/ffmpeg \
    Ytdlp__FfprobePath=/usr/bin/ffprobe

CMD ["/tubeshade/build/docker/test-image/cover.sh"]
