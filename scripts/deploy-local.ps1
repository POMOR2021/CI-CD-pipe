# Скрипт для локального развертывания с Docker Compose

$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting local deployment..." -ForegroundColor Green

# Проверка наличия .env файла
if (-not (Test-Path ".env")) {
    Write-Host "⚠️  .env file not found. Creating from .env.example..." -ForegroundColor Yellow
    Copy-Item ".env.example" ".env"
    Write-Host "📝 Please edit .env file with your credentials" -ForegroundColor Yellow
    exit 1
}

# Остановка существующих контейнеров
Write-Host "🛑 Stopping existing containers..." -ForegroundColor Cyan
docker-compose down

# Сборка образа
Write-Host "🔨 Building Docker image..." -ForegroundColor Cyan
docker-compose build

# Запуск контейнеров
Write-Host "▶️  Starting containers..." -ForegroundColor Cyan
docker-compose up -d

# Ожидание запуска PostgreSQL
Write-Host "⏳ Waiting for PostgreSQL to be ready..." -ForegroundColor Cyan
Start-Sleep -Seconds 10

# Проверка статуса
Write-Host "✅ Checking container status..." -ForegroundColor Cyan
docker-compose ps

# Вывод логов
Write-Host "📋 Application logs:" -ForegroundColor Cyan
docker-compose logs webapp

Write-Host ""
Write-Host "✨ Deployment completed!" -ForegroundColor Green
Write-Host "🌐 Application is running at: http://localhost:8080" -ForegroundColor Green
Write-Host "🗄️  PostgreSQL is running at: localhost:5432" -ForegroundColor Green
Write-Host ""
Write-Host "📊 To view logs: docker-compose logs -f webapp" -ForegroundColor Yellow
Write-Host "🛑 To stop: docker-compose down" -ForegroundColor Yellow
