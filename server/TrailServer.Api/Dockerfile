FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src
COPY server/TrailServer.Api/TrailServer.Api.csproj server/TrailServer.Api/
COPY server/TrailServer.RadioBridge/TrailServer.RadioBridge.csproj server/TrailServer.RadioBridge/
COPY server/TrailServer.RadioContract/TrailServer.RadioContract.csproj server/TrailServer.RadioContract/
RUN dotnet restore server/TrailServer.Api/TrailServer.Api.csproj
COPY server/TrailServer.Api/ server/TrailServer.Api/
COPY server/TrailServer.RadioBridge/ server/TrailServer.RadioBridge/
COPY server/TrailServer.RadioContract/ server/TrailServer.RadioContract/
RUN dotnet publish server/TrailServer.Api/TrailServer.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TrailServer.Api.dll"]
