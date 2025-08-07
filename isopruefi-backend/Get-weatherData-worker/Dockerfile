FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Get-weatherData-worker/Get-weatherData-worker.csproj", "Get-weatherData-worker/"]
COPY ["Database/Database.csproj", "Database/"]
RUN dotnet restore "Get-weatherData-worker/Get-weatherData-worker.csproj"
RUN dotnet restore Database/Database.csproj
COPY ["Get-weatherData-worker/", "Get-weatherData-worker/"]
COPY ["Database/", "Database/"]
WORKDIR "/src/Get-weatherData-worker"
RUN dotnet build "Get-weatherData-worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Get-weatherData-worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Get-weatherData-worker.dll"]
