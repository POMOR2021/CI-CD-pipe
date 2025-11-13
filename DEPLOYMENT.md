# Руководство по развертыванию

## Подготовка инфраструктуры в Yandex Cloud

### 1. Установка Yandex Cloud CLI

```bash
# Linux/macOS
curl -sSL https://storage.yandexcloud.net/yandexcloud-yc/install.sh | bash

# Windows (PowerShell)
iex (New-Object System.Net.WebClient).DownloadString('https://storage.yandexcloud.net/yandexcloud-yc/install.ps1')

# Инициализация
yc init
```

### 2. Создание сервисного аккаунта

```bash
# Создание сервисного аккаунта
yc iam service-account create --name imagegallery-sa

# Получение ID сервисного аккаунта
SA_ID=$(yc iam service-account get imagegallery-sa --format json | jq -r .id)

# Назначение ролей
yc resource-manager folder add-access-binding <FOLDER_ID> \
  --role editor \
  --subject serviceAccount:$SA_ID

yc resource-manager folder add-access-binding <FOLDER_ID> \
  --role container-registry.images.puller \
  --subject serviceAccount:$SA_ID

# Создание ключа доступа для Object Storage
yc iam access-key create --service-account-name imagegallery-sa

# Создание авторизованного ключа
yc iam key create --service-account-name imagegallery-sa --output key.json
```

### 3. Создание Container Registry

```bash
# Создание реестра
yc container registry create --name imagegallery-registry

# Получение ID реестра
REGISTRY_ID=$(yc container registry get imagegallery-registry --format json | jq -r .id)

# Настройка Docker для работы с реестром
yc container registry configure-docker
```

### 4. Создание Object Storage бакета

```bash
# Создание бакета
yc storage bucket create \
  --name imagegallery-bucket-$(date +%s) \
  --default-storage-class standard \
  --max-size 10737418240

# Настройка CORS для бакета
cat > cors.json <<EOF
{
  "CORSRules": [
    {
      "AllowedOrigins": ["*"],
      "AllowedMethods": ["GET", "PUT", "POST", "DELETE"],
      "AllowedHeaders": ["*"],
      "MaxAgeSeconds": 3000
    }
  ]
}
EOF

yc storage bucket update --name imagegallery-bucket-<timestamp> --cors-file cors.json

# Настройка публичного доступа на чтение
yc storage bucket update --name imagegallery-bucket-<timestamp> --public-read
```

### 5. Создание PostgreSQL кластера

```bash
# Создание сети и подсети (если нет)
yc vpc network create --name imagegallery-network
yc vpc subnet create \
  --name imagegallery-subnet \
  --network-name imagegallery-network \
  --zone ru-central1-a \
  --range 10.128.0.0/24

# Создание PostgreSQL кластера
yc managed-postgresql cluster create \
  --name imagegallery-db \
  --environment production \
  --network-name imagegallery-network \
  --postgresql-version 16 \
  --resource-preset s2.micro \
  --host zone-id=ru-central1-a,subnet-name=imagegallery-subnet \
  --disk-size 10 \
  --disk-type network-ssd \
  --user name=dbuser,password=SecurePassword123! \
  --database name=imagegallery,owner=dbuser

# Получение хоста для подключения
yc managed-postgresql cluster list-hosts imagegallery-db
```

### 6. Создание Kubernetes кластера

```bash
# Создание кластера
yc managed-kubernetes cluster create \
  --name imagegallery-k8s \
  --network-name imagegallery-network \
  --zone ru-central1-a \
  --subnet-name imagegallery-subnet \
  --public-ip \
  --release-channel stable \
  --version 1.28 \
  --service-account-name imagegallery-sa \
  --node-service-account-name imagegallery-sa

# Создание группы узлов
yc managed-kubernetes node-group create \
  --name imagegallery-nodes \
  --cluster-name imagegallery-k8s \
  --platform standard-v3 \
  --cores 2 \
  --memory 4 \
  --disk-type network-ssd \
  --disk-size 30 \
  --fixed-size 2 \
  --location zone=ru-central1-a,subnet-name=imagegallery-subnet \
  --network-interface subnets=imagegallery-subnet,ipv4-address=nat

# Настройка kubectl
yc managed-kubernetes cluster get-credentials imagegallery-k8s --external
```

## Развертывание приложения

### 1. Сборка и публикация Docker образа

```bash
# Сборка образа
docker build -t cr.yandex/$REGISTRY_ID/imagegallery:latest .

# Публикация в Container Registry
docker push cr.yandex/$REGISTRY_ID/imagegallery:latest
```

### 2. Настройка Kubernetes секретов

```bash
# Создание секрета с учетными данными
kubectl create secret generic imagegallery-secrets \
  --from-literal=connection-string="Host=<postgres-host>;Port=6432;Database=imagegallery;Username=dbuser;Password=SecurePassword123!;SSL Mode=Require" \
  --from-literal=yandex-access-key="<ACCESS_KEY>" \
  --from-literal=yandex-secret-key="<SECRET_KEY>" \
  --from-literal=yandex-bucket-name="imagegallery-bucket-<timestamp>"
```

### 3. Применение манифестов

```bash
# Применить ConfigMap
kubectl apply -f k8s-configmap.yaml

# Применить Deployment
# Сначала обновите image в k8s-deployment.yaml
sed -i "s/<your-registry-id>/$REGISTRY_ID/g" k8s-deployment.yaml
kubectl apply -f k8s-deployment.yaml

# Применить Service
kubectl apply -f k8s-service.yaml

# Проверить статус
kubectl get pods
kubectl get services
```

### 4. Настройка Load Balancer

```bash
# Получить внешний IP
kubectl get service imagegallery-service

# Дождаться назначения внешнего IP
kubectl get service imagegallery-service -w
```

### 5. Настройка Ingress (опционально)

```bash
# Установка Ingress Controller
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml

# Применить Ingress
kubectl apply -f k8s-ingress.yaml

# Проверить статус
kubectl get ingress
```

## Настройка CI/CD

### 1. Настройка GitHub Secrets

Перейдите в Settings → Secrets and variables → Actions и добавьте:

```
YC_REGISTRY_KEY - содержимое key.json
YC_REGISTRY_ID - ID Container Registry
YC_SA_JSON_CREDENTIALS - содержимое key.json
YC_CLOUD_ID - ID облака
YC_FOLDER_ID - ID каталога
YC_K8S_CLUSTER_NAME - imagegallery-k8s
```

### 2. Получение необходимых ID

```bash
# Cloud ID
yc config get cloud-id

# Folder ID
yc config get folder-id

# Registry ID
yc container registry get imagegallery-registry --format json | jq -r .id
```

### 3. Проверка pipeline

```bash
# Push в main ветку запустит автоматическое развертывание
git add .
git commit -m "Initial deployment"
git push origin main
```

## Мониторинг и обслуживание

### Просмотр логов

```bash
# Логи приложения
kubectl logs -f deployment/imagegallery-deployment

# Логи всех подов
kubectl logs -f -l app=imagegallery

# Логи за последний час
kubectl logs --since=1h deployment/imagegallery-deployment
```

### Масштабирование

```bash
# Ручное масштабирование
kubectl scale deployment imagegallery-deployment --replicas=3

# Автомасштабирование
kubectl autoscale deployment imagegallery-deployment \
  --min=2 \
  --max=10 \
  --cpu-percent=80
```

### Обновление приложения

```bash
# Обновление образа
kubectl set image deployment/imagegallery-deployment \
  imagegallery=cr.yandex/$REGISTRY_ID/imagegallery:v2

# Откат к предыдущей версии
kubectl rollout undo deployment/imagegallery-deployment

# Проверка статуса обновления
kubectl rollout status deployment/imagegallery-deployment
```

### Резервное копирование

```bash
# Экспорт конфигурации
kubectl get all -o yaml > backup.yaml

# Резервное копирование базы данных
yc managed-postgresql cluster backup imagegallery-db
```

## Устранение неполадок

### Проблемы с подключением к базе данных

```bash
# Проверка доступности PostgreSQL
kubectl run -it --rm debug --image=postgres:16 --restart=Never -- \
  psql "host=<postgres-host> port=6432 dbname=imagegallery user=dbuser password=SecurePassword123! sslmode=require"
```

### Проблемы с Object Storage

```bash
# Проверка доступа к бакету
kubectl run -it --rm debug --image=amazon/aws-cli --restart=Never -- \
  s3 ls s3://imagegallery-bucket-<timestamp> \
  --endpoint-url=https://storage.yandexcloud.net
```

### Проблемы с образом

```bash
# Проверка образа в реестре
yc container image list --registry-id=$REGISTRY_ID

# Проверка pull секрета
kubectl get secret -n default
```

## Безопасность

### Обновление секретов

```bash
# Обновление секрета
kubectl delete secret imagegallery-secrets
kubectl create secret generic imagegallery-secrets \
  --from-literal=connection-string="<new-connection-string>" \
  --from-literal=yandex-access-key="<new-access-key>" \
  --from-literal=yandex-secret-key="<new-secret-key>" \
  --from-literal=yandex-bucket-name="<bucket-name>"

# Перезапуск подов для применения изменений
kubectl rollout restart deployment/imagegallery-deployment
```

### Настройка Network Policies

```bash
# Создание Network Policy для ограничения трафика
kubectl apply -f - <<EOF
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: imagegallery-network-policy
spec:
  podSelector:
    matchLabels:
      app: imagegallery
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - podSelector: {}
    ports:
    - protocol: TCP
      port: 8080
  egress:
  - to:
    - podSelector: {}
  - to:
    - namespaceSelector: {}
    ports:
    - protocol: TCP
      port: 443
    - protocol: TCP
      port: 6432
EOF
```

## Оптимизация затрат

### Использование preemptible узлов

```bash
# Создание группы узлов с preemptible VM
yc managed-kubernetes node-group create \
  --name imagegallery-preemptible-nodes \
  --cluster-name imagegallery-k8s \
  --platform standard-v3 \
  --cores 2 \
  --memory 4 \
  --disk-type network-ssd \
  --disk-size 30 \
  --fixed-size 1 \
  --preemptible \
  --location zone=ru-central1-a,subnet-name=imagegallery-subnet
```

### Настройка автоматического выключения

```bash
# Остановка кластера в нерабочее время (через cron или GitHub Actions)
yc managed-kubernetes cluster stop imagegallery-k8s

# Запуск кластера
yc managed-kubernetes cluster start imagegallery-k8s
```

## Удаление ресурсов

```bash
# Удаление Kubernetes ресурсов
kubectl delete -f k8s-deployment.yaml
kubectl delete -f k8s-service.yaml
kubectl delete -f k8s-configmap.yaml
kubectl delete secret imagegallery-secrets

# Удаление Kubernetes кластера
yc managed-kubernetes cluster delete imagegallery-k8s

# Удаление PostgreSQL кластера
yc managed-postgresql cluster delete imagegallery-db

# Удаление бакета
yc storage bucket delete imagegallery-bucket-<timestamp>

# Удаление Container Registry
yc container registry delete imagegallery-registry

# Удаление сети
yc vpc subnet delete imagegallery-subnet
yc vpc network delete imagegallery-network
```
