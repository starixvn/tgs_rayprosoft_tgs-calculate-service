#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.CalculateCommitter/Tgs.CalculateCommitter.csproj", "Tgs.CalculateCommitter/"]
COPY ["CalculateCommitter.Core/CalculateCommitter.Core.csproj", "CalculateCommitter.Core/"]
COPY ["Tgs.Calculate.Common/Tgs.Calculate.Common.csproj", "Tgs.Calculate.Common/"]
COPY ["CalculateCommitter.Repositories/CalculateCommitter.Repositories.csproj", "CalculateCommitter.Repositories/"]
COPY ["CalculateRepositories/CalculateRepositories.csproj", "CalculateRepositories/"]
RUN dotnet restore "./Tgs.CalculateCommitter/./Tgs.CalculateCommitter.csproj"
COPY . .
WORKDIR "/src/Tgs.CalculateCommitter"
RUN dotnet build "./Tgs.CalculateCommitter.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.CalculateCommitter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.CalculateCommitter.dll"]