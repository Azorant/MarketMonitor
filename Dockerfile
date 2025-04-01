FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
RUN apk add --no-cache icu-libs font-opensans
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MarketMonitor.sln", "."]
COPY ["MarketMonitor.Bot/MarketMonitor.Bot.csproj", "MarketMonitor.Bot/"]
COPY ["MarketMonitor.Database/MarketMonitor.Database.csproj", "MarketMonitor.Database/"]
COPY ["MarketMonitor.Startup/MarketMonitor.Startup.csproj", "MarketMonitor.Startup/"]
COPY ["MarketMonitor.Website/MarketMonitor.Website.csproj", "MarketMonitor.Website/"]
RUN dotnet restore
COPY . .
WORKDIR "/src/MarketMonitor.Startup"
RUN dotnet build "MarketMonitor.Startup.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MarketMonitor.Startup.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5123
EXPOSE 3400
ENTRYPOINT ["dotnet", "MarketMonitor.Startup.dll"]
