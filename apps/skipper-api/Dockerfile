# Build from the monorepo root: docker build -f apps/skipper-api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore inputs first so source edits reuse dependency layers.
COPY apps/skipper-api/skipper-api.csproj apps/skipper-api/packages.lock.json apps/skipper-api/
RUN dotnet restore apps/skipper-api/skipper-api.csproj --locked-mode

COPY apps/skipper-api/ apps/skipper-api/
RUN dotnet publish apps/skipper-api/skipper-api.csproj --no-restore \
    --configuration Release --output /out/skipper /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
LABEL org.opencontainers.image.title="CritterOps Skipper" \
      org.opencontainers.image.description="Skipper ASP.NET Core API"
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=5198
EXPOSE 5198
COPY --from=build /out/skipper/ ./
USER $APP_UID
HEALTHCHECK NONE
ENTRYPOINT ["dotnet", "skipper-api.dll"]

