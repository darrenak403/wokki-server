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

RUN dotnet restore "src/Wokki.Api/Wokki.Api.csproj"

COPY src/ src/
RUN dotnet publish "src/Wokki.Api/Wokki.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8386
EXPOSE 8386

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Wokki.Api.dll"]
