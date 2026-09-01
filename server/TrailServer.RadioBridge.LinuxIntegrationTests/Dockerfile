FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src
COPY server/TrailServer.RadioContract/TrailServer.RadioContract.csproj server/TrailServer.RadioContract/
COPY server/TrailServer.RadioBridge/TrailServer.RadioBridge.csproj server/TrailServer.RadioBridge/
COPY server/TrailServer.RadioBridge.LinuxIntegrationTests/TrailServer.RadioBridge.LinuxIntegrationTests.csproj server/TrailServer.RadioBridge.LinuxIntegrationTests/
RUN dotnet restore server/TrailServer.RadioBridge.LinuxIntegrationTests/TrailServer.RadioBridge.LinuxIntegrationTests.csproj
COPY server/ server/
RUN dotnet publish server/TrailServer.RadioBridge.LinuxIntegrationTests/TrailServer.RadioBridge.LinuxIntegrationTests.csproj --configuration Release --no-restore --output /out

FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim
WORKDIR /app
COPY --from=build --chown=1654:1654 /out/ ./
USER 1654:1654
ENTRYPOINT ["dotnet", "TrailServer.RadioBridge.LinuxIntegrationTests.dll"]
