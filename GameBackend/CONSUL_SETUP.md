# API Gateway Consul 服务发现配置

## 概述

本文档说明如何配置和检测API Gateway的Consul服务发现功能。

## 架构说明

### 服务发现组件

1. **Consul Server** - 服务注册和发现中心
2. **API Gateway** - 作为微服务网关注册到Consul
3. **健康检查** - 定期检查服务状态
4. **服务检测脚本** - 自动化测试工具

### Consul应该发现的服务

- `game-api-gateway` (端口5000) - API网关服务
- `game-auth-service` (端口5001) - 认证服务
- `game-game-service` (端口5002) - 游戏逻辑服务
- `game-room-service` (端口5003) - 房间管理服务
- `game-chat-service` (端口5004) - 聊天服务

## 快速开始

### 1. 启动Consul

```bash
# 使用Docker启动Consul
docker run -d --name consul -p 8500:8500 consul:latest

# 或者使用docker-compose
docker-compose up -d consul
```

### 2. 启动API Gateway

```bash
cd GameBackend
dotnet run --project src/Game.ApiGateway
```

### 3. 验证服务注册

访问Consul UI: http://localhost:8500

或者使用测试脚本:

```powershell
# Windows PowerShell
.\test-consul-discovery.ps1

# Linux/Mac Bash
chmod +x test-consul-discovery.sh
./test-consul-discovery.sh
```

## 配置说明

### API Gateway配置 (appsettings.json)

```json
{
  "Consul": {
    "ServiceName": "game-api-gateway",
    "Host": "localhost",
    "Port": 5000,
    "ConsulHost": "localhost",
    "ConsulPort": 8500,
    "Tags": ["api-gateway", "game", "microservice"],
    "HealthCheck": {
      "Endpoint": "/health",
      "IntervalSeconds": 10,
      "TimeoutSeconds": 5,
      "DeregisterCriticalServicesAfterSeconds": 30
    }
  }
}
```

### 健康检查端点

API Gateway提供 `/api/health` 端点，返回:

```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "service": "Game.ApiGateway",
  "version": "1.0.0",
  "port": 5000,
  "dependencies": {
    "AuthService": {
      "status": "healthy",
      "statusCode": 200,
      "url": "http://localhost:5001"
    },
    "GameService": {
      "status": "unreachable",
      "error": "Connection refused",
      "url": "http://localhost:5002"
    }
  }
}
```

## 检测方法

### 方法1: Consul UI

1. 访问 http://localhost:8500
2. 点击 "Services" 菜单
3. 查看 `game-api-gateway` 服务状态
4. 检查健康检查状态

### 方法2: Consul API

```bash
# 查看所有服务
curl http://localhost:8500/v1/catalog/services

# 查看API Gateway详情
curl http://localhost:8500/v1/catalog/service/game-api-gateway

# 查看健康状态
curl http://localhost:8500/v1/health/service/game-api-gateway

# 只查看健康的服务实例
curl http://localhost:8500/v1/health/service/game-api-gateway?passing=true
```

### 方法3: 健康检查端点

```bash
# API Gateway健康检查
curl http://localhost:5000/api/health

# 其他服务健康检查
curl http://localhost:5001/health  # Auth Service
curl http://localhost:5004/health  # Chat Service
```

### 方法4: 自动化测试脚本

运行完整的检测脚本:

```powershell
.\test-consul-discovery.ps1
```

脚本会检查:
1. Consul服务状态
2. API Gateway健康状态
3. 服务注册状态
4. 服务发现功能

## 故障排除

### 常见问题

1. **Consul未启动**
   ```bash
   # 检查Consul状态
   curl http://localhost:8500/v1/status/leader
   ```

2. **API Gateway未注册到Consul**
   - 检查appsettings.json中的Consul配置
   - 查看API Gateway启动日志
   - 确认Consul连接正常

3. **健康检查失败**
   ```bash
   # 检查健康检查端点
   curl http://localhost:5000/api/health
   
   # 查看Consul中的健康检查状态
   curl http://localhost:8500/v1/health/service/game-api-gateway
   ```

4. **端口冲突**
   - 确认端口5000和8500未被占用
   - 修改配置文件中的端口设置

### 日志查看

```bash
# Consul日志
docker logs consul

# API Gateway日志
# 在API Gateway启动终端中查看输出
```

## 生产环境注意事项

1. **安全性**
   - 启用Consul ACL
   - 使用HTTPS通信
   - 配置防火墙规则

2. **高可用性**
   - 部署多个Consul节点
   - 配置集群模式
   - 设置健康检查超时

3. **性能优化**
   - 调整健康检查间隔
   - 配置服务缓存
   - 监控Consul性能

## 下一步

1. 为其他微服务添加Consul配置
2. 配置服务间负载均衡
3. 实现服务网格功能
4. 添加监控和告警
