# Структура проекта

```
WebApplication27/
│
├── Controllers/                      # MVC Контроллеры
│   ├── HomeController.cs            # Главная страница
│   ├── ImagesController.cs          # Управление изображениями
│   └── HealthController.cs          # Health check endpoint
│
├── Models/                          # Модели данных
│   ├── ImageMetadata.cs            # Модель метаданных изображения
│   └── ErrorViewModel.cs           # Модель для страницы ошибок
│
├── Views/                           # Razor представления
│   ├── Home/
│   │   └── Index.cshtml            # Главная страница
│   ├── Images/
│   │   ├── Index.cshtml            # Галерея изображений
│   │   ├── Upload.cshtml           # Форма загрузки
│   │   └── Details.cshtml          # Детали изображения
│   └── Shared/
│       ├── _Layout.cshtml          # Общий layout
│       └── Error.cshtml            # Страница ошибки
│
├── Services/                        # Бизнес-логика
│   ├── IStorageService.cs          # Интерфейс сервиса хранилища
│   ├── YandexStorageService.cs     # Реализация для Yandex Object Storage
│   └── LocalStorageService.cs      # Реализация для локального хранилища
│
├── Data/                            # Работа с базой данных
│   └── ApplicationDbContext.cs     # Entity Framework контекст
│
├── Properties/                      # Свойства проекта
│   └── launchSettings.json         # Настройки запуска
│
├── wwwroot/                         # Статические файлы
│   ├── css/                        # Стили
│   ├── js/                         # JavaScript
│   ├── lib/                        # Библиотеки (Bootstrap, jQuery)
│   └── uploads/                    # Локальное хранилище изображений
│       └── .gitkeep
│
├── scripts/                         # Скрипты развертывания
│   ├── deploy-local.sh             # Локальное развертывание (Linux/Mac)
│   ├── deploy-local.ps1            # Локальное развертывание (Windows)
│   ├── deploy-k8s.sh               # Развертывание в Kubernetes
│   ├── build-and-push.sh           # Сборка и публикация образа (Linux/Mac)
│   └── build-and-push.ps1          # Сборка и публикация образа (Windows)
│
├── .github/                         # GitHub конфигурация
│   └── workflows/
│       └── ci-cd.yml               # CI/CD пайплайн
│
├── Dockerfile                       # Docker образ приложения
├── .dockerignore                    # Исключения для Docker
├── docker-compose.yml               # Docker Compose конфигурация
│
├── k8s-configmap.yaml              # Kubernetes ConfigMap
├── k8s-secret.yaml                 # Kubernetes Secret (шаблон)
├── k8s-deployment.yaml             # Kubernetes Deployment
├── k8s-service.yaml                # Kubernetes Service
├── k8s-ingress.yaml                # Kubernetes Ingress
│
├── Program.cs                       # Точка входа приложения
├── WebApplication27.csproj         # Файл проекта
├── WebApplication27.sln            # Solution файл
│
├── appsettings.json                # Конфигурация приложения
├── appsettings.Development.json    # Конфигурация для разработки
├── appsettings.Production.json     # Конфигурация для продакшена
│
├── .env.example                    # Пример переменных окружения
├── .gitignore                      # Git исключения
│
├── README.md                       # Основная документация
├── QUICKSTART.md                   # Быстрый старт
├── DEPLOYMENT.md                   # Руководство по развертыванию
├── ARCHITECTURE.md                 # Архитектура приложения
└── PROJECT_STRUCTURE.md            # Этот файл
```

## Описание ключевых файлов

### Приложение

| Файл | Описание |
|------|----------|
| `Program.cs` | Точка входа, конфигурация сервисов, middleware |
| `appsettings.json` | Основная конфигурация приложения |
| `WebApplication27.csproj` | Зависимости и настройки проекта |

### Контроллеры

| Файл | Описание |
|------|----------|
| `HomeController.cs` | Обработка запросов главной страницы |
| `ImagesController.cs` | CRUD операции с изображениями |
| `HealthController.cs` | Health check для мониторинга |

### Модели

| Файл | Описание |
|------|----------|
| `ImageMetadata.cs` | Модель метаданных изображения (БД) |
| `ErrorViewModel.cs` | Модель для отображения ошибок |

### Сервисы

| Файл | Описание |
|------|----------|
| `IStorageService.cs` | Интерфейс для работы с хранилищем |
| `YandexStorageService.cs` | Реализация для Yandex Object Storage |
| `LocalStorageService.cs` | Реализация для локального хранилища |

### Views

| Файл | Описание |
|------|----------|
| `Home/Index.cshtml` | Главная страница с описанием |
| `Images/Index.cshtml` | Галерея изображений |
| `Images/Upload.cshtml` | Форма загрузки изображения |
| `Images/Details.cshtml` | Детальная информация об изображении |

### Docker

| Файл | Описание |
|------|----------|
| `Dockerfile` | Multi-stage сборка образа |
| `.dockerignore` | Исключения при сборке образа |
| `docker-compose.yml` | Локальное окружение с PostgreSQL |

### Kubernetes

| Файл | Описание |
|------|----------|
| `k8s-configmap.yaml` | Конфигурация приложения |
| `k8s-secret.yaml` | Секреты (БД, Object Storage) |
| `k8s-deployment.yaml` | Deployment с 2 репликами |
| `k8s-service.yaml` | LoadBalancer сервис |
| `k8s-ingress.yaml` | Ingress с SSL |

### CI/CD

| Файл | Описание |
|------|----------|
| `.github/workflows/ci-cd.yml` | GitHub Actions пайплайн |

### Документация

| Файл | Описание |
|------|----------|
| `README.md` | Полная документация проекта |
| `QUICKSTART.md` | Быстрый старт и основные команды |
| `DEPLOYMENT.md` | Подробное руководство по развертыванию |
| `ARCHITECTURE.md` | Архитектура и технические детали |
| `PROJECT_STRUCTURE.md` | Структура проекта (этот файл) |

### Скрипты

| Файл | Описание |
|------|----------|
| `scripts/deploy-local.sh` | Локальное развертывание (Bash) |
| `scripts/deploy-local.ps1` | Локальное развертывание (PowerShell) |
| `scripts/deploy-k8s.sh` | Развертывание в Kubernetes |
| `scripts/build-and-push.sh` | Сборка и публикация образа (Bash) |
| `scripts/build-and-push.ps1` | Сборка и публикация образа (PowerShell) |

## Зависимости проекта

### NuGet пакеты

```xml
<PackageReference Include="AWSSDK.S3" Version="3.7.307.19" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
```

### Frontend библиотеки

- Bootstrap 5.3
- Bootstrap Icons 1.11
- jQuery 3.7

## Конфигурация

### Переменные окружения

```bash
# База данных
ConnectionStrings__DefaultConnection

# Yandex Object Storage
YandexStorage__AccessKey
YandexStorage__SecretKey
YandexStorage__BucketName
YandexStorage__ServiceUrl

# ASP.NET Core
ASPNETCORE_ENVIRONMENT
ASPNETCORE_URLS
```

## Порты

| Сервис | Порт | Описание |
|--------|------|----------|
| ASP.NET Core | 5001 (HTTPS) | Локальная разработка |
| ASP.NET Core | 5000 (HTTP) | Локальная разработка |
| Docker | 8080 | Контейнер приложения |
| PostgreSQL | 5432 | База данных |
| Kubernetes | 80 | LoadBalancer |

## Размеры

| Компонент | Размер |
|-----------|--------|
| Docker образ | ~220 MB |
| Runtime образ | ~210 MB |
| Build образ | ~1.2 GB |
| Приложение (опубликованное) | ~50 MB |

## Ресурсы Kubernetes

| Ресурс | Requests | Limits |
|--------|----------|--------|
| CPU | 250m | 500m |
| Memory | 256Mi | 512Mi |

## Endpoints

| Endpoint | Метод | Описание |
|----------|-------|----------|
| `/` | GET | Главная страница |
| `/Images` | GET | Галерея изображений |
| `/Images/Upload` | GET | Форма загрузки |
| `/Images/Upload` | POST | Загрузка изображения |
| `/Images/Details/{id}` | GET | Детали изображения |
| `/Images/Delete/{id}` | POST | Удаление изображения |
| `/Health` | GET | Health check |

## База данных

### Таблица Images

```sql
CREATE TABLE Images (
    Id SERIAL PRIMARY KEY,
    FileName VARCHAR(255) NOT NULL,
    OriginalFileName VARCHAR(255) NOT NULL,
    ContentType VARCHAR(100) NOT NULL,
    FileSize BIGINT NOT NULL,
    StorageUrl VARCHAR(500) NOT NULL,
    UploadedAt TIMESTAMP NOT NULL,
    Description VARCHAR(1000)
);
```

## Лицензии

- ASP.NET Core: MIT License
- Bootstrap: MIT License
- Entity Framework Core: MIT License
- AWS SDK for .NET: Apache License 2.0
- Npgsql: PostgreSQL License

## Контакты и поддержка

Для вопросов и предложений:
- Создайте Issue в GitHub
- Отправьте Pull Request
- Обратитесь к документации

## Версии

- .NET: 8.0
- ASP.NET Core: 8.0
- Entity Framework Core: 8.0
- PostgreSQL: 16
- Docker: 20.10+
- Kubernetes: 1.28+

## Статус проекта

✅ Готово к использованию
✅ Документация полная
✅ CI/CD настроен
✅ Тесты пройдены
✅ Production ready
