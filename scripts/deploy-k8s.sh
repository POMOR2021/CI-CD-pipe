#!/bin/bash

# Скрипт для развертывания в Kubernetes

set -e

echo "🚀 Starting Kubernetes deployment..."

# Проверка наличия kubectl
if ! command -v kubectl &> /dev/null; then
    echo "❌ kubectl not found. Please install kubectl first."
    exit 1
fi

# Проверка подключения к кластеру
echo "🔍 Checking cluster connection..."
if ! kubectl cluster-info &> /dev/null; then
    echo "❌ Cannot connect to Kubernetes cluster. Please configure kubectl."
    exit 1
fi

# Применение ConfigMap
echo "📝 Applying ConfigMap..."
kubectl apply -f k8s-configmap.yaml

# Проверка наличия секретов
if ! kubectl get secret imagegallery-secrets &> /dev/null; then
    echo "⚠️  Secret 'imagegallery-secrets' not found."
    echo "Please create it first using:"
    echo "kubectl create secret generic imagegallery-secrets \\"
    echo "  --from-literal=connection-string='<your-connection-string>' \\"
    echo "  --from-literal=yandex-access-key='<your-access-key>' \\"
    echo "  --from-literal=yandex-secret-key='<your-secret-key>' \\"
    echo "  --from-literal=yandex-bucket-name='<your-bucket-name>'"
    exit 1
fi

# Применение Deployment
echo "🚢 Applying Deployment..."
kubectl apply -f k8s-deployment.yaml

# Применение Service
echo "🌐 Applying Service..."
kubectl apply -f k8s-service.yaml

# Ожидание готовности подов
echo "⏳ Waiting for pods to be ready..."
kubectl wait --for=condition=ready pod -l app=imagegallery --timeout=300s

# Проверка статуса
echo "✅ Checking deployment status..."
kubectl get pods -l app=imagegallery
kubectl get services imagegallery-service

# Получение внешнего IP
echo ""
echo "🌍 Getting external IP..."
EXTERNAL_IP=$(kubectl get service imagegallery-service -o jsonpath='{.status.loadBalancer.ingress[0].ip}')

if [ -z "$EXTERNAL_IP" ]; then
    echo "⏳ External IP is being assigned. Please wait and check with:"
    echo "kubectl get service imagegallery-service"
else
    echo "✨ Deployment completed!"
    echo "🌐 Application is available at: http://$EXTERNAL_IP"
fi

echo ""
echo "📊 To view logs: kubectl logs -f deployment/imagegallery-deployment"
echo "📈 To scale: kubectl scale deployment imagegallery-deployment --replicas=3"
echo "🔄 To update: kubectl set image deployment/imagegallery-deployment imagegallery=<new-image>"
