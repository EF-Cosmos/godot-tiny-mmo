# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Godot Tiny MMO is an experimental open-source MMORPG framework built with Godot 4.4+. The project consists of two parts:

1. **Godot 原生游戏** (`/`) - 使用 GDScript 的客户端和服务器，采用自定义字节打包网络协议
2. **GameBackend** (`/GameBackend`) - 基于 .NET 的微服务后端架构，正在逐步替代原有服务器逻辑

项目正在进行架构重构，逐渐将游戏后端从 GDScript 迁移到 .NET 微服务架构。

## Running the Project

### Godot 原生游戏

1. Open in Godot 4.4 or 4.5
2. Debug → "Customizable Run Instance..." → Enable "Multiple Instances" (4+)
3. Set Feature Tags:
   - Exactly one `gateway-server`
   - Exactly one `master-server`
   - Exactly one `world-server`
   - One or more `client`
4. Optional launch args: `--headless` (servers), `--config=path.cfg`
5. Press F5

Configuration files are in `data/config/` (world_config.cfg, gateway_config.cfg, master_config.cfg, client_config.cfg).

### GameBackend 微服务

```bash
cd GameBackend

# 启动所有服务（需要 Docker）
docker-compose up -d

# 或单独运行各个服务
dotnet run --project Game.ApiGateway
dotnet run --project Game.AuthService
dotnet run --project Game.GameService
dotnet run --project Game.RoomService
dotnet run --project Game.ChatService
```

服务端口：
- **ApiGateway**: `5000`
- **AuthService**: `5298` (HTTP), `7072` (gRPC)
- **ChatService**: `8090`
- **Consul**: `8500`
- **PostgreSQL**: `5432`
- **Redis**: `6379`

## Architecture

### Three-Server Model

- **Gateway Server** (`source/server/gateway/`): HTTP REST API for authentication, routes to master
- **Master Server** (`source/server/master/`): Central orchestrator, account database, bridges gateways and world servers
- **World Server** (`source/server/world/`): Hosts gameplay instances, 10 physics ticks/sec, max 200 players

### Directory Structure

```
source/
├── client/          # Client-only (UI, local player, network clients)
│   ├── autoload/    # ClientState singleton
│   ├── gateway/     # Auth & character selection UI
│   ├── network/     # InstanceClient, WorldClient
│   └── ui/          # HUD, menus, inventory, chat
├── common/          # Shared client+server code
│   ├── gameplay/    # Characters, combat, items, maps, time
│   ├── network/     # Wire protocol, RPC endpoints, state sync
│   └── registry/    # ContentRegistryHub, PathRegistry
└── server/
    ├── gateway/     # REST auth endpoints
    ├── master/      # Account management, orchestration
    └── world/       # Instance management, gameplay handlers
        └── components/data_request_handlers/  # RPC handlers
```

### State Synchronization

The custom netcode uses packed binary format (`PackedByteArray`) via `source/common/network/wire.gd`:
- **StateSynchronizer**: Applies baselines (full state) and deltas (incremental updates)
- **PathRegistry**: Maps property paths to Field IDs (integers) for bandwidth efficiency
- Zone-based updates for spatial optimization

Key sync paths: `:position`, `:flipped`, `:anim`, `:pivot`, `:display_name`, `:skin_id`, `:zone_flags`

### Data Request Pattern

Server RPC handlers follow the `DataRequestHandler` pattern in `source/server/world/components/data_request_handlers/`:
- `action.perform` - Attack/ability execution
- `chat.message.send` - Chat messages
- `attribute.get/spend` - Character stats
- `guild.create/get/search/quit` - Guild operations

Client calls: `InstanceClient.request_data(key, callback, args)` or `InstanceClient.subscribe(key, callback)`

### Content Registry

`ContentRegistryHub` (`source/common/registry/content_registry_hub.gd`) maps slugs to Resource IDs. Indexes auto-load from `source/common/registry/indexes/`.

### Authentication Flow

1. Client → Gateway (HTTP): Login/guest at `/v1/login` or `/v1/guest`
2. Gateway → Master: Get world list, character data
3. Master → Gateway: Generate auth token
4. Client → World Server (WebSocket): Connect with token
5. World Server validates token, spawns player

Gateway API endpoints defined in `source/common/network/gateway_api.gd`.

## Key Classes

- **Character** → **Player** → **LocalPlayer**: Character hierarchy (CharacterBody2D-based)
- **InstanceClient/WorldClient**: Client networking
- **ServerInstance**: Per-map instance on world server (extends SubViewport)
- **WorldServer/InstanceManager**: World server bootstrap and instance management
- **Wire**: Binary serialization protocol

## Database

World server uses QAD format via `WorldDatabase` (`source/server/world/components/world_database.gd`) for PlayerData, Guild, ServerRoles. Master server stores accounts as `.tres` resources.

## Build Separation

Feature tags (`OS.has_feature()`) determine client vs server builds. The TinyMMO plugin (`addons/tinymmo/`) handles export modifications—removes client autoloads on server builds.

---

# GameBackend 微服务架构

## 概述

`GameBackend` 是基于 .NET 10.0 和 ASP.NET Core 的微服务架构，旨在替代原有的 GDScript 服务器。采用云原生设计，支持水平扩展、服务发现、分布式追踪等特性。

## 目录结构

```
GameBackend/
├── Game.ApiGateway/        # API 网关 (Ocelot)
├── Game.AuthService/       # 认证服务
├── Game.GameService/       # 游戏逻辑服务
├── Game.RoomService/       # 房间管理服务
├── Game.ChatService/       # 聊天服务 (SignalR)
├── Game.Shared/            # 共享代码库
│   ├── Consul/            # 服务发现实现
│   ├── Dtos/              # 数据传输对象
│   └── Models/            # 共享模型
└── docker-compose.yml     # 容器编排配置
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

### Game.AuthService (端口: 5298/7072)

**职责**: 用户认证和授权

**核心功能**:
- 用户注册/登录
- JWT 令牌发放和验证
- 用户信息管理
- 健康检查

**关键文件**:
- `Controllers/AuthController.cs`
- `Services/` - 认证业务逻辑

### Game.GameService

**职责**: 游戏核心逻辑

**核心功能**:
- 玩家状态管理
- 游戏规则执行
- 游戏数据持久化
- 与 Godot World Server 的数据同步

**关键文件**:
- `Controllers/` - 游戏逻辑控制器
- `Services/` - 游戏业务服务

### Game.RoomService

**职责**: 房间/实例管理

**核心功能**:
- 游戏房间创建和销毁
- 玩家加入/离开房间
- 房间状态同步
- 配对逻辑

**关键文件**:
- `Controllers/` - 房间管理控制器
- `Services/` - 房间管理服务

### Game.ChatService (端口: 8090)

**职责**: 实时聊天通信

**核心功能**:
- SignalR 实时双向通信
- 房间聊天
- 私聊功能
- 消息持久化 (PostgreSQL)
- 在线用户状态管理 (Redis)

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
  - `ConsulHostedService.cs` - 后台服务健康检查
- `Dtos/` - 数据传输对象 (DTO)
- `Models/` - 共享数据模型
  - `User.cs`, `Game.cs`, `Room.cs`, `Message.cs`
  - `*.MemoryPack.cs` - MemoryPack 序列化

## 技术栈

| 类别 | 技术 |
|------|------|
| **框架** | .NET 10.0, ASP.NET Core 10.0 |
| **API 网关** | Ocelot |
| **服务发现** | Consul |
| **实时通信** | SignalR |
| **数据库** | PostgreSQL + Entity Framework Core |
| **缓存** | Redis (StackExchange.Redis) |
| **认证** | JWT Bearer + ASP.NET Core Identity |
| **追踪** | OpenTelemetry + Jaeger |
| **序列化** | MemoryPack, Newtonsoft.Json |
| **容器化** | Docker + docker-compose |
| **API 文档** | Swagger/OpenAPI |

## 服务间通信

```
                    ┌─────────────────┐
                    │   Godot Client  │
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
  "Routes": [...],      // 路由规则
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
dotnet run --project Game.AuthService

# 查看服务注册
curl http://localhost:8500/v1/agent/services

# 查看 Jaeger 追踪
浏览器打开: http://localhost:16686
```

## 注意事项

1. **服务注册**: 每个服务启动时会自动注册到 Consul
2. **健康检查**: Consul 会定期检查服务健康状态
3. **JWT 认证**: ApiGateway 统一处理认证，下游服务信任网关
4. **序列化**: 高性能场景使用 MemoryPack，通用场景使用 JSON
5. **分布式事务**: 当前未实现，跨服务操作需考虑最终一致性
