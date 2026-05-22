# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
WORKDIR /repo

COPY src/dotnet/src/CarbonOps.Domain/CarbonOps.Domain.csproj src/dotnet/src/CarbonOps.Domain/
COPY src/dotnet/src/CarbonOps.Contracts/CarbonOps.Contracts.csproj src/dotnet/src/CarbonOps.Contracts/
COPY src/dotnet/src/CarbonOps.Application/CarbonOps.Application.csproj src/dotnet/src/CarbonOps.Application/
COPY src/dotnet/src/CarbonOps.Infrastructure/CarbonOps.Infrastructure.csproj src/dotnet/src/CarbonOps.Infrastructure/
COPY src/dotnet/src/CarbonOps.Api/CarbonOps.Api.csproj src/dotnet/src/CarbonOps.Api/

RUN dotnet restore src/dotnet/src/CarbonOps.Api/CarbonOps.Api.csproj

FROM restore AS build
ARG BUILD_CONFIGURATION=Release

COPY src/dotnet/src/ src/dotnet/src/

RUN dotnet build src/dotnet/src/CarbonOps.Api/CarbonOps.Api.csproj \
    --configuration "${BUILD_CONFIGURATION}" \
    --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish src/dotnet/src/CarbonOps.Api/CarbonOps.Api.csproj \
    --configuration "${BUILD_CONFIGURATION}" \
    --no-build \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "CarbonOps.Api.dll"]
