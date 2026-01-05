# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 提供项目指导。

## 项目概述

Godot Tiny MMO 是一个实验性开源 MMORPG 框架，基于 Godot 4.5 和 .NET 10.0 构建。项目由两部分组成：

1. **Godot 原生游戏** (`/`) - 使用 GDScript 的客户端和服务器，采用自定义字节打包网络协议
2. **GameBackend** (`/GameBackend`) - 基于 .NET 10.0 的微服务后端架构，正在逐步替代原有服务器逻辑

项目正在进行架构重构，逐渐将游戏后端从 GDScript 迁移到 .NET 微服务架构。

---

## 运行项目

### 混合架构开发环境 (推荐)

本项目采用 **Docker 后端 + 本地 Godot 进程** 的混合开发模式。

#### 1. 启动后端基础设施 (Docker)

```bash
cd GameBackend
docker-compose up -d
```
这将启动 Consul, Postgres, Redis, API Gateway, Auth/Game/Room/Chat Services。

#### 2. 启动 Godot 服务器实例 (本地)

需要打开 3 个终端窗口，分别运行以下命令（或在 Godot 编辑器中运行对应场景）：

**Master Server** (协调中心):
```bash
godot --headless source/server/master/master_main.tscn
```

**Gateway Server** (客户端入口):
```bash
godot --headless source/server/gateway/gateway_main.tscn
```

**World Server** (游戏逻辑):
```bash
godot --headless source/server/world/world_main.tscn
```

#### 3. 启动客户端

```bash
godot source/client/client_main.tscn
```

### 配置文件

- **Godot 配置**: `data/config/*.cfg`
- **Docker 配置**: `GameBackend/docker-compose.yml`

---

## 架构

### 混合架构概览

- **基础设施层 (Docker)**: 负责通用业务逻辑、数据持久化、服务发现。
  - **API Gateway (:5000)**: 统一 HTTP 入口。
  - **Auth Service (:5001)**: 用户认证。
  - **Game Service (:5002)**: 游戏数据持久化。
  - **Chat Service (:5004)**: 实时聊天 (SignalR)。
- **游戏服务器层 (Godot Headless)**: 负责实时游戏逻辑、物理模拟。
  - **Gateway Server (:8088)**: 处理客户端连接，负载均衡。
  - **Master Server (:8064)**: 协调 Gateway 和 World，管理注册。
  - **World Server (:8087)**: 运行实际游戏地图。

### 目录结构

```
source/
├── client/          # 客户端专用代码
│   ├── autoload/    # ClientState 单例
│   ├── gateway/     # 认证和角色选择 UI
│   ├── network/     # InstanceClient, WorldClient
│   │   └── microservices/  # 微服务客户端 (ChatServiceManager)
│   └── ui/          # HUD、菜单、背包、聊天
├── common/          # 客户端+服务器共享代码
│   ├── gameplay/    # 角色、战斗、物品、地图、时间
│   ├── network/     # Wire 协议、RPC 端点、状态同步
│   └── registry/    # ContentRegistryHub, PathRegistry
└── server/
    ├── gateway/     # Godot Gateway Server (连接入口)
    ├── master/      # Godot Master Server (协调器)
    └── world/       # Godot World Server (游戏逻辑)
GameBackend/         # .NET 微服务后端
    ├── src/         # 微服务源码
    └── docker/      # Docker 配置
```

### 状态同步

自定义网络代码使用字节打包格式 (`PackedByteArray`)，通过 `source/common/network/wire.gd`:
- **StateSynchronizer**: 应用基线 (完整状态) 和增量 (更新)
- **PathRegistry**: 将属性路径映射到字段 ID (整数) 以节省带宽
- 基于区域的空间更新优化

**关键同步路径**: `:position`, `:flipped`, `:anim`, `:pivot`, `:display_name`, `:skin_id`, `:zone_flags`

### 数据请求模式

服务器 RPC 处理器遵循 `DataRequestHandler` 模式，位于 `source/server/world/components/data_request_handlers/`:

| 处理器 | 用途 |
|--------|------|
| `action.perform.gd` | 攻击/技能执行 |
| `attribute.get.gd` | 获取角色属性 |
| `attribute.spend.gd` | 消耗属性点 |
| `chat.message.send.gd` | 发送聊天消息 |
| `chat.command.exec.gd` | 执行聊天命令 |
| `guild.create.gd` | 创建公会 |
| `guild.get.gd` | 获取公会信息 |
| `guild.search.gd` | 搜索公会 |
| `guild.quit.gd` | 离开公会 |
| `guild.self.gd` | 获取玩家公会 |
| `inventory.get.gd` | 获取玩家背包 |
| `item.equip.gd` | 装备物品 |
| `profile.get.gd` | 获取玩家资料 |
| `get.server_time.gd` | 获取服务器时间 |

**客户端调用**: `InstanceClient.request_data(key, callback, args)` 或 `InstanceClient.subscribe(key, callback)`

### 内容注册表

`ContentRegistryHub` (`source/common/registry/content_registry_hub.gd`) 将 slug 映射到资源 ID。索引从 `source/common/registry/indexes/` 自动加载。

### 认证流程

1. 客户端 → API Gateway (HTTP): 在 `/api/auth/login` 或 `/api/auth/guest` 登录/游客
2. API Gateway → Auth Service: 验证凭据，返回 JWT Token
3. 客户端 → Godot Gateway Server (ENet): 使用 Token 连接
4. Godot Gateway → Master: 验证 Token
5. Master → Godot Gateway: 返回可用的 World Server 信息
6. 客户端 → Godot World Server (ENet): 连接并进入游戏

Gateway API 端点定义在 `source/common/network/gateway_api.gd`:

```
POST /api/auth/login              - 用户登录
POST /api/auth/guest              - 游客登录
POST /api/auth/register           - 创建账户
POST /api/game/world/characters   - 获取玩家角色
POST /api/game/world/character/create - 创建角色
POST /api/game/world/enter        - 进入世界
```

## 核心类

- **Character** → **Player** → **LocalPlayer**: 角色层级结构 (基于 CharacterBody2D)
- **InstanceClient/WorldClient**: 客户端网络
- **ServerInstance**: World 服务器上的每地图实例 (继承 SubViewport)
- **WorldServer/InstanceManager**: World 服务器启动和实例管理
- **Wire**: 二进制序列化协议

## 数据库

World 服务器使用 QAD 格式，通过 `WorldDatabase` (`source/server/world/components/world_database.gd`) 存储 PlayerData、Guild、ServerRoles。Master 服务器将账户存储为 `.tres` 资源。

## 构建分离

特性标签 (`OS.has_feature()`) 决定客户端还是服务器构建。TinyMMO 插件 (`addons/tinymmo/`) 处理导出修改——在服务器构建时移除客户端 autoload。

---

# GameBackend 微服务架构

## 概述

`GameBackend` 是基于 .NET 10.0 和 ASP.NET Core 的微服务架构，旨在替代原有的 GDScript 服务器。采用云原生设计，支持水平扩展、服务发现、分布式追踪等特性。

## 目录结构

```
GameBackend/
├── src/
│   ├── Game.ApiGateway/        # API 网关 (Ocelot)
│   ├── Game.AuthService/       # 认证服务
│   ├── Game.GameService/       # 游戏逻辑服务
│   ├── Game.RoomService/       # 房间管理服务
│   └── Game.ChatService/       # 聊天服务 (SignalR)
├── shared/
│   └── Game.Shared/            # 共享代码库
│       ├── Consul/            # 服务发现实现
│       ├── Dtos/              # 数据传输对象
│       └── Models/            # 共享模型
├── docker/                     # Docker 配置
├── k8s/                        # Kubernetes 清单
└── docker-compose.yml          # 容器编排配置
```

## 微服务详解

### Game.ApiGateway (端口: 5000)

**职责**: 统一入口、路由转发、认证鉴权

**核心功能**:
- Ocelot 路由配置 (`OcelotConfiguration/ocelot.json`)
- JWT Bearer 认证
- CORS 策略
- 负载均衡（与服务发现集成）
- 全局中间件（异常处理、限流）

**关键文件**:
- `Program.cs` - 启动配置
- `Middleware/` - 自定义中间件
- `Services/` - 各微服务的客户端封装

### Game.AuthService (端口: 5001)

**职责**: 用户认证和授权

**核心功能**:
- 用户注册/登录
- JWT 令牌发放和验证
- 用户信息管理
- 健康检查
- PostgreSQL 数据持久化

**关键文件**:
- `Controllers/AuthController.cs`
- `Data/AuthDbContext.cs` - EF Core 数据上下文
- `Services/` - 认证业务逻辑

### Game.GameService (端口: 5002)

**职责**: 游戏核心逻辑

**核心功能**:
- 玩家状态管理
- 游戏规则执行
- 游戏数据持久化
- 与 Godot World Server 的数据同步

**关键文件**:
- `Controllers/` - 游戏逻辑控制器
- `Services/` - 游戏业务服务

### Game.RoomService (端口: 5003)

**职责**: 房间/实例管理

**核心功能**:
- 游戏房间创建和销毁
- 玩家加入/离开房间
- 房间状态同步
- 匹配逻辑
- 内存中服务器管理 (GameServerManager)

**关键文件**:
- `Controllers/` - 房间管理控制器
- `Services/` - 房间管理服务

### Game.ChatService (端口: 5004)

**职责**: 实时聊天通信

**核心功能**:
- SignalR 实时双向通信
- 房间聊天
- 私聊功能
- 消息持久化 (PostgreSQL)
- 在线用户状态管理 (内存/Redis)
- 多频道支持

**关键文件**:
- `Hubs/ChatHub.cs` - SignalR Hub
- `Controllers/ChatController.cs` - REST API
- `Data/ChatDbContext.cs` - EF Core 数据上下文

### Game.Shared

**职责**: 跨服务共享代码

**内容**:
- `Consul/` - 服务发现和注册
  - `ConsulExtensions.cs` - 服务注册扩展
  - `ConsulClient.cs` - Consul 客户端封装
  - `ConsulServiceOptions.cs` - 服务配置选项
- `Dtos/` - 数据传输对象 (DTO)
- `Models/` - 共享数据模型
  - `User.cs`, `Game.cs`, `Room.cs`, `Message.cs`
  - `*.MemoryPack.cs` - MemoryPack 序列化

## 技术栈

| 类别 | 技术 |
|------|------|
| **框架** | .NET 10.0, ASP.NET Core 10.0 |
| **API 网关** | Ocelot 23.4.2 |
| **服务发现** | Consul 1.15 |
| **实时通信** | SignalR (ASP.NET Core) |
| **数据库** | PostgreSQL 15 + EF Core 10.0 |
| **缓存** | Redis 7 (StackExchange.Redis) - *可选/部分集成* |
| **认证** | JWT Bearer |
| **追踪** | OpenTelemetry + Jaeger |
| **序列化** | MemoryPack 1.10.0, Newtonsoft.Json |
| **容器化** | Docker + docker-compose |
| **编排** | Kubernetes |

## 服务间通信

```
                    ┌─────────────────┐
                    │   Godot 客户端  │
                    └────────┬────────┘
                             │ HTTP/SignalR
                             ▼
                    ┌─────────────────┐
                    │  ApiGateway     │
                    │   (Ocelot)      │
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        ▼                    ▼                    ▼
┌───────────────┐    ┌──────────────┐    ┌──────────────┐
│  AuthService  │    │ GameService  │    │ RoomService  │
└───────┬───────┘    └──────┬───────┘    └──────┬───────┘
        │                   │                   │
        └───────────────────┼───────────────────┘
                            ▼
                    ┌───────────────┐
                    │   PostgreSQL  │
                    └───────────────┘
```

## 配置文件

### docker-compose.yml

定义所有服务的容器编排：
- Consul (服务发现)
- PostgreSQL (数据库)
- Redis (缓存)
- Jaeger (分布式追踪)
- 各微服务实例

### Ocelot 配置 (ocelot.json)

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/Auth/{everything}",
      "UpstreamPathTemplate": "/api/auth/{everything}",
      "ServiceName": "auth-service"
    },
    {
      "DownstreamPathTemplate": "/api/{everything}",
      "UpstreamPathTemplate": "/api/game/{everything}",
      "ServiceName": "game-service"
    },
    {
      "DownstreamPathTemplate": "/api/{everything}",
      "UpstreamPathTemplate": "/api/room/{everything}",
      "ServiceName": "room-service"
    },
    {
      "DownstreamPathTemplate": "/api/{everything}",
      "UpstreamPathTemplate": "/api/chat/{everything}",
      "ServiceName": "chat-service"
    }
  ],
  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Type": "Consul",
      "Host": "localhost",
      "Port": 8500
    }
  }
}
```

## 数据库设计

### ChatService 数据库

| 表 | 说明 |
|----|------|
| Messages | 聊天消息记录 |
| Rooms | 聊天房间 |
| Participants | 房间参与者 |

### AuthService 数据库

| 表 | 说明 |
|----|------|
| Users | 用户账户信息 |

## 迁移计划

| 阶段 | 内容 |
|------|------|
| **Phase 1** | AuthService 上线，处理用户认证 |
| **Phase 2** | ChatService 上线，替代原有聊天系统 |
| **Phase 3** | RoomService 上线，管理游戏实例 |
| **Phase 4** | GameService 上线，逐步迁移游戏逻辑 |
| **Phase 5** | 完全替换 GDScript 服务器，Godot 仅保留客户端 |

## 开发指南

### 添加新微服务

1. 创建新项目: `dotnet new webapi -n Game.NewService`
2. 添加 Consul 注册: 使用 `Game.Shared` 中的扩展
3. 在 ApiGateway 的 `ocelot.json` 添加路由
4. 在 `docker-compose.yml` 添加服务定义
5. 在 `Game.Shared/Models` 添加共享模型

### 调试

```bash
# 启动基础设施
docker-compose up -d consul postgresql redis

# 启动单个服务（开发模式）
dotnet run --project src/Game.AuthService

# 查看服务注册
curl http://localhost:8500/v1/agent/services

# 查看 Jaeger 追踪
浏览器打开: http://localhost:16686
```

### 数据库迁移

```bash
# 添加迁移
dotnet ef migrations add MigrationName --project src/Game.AuthService

# 应用迁移
dotnet ef database update --project src/Game.AuthService
```

## 注意事项

1. **服务注册**: 每个服务启动时会自动注册到 Consul
2. **健康检查**: Consul 会定期检查服务健康状态
3. **JWT 认证**: ApiGateway 统一处理认证，下游服务信任网关
4. **序列化**: 高性能场景使用 MemoryPack，通用场景使用 JSON
5. **分布式事务**: 当前未实现，跨服务操作需考虑最终一致性
6. **CORS**: 各服务配置了适当的 CORS 策略以允许跨域请求

## 与 Godot 前端集成

### 聊天微服务集成

在 Godot 项目中启用聊天微服务：

1. **配置启用** - 在 `data/config/master_config.cfg` 中添加：
```ini
[chat-service]
enabled=true
address="127.0.0.1"
port=5004
protocol="http"
ws_endpoint="/chatHub"
jwt_key="development-key-for-jwt-signing-change-in-production"
```

2. **使用聊天管理器**:
```gdscript
# 初始化聊天服务
var chat_manager = ChatServiceManager.get_instance()
await chat_manager.initialize(player_id, username, display_name)

# 发送消息
chat_manager.send_message(room_id, "Hello World!", "Text")

# 加入房间
chat_manager.join_room(1, username)  # 1 = 全局聊天

# 监听消息
chat_manager.message_received.connect(_on_message_received)

func _on_message_received(message: Dictionary):
    print("收到消息: %s" % message.text)
```

3. **数据处理器集成**:
现有的聊天数据处理器 (`source/server/world/components/data_request_handlers/chat.message.send.gd`) 已自动集成微服务支持，会：
- 优先使用聊天微服务
- 微服务不可用时自动降级到本地聊天
- 保持与现有 UI 和系统的完全兼容
