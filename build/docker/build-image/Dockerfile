ARG DOTNET_SDK_VERSION="9.0.305"

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}

ENV CI=true
RUN apt-get update && apt-get install -y \
    npm \
    zip \
    lintian \
    gettext \
    tree

LABEL org.opencontainers.image.source=https://github.com/Tubeshade/Tubeshade
