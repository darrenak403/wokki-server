# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "global.json", "./"]
COPY ["Wokki.slnx", "./"]
COPY ["src/Wokki.Common/Wokki.Common.csproj", "src/Wokki.Common/"]
COPY ["src/Wokki.Domain/Wokki.Domain.csproj", "src/Wokki.Domain/"]
COPY ["src/Wokki.Application/Wokki.Application.csproj", "src/Wokki.Application/"]
COPY ["src/Wokki.Infrastructure/Wokki.Infrastructure.csproj", "src/Wokki.Infrastructure/"]
COPY ["src/Wokki.Api/Wokki.Api.csproj", "src/Wokki.Api/"]

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "src/Wokki.Api/Wokki.Api.csproj"

COPY src/ src/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "src/Wokki.Api/Wokki.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:DebugType=None \
    /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8386 \
    DOTNET_gcServer=1 \
    DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

RUN apt-get update \
    && apt-get install -y --no-install-recommends wget libicu-dev libkrb5-3 \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system wokki \
    && useradd --system --gid wokki --uid 10001 --create-home wokki

COPY --from=build --chown=wokki:wokki /app/publish .

USER wokki
EXPOSE 8386

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD wget -qO- http://127.0.0.1:8386/health/ >/dev/null || exit 1

ENTRYPOINT ["dotnet", "Wokki.Api.dll"]
