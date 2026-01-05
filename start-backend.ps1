$ErrorActionPreference = "Stop"

Write-Host "=== Tiny MMO Backend Launcher ===" -ForegroundColor Cyan

# Check if Docker is running
Write-Host "Checking Docker status..." -ForegroundColor Gray
try {
    docker info > $null
    if ($LASTEXITCODE -ne 0) { throw "Docker is not running" }
}
catch {
    Write-Error "Docker is not running or not installed. Please start Docker Desktop."
    exit 1
}

$backendDir = Join-Path $PSScriptRoot "GameBackend"

if (-not (Test-Path $backendDir)) {
    Write-Error "GameBackend directory not found at $backendDir"
    exit 1
}

Push-Location $backendDir

try {
    Write-Host "Stopping any existing containers..." -ForegroundColor Yellow
    docker compose down

    Write-Host "Building and starting services..." -ForegroundColor Yellow
    # Use 'docker compose' (v2) or fallback to 'docker-compose' (v1)
    try {
        docker compose up --build -d
    }
    catch {
        Write-Warning "'docker compose' failed, trying legacy 'docker-compose'..."
        docker-compose up --build -d
    }
    
    Write-Host "`nBackend services started successfully!" -ForegroundColor Green
    Write-Host "----------------------------------------"
    Write-Host "Infrastructure:"
    Write-Host "  Consul UI:    http://localhost:8500"
    Write-Host "  Postgres:     localhost:5432"
    Write-Host "  Redis:        localhost:6379"
    Write-Host "Microservices:"
    Write-Host "  Game Service: http://localhost:5002/swagger"
    Write-Host "  Room Service: http://localhost:5003/swagger"
    Write-Host "  Chat Service: http://localhost:5004/swagger"
    Write-Host "----------------------------------------"
    
    Write-Host "To view logs, run: docker compose logs -f" -ForegroundColor Gray
    Write-Host "To stop, run: docker compose down" -ForegroundColor Gray
}
catch {
    Write-Error "Failed to start backend: $_"
}
finally {
    Pop-Location
}
