# GKMPS School ERP -- single deployable (SchoolERP.Api with its Business, DataAccess and
# Common tiers). Build from the repository root: docker build -t schoolerp .

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first, from the project files only, so dependency layers are cached between builds.
COPY src/SchoolERP.Common/SchoolERP.Common.csproj src/SchoolERP.Common/
COPY src/SchoolERP.DataAccess/SchoolERP.DataAccess.csproj src/SchoolERP.DataAccess/
COPY src/SchoolERP.Business/SchoolERP.Business.csproj src/SchoolERP.Business/
COPY src/SchoolERP.Api/SchoolERP.Api.csproj src/SchoolERP.Api/
RUN dotnet restore src/SchoolERP.Api/SchoolERP.Api.csproj

COPY src/ src/
RUN dotnet publish src/SchoolERP.Api/SchoolERP.Api.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
COPY --from=build /app/publish .
USER appuser

ENTRYPOINT ["dotnet", "SchoolERP.Api.dll"]
