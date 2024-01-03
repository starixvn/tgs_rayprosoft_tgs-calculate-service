#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.CalculateService/Tgs.CalculateService.csproj", "Tgs.CalculateService/"]
COPY ["Tgs.Calculate.Common/Tgs.Calculate.Common.csproj", "Tgs.Calculate.Common/"]
RUN dotnet restore "./Tgs.CalculateService/./Tgs.CalculateService.csproj"
COPY . .
WORKDIR "/src/Tgs.CalculateService"
RUN dotnet build "./Tgs.CalculateService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.CalculateService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.CalculateService.dll"]