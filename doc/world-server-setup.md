# World Server 配置与启动指南

本文档说明如何配置和启动 Godot Tiny MMO 的 World Server。

---

## 目录

- [架构概述](#架构概述)
- [配置文件说明](#配置文件说明)
- [启动 World Server](#启动-world-server)
- [多实例配置](#多实例配置)
- [与 RoomService 集成](#与-roomservice-集成)
- [故障排除](#故障排除)

---

## 架构概述

### 服务器职责

```
┌─────────────────────────────────────────────────────────────┐
│                      服务器架构                               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐      ┌──────────────┐                    │
│  │   Gateway    │──────│    Master    │                     │
│ │   Server     │      │    Server    │                     │
│  │  (HTTP+RPC)  │      │  (协调中心)   │                     │
│  └──────────────┘      └──────┬───────┘                     │
│                              │                              │
│                        ┌──────┴───────┐                     │
│                        │              │                     │
│                 ┌──────▼─────┐ ┌─────▼─────┐               │
│                 │  World-1   │ │  World-2  │               │
│                 │ (主世界)    │ │ (森林副本)  │               │
│                 └────────────┘ └───────────┘               │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**各服务器职责：**

| 服务器 | 职责 | 端口 |
|--------|------|------|
| **Gateway Server** | HTTP 认证入口、客户端连接管理 | 8088 |
| **Master Server** | 协调 Gateway 和 World、管理注册 | 8064/8062 |
| **World Server** | 游戏逻辑实例、RPC 处理、状态同步 | 可配置 |

**RPC 处理层级：**
- 客户端 → World Server 直接处理游戏逻辑 RPC
- Gateway Server 仅处理认证 HTTP 请求，不代理游戏 RPC
- Master Server 协调服务器间的通信

---

## 配置文件说明

### 默认配置文件位置

```
data/config/world_config.cfg
```

### 配置文件结构

```ini
[world-server]
# === 网络配置 ===
port=8087                      # World Server 监听端口
bind_address="127.0.0.1"       # 绑定地址

# === TLS 证书（可选） ===
certificate_path="res://data/config/tls/certificate.crt"
key_path="res://data/config/tls/key.key"

# === 服务器信息 ===
name="Classic"                  # 服务器显示名称
motd="Welcome to Tiny MMO!"     # 欢迎消息
max_players=200                 # 最大玩家数

# === RoomService 标识 ===
map_name="World"                # 地图名称（用于查询区分）
server_type="world"             # 服务器类型：world/instance/pvp/dungeon
instance_id=""                  # 实例 ID（留空自动生成）
region="default"                # 区域标识

# === 游戏设置（自动转换为标签） ===
hardcore=false                  # 硬核模式
pvp=true                        # 允许 PvP
bonus_xp=0.0                    # 经验加成 (0.0-2.0)
max_character=5                 # 每账户最大角色数

# === 数据库 ===
database_path="."               # QAD 数据库存储路径

[world-manager-client]
# Master Server 连接配置
address="127.0.0.1"
port=8062
certificate_path="res://data/config/tls/certificate.crt"

[game-service]
# .NET Game Service (可选)
url="http://localhost:5002"

[chat-service]
# SignalR 聊天服务 (可选)
enabled=true
address="127.0.0.1"
port=5004
ws_endpoint="/chatHub"
```

### 配置参数详解

#### 网络配置

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `port` | int | 8087 | WebSocket 监听端口 |
| `bind_address` | string | 127.0.0.1 | 绑定 IP 地址 |
| `certificate_path` | string | - | TLS 证书路径 |
| `key_path` | string | - | TLS 私钥路径 |

#### 服务器标识

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `name` | string | "Classic" | 服务器显示名称 |
| `map_name` | string | "World" | 地图名称，用于区分不同地图服务器 |
| `server_type` | string | "world" | 服务器类型，可选值：`world`, `instance`, `pvp`, `dungeon` |
| `instance_id` | string | 自动生成 | 实例唯一标识，格式：`mapname-port` |
| `region` | string | "default" | 区域标识 |

#### 游戏设置

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `hardcore` | bool | false | 硬核模式（死亡惩罚） |
| `pvp` | bool | true | 允许玩家对战 |
| `bonus_xp` | float | 0.0 | 经验加成倍率 |
| `max_players` | int | 200 | 最大同时在线玩家 |
| `max_character` | int | 5 | 每账户最大角色数 |

---

## 启动 World Server

### 前置条件

启动 World Server 前，需要先启动以下服务：

```bash
# 1. 启动后端基础设施（Docker）
cd GameBackend
docker-compose up -d

# 2. 启动 Master Server
godot --headless source/server/master/master_main.tscn

# 3. 启动 Gateway Server
godot --headless source/server/gateway/gateway_main.tscn
```

### 基本启动命令

```bash
# 使用默认配置文件
godot --headless source/server/world/world_main.tscn

# 指定配置文件
godot --headless source/server/world/world_main.tscn --config=data/config/world_config_forest.cfg
```

### 完整启动流程

**终端 1 - Master Server:**
```bash
godot --headless source/server/master/master_main.tscn
```

**终端 2 - Gateway Server:**
```bash
godot --headless source/server/gateway/gateway_main.tscn
```

**终端 3 - World Server (主世界):**
```bash
godot --headless source/server/world/world_main.tscn
```

**终端 4 - World Server (森林实例):**
```bash
godot --headless source/server/world/world_main.tscn --config=data/config/world_config_forest.cfg
```

### 启动脚本示例

创建 `start-world-servers.sh`：

```bash
#!/bin/bash

# 启动脚本示例

GODOT="godot"
HEADLESS="--headless"

# 主世界
$GODOT $HEADLESS source/server/world/world_main.tscn &
WORLD1_PID=$!

# 森林实例
$GODOT $HEADLESS source/server/world/world_main.tscn \
  --config=data/config/world_config_forest.cfg &
WORLD2_PID=$!

# 副本实例
$GODOT $HEADLESS source/server/world/world_main.tscn \
  --config=data/config/world_config_dungeon.cfg &
WORLD3_PID=$!

echo "Started World Servers with PIDs: $WORLD1_PID, $WORLD2_PID, $WORLD3_PID"

# 等待
wait $WORLD1_PID $WORLD2_PID $WORLD3_PID
```

---

## 多实例配置

### 配置多个地图服务器

#### 主世界配置

**文件:** `data/config/world_config.cfg`

```ini
[world-server]
port=8087
name="Main World"
map_name="World"
server_type="world"
instance_id=""
max_players=200
```

#### 森林实例配置

**文件:** `data/config/world_config_forest.cfg`

```ini
[world-server]
port=8089
name="Forest Instance 1"
map_name="Forest"
server_type="instance"
instance_id="forest-1"
max_players=100
hardcore=false
pvp=false
bonus_xp=1.0
```

#### 副本配置

**文件:** `data/config/world_config_dungeon.cfg`

```ini
[world-server]
port=8090
name="Dark Dungeon"
map_name="Dungeon"
server_type="dungeon"
instance_id="dungeon-1"
max_players=50
hardcore=true
pvp=false
bonus_xp=2.0
```

### 创建新实例配置模板

复制并修改配置文件：

```bash
# 复制默认配置
cp data/config/world_config.cfg data/config/world_config_myzone.cfg

# 编辑配置，修改以下字段：
# - port: 使用不同端口
# - name: 服务器名称
# - map_name: 地图名称
# - server_type: 服务器类型
# - instance_id: 实例 ID
```

### 多实例端口分配建议

| 服务器 | 端口范围 | 说明 |
|--------|----------|------|
| Master Server | 8062, 8064 | 协调服务 |
| Gateway Server | 8088 | 客户端入口 |
| World Server - 主世界 | 8087 | 主游戏世界 |
| World Server - 实例 | 8089-8099 | 各种副本/地图 |
| Chat Service | 5004 | 聊天服务 |

---

## 与 RoomService 集成

### 注册流程

World Server 启动时自动向 RoomService 注册：

```gdscript
# source/server/world/components/world_server.gd

func _register_to_room_service() -> void:
    # 从配置读取服务器类型和实例 ID
    var server_type := server_config.get("server_type", "world")
    var instance_id := server_config.get("instance_id", "")
    
    # 自动生成 instance_id
    if instance_id.is_empty():
        var map_name = server_config.get("map_name", "World")
        instance_id = "%s-%d" % [map_name, server_config.port]
    
    # 构建标签字典
    var tags := {
        "hardcore": str(server_config.get("hardcore", false)).to_lower(),
        "pvp": str(server_config.get("pvp", false)).to_lower(),
        "bonus_xp": str(server_config.get("bonus_xp", 0.0))
    }
    
    # 向 RoomService 注册
    # POST http://localhost:5003/api/rooms/register
```

### 查询已注册的服务器

```bash
# 查询所有服务器
curl http://localhost:5003/api/rooms

# 按地图名称查询
curl "http://localhost:5003/api/rooms?mapName=Forest"

# 按服务器类型查询
curl "http://localhost:5003/api/rooms/type/instance"
```

### 注册数据示例

```json
{
  "id": "abc-123-def",
  "name": "Forest Instance 1",
  "address": "127.0.0.1",
  "port": 8089,
  "mapName": "Forest",
  "serverType": "instance",
  "instanceId": "forest-1",
  "tags": {
    "hardcore": "false",
    "pvp": "false",
    "bonus_xp": "1.0"
  },
  "currentPlayers": 0,
  "maxPlayers": 100,
  "isActive": true
}
```

---

## 故障排除

### 常见问题

#### 1. World Server 无法启动

**症状:** 启动时出现错误或立即退出

**解决方案:**
- 检查端口是否被占用：`lsof -i :8087`
- 确认 Master Server 已启动
- 检查配置文件语法是否正确

#### 2. 无法连接到 RoomService

**症状:** 日志显示 "Failed to register to Room Service"

**解决方案:**
- 确认 RoomService 正在运行：`docker ps | grep room-service`
- 检查 RoomService URL 是否正确（默认：`http://localhost:5003/api/server`）
- 查看防火墙设置

#### 3. Master Server 连接失败

**症状:** 日志显示无法连接到 Master Server

**解决方案:**
```bash
# 检查 Master Server 是否运行
ps aux | grep master_main

# 检查端口
netstat -an | grep 8062

# 检查配置文件中的 address 和 port
```

#### 4. 实例 ID 冲突

**症状:** 多个服务器使用相同的 instance_id

**解决方案:**
- 在配置文件中明确设置不同的 `instance_id`
- 或让系统自动生成（留空配置文件中的 `instance_id`）

### 日志级别

修改日志输出级别，编辑启动命令：

```bash
# 详细日志
godot --headless --verbose source/server/world/world_main.tscn

# 静默模式
godot --headless --quiet source/server/world/world_main.tscn
```

### 调试模式

在编辑器中运行 World Server（非 headless）：

```bash
godot source/server/world/world_main.tscn
```

这将显示窗口和调试信息，方便开发调试。

---

## 配置最佳实践

### 1. 端口规划

为不同环境使用不同的端口范围：

| 环境 | 主世界 | 实例端口范围 |
|------|--------|-------------|
| 开发 | 8087 | 8089-8099 |
| 测试 | 9087 | 9089-9099 |
| 生产 | 8087 | 8089-8199 |

### 2. 命名规范

- `server_type`: 使用小写，如 `world`, `instance`, `pvp`
- `instance_id`: 使用 `mapname-number` 格式，如 `forest-1`
- `map_name`: 使用 PascalCase，如 `DarkForest`, `IceCave`

### 3. 资源分配

根据服务器类型调整资源：

| 类型 | 推荐玩家数 | bonus_xp | hardcore |
|------|-----------|----------|----------|
| world | 200-500 | 0.0 | false |
| instance | 50-100 | 1.0 | false |
| dungeon | 20-50 | 2.0 | true |
| pvp | 100-200 | 0.5 | true |

### 4. 配置文件管理

```bash
# 目录结构
data/config/
├── world_config.cfg              # 默认配置
├── world_config_forest.cfg       # 森林实例
├── world_config_dungeon.cfg      # 副本
├── world_config_pvp.cfg          # PvP 服务器
└── prod/                         # 生产环境配置
    ├── world_config.cfg
    └── instances/
        ├── forest-1.cfg
        └── dungeon-1.cfg
```

---

## 参考资料

- [项目架构文档](CLAUDE.md)
- [RoomService 集成指南](../GameBackend/README.md)
- [Godot 官方文档 - Multiplayer API](https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html)
