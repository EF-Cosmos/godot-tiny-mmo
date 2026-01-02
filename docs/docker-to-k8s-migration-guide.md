# GameBackend Docker Compose 到 Kubernetes 迁移指南

## 概述

本文档详细说明如何将 GameBackend 项目从 Docker Compose 迁移到 Kubernetes，同时保留现有的 Consul 服务发现和 Ocelot API 网关功能。

## 架构对比

### 当前 Docker Compose 架构

```
Docker Compose:
  Consul (端口 8500)
  PostgreSQL (端口 5432)
  Redis (端口 6379)
  API Gateway (端口 5000)
  Auth Service (端口 5001)
  Game Service (端口 5002)
  Room Service (端口 5003)
  Chat Service (端口 5004)
```

### 目标 Kubernetes 架构

```
Kubernetes:
  Namespace: game-backend
  Infrastructure:
    - PostgreSQL (ClusterIP: postgres-service:5432)
    - Redis (ClusterIP: redis-service:6379)
    - Consul (ClusterIP: consul:8500)
  Microservices:
    - API Gateway (ClusterIP: api-gateway-service:8080)
    - Auth Service (ClusterIP: auth-service:8080)
    - Game Service (ClusterIP: game-service:8080)
    - Room Service (ClusterIP: room-service:8080)
    - Chat Service (ClusterIP: chat-service:8080)
  Ingress: game-backend-ingress
```

## 关键变化

### 1. 服务发现机制

**Docker Compose:**
- 服务通过 `docker-compose` 网络互相发现
- 使用服务名（如 `postgres`, `redis`）作为主机名
- Consul 用于更复杂的服务发现

**Kubernetes:**
- CoreDNS 自动解析服务名到 ClusterIP
- 通过 K8s Service 名称互相访问（如 `postgres-service`, `redis-service`）
- Consul 继续提供服务注册和发现功能

### 2. 端口映射

**Docker Compose:**
```yaml
ports:
  - "5000:8080"  # 主机端口:容器端口
```

**Kubernetes:**
```yaml
ports:
  - containerPort: 8080  # 容器内部端口
# 通过 Service 暴露
ports:
  - port: 8080  # Service 端口
    targetPort: 8080  # Pod 端口
```

### 3. 配置管理

**Docker Compose:**
```yaml
environment:
  - ConnectionStrings__PostgreSQL=Host=postgres;...
```

**Kubernetes:**
```yaml
env:
  - name: ConnectionStrings__PostgreSQL
    value: "Host=postgres-service;..."
  - name: DB_PASSWORD
    valueFrom:
      secretKeyRef:
        name: postgres-secret
        key: password
```

## 迁移步骤

### 阶段 1: 准备阶段

#### 1.1 安装 Kubernetes 工具

```bash
# 安装 kubectl
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
chmod +x kubectl
sudo mv kubectl /usr/local/bin/

# 验证安装
kubectl version --client
```

#### 1.2 准备 Kubernetes 集群

**选项 A: 使用 Minikube（本地开发）**
```bash
# 安装 Minikube
curl -Lo minikube https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
chmod +x minikube
sudo mv minikube /usr/local/bin/

# 启动集群
minikube start --driver=docker --cpus=4 --memory=8192

# 启用 Ingress
minikube addons enable ingress
```

**选项 B: 使用云服务商（生产环境）**
- Google Kubernetes Engine (GKE)
- Azure Kubernetes Service (AKS)
- Amazon EKS
- 阿里云 ACK

#### 1.3 构建 Docker 镜像

```bash
cd GameBackend

# 构建所有服务的镜像（假设已有 Dockerfile）
docker build -t game-api-gateway:latest -f docker/Dockerfile.gateway .
docker build -t game-auth-service:latest -f docker/Dockerfile.auth .
docker build -t game-game-service:latest -f docker/Dockerfile.game .
docker build -t game-room-service:latest -f docker/Dockerfile.room .
docker build -t game-chat-service:latest -f docker/Dockerfile.chat .

# 如果使用 Minikube，将镜像加载到 Minikube
minikube image load game-api-gateway:latest
minikube image load game-auth-service:latest
minikube image load game-game-service:latest
minikube image load game-room-service:latest
minikube image load game-chat-service:latest

# 或者推送到容器注册表
docker tag game-api-gateway:latest your-registry.com/game-api-gateway:latest
docker push your-registry.com/game-api-gateway:latest
# 对其他镜像重复以上操作
```

### 阶段 2: 部署基础设施

#### 2.1 创建 Namespace

```bash
kubectl apply -f GameBackend/k8s/namespace.yaml
```

#### 2.2 部署 PostgreSQL

```bash
kubectl apply -f GameBackend/k8s/infrastructure/postgres.yaml

# 等待 PostgreSQL 就绪
kubectl wait --for=condition=ready pod -l app=postgres -n game-backend --timeout=300s

# 查看状态
kubectl get pods -n game-backend
kubectl get pvc -n game-backend
```

#### 2.3 部署 Redis

```bash
kubectl apply -f GameBackend/k8s/infrastructure/redis.yaml

# 等待 Redis 就绪
kubectl wait --for=condition=ready pod -l app=redis -n game-backend --timeout=300s

# 查看状态
kubectl get pods -n game-backend
```

#### 2.4 部署 Consul

```bash
kubectl apply -f GameBackend/k8s/infrastructure/consul.yaml

# 等待 Consul 就绪
kubectl wait --for=condition=ready pod -l app=consul -n game-backend --timeout=300s

# 查看状态
kubectl get pods -n game-backend

# 测试 Consul 连接
kubectl port-forward -n game-backend svc/consul 8500:8500
# 访问 http://localhost:8500
```

### 阶段 3: 部署微服务

#### 3.1 部署 API Gateway

```bash
kubectl apply -f GameBackend/k8s/services/api-gateway.yaml

# 等待部署完成
kubectl rollout status deployment/api-gateway -n game-backend

# 查看状态
kubectl get pods -n game-backend -l app=api-gateway
kubectl get hpa -n game-backend
```

#### 3.2 部署 Auth Service

```bash
kubectl apply -f GameBackend/k8s/services/auth-service.yaml

# 等待部署完成
kubectl rollout status deployment/auth-service -n game-backend

# 查看状态
kubectl get pods -n game-backend -l app=auth-service
```

#### 3.3 部署 Game 和 Room Services

```bash
kubectl apply -f GameBackend/k8s/services/game-room-services.yaml

# 等待部署完成
kubectl rollout status deployment/game-service -n game-backend
kubectl rollout status deployment/room-service -n game-backend
```

#### 3.4 部署 Chat Service

```bash
kubectl apply -f GameBackend/k8s/services/chat-service.yaml

# 等待部署完成
kubectl rollout status deployment/chat-service -n game-backend
```

### 阶段 4: 配置外部访问

#### 4.1 安装 Ingress Controller（如果未安装）

```bash
# 对于 Minikube
minikube addons enable ingress

# 或者手动安装 Nginx Ingress Controller
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.9.4/deploy/static/provider/cloud/deploy.yaml
```

#### 4.2 部署 Ingress

```bash
kubectl apply -f GameBackend/k8s/ingress.yaml

# 查看状态
kubectl get ingress -n game-backend
```

#### 4.3 配置域名（可选）

**本地开发（Minikube）:**
```bash
# 获取 Minikube IP
minikube ip

# 编辑 /etc/hosts
sudo nano /etc/hosts
# 添加: <minikube-ip> api.game.example.com
```

**生产环境:**
- 配置 DNS 记录指向 Ingress LoadBalancer IP
- 或使用 Cloudflare/AWS Route53 等 DNS 服务

### 阶段 5: 验证部署

#### 5.1 检查所有 Pod 状态

```bash
kubectl get pods -n game-backend
kubectl get pvc -n game-backend
kubectl get svc -n game-backend
kubectl get hpa -n game-backend
```

#### 5.2 测试服务连通性

```bash
# 测试 API Gateway
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- sh -c "curl http://api-gateway-service:8080/health" -n game-backend

# 测试 Consul
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- sh -c "curl http://consul:8500/v1/catalog/services" -n game-backend
```

#### 5.3 验证服务注册到 Consul

```bash
# 端口转发访问 Consul UI
kubectl port-forward -n game-backend svc/consul 8500:8500

# 浏览器访问 http://localhost:8500
# 检查 Services 标签页，应该看到：
# - game-api-gateway
# - auth-service
# - game-service
# - room-service
# - chat-service
```

#### 5.4 测试外部访问

```bash
# 如果配置了 Ingress 和域名
curl http://api.game.example.com/api/health

# 本地 Minikube 测试
minikube tunnel  # 在另一个终端运行
curl http://$(minikube ip)/api/health
```

## 配置文件说明

### 关键配置文件列表

| 文件 | 说明 |
|------|------|
| `namespace.yaml` | 创建 game-backend 命名空间 |
| `infrastructure/postgres.yaml` | PostgreSQL 部署和 PVC |
| `infrastructure/redis.yaml` | Redis 部署和 PVC |
| `infrastructure/consul.yaml` | Consul 服务发现 |
| `services/api-gateway.yaml` | API Gateway + Ocelot 配置 |
| `services/auth-service.yaml` | 认证服务 |
| `services/chat-service.yaml` | 聊天服务 |
| `services/game-room-services.yaml` | 游戏和房间服务 |
| `ingress.yaml` | 外部访问入口 |

### Ocelot 配置变化

**Docker Compose 版本 (appsettings.json):**
```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/auth/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",  // 或环境变量
          "Port": 5001
        }
      ]
    }
  ],
  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Host": "localhost",
      "Port": 8500,
      "Type": "Consul"
    }
  }
}
```

**Kubernetes 版本 (ConfigMap):**
```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/auth/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "auth-service",  // K8s Service 名称
          "Port": 8080
        }
      ]
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://api-gateway-service:8080",
    "ServiceDiscoveryProvider": {
      "Host": "consul",  // K8s Service 名称
      "Port": 8500,
      "Type": "Consul"
    }
  }
}
```

## 常见问题

### Q1: Pod 一直处于 Pending 状态

**原因：** PVC 没有绑定到 PV

**解决：**
```bash
kubectl get pvc -n game-backend
kubectl describe pvc postgres-pvc -n game-backend

# 如果使用 Minikube，确保有足够的磁盘空间
minikube ssh
df -h
```

### Q2: 服务无法连接到数据库

**原因：** 服务名或端口配置错误

**解决：**
```bash
# 检查 Service 是否存在
kubectl get svc -n game-backend

# 测试连接
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- sh -c "nc -zv postgres-service 5432" -n game-backend

# 检查 Pod 的环境变量
kubectl exec -it <pod-name> -n game-backend -- env | grep -i postgres
```

### Q3: Consul 服务注册失败

**原因：** Consul 容器还没就绪

**解决：**
```bash
# 等待 Consul 完全启动
kubectl wait --for=condition=ready pod -l app=consul -n game-backend --timeout=300s

# 查看 Consul 日志
kubectl logs -n game-backend -l app=consul --tail=100

# 检查健康检查
kubectl exec -n game-backend consul-xxxxx -- consul health
```

### Q4: HPA 不工作

**原因：** metrics-server 未安装

**解决：**
```bash
# 安装 metrics-server
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

# 验证
kubectl top pods -n game-backend
kubectl get hpa -n game-backend
```

## 运维命令

### 查看日志

```bash
# 查看所有服务日志
kubectl logs -f -n game-backend -l app=api-gateway
kubectl logs -f -n game-backend -l app=auth-service
kubectl logs -f -n game-backend -l app=game-service
kubectl logs -f -n game-backend -l app=room-service
kubectl logs -f -n game-backend -l app=chat-service

# 查看特定 Pod 日志
kubectl logs -f <pod-name> -n game-backend
```

### 扩容/缩容

```bash
# 手动扩容
kubectl scale deployment api-gateway --replicas=3 -n game-backend

# 查看 HPA 状态
kubectl get hpa -n game-backend
kubectl describe hpa api-gateway-hpa -n game-backend
```

### 滚动更新

```bash
# 更新镜像
kubectl set image deployment/api-gateway api-gateway=game-api-gateway:v2.0 -n game-backend

# 查看更新状态
kubectl rollout status deployment/api-gateway -n game-backend

# 回滚到上一个版本
kubectl rollout undo deployment/api-gateway -n game-backend

# 查看更新历史
kubectl rollout history deployment/api-gateway -n game-backend
```

### 故障排查

```bash
# 查看 Pod 详情
kubectl describe pod <pod-name> -n game-backend

# 进入 Pod 调试
kubectl exec -it <pod-name> -n game-backend -- /bin/bash

# 查看事件
kubectl get events -n game-backend --sort-by='.lastTimestamp'

# 查看 Service Endpoints
kubectl get endpoints -n game-backend
kubectl describe svc postgres-service -n game-backend
```

## 性能优化

### 资源限制调整

根据实际情况调整 `resources.requests` 和 `resources.limits`：

```yaml
resources:
  requests:
    cpu: "500m"      # 0.5 CPU 核心
    memory: "512Mi"  # 512 MB 内存
  limits:
    cpu: "1000m"     # 1 CPU 核心
    memory: "1Gi"    # 1 GB 内存
```

### HPA 调整

根据负载情况调整自动扩缩容参数：

```yaml
minReplicas: 2
maxReplicas: 10
metrics:
- type: Resource
  resource:
    name: cpu
    target:
      type: Utilization
      averageUtilization: 70  # 70% CPU 使用率时扩容
```

### 数据库优化

```yaml
# 增加 PostgreSQL 资源
resources:
  requests:
    cpu: "1000m"
    memory: "2Gi"
  limits:
    cpu: "2000m"
    memory: "4Gi"
```

## 生产环境检查清单

- [ ] 修改所有密码和密钥
- [ ] 启用 HTTPS/TLS
- [ ] 配置域名和 DNS
- [ ] 设置资源限制
- [ ] 配置备份策略（数据库持久化）
- [ ] 启用监控和日志（Prometheus + Grafana）
- [ ] 配置告警规则
- [ ] 设置自动滚动更新
- [ ] 配置网络策略（NetworkPolicy）
- [ ] 启用 Pod Disruption Budget
- [ ] 配置 ConfigMap 和 Secret 的版本管理
- [ ] 设置 Pod Security Context
- [ ] 配置 Service Account 和 RBAC

## 回滚方案

如果迁移出现问题，可以快速回滚到 Docker Compose：

```bash
# 1. 删除 K8s 资源
kubectl delete namespace game-backend

# 2. 恢复 Docker Compose
cd GameBackend
docker-compose up -d

# 3. 验证服务
docker-compose ps
```

## 下一步

1. **监控和日志**：部署 Prometheus + Grafana + Loki
2. **CI/CD 集成**：配置 GitHub Actions 或 GitLab CI
3. **自动化测试**：集成单元测试和集成测试
4. **文档更新**：更新 API 文档和运维手册
5. **团队培训**：培训团队使用 Kubernetes

## 参考资料

- [Kubernetes 官方文档](https://kubernetes.io/docs/)
- [Ocelot 文档](https://ocelot.com/)
- [Consul 文档](https://www.consul.io/docs)
- [Minikube 文档](https://minikube.sigs.k8s.io/docs/)
- [Nginx Ingress Controller](https://kubernetes.github.io/ingress-nginx/)

## 总结

通过本迁移指南，您已成功将 GameBackend 从 Docker Compose 迁移到 Kubernetes，同时保留了：

- ✅ Consul 服务发现
- ✅ Ocelot API 网关
- ✅ 所有微服务功能
- ✅ PostgreSQL 和 Redis 数据持久化

Kubernetes 提供了更强大的功能：
- 自动扩缩容（HPA）
- 滚动更新
- 健康检查和自愈
- 服务发现（CoreDNS）
- 配置管理（ConfigMap + Secret）
- 持久化存储（PVC）

祝您使用愉快！🎉
