# Kubernetes 中 Ocelot 与 CoreDNS 的关系说明

## 核心问题

**使用 K8s 是否意味着 Ocelot 没用了？还是被 CoreDNS 替代了？**

## 简短答案

**Ocelot 仍然有用，CoreDNS 并不是替代 Ocelot，而是帮助 Ocelot 更好地工作！**

两者是**互补关系**，不是竞争关系。

## 详细解释

### Ocelot 的职责

Ocelot 是一个 .NET API 网关，主要负责：

1. **路由转发** - 根据 URL 路径将请求转发到不同的后端服务
2. **认证授权** - JWT 验证、OAuth2 等安全功能
3. **限流熔断** - API 限流、服务熔断保护
4. **负载均衡** - 在多个服务实例间分配流量
5. **请求/响应转换** - 修改请求头、响应数据等
6. **聚合** - 合并多个服务的响应
7. **服务发现集成** - 与 Consul、Eureka 等服务发现工具集成

### CoreDNS 的职责

CoreDNS 是 Kubernetes 的 DNS 服务，主要负责：

1. **服务发现** - 将服务名（如 `game-auth-service`）解析为 Pod IP
2. **DNS 记录管理** - 管理 K8s 中所有服务的 DNS 记录
3. **反向解析** - IP 到服务名的反向查询
4. **自定义 DNS 规则** - 支持复杂的 DNS 重写和转发规则

## 两者在 K8s 中的协作关系

### 架构图

```
                        ┌─────────────────┐
                        │   Ingress       │
                        │   (Nginx)       │
                        └────────┬────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  Ocelot API Gateway     │
                    │  (路由、认证、限流)      │
                    │  ┌──────────────────┐  │
                    │  │   CoreDNS       │  │
                    │  │  (服务发现)      │  │
                    │  └──────┬───────────┘  │
                    └─────────┼──────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                    │
    ┌────▼─────┐        ┌─────▼──────┐      ┌─────▼──────┐
    │  Auth    │        │   Game     │      │   Room     │
    │ Service  │        │  Service   │      │  Service   │
    │  :5001   │        │  :5002     │      │  :5003     │
    └──────────┘        └────────────┘      └────────────┘
```

### 实际工作流程

1. **外部请求** → Ingress → Ocelot API Gateway
2. **Ocelot 需要路由** → 查询 CoreDNS 解析服务名
3. **CoreDNS 返回** → 后端服务的 ClusterIP 或 Pod IP
4. **Ocelot 转发** → 将请求转发到后端服务

## 在 K8s 中配置 Ocelot

### 方式一：Ocelot 使用 K8s Service 发现

```csharp
// Program.cs - Ocelot 配置
var builder = WebApplication.CreateBuilder(args);

// 添加 Ocelot
builder.Services.AddOcelot();

var app = builder.Build();

// 配置 Ocelot
await app.UseOcelot();
app.Run();
```

```json
// ocelot.json - 路由配置
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/auth/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          // 直接使用 K8s Service 名称，CoreDNS 会自动解析
          "Host": "game-auth-service",
          "Port": 5001
        }
      ],
      "UpstreamPathTemplate": "/api/auth/{everything}",
      "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"]
    },
    {
      "DownstreamPathTemplate": "/api/game/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          // 直接使用 K8s Service 名称
          "Host": "game-game-service",
          "Port": 5002
        }
      ],
      "UpstreamPathTemplate": "/api/game/{everything}",
      "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"]
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:8080"
  }
}
```

### 方式二：Ocelot 集成 Consul（推荐用于服务发现）

```json
// ocelot.json
{
  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Host": "consul-service",  // 使用 K8s Service 名称
      "Port": 8500,
      "Type": "Consul"
    }
  },
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/auth/{everything}",
      "DownstreamScheme": "http",
      "DownstreamServiceName": "game-auth-service",  // Consul 服务名
      "UpstreamPathTemplate": "/api/auth/{everything}",
      "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"],
      "UseServiceDiscovery": true
    }
  ]
}
```

### K8s 部署配置

```yaml
# k8s/services/api-gateway-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: game-api-gateway
spec:
  replicas: 2
  selector:
    matchLabels:
      app: game-api-gateway
  template:
    metadata:
      labels:
        app: game-api-gateway
    spec:
      containers:
      - name: api-gateway
        image: game-api-gateway:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        volumeMounts:
        - name: ocelot-config
          mountPath: /app/ocelot.json
          subPath: ocelot.json
      volumes:
      - name: ocelot-config
        configMap:
          name: ocelot-config
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: ocelot-config
data:
  ocelot.json: |
    {
      "Routes": [
        {
          "DownstreamPathTemplate": "/api/auth/{everything}",
          "DownstreamScheme": "http",
          "DownstreamHostAndPorts": [
            {
              "Host": "game-auth-service",  # K8s Service 名称，CoreDNS 解析
              "Port": 5001
            }
          ],
          "UpstreamPathTemplate": "/api/auth/{everything}",
          "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"]
        }
      ],
      "GlobalConfiguration": {
        "BaseUrl": "http://game-api-gateway-service:8080"
      }
    }
---
apiVersion: v1
kind: Service
metadata:
  name: game-api-gateway-service
spec:
  selector:
    app: game-api-gateway
  ports:
  - port: 8080
    targetPort: 8080
  type: ClusterIP
```

## 对比：Ocelot vs K8s Ingress

有人可能会问：**K8s Ingress 也能做路由，为什么还需要 Ocelot？**

### K8s Ingress 的优势

- ✅ 原生支持，不需要额外安装
- ✅ 统一的流量入口管理
- ✅ 支持 HTTPS/TLS
- ✅ 支持基于主机名和路径路由
- ✅ 与 K8s 集成度高

### Ocelot 的优势

- ✅ **应用层路由** - 可以基于请求头、Cookie、JWT Claims 等复杂规则路由
- ✅ **认证授权** - 内置 JWT、OAuth2 支持
- ✅ **限流熔断** - 细粒度的 API 限流和服务熔断
- ✅ **请求/响应转换** - 可以修改请求和响应内容
- ✅ **服务聚合** - 合并多个服务的响应
- ✅ **可编程性** - .NET 代码编写，灵活性高
- ✅ **与 .NET 生态集成** - 与 ASP.NET Core 完美集成

### 推荐架构

```
Internet → K8s Ingress → Ocelot API Gateway → 微服务
         (TLS, 基础路由)  (认证, 限流, 聚合)
```

**两者配合使用**：
- K8s Ingress 处理 HTTPS/TLS 和基础路由
- Ocelot 处理应用层的认证、限流、聚合等复杂逻辑

## 完整的 K8s 配置示例

### 1. 部署所有微服务

```yaml
# k8s/services/auth-service.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: game-auth-service
spec:
  replicas: 2
  selector:
    matchLabels:
      app: game-auth-service
  template:
    metadata:
      labels:
        app: game-auth-service
    spec:
      containers:
      - name: auth-service
        image: game-auth-service:latest
        ports:
        - containerPort: 5001
---
apiVersion: v1
kind: Service
metadata:
  name: game-auth-service  # 这个名字会被 CoreDNS 解析
spec:
  selector:
    app: game-auth-service
  ports:
  - port: 5001
    targetPort: 5001
  type: ClusterIP
```

### 2. 部署 Ocelot Gateway

```yaml
# k8s/gateway/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: game-api-gateway
spec:
  replicas: 2
  selector:
    matchLabels:
      app: game-api-gateway
  template:
    metadata:
      labels:
        app: game-api-gateway
    spec:
      containers:
      - name: api-gateway
        image: game-api-gateway:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: OCELOT_JSON
          value: "/app/config/ocelot.json"
        volumeMounts:
        - name: config
          mountPath: /app/config
      volumes:
      - name: config
        configMap:
          name: ocelot-config
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: ocelot-config
data:
  ocelot.json: |
    {
      "Routes": [
        {
          "DownstreamPathTemplate": "/api/auth/{everything}",
          "DownstreamScheme": "http",
          "DownstreamHostAndPorts": [
            {
              "Host": "game-auth-service",  # ← CoreDNS 自动解析
              "Port": 5001
            }
          ],
          "UpstreamPathTemplate": "/api/auth/{everything}",
          "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"]
        },
        {
          "DownstreamPathTemplate": "/api/game/{everything}",
          "DownstreamScheme": "http",
          "DownstreamHostAndPorts": [
            {
              "Host": "game-game-service",  # ← CoreDNS 自动解析
              "Port": 5002
            }
          ],
          "UpstreamPathTemplate": "/api/game/{everything}",
          "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"]
        },
        {
          "DownstreamPathTemplate": "/api/room/{everything}",
          "DownstreamScheme": "http",
          "DownstreamHostAndPorts": [
            {
              "Host": "game-room-service",  # ← CoreDNS 自动解析
              "Port": 5003
            }
          ],
          "UpstreamPathTemplate": "/api/room/{everything}",
          "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"]
        },
        {
          "DownstreamPathTemplate": "/api/chat/{everything}",
          "DownstreamScheme": "http",
          "DownstreamHostAndPorts": [
            {
              "Host": "game-chat-service",  # ← CoreDNS 自动解析
              "Port": 5004
            }
          ],
          "UpstreamPathTemplate": "/api/chat/{everything}",
          "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"]
        }
      ],
      "GlobalConfiguration": {
        "BaseUrl": "http://game-api-gateway-service:8080"
      }
    }
---
apiVersion: v1
kind: Service
metadata:
  name: game-api-gateway-service
spec:
  selector:
    app: game-api-gateway
  ports:
  - port: 8080
    targetPort: 8080
  type: ClusterIP
```

### 3. 配置 K8s Ingress

```yaml
# k8s/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: game-ingress
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /$1
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - api.game.example.com
    secretName: game-tls-cert
  rules:
  - host: api.game.example.com
    http:
      paths:
      - path: /api/(.*)
        pathType: ImplementationSpecific
        backend:
          service:
            name: game-api-gateway-service  # 转发到 Ocelot
            port:
              number: 8080
```

## 验证 CoreDNS 解析

```bash
# 在 Pod 中测试 DNS 解析
kubectl exec -it game-api-gateway-xxxxx -- nslookup game-auth-service

# 输出示例：
# Server:    10.96.0.10
# Address 1: 10.96.0.10 kube-dns.kube-system.svc.cluster.local
# 
# Name:      game-auth-service.default.svc.cluster.local
# Address 1: 10.100.0.5 game-auth-service.default.svc.cluster.local

# 测试连接
kubectl exec -it game-api-gateway-xxxxx -- curl http://game-auth-service:5001/health
```

## 总结

### 关键要点

1. **Ocelot 不是被替代** - Ocelot 提供 API 网关的核心功能（路由、认证、限流等）
2. **CoreDNS 是辅助工具** - 帮助 Ocelot 解析服务名到 IP 地址
3. **两者协同工作** - CoreDNS 做服务发现，Ocelot 做路由和流量控制
4. **K8s Ingress 是可选的** - 可以作为外层入口，配合 Ocelot 使用

### 决策建议

| 场景 | 推荐方案 |
|------|---------|
| **需要应用层认证、限流** | Ocelot + CoreDNS |
| **简单的路径路由** | K8s Ingress |
| **复杂的服务聚合** | Ocelot + CoreDNS |
| **需要 TLS 终止** | K8s Ingress + Ocelot |
| **微服务内部通信** | CoreDNS 直接访问 |

### 最终推荐

```
┌─────────────────────────────────────────────────┐
│         K8s Ingress (TLS, 基础路由)             │
└────────────────────┬────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────┐
│         Ocelot API Gateway (应用层功能)          │
│  • 认证授权  • 限流熔断  • 服务聚合  • 路由     │
│     ↑          ↑          ↑          ↑          │
│     │          │          │          │          │
│  ┌──┴───┐  ┌──┴───┐  ┌──┴───┐  ┌──┴───┐      │
│  │Core  │  │Core  │  │Core  │  │Core  │      │
│  │DNS   │  │DNS   │  │DNS   │  │DNS   │      │
│  └──┬───┘  └──┬───┘  └──┬───┘  └──┬───┘      │
└─────┼────────┼────────┼────────┼──────────────┘
      │        │        │        │
  ┌───▼──┐ ┌──▼──┐ ┌──▼──┐ ┌──▼──┐
  │ Auth │ │Game │ │Room │ │Chat │
  │SVC   │ │SVC  │ │SVC  │ │SVC  │
  └──────┘ └─────┘ └─────┘ └─────┘
```

**Ocelot 继续使用！CoreDNS 让它工作得更好！**
