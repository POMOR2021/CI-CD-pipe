# Устранение проблем

## Проблема с NuGet (NU1301 - SSL connection error)

### Причины
- Проблемы с интернет-соединением
- Блокировка файрволом или антивирусом
- Проблемы с SSL сертификатами
- Прокси-сервер

### Решения

#### 1. Очистка кэша NuGet
```powershell
dotnet nuget locals all --clear
```

#### 2. Восстановление с дополнительными параметрами
```powershell
dotnet restore --disable-parallel --force --no-cache
```

#### 3. Использование HTTP вместо HTTPS (временно)
Создайте или отредактируйте `nuget.config`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="http://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

#### 4. Отключение антивируса временно
Некоторые антивирусы блокируют SSL соединения NuGet.

#### 5. Проверка прокси
```powershell
# Проверить настройки прокси
netsh winhttp show proxy

# Сбросить прокси
netsh winhttp reset proxy
```

#### 6. Обновление .NET SDK
```powershell
# Проверить версию
dotnet --version

# Скачать последнюю версию с https://dotnet.microsoft.com/download
```

#### 7. Ручная загрузка пакетов
Если ничего не помогает, можно скачать пакеты вручную:

1. Перейдите на https://www.nuget.org
2. Найдите нужные пакеты:
   - AWSSDK.S3 (3.7.307.19)
   - Microsoft.EntityFrameworkCore (8.0.0)
   - Microsoft.EntityFrameworkCore.InMemory (8.0.0)
   - Npgsql.EntityFrameworkCore.PostgreSQL (8.0.0)
   - Microsoft.EntityFrameworkCore.Tools (8.0.0)

3. Скачайте .nupkg файлы

4. Создайте локальный источник:
```powershell
# Создать папку для пакетов
mkdir C:\LocalNuGet

# Скопировать .nupkg файлы в C:\LocalNuGet

# Добавить локальный источник
dotnet nuget add source C:\LocalNuGet --name LocalPackages
```

5. Обновите `nuget.config`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="LocalPackages" value="C:\LocalNuGet" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

#### 8. Использование Visual Studio
Откройте проект в Visual Studio 2022 и используйте встроенный Package Manager:
- Tools → NuGet Package Manager → Manage NuGet Packages for Solution
- Restore пакеты через GUI

#### 9. Проверка сетевых настроек
```powershell
# Проверить доступность NuGet
Test-NetConnection api.nuget.org -Port 443

# Проверить DNS
nslookup api.nuget.org

# Проверить через curl
curl https://api.nuget.org/v3/index.json
```

#### 10. Временное решение - использовать другой источник
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <!-- MyGet зеркало -->
    <add key="myget" value="https://www.myget.org/F/nuget-mirror/api/v3/index.json" />
    <!-- Официальный источник -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

### Для корпоративной сети

Если вы за корпоративным прокси:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <config>
    <add key="http_proxy" value="http://proxy.company.com:8080" />
    <add key="http_proxy.user" value="domain\username" />
    <add key="http_proxy.password" value="password" />
  </config>
</configuration>
```

---

## Другие распространенные проблемы

### Ошибка компиляции

**Проблема:** Ошибки компиляции C#

**Решение:**
```powershell
# Очистить проект
dotnet clean

# Пересобрать
dotnet build
```

### Порт уже используется

**Проблема:** `Address already in use`

**Решение:**
```powershell
# Найти процесс на порту 5000
netstat -ano | findstr :5000

# Убить процесс (замените PID)
taskkill /PID <PID> /F

# Или изменить порт в appsettings.json
```

### Docker проблемы

**Проблема:** Docker образ не собирается

**Решение:**
```powershell
# Очистить Docker кэш
docker system prune -a

# Пересобрать без кэша
docker build --no-cache -t imagegallery:latest .
```

### Kubernetes проблемы

**Проблема:** Поды не запускаются

**Решение:**
```bash
# Проверить логи
kubectl logs <pod-name>

# Проверить события
kubectl describe pod <pod-name>

# Проверить секреты
kubectl get secret imagegallery-secrets -o yaml
```

### База данных недоступна

**Проблема:** Не удается подключиться к PostgreSQL

**Решение:**
1. Проверить строку подключения
2. Проверить доступность хоста
3. Проверить учетные данные
4. Проверить firewall правила

```powershell
# Тест подключения
Test-NetConnection <postgres-host> -Port 5432
```

---

## Контакты

Если проблема не решена:
1. Проверьте логи приложения
2. Создайте Issue на GitHub
3. Обратитесь к документации
