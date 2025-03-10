FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Template.sln", "."]
COPY ["Template.Bot/Template.Bot.csproj", "Template.Bot/"]
COPY ["Template.Database/Template.Database.csproj", "Template.Database/"]
COPY ["Template.Startup/Template.Startup.csproj", "Template.Startup/"]
COPY ["Template.Website/Template.Website.csproj", "Template.Website/"]
RUN dotnet restore
COPY . .
WORKDIR "/src/Template.Startup"
RUN dotnet build "Template.Startup.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Template.Startup.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Template.Startup.dll"]
