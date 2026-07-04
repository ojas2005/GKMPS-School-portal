# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["BuildingBlocks/SchoolERP.Shared/SchoolERP.Shared.csproj", "BuildingBlocks/SchoolERP.Shared/"]
COPY ["Services/Identity.API/SchoolERP.Identity.API.csproj", "Services/Identity.API/"]
RUN dotnet restore "Services/Identity.API/SchoolERP.Identity.API.csproj"

COPY . .
WORKDIR /src/Services/Identity.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
COPY --from=build /app/publish .
USER appuser

ENTRYPOINT ["dotnet", "SchoolERP.Identity.API.dll"]
