# Галерея изображений - Image Gallery

Современное веб-приложение на ASP.NET Core для загрузки, хранения и управления изображениями с использованием облачных технологий Яндекс.Облака.

> 🌐 **Как работает интеграция с Yandex Cloud?** См. [YANDEX_CLOUD_INTEGRATION.md](YANDEX_CLOUD_INTEGRATION.md)
> 
> ⚠️ **Проблемы с восстановлением пакетов NuGet?** См. [FIX_NUGET.md](FIX_NUGET.md)

## 🚀 Технологии

### Backend
- **ASP.NET Core 8.0** - современный фреймворк для веб-приложений
- **Entity Framework Core** - ORM для работы с базой данных
- **PostgreSQL** - реляционная база данных для хранения метаданных
- **Yandex Object Storage (S3)** - облачное хранилище для изображений

### DevOps
- **Docker** - контейнеризация приложения
- **Kubernetes** - оркестрация контейнеров
- **GitHub Actions** - CI/CD пайплайн
- **Yandex Managed Service for Kubernetes** - управляемый Kubernetes кластер

### Frontend
- **Bootstrap 5** - современный CSS фреймворк
- **Bootstrap Icons** - иконки
- **Razor Pages** - серверный рендеринг

## 📋 Функциональность

- ✅ Загрузка изображений (JPG, JPEG, PNG, GIF, WEBP)
- ✅ Хранение изображений в Yandex Object Storage
- ✅ Хранение метаданных в PostgreSQL
- ✅ Просмотр галереи изображений
- ✅ Детальная информация о каждом изображении
- ✅ Удаление изображений
- ✅ Адаптивный дизайн
- ✅ Предварительный просмотр перед загрузкой

## 🏗️ Архитектура

```
┌─────────────────┐
│   User Browser  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Load Balancer  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      ┌──────────────────┐
│   ASP.NET Core  │─────▶│   PostgreSQL     │
│   Application   │      │   (Metadata)     │
└────────┬────────┘      └──────────────────┘
         │
         ▼
┌─────────────────┐
│ Yandex Object   │
│    Storage      │
│   (Images)      │
└─────────────────┘
```

## 🛠️ Локальная разработка

### Предварительные требования

- .NET 8.0 SDK
- Docker Desktop
- Visual Studio 2022 или VS Code

### Запуск локально

1. **Клонировать репозиторий**
```bash
git clone <repository-url>
cd WebApplication27
```

2. **Восстановить зависимости**
```bash
dotnet restore
```

3. **Запустить приложение**
```bash
dotnet run
```

Приложение будет доступно по адресу: `https://localhost:5001`

### Запуск с Docker Compose

```bash
docker-compose up -d
```

Это запустит:
- Приложение на порту 8080
- PostgreSQL на порту 5432

## 🐳 Docker

### Сборка Docker образа

```bash
docker build -t imagegallery:latest .
```

### Запуск контейнера

```bash
docker run -d -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=imagegallery;Username=user;Password=pass" \
  -e YandexStorage__AccessKey="your-access-key" \
  -e YandexStorage__SecretKey="your-secret-key" \
  -e YandexStorage__BucketName="your-bucket-name" \
  imagegallery:latest
```

## ☸️ Kubernetes

### Настройка секретов

1. **Создать Secret для учетных данных**

Отредактируйте `k8s-secret.yaml` и добавьте:
- Строку подключения к PostgreSQL
- Access Key для Yandex Object Storage
- Secret Key для Yandex Object Storage
- Имя бакета

```bash
kubectl apply -f k8s-secret.yaml
```

2. **Применить ConfigMap**

```bash
kubectl apply -f k8s-configmap.yaml
```

### Развертывание приложения

```bash
# Применить все манифесты
kubectl apply -f k8s-deployment.yaml
kubectl apply -f k8s-service.yaml
kubectl apply -f k8s-ingress.yaml

# Проверить статус
kubectl get pods
kubectl get services
kubectl get ingress
```

### Масштабирование

```bash
# Увеличить количество реплик
kubectl scale deployment imagegallery-deployment --replicas=3

# Автомасштабирование
kubectl autoscale deployment imagegallery-deployment --min=2 --max=10 --cpu-percent=80
```

## 🔄 CI/CD Pipeline

### GitHub Actions

Pipeline автоматически выполняет:

1. **Build & Test** - сборка и тестирование приложения
2. **Docker Build & Push** - сборка и публикация Docker образа в Yandex Container Registry
3. **Deploy to Kubernetes** - развертывание в Yandex Managed Kubernetes

### Настройка GitHub Secrets

Добавьте следующие секреты в GitHub:

- `YC_REGISTRY_KEY` - JSON ключ для Container Registry
- `YC_REGISTRY_ID` - ID Container Registry
- `YC_SA_JSON_CREDENTIALS` - JSON ключ сервисного аккаунта
- `YC_CLOUD_ID` - ID облака
- `YC_FOLDER_ID` - ID каталога
- `YC_K8S_CLUSTER_NAME` - имя Kubernetes кластера

## 🌐 Настройка Yandex Cloud

### 1. Создание Object Storage бакета

```bash
# Через yc CLI
yc storage bucket create --name imagegallery-bucket --default-storage-class standard

# Настройка публичного доступа
yc storage bucket update --name imagegallery-bucket --public-read
```

### 2. Создание PostgreSQL кластера

```bash
yc managed-postgresql cluster create \
  --name imagegallery-db \
  --environment production \
  --network-name default \
  --postgresql-version 16 \
  --resource-preset s2.micro \
  --disk-size 10 \
  --disk-type network-ssd \
  --user name=dbuser,password=SecurePassword123 \
  --database name=imagegallery,owner=dbuser
```

### 3. Создание Kubernetes кластера

```bash
yc managed-kubernetes cluster create \
  --name imagegallery-k8s \
  --network-name default \
  --zone ru-central1-a \
  --public-ip \
  --release-channel stable \
  --version 1.28

# Создание node group
yc managed-kubernetes node-group create \
  --name imagegallery-nodes \
  --cluster-name imagegallery-k8s \
  --platform standard-v3 \
  --cores 2 \
  --memory 4 \
  --disk-type network-ssd \
  --disk-size 30 \
  --fixed-size 2
```

### 4. Создание Container Registry

```bash
yc container registry create --name imagegallery-registry
```

## 📊 Мониторинг и логи

### Просмотр логов

```bash
# Логи приложения
kubectl logs -f deployment/imagegallery-deployment

# Логи конкретного пода
kubectl logs -f <pod-name>

# Логи за последние 1 час
kubectl logs --since=1h deployment/imagegallery-deployment
```

### Health Check

Приложение предоставляет endpoint для проверки здоровья:

```bash
curl http://<service-ip>/health
```

## 🔒 Безопасность

- ✅ Использование Kubernetes Secrets для хранения учетных данных
- ✅ Запуск контейнера от непривилегированного пользователя
- ✅ HTTPS редирект через Ingress
- ✅ Валидация загружаемых файлов
- ✅ Ограничение размера файлов (10 MB)
- ✅ Проверка типов файлов

## 📝 Конфигурация

### Переменные окружения

| Переменная | Описание | Пример |
|-----------|----------|--------|
| `ConnectionStrings__DefaultConnection` | Строка подключения к PostgreSQL | `Host=localhost;Database=imagegallery;Username=user;Password=pass` |
| `YandexStorage__AccessKey` | Access Key для Object Storage | `YCAJExxxxxxxxx` |
| `YandexStorage__SecretKey` | Secret Key для Object Storage | `YCMxxxxxxxxxxxxxx` |
| `YandexStorage__BucketName` | Имя бакета | `imagegallery-bucket` |
| `YandexStorage__ServiceUrl` | URL сервиса | `https://storage.yandexcloud.net` |
| `ASPNETCORE_ENVIRONMENT` | Окружение | `Production` |

## 🧪 Тестирование

```bash
# Запуск всех тестов
dotnet test

# Запуск с покрытием кода
dotnet test /p:CollectCoverage=true
```

## 📈 Производительность

- Поддержка горизонтального масштабирования
- Кэширование статических файлов
- Оптимизированные Docker образы (multi-stage build)
- Health checks для автоматического восстановления
- Resource limits в Kubernetes

## 🤝 Вклад в проект

1. Fork репозитория
2. Создайте feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit изменения (`git commit -m 'Add some AmazingFeature'`)
4. Push в branch (`git push origin feature/AmazingFeature`)
5. Откройте Pull Request

## 📄 Лицензия

Этот проект создан в образовательных целях.

## 👥 Автор

Экзаменационный проект по DevOps и облачным технологиям

## 🔗 Полезные ссылки

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Yandex Cloud Documentation](https://cloud.yandex.ru/docs)
- [Kubernetes Documentation](https://kubernetes.io/docs)
- [Docker Documentation](https://docs.docker.com)
