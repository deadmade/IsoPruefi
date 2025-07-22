FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MQTT-Receiver-Worker/MQTT-Receiver-Worker.csproj", "MQTT-Receiver-Worker/"]
RUN dotnet restore "MQTT-Receiver-Worker/MQTT-Receiver-Worker.csproj"
COPY . .
WORKDIR "/src/MQTT-Receiver-Worker"
RUN dotnet build "./MQTT-Receiver-Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MQTT-Receiver-Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MQTT-Receiver-Worker.dll"]
