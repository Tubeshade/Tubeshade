# syntax=docker/dockerfile:1

ARG DOTNET_CHANNEL="10.0"
ARG DOTNET_SDK_VERSION="${DOTNET_CHANNEL}.301"
ARG DOTNET_RUNTIME_VERSION="${DOTNET_CHANNEL}.9"
ARG DOTNET_RUNTIME="linux-x64"
ARG ALPINE_VERSION="3.23"
ARG BUILD_NUMBER="1"

FROM ghcr.io/tubeshade/tubeshade-build:${DOTNET_SDK_VERSION} AS build
ARG BUILD_NUMBER
ARG DOTNET_CHANNEL
ARG DOTNET_RUNTIME

WORKDIR /tubeshade

RUN unzip /root/deno.zip deno -d /usr/bin && rm /root/deno.zip && chmod +xr-w /usr/bin/deno

COPY --parents ./.config/dotnet-tools.json ./

RUN dotnet tool restore

COPY ./ ./

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=npm,target=/root/.npm \
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
    Ytdlp__JavascriptRuntimePath=deno:/usr/bin/deno

CMD ["/tubeshade/build/docker/test-image/cover.sh"]
