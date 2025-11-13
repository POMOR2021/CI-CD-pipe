#!/bin/bash

# Скрипт для сборки и публикации Docker образа в Yandex Container Registry

set -e

# Проверка аргументов
if [ -z "$1" ]; then
    echo "Usage: $0 <registry-id> [tag]"
    echo "Example: $0 crp1234567890 latest"
    exit 1
fi

REGISTRY_ID=$1
TAG=${2:-latest}
IMAGE_NAME="imagegallery"
FULL_IMAGE="cr.yandex/$REGISTRY_ID/$IMAGE_NAME:$TAG"

echo "🔨 Building Docker image..."
docker build -t $FULL_IMAGE .

echo "✅ Image built successfully: $FULL_IMAGE"

# Проверка авторизации в Container Registry
echo "🔐 Checking Container Registry authentication..."
if ! docker images cr.yandex/$REGISTRY_ID/* &> /dev/null; then
    echo "⚠️  Not authenticated. Running: yc container registry configure-docker"
    yc container registry configure-docker
fi

echo "📤 Pushing image to Container Registry..."
docker push $FULL_IMAGE

echo "✨ Image pushed successfully!"
echo "🏷️  Image: $FULL_IMAGE"
echo ""
echo "To deploy this image to Kubernetes:"
echo "kubectl set image deployment/imagegallery-deployment imagegallery=$FULL_IMAGE"
