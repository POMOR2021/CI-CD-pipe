#!/bin/bash

# Скрипт для локального развертывания с Docker Compose

set -e

echo "🚀 Starting local deployment..."

# Проверка наличия .env файла
if [ ! -f .env ]; then
    echo "⚠️  .env file not found. Creating from .env.example..."
    cp .env.example .env
    echo "📝 Please edit .env file with your credentials"
    exit 1
fi

# Остановка существующих контейнеров
echo "🛑 Stopping existing containers..."
docker-compose down

# Сборка образа
echo "🔨 Building Docker image..."
docker-compose build

# Запуск контейнеров
echo "▶️  Starting containers..."
docker-compose up -d

# Ожидание запуска PostgreSQL
echo "⏳ Waiting for PostgreSQL to be ready..."
sleep 10

# Проверка статуса
echo "✅ Checking container status..."
docker-compose ps

# Вывод логов
echo "📋 Application logs:"
docker-compose logs webapp

echo ""
echo "✨ Deployment completed!"
echo "🌐 Application is running at: http://localhost:8080"
echo "🗄️  PostgreSQL is running at: localhost:5432"
echo ""
echo "📊 To view logs: docker-compose logs -f webapp"
echo "🛑 To stop: docker-compose down"
