FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["source/Absolute.Cinema.IdentityService/Absolute.Cinema.IdentityService.csproj", "source/Absolute.Cinema.IdentityService/"]
RUN dotnet restore "source/Absolute.Cinema.IdentityService/Absolute.Cinema.IdentityService.csproj"
COPY . .
WORKDIR "/src/source/Absolute.Cinema.IdentityService"
RUN dotnet build "Absolute.Cinema.IdentityService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Absolute.Cinema.IdentityService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Absolute.Cinema.IdentityService.dll"]
