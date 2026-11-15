# ChattyDuck.Core.SSO container image.
#
# Build from the REPOSITORY ROOT, because the project depends on the shared build files there
# (Directory.Build.props, Directory.Packages.props, .editorconfig):
#
#   docker build -f src/core/ChattyDuck.Core.SSO/Dockerfile -t chattyduck/core-sso .
#
# Multi-stage build: the SDK image (large) compiles and publishes, the ASP.NET runtime image (small) runs the result.

ARG DOTNET_VERSION=10.0

# ---- build -------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copy only what `dotnet restore` needs first. As long as these files do not change, Docker reuses the cached
# restore layer and later builds skip downloading packages.
COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/core/ChattyDuck.Core.SSO/ChattyDuck.Core.SSO.csproj src/core/ChattyDuck.Core.SSO/
RUN dotnet restore src/core/ChattyDuck.Core.SSO/ChattyDuck.Core.SSO.csproj

COPY src/core/ChattyDuck.Core.SSO/ src/core/ChattyDuck.Core.SSO/
RUN dotnet publish src/core/ChattyDuck.Core.SSO/ChattyDuck.Core.SSO.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- runtime -----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

# The Development logging setup writes IdentityServer diagnostics to ./diagnostics. The app runs as the
# non-root "app" user, which cannot create folders in /app, so create it up front and hand it over.
RUN mkdir -p /app/diagnostics && chown "$APP_UID" /app/diagnostics

COPY --from=build /app/publish .

# The official .NET images listen on 8080 (HTTP) by default (ASPNETCORE_HTTP_PORTS=8080).
EXPOSE 8080

# Run as the built-in non-root user (APP_UID is defined by the base image).
USER $APP_UID

# TODO: Add a HEALTHCHECK once the SSO exposes a health endpoint (ADR 0002 requires one for every service).

ENTRYPOINT ["dotnet", "ChattyDuck.Core.SSO.dll"]
