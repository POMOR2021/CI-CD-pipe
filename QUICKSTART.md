# Быстрый старт

## 🚀 Локальный запуск (без Docker)

### Требования
- .NET 8.0 SDK
- Visual Studio 2022 или VS Code

### Шаги

1. **Восстановить пакеты**
```bash
dotnet restore
```

2. **Запустить приложение**
```bash
dotnet run
```

3. **Открыть в браузере**
```
https://localhost:5001
```

Приложение будет использовать:
- In-Memory базу данных
- Локальное хранилище файлов в `wwwroot/uploads`

---

## 🐳 Локальный запуск с Docker Compose

### Требования
- Docker Desktop

### Шаги

1. **Создать .env файл**
```bash
cp .env.example .env
```

2. **Запустить контейнеры**
```bash
docker-compose up -d
```

3. **Открыть в браузере**
```
http://localhost:8080
```

Приложение будет использовать:
- PostgreSQL в контейнере
- Локальное хранилище файлов

### Остановка
```bash
docker-compose down
```

---

## ☸️ Развертывание в Kubernetes (Yandex Cloud)

### Требования
- Yandex Cloud аккаунт
- yc CLI
- kubectl

### Быстрое развертывание

1. **Создать инфраструктуру**
```bash
# PostgreSQL
yc managed-postgresql cluster create \
  --name imagegallery-db \
  --environment production \
  --network-name default \
  --postgresql-version 16 \
  --resource-preset s2.micro \
  --disk-size 10 \
  --user name=dbuser,password=SecurePass123! \
  --database name=imagegallery,owner=dbuser

# Object Storage бакет
yc storage bucket create --name imagegallery-bucket-$(date +%s)

# Kubernetes кластер
yc managed-kubernetes cluster create \
  --name imagegallery-k8s \
  --network-name default \
  --zone ru-central1-a \
  --public-ip

# Node group
yc managed-kubernetes node-group create \
  --name imagegallery-nodes \
  --cluster-name imagegallery-k8s \
  --fixed-size 2 \
  --cores 2 \
  --memory 4

# Container Registry
yc container registry create --name imagegallery-registry
```

2. **Собрать и загрузить образ**
```bash
# Получить ID реестра
REGISTRY_ID=$(yc container registry get imagegallery-registry --format json | jq -r .id)

# Собрать и загрузить
docker build -t cr.yandex/$REGISTRY_ID/imagegallery:latest .
docker push cr.yandex/$REGISTRY_ID/imagegallery:latest
```

3. **Настроить kubectl**
```bash
yc managed-kubernetes cluster get-credentials imagegallery-k8s --external
```

4. **Создать секреты**
```bash
kubectl create secret generic imagegallery-secrets \
  --from-literal=connection-string="Host=<postgres-host>;Port=6432;Database=imagegallery;Username=dbuser;Password=SecurePass123!;SSL Mode=Require" \
  --from-literal=yandex-access-key="<ACCESS_KEY>" \
  --from-literal=yandex-secret-key="<SECRET_KEY>" \
  --from-literal=yandex-bucket-name="<BUCKET_NAME>"
```

5. **Развернуть приложение**
```bash
# Обновить image в k8s-deployment.yaml
sed -i "s/<your-registry-id>/$REGISTRY_ID/g" k8s-deployment.yaml

# Применить манифесты
kubectl apply -f k8s-configmap.yaml
kubectl apply -f k8s-deployment.yaml
kubectl apply -f k8s-service.yaml

# Дождаться готовности
kubectl wait --for=condition=ready pod -l app=imagegallery --timeout=300s

# Получить внешний IP
kubectl get service imagegallery-service
```

6. **Открыть приложение**
```
http://<EXTERNAL-IP>
```

---

## 🔄 Настройка CI/CD

### GitHub Actions

1. **Добавить секреты в GitHub**

Перейти в: `Settings → Secrets and variables → Actions`

Добавить:
- `YC_REGISTRY_KEY` - JSON ключ сервисного аккаунта
- `YC_REGISTRY_ID` - ID Container Registry
- `YC_SA_JSON_CREDENTIALS` - JSON ключ сервисного аккаунта
- `YC_CLOUD_ID` - ID облака
- `YC_FOLDER_ID` - ID каталога
- `YC_K8S_CLUSTER_NAME` - имя кластера

2. **Push в main**
```bash
git add .
git commit -m "Deploy application"
git push origin main
```

Pipeline автоматически:
- Соберет приложение
- Создаст Docker образ
- Загрузит в Container Registry
- Развернет в Kubernetes

---

## 📊 Проверка работы

### Локально

```bash
# Проверка здоровья
curl http://localhost:8080/health

# Просмотр логов
docker-compose logs -f webapp
```

### Kubernetes

```bash
# Статус подов
kubectl get pods -l app=imagegallery

# Логи
kubectl logs -f deployment/imagegallery-deployment

# Проверка здоровья
kubectl exec -it deployment/imagegallery-deployment -- curl http://localhost:8080/health
```

---

## 🧪 Тестирование функционала

1. **Открыть главную страницу**
   - Должна отобразиться страница с описанием

2. **Перейти в галерею**
   - Нажать "Перейти к галерее"
   - Должна отобразиться пустая галерея

3. **Загрузить изображение**
   - Нажать "Загрузить изображение"
   - Выбрать файл (JPG, PNG, GIF, WEBP)
   - Добавить описание (опционально)
   - Нажать "Загрузить"

4. **Просмотреть галерею**
   - Изображение должно появиться в галерее
   - Клик на изображение открывает модальное окно

5. **Просмотреть детали**
   - Нажать "Детали"
   - Должна отобразиться информация о файле

6. **Удалить изображение**
   - Нажать "Удалить"
   - Подтвердить удаление
   - Изображение должно исчезнуть из галереи

---

## 🛠️ Полезные команды

### Docker

```bash
# Пересобрать образ
docker-compose build --no-cache

# Просмотр логов
docker-compose logs -f

# Остановить и удалить контейнеры
docker-compose down -v

# Зайти в контейнер
docker-compose exec webapp bash
```

### Kubernetes

```bash
# Масштабирование
kubectl scale deployment imagegallery-deployment --replicas=3

# Обновление образа
kubectl set image deployment/imagegallery-deployment imagegallery=cr.yandex/$REGISTRY_ID/imagegallery:v2

# Откат
kubectl rollout undo deployment/imagegallery-deployment

# Просмотр событий
kubectl get events --sort-by='.lastTimestamp'

# Описание пода
kubectl describe pod <pod-name>
```

### Yandex Cloud

```bash
# Список кластеров
yc managed-kubernetes cluster list

# Список бакетов
yc storage bucket list

# Список образов в реестре
yc container image list --registry-id=$REGISTRY_ID

# Логи PostgreSQL
yc managed-postgresql cluster list-logs imagegallery-db
```

---

## 🐛 Устранение проблем

### Приложение не запускается

```bash
# Проверить логи
kubectl logs deployment/imagegallery-deployment

# Проверить события
kubectl describe pod <pod-name>

# Проверить секреты
kubectl get secret imagegallery-secrets -o yaml
```

### Не подключается к БД

```bash
# Проверить строку подключения
kubectl get secret imagegallery-secrets -o jsonpath='{.data.connection-string}' | base64 -d

# Проверить доступность PostgreSQL
kubectl run -it --rm debug --image=postgres:16 --restart=Never -- \
  psql "<connection-string>"
```

### Не загружаются изображения

```bash
# Проверить ключи Object Storage
kubectl get secret imagegallery-secrets -o jsonpath='{.data.yandex-access-key}' | base64 -d

# Проверить доступ к бакету
yc storage bucket get <bucket-name>
```

---

## 📚 Дополнительная документация

- [README.md](README.md) - Полная документация
- [DEPLOYMENT.md](DEPLOYMENT.md) - Подробное руководство по развертыванию
- [ARCHITECTURE.md](ARCHITECTURE.md) - Архитектура приложения

---

## 💡 Советы

1. **Для разработки** используйте локальный запуск без Docker
2. **Для тестирования** используйте Docker Compose
3. **Для продакшена** используйте Kubernetes в Yandex Cloud
4. **Всегда проверяйте** логи при возникновении проблем
5. **Используйте** preemptible VM для экономии в dev окружении
6. **Настройте** автомасштабирование для продакшена
7. **Регулярно делайте** бэкапы базы данных
8. **Мониторьте** использование ресурсов и затраты

---

## 🎯 Следующие шаги

После успешного запуска:

1. ✅ Настроить доменное имя
2. ✅ Добавить SSL сертификат
3. ✅ Настроить мониторинг
4. ✅ Настроить алерты
5. ✅ Добавить автотесты
6. ✅ Настроить резервное копирование
7. ✅ Оптимизировать производительность
8. ✅ Добавить CDN для изображений
