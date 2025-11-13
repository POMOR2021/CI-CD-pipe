# Архитектура приложения

## Обзор

Приложение "Галерея изображений" представляет собой современное облачное веб-приложение, построенное на основе микросервисной архитектуры с использованием контейнеризации и оркестрации.

## Компоненты системы

### 1. Веб-приложение (ASP.NET Core)

**Технологии:**
- ASP.NET Core 8.0 MVC
- Entity Framework Core
- Razor Pages

**Функции:**
- Обработка HTTP запросов
- Бизнес-логика приложения
- Взаимодействие с базой данных
- Интеграция с Object Storage
- Рендеринг пользовательского интерфейса

**Структура:**
```
WebApplication27/
├── Controllers/          # MVC контроллеры
│   ├── HomeController.cs
│   ├── ImagesController.cs
│   └── HealthController.cs
├── Models/              # Модели данных
│   ├── ImageMetadata.cs
│   └── ErrorViewModel.cs
├── Views/               # Razor представления
│   ├── Home/
│   ├── Images/
│   └── Shared/
├── Services/            # Бизнес-логика
│   ├── IStorageService.cs
│   ├── YandexStorageService.cs
│   └── LocalStorageService.cs
├── Data/                # Контекст базы данных
│   └── ApplicationDbContext.cs
└── wwwroot/             # Статические файлы
```

### 2. База данных (PostgreSQL)

**Назначение:**
- Хранение метаданных изображений
- Информация о файлах (имя, размер, дата загрузки)
- Описания изображений

**Схема данных:**
```sql
Table: Images
- Id (int, PK)
- FileName (varchar(255))
- OriginalFileName (varchar(255))
- ContentType (varchar(100))
- FileSize (bigint)
- StorageUrl (varchar(500))
- UploadedAt (timestamp)
- Description (varchar(1000), nullable)
```

### 3. Object Storage (Yandex S3)

**Назначение:**
- Хранение файлов изображений
- Публичный доступ к изображениям
- Масштабируемое хранилище

**Конфигурация:**
- Публичный доступ на чтение
- CORS настройки для веб-доступа
- Стандартный класс хранения

## Архитектурные паттерны

### 1. Repository Pattern

Используется для абстракции доступа к данным через Entity Framework Core:

```csharp
ApplicationDbContext → DbSet<ImageMetadata> → PostgreSQL
```

### 2. Service Layer Pattern

Бизнес-логика вынесена в сервисы:

```csharp
IStorageService
├── YandexStorageService (Production)
└── LocalStorageService (Development)
```

### 3. Dependency Injection

Все зависимости регистрируются в `Program.cs`:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>()
builder.Services.AddScoped<IStorageService, YandexStorageService>()
```

## Инфраструктура

### Kubernetes архитектура

```
┌─────────────────────────────────────────┐
│         Yandex Cloud Load Balancer      │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│         Kubernetes Service              │
│         (LoadBalancer)                  │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│         Kubernetes Deployment           │
│  ┌─────────────┐    ┌─────────────┐   │
│  │   Pod 1     │    │   Pod 2     │   │
│  │  Container  │    │  Container  │   │
│  └─────────────┘    └─────────────┘   │
└─────────────────────────────────────────┘
         │                    │
         ▼                    ▼
┌─────────────────┐  ┌─────────────────┐
│   PostgreSQL    │  │ Object Storage  │
│   (Managed)     │  │   (S3 API)      │
└─────────────────┘  └─────────────────┘
```

### Компоненты Kubernetes

**Deployment:**
- Управляет репликами приложения
- Обеспечивает rolling updates
- Health checks (liveness, readiness)
- Resource limits (CPU, Memory)

**Service:**
- Тип: LoadBalancer
- Внешний доступ к приложению
- Балансировка нагрузки между подами

**ConfigMap:**
- Конфигурация приложения
- Переменные окружения
- Настройки без секретных данных

**Secret:**
- Учетные данные базы данных
- Ключи доступа к Object Storage
- Чувствительная информация

**Ingress (опционально):**
- SSL/TLS терминация
- Маршрутизация по доменам
- Интеграция с cert-manager

## Потоки данных

### 1. Загрузка изображения

```
User → Browser
  ↓
  POST /Images/Upload
  ↓
ImagesController.Upload()
  ↓
  ├─→ Validation (size, type)
  ├─→ Generate unique filename
  ├─→ IStorageService.UploadFileAsync()
  │     ↓
  │   Yandex Object Storage (S3 API)
  │     ↓
  │   Return public URL
  ↓
Save metadata to PostgreSQL
  ↓
Redirect to /Images/Index
```

### 2. Просмотр галереи

```
User → Browser
  ↓
  GET /Images/Index
  ↓
ImagesController.Index()
  ↓
Query PostgreSQL for metadata
  ↓
Render view with image URLs
  ↓
Browser loads images from Object Storage
```

### 3. Удаление изображения

```
User → Browser
  ↓
  POST /Images/Delete/{id}
  ↓
ImagesController.DeleteConfirmed()
  ↓
  ├─→ Find metadata in PostgreSQL
  ├─→ IStorageService.DeleteFileAsync()
  │     ↓
  │   Delete from Object Storage
  ↓
Delete metadata from PostgreSQL
  ↓
Redirect to /Images/Index
```

## CI/CD Pipeline

```
GitHub Push
  ↓
GitHub Actions Workflow
  ↓
  ├─→ Build & Test
  │     ├─→ dotnet restore
  │     ├─→ dotnet build
  │     └─→ dotnet test
  ↓
  ├─→ Build Docker Image
  │     ├─→ docker build
  │     └─→ docker push to Yandex CR
  ↓
  └─→ Deploy to Kubernetes
        ├─→ Configure kubectl
        ├─→ kubectl set image
        └─→ kubectl rollout status
```

## Безопасность

### 1. Аутентификация и авторизация

- Kubernetes Secrets для хранения учетных данных
- Service Account для доступа к Yandex Cloud API
- IAM роли для Container Registry и Object Storage

### 2. Сетевая безопасность

- Network Policies в Kubernetes
- Security Groups в Yandex Cloud
- HTTPS через Ingress/Load Balancer
- Private endpoints для PostgreSQL

### 3. Безопасность приложения

- Валидация входных данных
- Ограничение размера файлов
- Проверка типов файлов
- Anti-forgery tokens
- SQL injection защита (EF Core)

### 4. Безопасность контейнеров

- Non-root пользователь в контейнере
- Минимальный base image (aspnet runtime)
- Регулярные обновления образов
- Сканирование уязвимостей

## Масштабируемость

### Горизонтальное масштабирование

```yaml
# Horizontal Pod Autoscaler
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
spec:
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 80
```

### Вертикальное масштабирование

- Увеличение ресурсов подов (CPU, Memory)
- Масштабирование PostgreSQL кластера
- Увеличение node pool в Kubernetes

### Кэширование

- Статические файлы кэшируются браузером
- CDN для Object Storage (опционально)
- Response caching в ASP.NET Core

## Мониторинг и наблюдаемость

### Метрики

- CPU и Memory использование подов
- Количество запросов
- Время отклика
- Ошибки приложения

### Логирование

- Структурированное логирование (JSON)
- Централизованный сбор логов
- Уровни логирования по окружению

### Health Checks

```
/health endpoint
  ↓
  ├─→ Application health
  ├─→ Database connectivity
  └─→ Storage accessibility
```

### Трейсинг

- Distributed tracing (опционально)
- Request correlation IDs
- Performance profiling

## Отказоустойчивость

### High Availability

- Минимум 2 реплики приложения
- Managed PostgreSQL с репликацией
- Object Storage с репликацией данных
- Load Balancer с health checks

### Disaster Recovery

- Автоматические бэкапы PostgreSQL
- Версионирование в Object Storage
- Kubernetes rolling updates
- Rollback механизмы

### Graceful Degradation

- Fallback на локальное хранилище
- In-memory database для разработки
- Retry логика для внешних сервисов
- Circuit breaker паттерн

## Производительность

### Оптимизации

- Асинхронные операции (async/await)
- Connection pooling для PostgreSQL
- Streaming для загрузки файлов
- Компрессия HTTP ответов
- Static file caching

### Benchmarks

- Время загрузки изображения: < 2s
- Время отображения галереи: < 1s
- Throughput: 100+ req/s на реплику
- Latency: p95 < 500ms

## Стоимость

### Оптимизация затрат

- Preemptible VM для dev окружения
- Автоматическое масштабирование
- Lifecycle policies для Object Storage
- Мониторинг использования ресурсов

### Примерная стоимость (месяц)

- Kubernetes кластер: ~$50-100
- PostgreSQL: ~$20-50
- Object Storage: ~$1-10 (зависит от объема)
- Load Balancer: ~$10-20
- Container Registry: ~$5-10

**Итого: ~$86-190/месяц**

## Будущие улучшения

1. **Функциональность:**
   - Обработка изображений (resize, crop)
   - Поддержка альбомов и тегов
   - Поиск по изображениям
   - Пользовательская аутентификация

2. **Инфраструктура:**
   - CDN для статических файлов
   - Redis для кэширования
   - Elasticsearch для поиска
   - Prometheus + Grafana для мониторинга

3. **DevOps:**
   - Helm charts для Kubernetes
   - GitOps с ArgoCD
   - Automated testing в CI/CD
   - Canary deployments

4. **Безопасность:**
   - OAuth2/OIDC аутентификация
   - Rate limiting
   - WAF (Web Application Firewall)
   - Secrets management (Vault)
