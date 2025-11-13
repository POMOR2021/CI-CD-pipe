# 🌐 Интеграция с Yandex Cloud

## Обзор

Приложение использует **3 основных сервиса** Yandex Cloud:

```
┌─────────────────────────────────────────────────┐
│           Ваше приложение                       │
│                                                 │
│  ┌──────────────┐  ┌──────────────┐            │
│  │  ASP.NET     │  │  Entity      │            │
│  │  Core App    │  │  Framework   │            │
│  └──────┬───────┘  └──────┬───────┘            │
│         │                  │                     │
└─────────┼──────────────────┼─────────────────────┘
          │                  │
          │                  │
    ┌─────▼─────┐      ┌────▼────────┐
    │  Yandex   │      │   Yandex    │
    │  Object   │      │  Managed    │
    │  Storage  │      │  PostgreSQL │
    │   (S3)    │      │             │
    └───────────┘      └─────────────┘
          │
          │
    ┌─────▼──────────┐
    │    Yandex      │
    │   Managed      │
    │  Kubernetes    │
    │     (MSK)      │
    └────────────────┘
```

---

## 1. 📦 Yandex Object Storage (S3)

### Назначение
Хранение загруженных изображений в облачном хранилище.

### Как работает

#### Код интеграции

**YandexStorageService.cs:**
```csharp
// Использует AWS SDK для .NET (S3-совместимый API)
using Amazon.S3;
using Amazon.S3.Model;

public class YandexStorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    // Загрузка файла
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,           // Имя бакета
            Key = fileName,                      // Имя файла
            InputStream = fileStream,            // Поток данных
            ContentType = contentType,           // MIME тип
            CannedACL = S3CannedACL.PublicRead  // Публичный доступ
        };

        await _s3Client.PutObjectAsync(request);
        return GetFileUrl(fileName);
    }

    // Получение URL
    public string GetFileUrl(string fileName)
    {
        return $"https://storage.yandexcloud.net/{_bucketName}/{fileName}";
    }
}
```

#### Настройка в Program.cs

```csharp
// Чтение конфигурации
var accessKey = builder.Configuration["YandexStorage:AccessKey"];
var secretKey = builder.Configuration["YandexStorage:SecretKey"];
var serviceUrl = "https://storage.yandexcloud.net";

// Создание S3 клиента
var credentials = new BasicAWSCredentials(accessKey, secretKey);
var config = new AmazonS3Config
{
    ServiceURL = serviceUrl,
    ForcePathStyle = true  // Важно для Yandex!
};

builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, config));
```

#### Конфигурация

**appsettings.json:**
```json
{
  "YandexStorage": {
    "AccessKey": "",        // Ключ доступа
    "SecretKey": "",        // Секретный ключ
    "BucketName": "",       // Имя бакета
    "ServiceUrl": "https://storage.yandexcloud.net"
  }
}
```

**Kubernetes Secret:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: imagegallery-secrets
stringData:
  yandex-access-key: "YCAJExxxxxxxxx"
  yandex-secret-key: "YCMxxxxxxxxxxxxxx"
  yandex-bucket-name: "imagegallery-bucket"
```

### Создание бакета

```bash
# Через yc CLI
yc storage bucket create \
  --name imagegallery-bucket \
  --default-storage-class standard \
  --public-read

# Или через веб-консоль:
# https://console.cloud.yandex.ru/folders/<folder-id>/storage
```

### Создание ключей доступа

```bash
# Создать сервисный аккаунт
yc iam service-account create --name storage-sa

# Назначить роль
yc resource-manager folder add-access-binding <folder-id> \
  --role storage.editor \
  --subject serviceAccount:<sa-id>

# Создать ключ доступа
yc iam access-key create --service-account-name storage-sa
```

Вывод:
```
access_key:
  id: aje...
  key_id: YCAJExxxxxxxxx          # Это AccessKey
secret: YCMxxxxxxxxxxxxxx         # Это SecretKey
```

---

## 2. 🗄️ Yandex Managed Service for PostgreSQL

### Назначение
Хранение метаданных изображений (имя, размер, дата загрузки, описание).

### Как работает

#### Код интеграции

**ApplicationDbContext.cs:**
```csharp
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public DbSet<ImageMetadata> Images { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImageMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired();
            entity.Property(e => e.StorageUrl).IsRequired();
        });
    }
}
```

#### Настройка в Program.cs

```csharp
// Чтение строки подключения
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    // Использовать PostgreSQL
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Fallback на In-Memory для разработки
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("ImageGalleryDb"));
}
```

#### Конфигурация

**Строка подключения:**
```
Host=<cluster-host>;Port=6432;Database=imagegallery;Username=dbuser;Password=<password>;SSL Mode=Require
```

**Kubernetes Secret:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: imagegallery-secrets
stringData:
  connection-string: "Host=c-xxx.rw.mdb.yandexcloud.net;Port=6432;Database=imagegallery;Username=dbuser;Password=SecurePass123!;SSL Mode=Require"
```

### Создание кластера PostgreSQL

```bash
# Создать кластер
yc managed-postgresql cluster create \
  --name imagegallery-db \
  --environment production \
  --network-name default \
  --postgresql-version 16 \
  --resource-preset s2.micro \
  --disk-size 10 \
  --disk-type network-ssd \
  --user name=dbuser,password=SecurePass123! \
  --database name=imagegallery,owner=dbuser

# Получить хост для подключения
yc managed-postgresql cluster list-hosts imagegallery-db
```

### Схема базы данных

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

---

## 3. ☸️ Yandex Managed Service for Kubernetes (MSK)

### Назначение
Оркестрация и управление контейнерами приложения.

### Как работает

#### Deployment манифест

**k8s-deployment.yaml:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: imagegallery-deployment
spec:
  replicas: 2
  template:
    spec:
      containers:
      - name: imagegallery
        image: cr.yandex/<registry-id>/imagegallery:latest
        ports:
        - containerPort: 8080
        env:
        # Подключение к PostgreSQL
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: imagegallery-secrets
              key: connection-string
        # Yandex Object Storage
        - name: YandexStorage__AccessKey
          valueFrom:
            secretKeyRef:
              name: imagegallery-secrets
              key: yandex-access-key
        - name: YandexStorage__SecretKey
          valueFrom:
            secretKeyRef:
              name: imagegallery-secrets
              key: yandex-secret-key
        - name: YandexStorage__BucketName
          valueFrom:
            secretKeyRef:
              name: imagegallery-secrets
              key: yandex-bucket-name
```

### Создание кластера Kubernetes

```bash
# Создать кластер
yc managed-kubernetes cluster create \
  --name imagegallery-k8s \
  --network-name default \
  --zone ru-central1-a \
  --public-ip \
  --release-channel stable \
  --version 1.28

# Создать группу узлов
yc managed-kubernetes node-group create \
  --name imagegallery-nodes \
  --cluster-name imagegallery-k8s \
  --fixed-size 2 \
  --cores 2 \
  --memory 4

# Настроить kubectl
yc managed-kubernetes cluster get-credentials imagegallery-k8s --external
```

---

## 4. 📦 Yandex Container Registry

### Назначение
Хранение Docker образов приложения.

### Как работает

#### Создание реестра

```bash
# Создать реестр
yc container registry create --name imagegallery-registry

# Получить ID реестра
REGISTRY_ID=$(yc container registry get imagegallery-registry --format json | jq -r .id)

# Настроить Docker
yc container registry configure-docker
```

#### Публикация образа

```bash
# Собрать образ
docker build -t cr.yandex/$REGISTRY_ID/imagegallery:latest .

# Загрузить в реестр
docker push cr.yandex/$REGISTRY_ID/imagegallery:latest
```

#### Использование в Kubernetes

```yaml
spec:
  containers:
  - name: imagegallery
    image: cr.yandex/<registry-id>/imagegallery:latest
    imagePullPolicy: Always
```

---

## 🔄 Полный поток данных

### Загрузка изображения

```
1. Пользователь → Браузер
   POST /Images/Upload (multipart/form-data)
   
2. Браузер → Kubernetes Load Balancer
   HTTP Request
   
3. Load Balancer → Pod (ASP.NET Core)
   Маршрутизация запроса
   
4. ImagesController.Upload()
   - Валидация файла
   - Генерация уникального имени
   
5. YandexStorageService.UploadFileAsync()
   - AWS SDK → Yandex Object Storage
   - Загрузка файла в бакет
   - Получение публичного URL
   
6. ApplicationDbContext.SaveChangesAsync()
   - Entity Framework → PostgreSQL
   - Сохранение метаданных
   
7. Redirect → /Images/Index
   - Отображение галереи
```

### Просмотр галереи

```
1. Пользователь → Браузер
   GET /Images/Index
   
2. ImagesController.Index()
   - Query PostgreSQL для метаданных
   
3. Render View
   - HTML с URL изображений
   
4. Браузер загружает изображения
   - Прямо из Object Storage
   - https://storage.yandexcloud.net/bucket/image.jpg
```

---

## 🔐 Безопасность

### Хранение секретов

**Локально (разработка):**
```json
// appsettings.Development.json
{
  "YandexStorage": {
    "AccessKey": "local-dev-key",
    "SecretKey": "local-dev-secret"
  }
}
```

**Kubernetes (продакшен):**
```yaml
# Kubernetes Secret (зашифрован)
apiVersion: v1
kind: Secret
metadata:
  name: imagegallery-secrets
type: Opaque
stringData:
  yandex-access-key: "YCAJExxxxxxxxx"
  yandex-secret-key: "YCMxxxxxxxxxxxxxx"
```

**Переменные окружения:**
```bash
# Kubernetes Pod получает секреты как env переменные
env:
- name: YandexStorage__AccessKey
  valueFrom:
    secretKeyRef:
      name: imagegallery-secrets
      key: yandex-access-key
```

### IAM роли

```bash
# Сервисный аккаунт для Object Storage
yc iam service-account create --name storage-sa

# Назначить роли
yc resource-manager folder add-access-binding <folder-id> \
  --role storage.editor \
  --subject serviceAccount:<sa-id>

# Сервисный аккаунт для Kubernetes
yc iam service-account create --name k8s-sa

# Назначить роли
yc resource-manager folder add-access-binding <folder-id> \
  --role container-registry.images.puller \
  --subject serviceAccount:<sa-id>
```

---

## 📊 Мониторинг

### Логи приложения

```bash
# Логи в Kubernetes
kubectl logs -f deployment/imagegallery-deployment

# Логи PostgreSQL
yc managed-postgresql cluster list-logs imagegallery-db

# Логи в Yandex Cloud Logging (если настроено)
yc logging read --group-id=<log-group-id>
```

### Метрики

```bash
# Метрики Kubernetes
kubectl top pods
kubectl top nodes

# Метрики PostgreSQL
yc managed-postgresql cluster list-operations imagegallery-db

# Метрики Object Storage
yc storage bucket stats imagegallery-bucket
```

---

## 💰 Стоимость

### Примерная стоимость (месяц)

| Сервис | Конфигурация | Стоимость |
|--------|--------------|-----------|
| Kubernetes | 2 узла (2 CPU, 4GB) | ~$50-80 |
| PostgreSQL | s2.micro, 10GB SSD | ~$20-30 |
| Object Storage | 10GB хранилище | ~$0.50-2 |
| Container Registry | 5GB образов | ~$0.50-1 |
| Load Balancer | Стандартный | ~$10-15 |
| **Итого** | | **~$81-128/месяц** |

### Оптимизация затрат

1. **Preemptible VM** для dev окружения (-70%)
2. **Автомасштабирование** - платите только за использование
3. **Lifecycle policies** для Object Storage
4. **Остановка кластера** в нерабочее время

---

## 🚀 Быстрый старт

### 1. Создать инфраструктуру

```bash
# Object Storage
yc storage bucket create --name imagegallery-bucket --public-read

# PostgreSQL
yc managed-postgresql cluster create \
  --name imagegallery-db \
  --postgresql-version 16 \
  --user name=dbuser,password=SecurePass123! \
  --database name=imagegallery,owner=dbuser

# Kubernetes
yc managed-kubernetes cluster create --name imagegallery-k8s

# Container Registry
yc container registry create --name imagegallery-registry
```

### 2. Получить учетные данные

```bash
# Access Key для Object Storage
yc iam access-key create --service-account-name storage-sa

# Строка подключения PostgreSQL
yc managed-postgresql cluster list-hosts imagegallery-db

# ID реестра
yc container registry get imagegallery-registry --format json | jq -r .id
```

### 3. Настроить секреты

```bash
kubectl create secret generic imagegallery-secrets \
  --from-literal=connection-string="Host=<host>;Port=6432;Database=imagegallery;Username=dbuser;Password=SecurePass123!;SSL Mode=Require" \
  --from-literal=yandex-access-key="YCAJExxxxxxxxx" \
  --from-literal=yandex-secret-key="YCMxxxxxxxxxxxxxx" \
  --from-literal=yandex-bucket-name="imagegallery-bucket"
```

### 4. Развернуть приложение

```bash
# Собрать и загрузить образ
docker build -t cr.yandex/$REGISTRY_ID/imagegallery:latest .
docker push cr.yandex/$REGISTRY_ID/imagegallery:latest

# Применить манифесты
kubectl apply -f k8s-configmap.yaml
kubectl apply -f k8s-deployment.yaml
kubectl apply -f k8s-service.yaml

# Получить внешний IP
kubectl get service imagegallery-service
```

---

## 📚 Дополнительная информация

- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Подробное руководство по развертыванию
- **[QUICKSTART.md](QUICKSTART.md)** - Быстрый старт
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Архитектура приложения

---

## 🎯 Ключевые моменты

1. ✅ **Object Storage** - S3-совместимый API через AWS SDK
2. ✅ **PostgreSQL** - Через Entity Framework Core и Npgsql
3. ✅ **Kubernetes** - Стандартные манифесты с секретами
4. ✅ **Container Registry** - Docker push/pull
5. ✅ **Автоматизация** - CI/CD через GitHub Actions

Все интеграции **готовы к использованию** - нужно только создать ресурсы в Yandex Cloud и настроить учетные данные! 🚀
