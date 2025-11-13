# 🔧 Настройка ВАШЕГО Yandex Cloud

## ⚠️ ВАЖНО!

Я создал **ШАБЛОНЫ** - готовую структуру приложения.

Теперь **ВЫ** должны:
1. ✅ Создать ресурсы в **ВАШЕМ** Yandex Cloud
2. ✅ Получить **ВАШИ** учетные данные
3. ✅ Заполнить **ВАШИ** данные в конфигурационных файлах
4. ✅ Настроить **ВАШ** GitHub репозиторий

---

## Шаг 1: Создать ресурсы в Yandex Cloud

### 1.1 Установить yc CLI

```bash
# Windows (PowerShell)
iex (New-Object System.Net.WebClient).DownloadString('https://storage.yandexcloud.net/yandexcloud-yc/install.ps1')

# Linux/macOS
curl -sSL https://storage.yandexcloud.net/yandexcloud-yc/install.sh | bash

# Инициализация
yc init
```

### 1.2 Создать сервисный аккаунт

```bash
# Создать сервисный аккаунт
yc iam service-account create --name imagegallery-sa

# Получить ID
SA_ID=$(yc iam service-account get imagegallery-sa --format json | jq -r .id)

# Назначить роли
yc resource-manager folder add-access-binding <ВАШ_FOLDER_ID> \
  --role editor \
  --subject serviceAccount:$SA_ID
```

### 1.3 Создать Object Storage бакет

```bash
# Создать бакет (имя должно быть уникальным!)
yc storage bucket create \
  --name imagegallery-bucket-$(date +%s) \
  --default-storage-class standard \
  --public-read

# Запишите имя бакета!
# Например: imagegallery-bucket-1699747200
```

### 1.4 Получить ключи доступа для Object Storage

```bash
# Создать ключ доступа
yc iam access-key create --service-account-name imagegallery-sa

# Вывод:
# access_key:
#   id: aje...
#   service_account_id: aje...
#   created_at: "2025-11-12T00:00:00Z"
#   key_id: YCAJExxxxxxxxxxxxxxxxx          ← ВАШ ACCESS KEY
# secret: YCMxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx ← ВАШ SECRET KEY

# ⚠️ СОХРАНИТЕ ЭТИ КЛЮЧИ! Они больше не будут показаны!
```

### 1.5 Создать PostgreSQL кластер

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
  --user name=dbuser,password=ВАШ_ПАРОЛЬ_ЗДЕСЬ \
  --database name=imagegallery,owner=dbuser

# Получить хост для подключения
yc managed-postgresql cluster list-hosts imagegallery-db

# Вывод:
# +---+------+--------+--------+-------------------+
# |...| NAME | ROLE   | HEALTH | ZONE              |
# +---+------+--------+--------+-------------------+
# |...| rc1a | MASTER | ALIVE  | ru-central1-a     |
# +---+------+--------+--------+-------------------+

# Полный хост будет: c-xxx.rw.mdb.yandexcloud.net
```

### 1.6 Создать Container Registry

```bash
# Создать реестр
yc container registry create --name imagegallery-registry

# Получить ID реестра
yc container registry get imagegallery-registry --format json | jq -r .id

# Вывод: crp1234567890abcdef ← ВАШ REGISTRY ID

# Настроить Docker
yc container registry configure-docker
```

### 1.7 Создать Kubernetes кластер

```bash
# Создать кластер
yc managed-kubernetes cluster create \
  --name imagegallery-k8s \
  --network-name default \
  --zone ru-central1-a \
  --public-ip \
  --release-channel stable \
  --service-account-name imagegallery-sa \
  --node-service-account-name imagegallery-sa

# Создать группу узлов
yc managed-kubernetes node-group create \
  --name imagegallery-nodes \
  --cluster-name imagegallery-k8s \
  --platform standard-v3 \
  --cores 2 \
  --memory 4 \
  --disk-type network-ssd \
  --disk-size 30 \
  --fixed-size 2

# Настроить kubectl
yc managed-kubernetes cluster get-credentials imagegallery-k8s --external
```

---

## Шаг 2: Заполнить конфигурационные файлы

### 2.1 Файл `k8s-secret.yaml`

**ОТКРОЙТЕ ФАЙЛ И ЗАПОЛНИТЕ:**

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: imagegallery-secrets
  namespace: default
type: Opaque
stringData:
  # PostgreSQL connection string
  connection-string: "Host=c-xxx.rw.mdb.yandexcloud.net;Port=6432;Database=imagegallery;Username=dbuser;Password=ВАШ_ПАРОЛЬ;SSL Mode=Require"
  
  # Yandex Object Storage credentials
  yandex-access-key: "YCAJExxxxxxxxxxxxxxxxx"      # ← ВАШ ACCESS KEY
  yandex-secret-key: "YCMxxxxxxxxxxxxxxxxxxxxxxx"  # ← ВАШ SECRET KEY
  yandex-bucket-name: "imagegallery-bucket-1699747200"  # ← ВАШЕ ИМЯ БАКЕТА
```

### 2.2 Файл `k8s-deployment.yaml`

**ОТКРОЙТЕ ФАЙЛ И ЗАМЕНИТЕ:**

Найдите строку:
```yaml
image: cr.yandex/<your-registry-id>/imagegallery:latest
```

Замените на:
```yaml
image: cr.yandex/crp1234567890abcdef/imagegallery:latest
```
(используйте ВАШ REGISTRY ID)

**ИЛИ используйте команду:**
```bash
# Автоматическая замена
REGISTRY_ID=$(yc container registry get imagegallery-registry --format json | jq -r .id)
sed -i "s/<your-registry-id>/$REGISTRY_ID/g" k8s-deployment.yaml
```

---

## Шаг 3: Настроить GitHub

### 3.1 Создать репозиторий на GitHub

1. Перейдите на https://github.com
2. Нажмите **New repository**
3. Назовите репозиторий (например: `imagegallery`)
4. Создайте репозиторий

### 3.2 Загрузить код в GitHub

```bash
# Инициализировать git
cd C:\Users\konuh\source\repos\WebApplication27
git init

# Добавить файлы
git add .
git commit -m "Initial commit"

# Добавить remote
git remote add origin https://github.com/ВАШ_USERNAME/imagegallery.git

# Загрузить код
git branch -M main
git push -u origin main
```

### 3.3 Настроить GitHub Secrets

Перейдите в **Settings → Secrets and variables → Actions** и добавьте:

| Secret Name | Значение | Как получить |
|-------------|----------|--------------|
| `YC_REGISTRY_KEY` | JSON ключ | `yc iam key create --service-account-name imagegallery-sa --output key.json` |
| `YC_REGISTRY_ID` | ID реестра | `yc container registry get imagegallery-registry --format json \| jq -r .id` |
| `YC_SA_JSON_CREDENTIALS` | JSON ключ | Тот же файл `key.json` |
| `YC_CLOUD_ID` | ID облака | `yc config get cloud-id` |
| `YC_FOLDER_ID` | ID каталога | `yc config get folder-id` |
| `YC_K8S_CLUSTER_NAME` | Имя кластера | `imagegallery-k8s` |

**Как добавить секрет:**
1. Нажмите **New repository secret**
2. Введите **Name** (например: `YC_REGISTRY_KEY`)
3. Вставьте **Value** (содержимое `key.json`)
4. Нажмите **Add secret**

---

## Шаг 4: Развернуть приложение

### 4.1 Собрать и загрузить Docker образ

```bash
# Получить ID реестра
REGISTRY_ID=$(yc container registry get imagegallery-registry --format json | jq -r .id)

# Собрать образ
docker build -t cr.yandex/$REGISTRY_ID/imagegallery:latest .

# Загрузить в реестр
docker push cr.yandex/$REGISTRY_ID/imagegallery:latest
```

### 4.2 Применить Kubernetes манифесты

```bash
# Применить ConfigMap
kubectl apply -f k8s-configmap.yaml

# Применить Secret (ПОСЛЕ заполнения!)
kubectl apply -f k8s-secret.yaml

# Применить Deployment
kubectl apply -f k8s-deployment.yaml

# Применить Service
kubectl apply -f k8s-service.yaml

# Проверить статус
kubectl get pods
kubectl get services
```

### 4.3 Получить внешний IP

```bash
# Получить IP адрес
kubectl get service imagegallery-service

# Вывод:
# NAME                    TYPE           EXTERNAL-IP      PORT(S)
# imagegallery-service    LoadBalancer   51.250.xxx.xxx   80:xxxxx/TCP

# Откройте в браузере:
# http://51.250.xxx.xxx
```

---

## Шаг 5: Настроить CI/CD

После того как вы:
1. ✅ Создали GitHub репозиторий
2. ✅ Загрузили код
3. ✅ Добавили GitHub Secrets

**CI/CD будет работать автоматически!**

При каждом push в `main` ветку:
1. Соберется Docker образ
2. Загрузится в Container Registry
3. Развернется в Kubernetes

---

## 📋 Чек-лист

Отметьте выполненные шаги:

### Yandex Cloud
- [ ] Установлен yc CLI
- [ ] Создан сервисный аккаунт
- [ ] Создан Object Storage бакет
- [ ] Получены Access Key и Secret Key
- [ ] Создан PostgreSQL кластер
- [ ] Создан Container Registry
- [ ] Создан Kubernetes кластер

### Конфигурация
- [ ] Заполнен `k8s-secret.yaml`
- [ ] Обновлен `k8s-deployment.yaml` (registry ID)
- [ ] Собран Docker образ
- [ ] Образ загружен в Registry

### GitHub
- [ ] Создан репозиторий
- [ ] Код загружен в GitHub
- [ ] Добавлены все GitHub Secrets

### Развертывание
- [ ] Применены Kubernetes манифесты
- [ ] Поды запущены
- [ ] Service создан
- [ ] Получен внешний IP
- [ ] Приложение доступно в браузере

---

## 🆘 Помощь

### Получить все ID сразу

```bash
echo "=== ВАШИ ДАННЫЕ ==="
echo "Cloud ID: $(yc config get cloud-id)"
echo "Folder ID: $(yc config get folder-id)"
echo "Registry ID: $(yc container registry get imagegallery-registry --format json | jq -r .id)"
echo "Bucket Name: $(yc storage bucket list --format json | jq -r '.[0].name')"
echo "PostgreSQL Host: $(yc managed-postgresql cluster list-hosts imagegallery-db --format json | jq -r '.[0].name').mdb.yandexcloud.net"
```

### Проверить статус ресурсов

```bash
# Object Storage
yc storage bucket list

# PostgreSQL
yc managed-postgresql cluster list

# Kubernetes
yc managed-kubernetes cluster list

# Container Registry
yc container registry list
```

### Удалить все ресурсы (если нужно начать заново)

```bash
# ⚠️ ОСТОРОЖНО! Это удалит ВСЕ ресурсы!

# Kubernetes
yc managed-kubernetes cluster delete imagegallery-k8s

# PostgreSQL
yc managed-postgresql cluster delete imagegallery-db

# Container Registry
yc container registry delete imagegallery-registry

# Object Storage
yc storage bucket delete imagegallery-bucket-xxxxx
```

---

## 💰 Стоимость

Примерная стоимость ресурсов:
- Kubernetes (2 узла): ~$50-80/месяц
- PostgreSQL (s2.micro): ~$20-30/месяц
- Object Storage (10GB): ~$0.50-2/месяц
- Container Registry: ~$0.50-1/месяц

**Итого: ~$71-113/месяц**

Не забудьте **остановить или удалить** ресурсы после тестирования!

---

## ✅ Готово!

После выполнения всех шагов у вас будет:
- ✅ Работающее приложение в Yandex Cloud
- ✅ Автоматический CI/CD через GitHub Actions
- ✅ Хранение изображений в Object Storage
- ✅ Метаданные в PostgreSQL
- ✅ Масштабируемое развертывание в Kubernetes

**Приложение будет доступно по внешнему IP адресу!** 🚀
