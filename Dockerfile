ARG DOTNET_CHANNEL="9.0"
ARG DOTNET_SDK_VERSION="${DOTNET_CHANNEL}.305"
ARG DOTNET_RUNTIME_VERSION="${DOTNET_CHANNEL}.9"
ARG DOTNET_RUNTIME="linux-musl-x64"
ARG ALPINE_VERSION="3.21"
ARG BUILD_NUMBER="1"

FROM ghcr.io/tubeshade/tubeshade-build:${DOTNET_SDK_VERSION} AS build
ARG BUILD_NUMBER
ARG DOTNET_RUNTIME

WORKDIR /tubeshade
COPY ./ ./

ADD --checksum="sha256:ac20c3f1958c0cb85361cc6af2028c4965f9f489ea739506b9577ec492f22cf6" --chmod="+x" https://github.com/yt-dlp/yt-dlp/releases/download/2025.09.26/yt-dlp_musllinux ./yt-dlp

RUN --mount=type=cache,target=/root/.nuget/packages \
    ./build/publish.sh "Tubeshade.Server" $DOTNET_RUNTIME $BUILD_NUMBER

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}-alpine${ALPINE_VERSION} AS tubeshade
ARG DOTNET_CHANNEL
ARG DOTNET_RUNTIME

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade
LABEL org.opencontainers.image.title=Tubeshade

WORKDIR /tubeshade

COPY --chmod=-w --from=build /tubeshade/yt-dlp ./

RUN apk add --no-cache icu-data-full icu-libs
RUN apk add --no-cache tzdata
RUN apk add --no-cache ffmpeg

COPY --chmod=-w --from=build [ \
"/tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/Tubeshade.Server", \
"/tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/appsettings.json", \
"/tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/*.xml", \
"./" ]

COPY --chmod=-w --from=build /tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/wwwroot/ ./wwwroot

ENV DOTNET_gcServer=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8 \
    Ytdlp__YtdlpPath=/tubeshade/yt-dlp \
    Ytdlp__FfmpegPath=/usr/bin/ffmpeg

USER app
VOLUME /home/app
EXPOSE 8080

ENTRYPOINT ["./Tubeshade.Server"]
