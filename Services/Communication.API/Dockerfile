FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["BuildingBlocks/SchoolERP.Shared/SchoolERP.Shared.csproj", "BuildingBlocks/SchoolERP.Shared/"]
COPY ["Services/Communication.API/SchoolERP.Communication.API.csproj", "Services/Communication.API/"]
RUN dotnet restore "Services/Communication.API/SchoolERP.Communication.API.csproj"

COPY . .
WORKDIR /src/Services/Communication.API
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
COPY --from=build /app/publish .
USER appuser

ENTRYPOINT ["dotnet", "SchoolERP.Communication.API.dll"]
