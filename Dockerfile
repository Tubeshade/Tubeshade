ARG DOTNET_CHANNEL="9.0"
ARG DOTNET_SDK_VERSION="${DOTNET_CHANNEL}.301"
ARG DOTNET_RUNTIME_VERSION="${DOTNET_CHANNEL}.6"
ARG DOTNET_RUNTIME="linux-musl-x64"
ARG ALPINE_VERSION="3.21"
ARG BUILD_NUMBER="1"

FROM ghcr.io/tubeshade/tubeshade-build:${DOTNET_SDK_VERSION} AS build
ARG BUILD_NUMBER
ARG DOTNET_RUNTIME

WORKDIR /tubeshade
COPY ./ ./

RUN --mount=type=cache,target=/root/.nuget/packages \
    ./build/publish.sh "Tubeshade.Server" $DOTNET_RUNTIME $BUILD_NUMBER

FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_RUNTIME_VERSION}-alpine${ALPINE_VERSION} AS tubeshade
ARG DOTNET_CHANNEL
ARG DOTNET_RUNTIME

WORKDIR /tubeshade
COPY --chmod=-w --from=build [ \
"/tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/Tubeshade.Server", \
"/tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/appsettings.json", \
"/tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/*.xml", \
"./" ]

COPY --chmod=-w --from=build /tubeshade/source/Tubeshade.Server/bin/Release/net${DOTNET_CHANNEL}/${DOTNET_RUNTIME}/publish/wwwroot/ ./wwwroot

ENV DOTNET_gcServer=0

USER app
VOLUME /home/app
EXPOSE 8080

ENTRYPOINT ["./Tubeshade.Server"]

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade
LABEL org.opencontainers.image.title=Tubeshade
