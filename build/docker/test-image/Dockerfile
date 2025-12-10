ARG DOTNET_CHANNEL="10.0"
ARG DOTNET_SDK_VERSION="${DOTNET_CHANNEL}.101"
ARG DOTNET_RUNTIME_VERSION="${DOTNET_CHANNEL}.1"
ARG DOTNET_RUNTIME="linux-x64"
ARG ALPINE_VERSION="3.22"
ARG BUILD_NUMBER="1"

FROM ghcr.io/tubeshade/tubeshade-build:${DOTNET_SDK_VERSION} AS build
ARG BUILD_NUMBER
ARG DOTNET_CHANNEL
ARG DOTNET_RUNTIME

WORKDIR /tubeshade
COPY ./ ./

ADD --checksum="sha256:457f3f2a86f747a5f3b860813b7ac28828c28f92f7c9030c9980b69c6637859f" --chmod="+xr-w" https://github.com/yt-dlp/yt-dlp/releases/download/2025.12.08/yt-dlp_linux /usr/bin/yt-dlp

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
