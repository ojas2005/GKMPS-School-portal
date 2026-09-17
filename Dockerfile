# GKMPS School ERP -- single deployable (SchoolERP.Api with its Business, DataAccess and
# Common tiers). Build from the repository root: docker build -t schoolerp .

# ---------- Build stage ----------
# Runs on the build machine's own architecture and cross-compiles for the target, so building
# an amd64 image on an Apple Silicon Mac doesn't run the .NET SDK under emulation.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first, from the project files only, so dependency layers are cached between builds.
COPY src/SchoolERP.Common/SchoolERP.Common.csproj src/SchoolERP.Common/
COPY src/SchoolERP.DataAccess/SchoolERP.DataAccess.csproj src/SchoolERP.DataAccess/
COPY src/SchoolERP.Business/SchoolERP.Business.csproj src/SchoolERP.Business/
COPY src/SchoolERP.Api/SchoolERP.Api.csproj src/SchoolERP.Api/
RUN dotnet restore src/SchoolERP.Api/SchoolERP.Api.csproj -a $TARGETARCH

COPY src/ src/
RUN dotnet publish src/SchoolERP.Api/SchoolERP.Api.csproj -c Release -a $TARGETARCH -o /app/publish --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
# Non-root user built into the .NET images (no RUN step, so no emulation when cross-building).
USER $APP_UID

ENTRYPOINT ["dotnet", "SchoolERP.Api.dll"]
