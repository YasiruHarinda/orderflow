# syntax=docker/dockerfile:1
# Build from the repository root:  docker build -f src/Gateway/Dockerfile .

# ---------- Stage 1: web. Builds the React store (src/Web) into static files. ----------
FROM node:22-alpine AS web
WORKDIR /web

# Dependencies first: this layer is only rebuilt when the lockfile changes.
COPY src/Web/package.json src/Web/package-lock.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm,sharing=locked \
    npm ci --no-audit --no-fund

COPY src/Web/ ./
RUN npm run build

# ---------- Stage 2: build. Has the full .NET SDK (large); used only to compile. ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# .dockerignore keeps bin/, obj/, node_modules, tests, docs and infra out of the build context.
COPY . .

# The gateway serves the store from wwwroot, so the site and the API share one origin.
COPY --from=web /web/dist src/Gateway/wwwroot

# The cache mount keeps downloaded NuGet packages between builds, so rebuilding after a
# code change does not download everything again. "locked" makes parallel builds take
# turns: two restores writing the same package folder at once can corrupt it.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish src/Gateway/Gateway.csproj \
      --configuration Release \
      --output /app \
      -p:UseAppHost=false

# ---------- Stage 3: runtime. Only what is needed to RUN the app. ----------
# "Chiseled" = Ubuntu with only the files .NET needs: no shell, no package manager,
# roughly half the size of the regular ASP.NET image. Fewer files = fewer vulnerabilities.
# Node is NOT in this image: it was only needed to build the static files.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .

# Never run as root. APP_UID (1654) is the non-root "app" user built into .NET images.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gateway.dll"]
