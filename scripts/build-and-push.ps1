# Скрипт для сборки и публикации Docker образа в Yandex Container Registry

param(
    [Parameter(Mandatory=$true)]
    [string]$RegistryId,
    
    [Parameter(Mandatory=$false)]
    [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"

$ImageName = "imagegallery"
$FullImage = "cr.yandex/$RegistryId/$ImageName`:$Tag"

Write-Host "🔨 Building Docker image..." -ForegroundColor Cyan
docker build -t $FullImage .

Write-Host "✅ Image built successfully: $FullImage" -ForegroundColor Green

# Проверка авторизации в Container Registry
Write-Host "🔐 Checking Container Registry authentication..." -ForegroundColor Cyan
try {
    yc container registry configure-docker
} catch {
    Write-Host "⚠️  Failed to configure docker. Make sure yc CLI is installed." -ForegroundColor Yellow
}

Write-Host "📤 Pushing image to Container Registry..." -ForegroundColor Cyan
docker push $FullImage

Write-Host ""
Write-Host "✨ Image pushed successfully!" -ForegroundColor Green
Write-Host "🏷️  Image: $FullImage" -ForegroundColor Green
Write-Host ""
Write-Host "To deploy this image to Kubernetes:" -ForegroundColor Yellow
Write-Host "kubectl set image deployment/imagegallery-deployment imagegallery=$FullImage" -ForegroundColor Yellow
