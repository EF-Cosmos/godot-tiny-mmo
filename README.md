> [!NOTE]
> 项目文档网站: [**ef-cosmos.github.io/godot-tiny-mmo/**](https://ef-cosmos.github.io/godot-tiny-mmo/)

[![Godot Engine](https://img.shields.io/badge/Godot-4.5+-blue?logo=godot-engine)](https://godotengine.org/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](https://opensource.org/licenses/MIT)

# Godot Tiny MMO

**实验性开源 MMORPG 框架**，基于 **Godot 4.5** 和 **.NET 10.0** 构建。
本项目探索了 Godot 在大型多人游戏中的可能性，采用高效字节打包网络协议和现代微服务后端架构。

## 项目概述

项目由两部分组成：

1. **Godot 原生游戏** (`/`) - 使用 GDScript 的客户端和服务器，采用自定义字节打包网络协议
2. **GameBackend** (`/GameBackend`) - 基于 .NET 10.0 的微服务后端架构，正在逐步替代原有服务器

```
                    ┌─────────────────┐
                    │  Godot 客户端   │
                    └────────┬────────┘
                             │
              ┌──────────────┴──────────────┐
              ▼                             ▼
     ┌─────────────────┐          ┌─────────────────┐
     │  Gateway 服务器 │          │  API 网关       │
     │   (GDScript)    │          │  (Ocelot/5000)  │
     └────────┬────────┘          └────────┬────────┘
              │                             │
              ▼                             ▼
     ┌─────────────────┐          ┌─────────────────┐
     │  Master 服务器  │◄────────►│   微服务集群    │
     │   (GDScript)    │          │  (.NET/Consul)  │
     └────────┬────────┘          └─────────────────┘
              │
              ▼
     ┌─────────────────┐
     │  World 服务器   │
     │  (游戏逻辑)     │
     └─────────────────┘
```

## 核心特性

- **跨平台**: 浏览器 + 桌面 + 移动端
- **统一代码库**: 客户端和多服务器在同一仓库
  - 快速迭代，一处开发，一键测试
  - 独立的导出预设，分离客户端/服务器构建
- **自定义网络代码**
  - 不依赖 Godot 的 `MultiplayerSynchronizer/Spawner`，获得更好的控制
  - 高效的字节打包协议 (`PackedByteArray` 通过 `Wire.gd`)
  - 基于区域的空间更新优化
- **真正的 MMO 架构**
  - **Gateway 服务器**: HTTP REST API 处理认证 (`/v1/login`, `/v1/guest`, `/v1/account/create`)
  - **Master 服务器**: 中央协调器，账户管理，连接 gateway 和 world 服务器
  - **World 服务器**: 托管游戏实例 (10 物理 tick/秒，每实例最多 200 玩家)
- **微服务后端** (开发中)
  - **API Gateway** (端口 5000): 基于 Ocelot 的路由和 JWT 认证
  - **AuthService** (端口 5001): 用户注册、登录、JWT 令牌
  - **GameService** (端口 5002): 游戏状态管理和规则
  - **RoomService** (端口 5003): 房间/实例管理，Redis 缓存
  - **ChatService** (端口 5004): 基于 SignalR 的实时聊天，PostgreSQL 持久化
  - 通过 Consul 服务发现，Jaeger 分布式追踪

---

## 功能列表

<details>
<summary>已实现功能:</summary>

- [x] **客户端-服务器连接** 通过 `WebSocketMultiplayerPeer`
- [x] **支持浏览器和桌面端运行**
- [x] **网络架构** (三服务器模型)
- [x] **认证系统** 通过 gateway 服务器的登录界面
- [x] **账户创建** 支持永久玩家账户
- [x] **服务器选择界面** 让玩家选择不同服务器
- [x] **QAD 数据库** 保存持久化数据
- [x] **游客登录** 快速访问选项
- [x] **游戏版本检查** 确保客户端兼容性
- [x] **角色创建** 支持职业选择
- [x] **基础 RPG 职业系统**: 骑士、盗贼、法师
- [x] **武器系统** - 每个职业至少一把可用武器
- [x] **基础战斗系统** 支持攻击动作
- [x] **实体同步** 同一实例内的玩家
- [x] **基于实例的聊天** 本地通信
- [x] **基于实例的地图** 实例间旅行
  - [x] **三张地图**: 主世界、地下城入口、地下城
- [x] **服务器端 NPC** (AI 逻辑在服务器处理)
- [x] **数据请求模式**: `action.perform`、`chat.message.send`、`attribute.get/spend`、公会操作

</details>

<details>
<summary>计划功能:</summary>

- [ ] **实体插值** 处理橡皮筋效应
- [ ] **私有实例** 单人或小队
- [ ] **服务器端反作弊** (速度黑客、传送验证等)
- [ ] **完整的微服务迁移** (所有服务迁移到 .NET 后端)

</details>

---

## 快速开始

### Godot 原生游戏

1. 在 **Godot 4.4 或 4.5** 中打开项目
2. 进入 Debug 选项卡，选择 **"Customizable Run Instance..."**
3. 启用 **Multiple Instances** 并将数量设置为 **4 或更多**
4. 在 **Feature Tags** 下，确保设置:
   - 恰好 **一个** `gateway-server` 标签
   - 恰好 **一个** `master-server` 标签
   - 恰好 **一个** `world-server` 标签
   - 至少 **一个或多个** `client` 标签
5. (可选) 在 **Launch Arguments** 下:
   - 对于服务器，添加 **--headless** 防止空窗口
   - 对于任何实例，添加 **--config=path.cfg** 使用非默认配置路径
6. 运行项目 (按 F5)

**配置文件** 位于 `data/config/`:
- `client_config.cfg` - 客户端设置
- `gateway_config.cfg` - Gateway 服务器设置
- `master_config.cfg` - Master 服务器设置
- `world_config.cfg` - World 服务器设置

<details>
<summary>配置示例截图:</summary>

<img width="1580" alt="debug-setup" src="https://github.com/user-attachments/assets/cff4dd67-00f2-4dda-986f-7f0bec0a695e" />

</details>

### GameBackend 微服务

#### 环境要求

- .NET 10.0 SDK
- Docker & Docker Compose
- (可选) PostgreSQL 15+, Redis 7+ 用于本地开发

#### 使用 Docker 快速启动

```bash
cd GameBackend

# 启动所有服务
docker-compose up -d

# 或仅启动聊天服务
chmod +x start-chat-service.sh
./start-chat-service.sh
```

#### 访问地址

| 服务 | URL | 说明 |
|------|-----|------|
| API Gateway | http://localhost:5000 | 主入口 |
| Auth Service | http://localhost:5001 | 用户认证 |
| Game Service | http://localhost:5002 | 游戏逻辑 |
| Room Service | http://localhost:5003 | 房间管理 |
| Chat Service | http://localhost:5004 | 实时聊天 |
| Consul UI | http://localhost:8500 | 服务发现 |
| Jaeger UI | http://localhost:16686 | 分布式追踪 |
| PostgreSQL | localhost:5432 | 数据库 (用户: postgres, 密码: password) |
| Redis | localhost:6379 | 缓存 |
| Redis Commander | http://localhost:8081 | Redis 管理界面 |
| pgAdmin | http://localhost:5050 | PostgreSQL 管理界面 |

#### 手动开发模式

```bash
cd GameBackend

# 先启动基础设施
docker-compose up -d consul postgresql redis

# 运行各个服务
dotnet run --project src/Game.ApiGateway
dotnet run --project src/Game.AuthService
dotnet run --project src/Game.GameService
dotnet run --project src/Game.RoomService
dotnet run --project src/Game.ChatService
```

<details>
<summary>查看详细 GameBackend 文档</summary>

详见 [GameBackend/README.md](GameBackend/README.md) 获取完整的 API 文档、配置选项和集成指南。

</details>

---

## 项目结构

### Godot 原生

```
source/
├── client/          # 客户端专用代码
│   ├── autoload/    # ClientState 单例
│   ├── gateway/     # 认证和角色选择 UI
│   ├── network/     # InstanceClient, WorldClient
│   └── ui/          # HUD、菜单、背包、聊天
├── common/          # 客户端+服务器共享代码
│   ├── gameplay/    # 角色、战斗、物品、地图、时间
│   ├── network/     # Wire 协议、RPC 端点、状态同步
│   ├── registry/    # ContentRegistryHub, PathRegistry
│   └── microservices/  # 微服务客户端 (ChatServiceManager)
└── server/
    ├── gateway/     # REST 认证端点 (/v1/*)
    ├── master/      # 账户管理、协调
    └── world/       # 实例管理、游戏逻辑处理
        └── components/data_request_handlers/  # RPC 处理器
```

### GameBackend

```
GameBackend/
├── src/
│   ├── Game.ApiGateway/        # Ocelot API 网关 (端口 5000)
│   ├── Game.AuthService/       # 认证服务 (端口 5001)
│   ├── Game.GameService/       # 游戏逻辑服务 (端口 5002)
│   ├── Game.RoomService/       # 房间管理 (端口 5003)
│   └── Game.ChatService/       # 聊天服务 SignalR (端口 5004)
├── shared/
│   └── Game.Shared/            # 共享代码 (Consul, Models, DTOs)
├── docker/                     # Docker 配置
├── k8s/                        # Kubernetes 清单
└── docker-compose.yml          # 容器编排
```

---

## 核心类和组件

### Godot 原生

- **Character** → **Player** → **LocalPlayer**: 角色层级结构 (基于 CharacterBody2D)
- **InstanceClient/WorldClient**: 客户端网络
- **ServerInstance**: World 服务器上的每地图实例 (继承 SubViewport)
- **WorldServer/InstanceManager**: World 服务器启动和实例管理
- **Wire**: 高效网络通信的二进制序列化协议

### 数据请求处理器 (服务器端)

位于 `source/server/world/components/data_request_handlers/`:

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

---

## 技术栈

### Godot 原生

| 组件 | 技术 |
|------|------|
| **引擎** | Godot 4.5 (GL 兼容模式) |
| **语言** | GDScript |
| **网络** | WebSocketMultiplayerPeer |
| **序列化** | 自定义 `PackedByteArray` 通过 `Wire.gd` |
| **数据库** | QAD 格式 (`.tres` 资源) |

### GameBackend 微服务

| 类别 | 技术 |
|------|------|
| **框架** | .NET 10.0, ASP.NET Core 10.0 |
| **API 网关** | Ocelot 23.4.2 |
| **服务发现** | Consul 1.15 |
| **实时通信** | SignalR (ASP.NET Core) |
| **分布式计算** | Microsoft Orleans 8.2.0 |
| **数据库** | PostgreSQL 15 + EF Core 10.0 |
| **缓存** | Redis 7 (StackExchange.Redis) |
| **认证** | JWT Bearer |
| **追踪** | OpenTelemetry + Jaeger |
| **序列化** | MemoryPack 1.10.0 |
| **容器化** | Docker + docker-compose |
| **编排** | Kubernetes (清单在 `k8s/`) |

---

## API 端点

### Gateway HTTP API (Godot 原生)

```
POST /v1/login              - 用户登录
POST /v1/guest              - 游客登录
POST /v1/account/create     - 创建账户
POST /v1/world/characters   - 获取玩家角色
POST /v1/world/character/create - 创建角色
POST /v1/world/enter        - 使用角色进入世界
```

### API Gateway 路由 (微服务)

```
/api/auth/*   → AuthService (登录、注册、令牌验证)
/api/game/*   → GameService (世界、角色、游戏状态)
/api/room/*   → RoomService (房间管理)
/api/chat/*   → ChatService (聊天室、消息)
```

<details>
<summary>查看完整 ChatService API</summary>

**REST API:**
```
GET    /api/chat/rooms                    - 获取公开房间
GET    /api/chat/rooms/{id}/messages      - 获取房间消息历史
POST   /api/chat/rooms                    - 创建新房间
POST   /api/chat/users/{id}/register      - 注册用户
GET    /api/chat/users/{id}               - 获取用户信息
PUT    /api/chat/users/{id}/status        - 更新用户状态
```

**SignalR Hub (`/chatHub`):**
- `JoinRoom(roomId, username)` - 加入房间
- `LeaveRoom(roomId)` - 离开房间
- `SendMessage(roomId, content, messageType)` - 发送消息
- `SendPrivateMessage(recipientId, content)` - 发送私聊
- `GetOnlineUsers(roomId)` - 获取房间在线用户

**服务器 → 客户端事件:**
- `ReceiveMessage(message)` - 收到新消息
- `ReceivePrivateMessage(message)` - 收到私聊消息
- `UserJoined(userInfo)` - 用户加入房间
- `UserLeft(userInfo)` - 用户离开房间
- `OnlineUsers(users)` - 在线用户列表更新

</details>

---

## 贡献

欢迎 fork 本仓库并提交 pull request！
也可以提交 [**Issue**](https://github.com/EF-Cosmos/godot-tiny-mmo/issues) 讨论问题或功能请求。

---

## 致谢

感谢所有为本项目做出贡献的人：
- **地图** 设计者 [@higaslk](https://github.com/higaslk)
- 宝贵的帮助和反馈: [@Jackiefrost](https://github.com/Jackietkfrost), [@d-Cadrius](https://github.com/d-Cadrius) 和多位匿名贡献者
- 原项目作者: [SlayHorizon](https://github.com/SlayHorizon/godot-tiny-mmo)

## 许可证

源代码采用 [MIT License](https://github.com/EF-Cosmos/godot-tiny-mmo/blob/main/LICENSE)。
