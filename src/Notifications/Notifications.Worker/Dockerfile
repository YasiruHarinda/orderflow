# syntax=docker/dockerfile:1
# Build from the repository root:  docker build -f src/Notifications/Notifications.Worker/Dockerfile .

# ---------- Stage 1: build. Has the full .NET SDK (large); used only to compile. ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# .dockerignore keeps bin/, obj/, tests, docs and infra out of the build context.
COPY . .

# The cache mount keeps downloaded NuGet packages between builds, so rebuilding after a
# code change does not download everything again.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/Notifications/Notifications.Worker/Notifications.Worker.csproj \
      --configuration Release \
      --output /app \
      -p:UseAppHost=false

# ---------- Stage 2: runtime. Only what is needed to RUN the app. ----------
# "Chiseled" = Ubuntu with only the files .NET needs: no shell, no package manager,
# roughly half the size of the regular ASP.NET image. Fewer files = fewer vulnerabilities.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .

# Never run as root. APP_UID (1654) is the non-root "app" user built into .NET images.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Notifications.Worker.dll"]