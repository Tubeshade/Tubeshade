ARG DOTNET_CHANNEL="10.0"
ARG DOTNET_SDK_VERSION="${DOTNET_CHANNEL}.101"
ARG DOTNET_RUNTIME_VERSION="${DOTNET_CHANNEL}.1"
ARG DOTNET_RUNTIME="linux-musl-x64"
ARG ALPINE_VERSION="3.22"
ARG BUILD_NUMBER="1"

FROM ghcr.io/tubeshade/tubeshade-build:${DOTNET_SDK_VERSION} AS build
ARG BUILD_NUMBER
ARG DOTNET_RUNTIME

WORKDIR /tubeshade
COPY ./ ./

RUN --mount=type=cache,target=/root/.nuget/packages \
    ./build/publish.sh "Tubeshade.Server" $DOTNET_RUNTIME $BUILD_NUMBER

FROM ghcr.io/tubeshade/tubeshade-runtime-deps:${DOTNET_RUNTIME_VERSION}-alpine${ALPINE_VERSION} AS tubeshade
ARG DOTNET_CHANNEL
ARG DOTNET_RUNTIME

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade
LABEL org.opencontainers.image.title=Tubeshade

WORKDIR /tubeshade

COPY --chmod=-w --from=build [ \
"/tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/Tubeshade.Server", \
"/tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/appsettings.json", \
"/tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/*.xml", \
"./" ]

COPY --chmod=-w --from=build /tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/wwwroot/ ./wwwroot

RUN mkdir /var/opt/tubeshade && chown app:app /var/opt/tubeshade
VOLUME /var/opt/tubeshade

ENV DOTNET_gcServer=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8 \
    Ytdlp__YtdlpPath=/usr/bin/yt-dlp \
    Ytdlp__FfmpegPath=/usr/bin/ffmpeg \
    Ytdlp__FfprobePath=/usr/bin/ffprobe \
    Ytdlp__JavascriptRuntimePath=/usr/bin/deno

USER app
VOLUME /home/app
EXPOSE 8080
HEALTHCHECK CMD test $(wget -qO- http://localhost:8080/healthz) = "Healthy" || exit 1

ENTRYPOINT ["./Tubeshade.Server"]
