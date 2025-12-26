FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8090
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["Game.ChatService/Game.ChatService.csproj", "Game.ChatService/"]
COPY ["Game.Shared/Game.Shared.csproj", "Game.Shared/"]

# Restore dependencies
RUN dotnet restore "Game.ChatService/Game.ChatService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Game.ChatService"
RUN dotnet build "Game.ChatService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Game.ChatService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Game.ChatService.dll"]