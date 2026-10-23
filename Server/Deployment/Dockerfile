FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/*.csproj ./
RUN dotnet restore
COPY backend/ ./
RUN dotnet publish --configuration Release --no-restore --output /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Railway mounts persistent volumes as root. Initialize only /data, then drop privileges.
RUN apt-get update && apt-get install -y --no-install-recommends gosu \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out/ ./
COPY webgl/ /app/wwwroot/
COPY entrypoint.sh /app/entrypoint.sh
RUN chmod 755 /app/entrypoint.sh && mkdir -p /data && chown app:app /data
ENV PORT=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    POLYRISK_WEBGL_ROOT=/app/wwwroot \
    POLYRISK_DATA_PATH=/data/lobbies.json \
    POLYRISK_REQUESTS_PER_MINUTE=3600
EXPOSE 8080
ENTRYPOINT ["/app/entrypoint.sh"]
