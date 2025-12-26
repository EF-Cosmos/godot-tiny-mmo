# C# Game Backend - 微服务架构

这是一个基于C#和ASP.NET Core的游戏后端项目，采用微服务架构设计，与Godot前端兼容。该系统集成了Orleans分布式计算框架、MemoryPack高性能序列化库、以及完整的可观测性监控体系。

## 项目结构

```
GameBackend/
├── Game.ApiGateway/           # API网关服务
├── Game.AuthService/          # 用户认证服务
├── Game.GameService/          # 游戏逻辑服务
├── Game.RoomService/          # 房间管理服务
├── Game.ChatService/          # 聊天服务
├── Game.Shared/              # 共享代码库
├── docker/                    # Docker配置文件
├── GameBackend.sln           # 解决方案文件
└── README.md                  # 项目说明
```

## 技术栈

- **ASP.NET Core 10.0** - 微服务框架
- **Entity Framework Core** - ORM框架
- **PostgreSQL** - 数据库
- **Redis** - 缓存和集群
- **Docker** - 容器化
- **JWT** - 身份验证
- **Orleans** - 分布式计算框架
- **MemoryPack** - 高性能序列化
- **OpenTelemetry** - 监控和追踪
- **Kubernetes** - 容器编排

## 微服务设计

### 1. API Gateway (端口 8080)
- 统一入口点
- 路由转发
- 认证授权
- 限流熔断
- CORS配置
- OpenTelemetry监控

### 2. Auth Service (端口 5001)
- 用户注册/登录
- JWT令牌生成
- 用户管理
- 密码加密
- Orleans集成支持

### 3. Game Service (端口 5002)
- 游戏逻辑管理
- 游戏状态同步
- 游戏规则验证
- MemoryPack序列化
- Orleans分布式处理

### 4. Room Service (端口 5003)
- 房间创建/管理
- 玩家加入/离开
- 房间状态同步
- Redis缓存支持

### 5. Chat Service (端口 8090)
- **实时聊天**: SignalR WebSocket支持实时通信
- **消息持久化**: PostgreSQL存储聊天历史
- **多频道支持**: 全局、交易、公会、私聊等频道
- **内容过滤**: 自动过滤敏感词汇和垃圾信息
- **用户管理**: 在线状态跟踪和用户权限管理
- **高性能缓存**: Redis缓存提升响应速度
- **OpenTelemetry监控**: 完整的可观测性支持

## 快速开始

### 环境要求
- .NET 10.0 SDK
- Docker & Docker Compose
- PostgreSQL 15+
- Redis 7+
- Jaeger (用于追踪) - 可选但推荐

### 使用Docker启动

#### 方式一：启动聊天服务（推荐）
```bash
cd GameBackend
chmod +x start-chat-service.sh
./start-chat-service.sh
```

#### 方式二：启动全部服务
```bash
cd GameBackend
docker-compose up -d
```

#### 访问服务
- **聊天服务API**: http://localhost:8090
- **聊天服务Swagger**: http://localhost:8090/swagger
- **API网关**: http://localhost:8080/swagger
- **PostgreSQL**: localhost:5432 (user: postgres, password: password)
- **Redis**: localhost:6379
- **Jaeger追踪**: http://localhost:16686
- **Redis管理**: http://localhost:8081
- **pgAdmin**: http://localhost:5050

### 手动启动

1. 启动数据库服务
```bash
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=game_db -p 5432:5432 postgres:15
docker run -d --name redis -p 6379:6379 redis:7-alpine
```

2. 启动各个服务
```bash
# API Gateway
cd Game.ApiGateway
dotnet run

# Auth Service
cd Game.AuthService
dotnet run

# 其他服务同理...
```

## API接口

### 用户认证
- `POST /api/users/register` - 用户注册
- `POST /api/users/login` - 用户登录
- `GET /api/users/{userId}` - 获取用户信息
- `POST /api/users/validate` - 验证令牌

### 游戏管理
- `GET /api/games` - 获取所有游戏
- `GET /api/games/{gameId}` - 获取游戏详情
- `POST /api/games` - 创建游戏
- `DELETE /api/games/{gameId}` - 删除游戏

### 房间管理
- `GET /api/rooms/{roomId}` - 获取房间信息
- `GET /api/rooms/game/{gameId}` - 获取游戏房间列表
- `GET /api/rooms/available` - 获取可用房间
- `POST /api/rooms` - 创建房间
- `POST /api/rooms/{roomId}/join/{userId}` - 加入房间
- `POST /api/rooms/{roomId}/leave/{userId}` - 离开房间

### 聊天功能 (Chat Service - 端口 8090)
- `GET /api/chat/rooms` - 获取所有公开房间
- `GET /api/chat/rooms/{roomId}/messages` - 获取房间消息历史
- `POST /api/chat/rooms` - 创建新房间
- `POST /api/chat/users/{userId}/register` - 注册用户
- `GET /api/chat/users/{userId}` - 获取用户信息
- `PUT /api/chat/users/{userId}/status` - 更新用户状态
- `GET /health` - 健康检查

#### WebSocket事件 (SignalR Hub: /chatHub)
**客户端 → 服务器**:
- `JoinRoom(roomId, username)` - 加入房间
- `LeaveRoom(roomId)` - 离开房间
- `SendMessage(roomId, content, messageType)` - 发送消息
- `SendPrivateMessage(recipientId, content)` - 发送私聊
- `GetOnlineUsers(roomId)` - 获取在线用户

**服务器 → 客户端**:
- `ReceiveMessage(message)` - 接收消息
- `ReceivePrivateMessage(message)` - 接收私聊
- `UserJoined(userInfo)` - 用户加入房间
- `UserLeft(userInfo)` - 用户离开房间
- `OnlineUsers(users)` - 在线用户列表更新

## 配置说明

### JWT配置
在 `appsettings.json` 中配置JWT相关参数：
```json
{
  "Jwt": {
    "Issuer": "game-backend",
    "Audience": "game-clients",
    "Key": "your-super-secret-jwt-key"
  }
}
```

### 服务配置
在 `appsettings.json` 中配置各个微服务的地址：
```json
{
  "Services": {
    "Auth": { "Url": "http://localhost:5001" },
    "Game": { "Url": "http://localhost:5002" },
    "Room": { "Url": "http://localhost:5003" },
    "Chat": { "Url": "http://localhost:5004" }
  }
}
```

### OpenTelemetry配置
在 `appsettings.json` 中配置OpenTelemetry相关参数：
```json
{
  "OpenTelemetry": {
    "Tracing": {
      "Endpoint": "http://localhost:14268/api/traces",
      "ExportProcessorType": "Batch"
    },
    "Metrics": {
      "Endpoint": "http://localhost:9090"
    }
  }
}
```

## 开发指南

### 添加新的微服务

1. 创建新的Web API项目
2. 添加对 `Game.Shared` 的引用
3. 实现相应的接口
4. 在API网关中添加路由
5. 更新docker-compose配置

### 数据库迁移

```bash
# 添加迁移
dotnet ef migrations add MigrationName --context ApplicationDbContext

# 应用迁移
dotnet ef database update --context ApplicationDbContext
```

### 测试

```bash
# 运行单元测试
dotnet test

# 运行集成测试
dotnet test --filter "Category=Integration"
```

## 部署

### Docker部署
```bash
# 构建镜像
docker-compose build

# 启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f
```

### Kubernetes部署 (建议)
- 使用Helm Charts或Kubernetes YAML文件
- 部署Orleans Silo集群
- 部署现有微服务
- 配置服务发现和负载均衡

### 生产环境配置
1. 修改JWT密钥
2. 配置HTTPS
3. 设置数据库连接字符串
4. 配置日志级别
5. 启用性能监控
6. 配置Jaeger追踪
7. 配置Prometheus指标收集

## 与Godot前端集成

### 聊天微服务集成

在Godot项目中启用聊天微服务：

1. **配置启用** - 在 `data/config/master_config.cfg` 中添加：
```ini
[chat-service]
enabled=true
address="127.0.0.1"
port=8090
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
现有的聊天数据处理器 (`chat.message.send.gd`) 已自动集成微服务支持，会：
- 优先使用聊天微服务
- 微服务不可用时自动降级到本地聊天
- 保持与现有UI和系统的完全兼容

### 传统API集成

Godot客户端也可以直接通过HTTP请求与后端通信：

```gdscript
# 用户登录示例
func login(username: String, password: String):
    var url = "http://localhost:8080/api/users/login"
    var headers = ["Content-Type: application/json"]
    var data = JSON.stringify({"username": username, "password": password})

    $HTTPClient.request(Method.POST, url, headers, data)

# 聊天服务用户注册
func register_chat_user(user_id: int, username: String):
    var url = "http://localhost:8090/api/chat/users/%d/register" % user_id
    var headers = ["Content-Type: application/json", "Authorization: Bearer YOUR_TOKEN"]
    var data = JSON.stringify({"username": username, "email": ""})

    $HTTPClient.request(Method.POST, url, headers, data)

# 获取聊天历史
func get_chat_history(room_id: int):
    var url = "http://localhost:8090/api/chat/rooms/%d/messages?limit=50" % room_id
    var headers = ["Authorization: Bearer YOUR_TOKEN"]

    $HTTPClient.request(Method.GET, url, headers, "")
```

### WebSocket客户端示例

```gdscript
extends Node

var websocket: WebSocketPeer

func _ready():
    connect_to_chat_service()

func connect_to_chat_service():
    websocket = WebSocketPeer.new()
    var url = "ws://localhost:8090/chatHub"
    var headers = ["Authorization: Bearer YOUR_TOKEN"]

    var error = websocket.connect_to_url(url, headers)
    if error != OK:
        print("WebSocket连接失败: %d" % error)

func _process(_delta):
    websocket.poll()

    while websocket.get_ready_state() == WebSocketPeer.STATE_OPEN:
        var packet = websocket.get_packet()
        if packet.size() > 0:
            handle_websocket_message(packet)

func send_chat_message(room_id: int, content: String):
    if websocket.get_ready_state() == WebSocketPeer.STATE_OPEN:
        var message = {
            "type": "SendMessage",
            "arguments": [{"roomId": room_id, "content": content}]
        }
        websocket.send_text(JSON.stringify(message))

func handle_websocket_message(packet: PackedByteArray):
    var message_string = packet.get_string_from_utf8()
    var json = JSON.new()
    json.parse(message_string)

    var message_data = json.data
    print("收到WebSocket消息: %s" % message_data)
```

## 贡献指南

1. Fork项目
2. 创建功能分支
3. 提交更改
4. 推送到分支
5. 创建Pull Request

## 许可证

MIT License

## 支持

如有问题，请创建Issue或联系开发团队。