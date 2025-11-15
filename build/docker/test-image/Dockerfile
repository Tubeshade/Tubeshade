ARG DOTNET_CHANNEL="9.0"
ARG DOTNET_SDK_VERSION="${DOTNET_CHANNEL}.306"
ARG DOTNET_RUNTIME_VERSION="${DOTNET_CHANNEL}.10"
ARG DOTNET_RUNTIME="linux-x64"
ARG ALPINE_VERSION="3.21"
ARG BUILD_NUMBER="1"

FROM ghcr.io/tubeshade/tubeshade-build:${DOTNET_SDK_VERSION} AS build
ARG BUILD_NUMBER
ARG DOTNET_CHANNEL
ARG DOTNET_RUNTIME

WORKDIR /tubeshade
COPY ./ ./

RUN touch /usr/bin/yt-dlp && touch /usr/bin/ffmpeg && touch /usr/bin/ffprobe && touch /usr/bin/deno

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
    Ytdlp__FfprobePath=/usr/bin/ffprobe \
    Ytdlp__JavascriptRuntimePath=/usr/bin/deno

CMD ["/tubeshade/build/docker/test-image/cover.sh"]
