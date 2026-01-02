# ET框架 vs 传统微服务架构对比分析

## 什么是 ET 框架？

**ET Framework** 是一个基于 .NET 的分布式游戏服务器框架，采用 **Actor 模型** 和 **进程内消息传递** 的设计理念，由中国开发者熊猫（panda）开源。

### 核心特性
- 基于 Actor 模型的分布式架构
- 进程内消息队列机制
- 组件化设计（Component）
- 内置网络通信、序列化、热更新等功能
- 单进程支持多服务器逻辑（世界、网关、地图等）

## 架构对比

### 当前项目架构（传统微服务）

```
┌─────────────────────────────────────────────────────────┐
│                    Ocelot API Gateway                    │
│              (路由、认证、限流、聚合)                      │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
    ┌────▼────┐ ┌───▼────┐ ┌───▼────┐
    │ Auth    │ │ Game   │ │ Room   │
    │ Service │ │ Service│ │ Service│
    │ :5001   │ │ :5002  │ │ :5003  │
    └────┬────┘ └───┬────┘ └───┬────┘
         │          │          │
    ┌────▼──────────▼──────────▼────┐
    │     PostgreSQL + Redis         │
    │     (共享数据库和缓存)           │
    └───────────────────────────────┘

特点：
• 每个服务是独立的进程
• 通过 HTTP/gRPC 通信
• 共享数据库
• 服务间通过 API 调用
• 独立部署和扩展
```

### ET 框架架构

```
┌─────────────────────────────────────────────────────────┐
│                    ET Server Process                    │
│  ┌─────────────────────────────────────────────────┐   │
│  │              Actor System (ECS)                 │   │
│  │                                                 │   │
│  │  ┌──────────┐   ┌──────────┐   ┌──────────┐   │   │
│  │  │ Gateway  │   │  World   │   │   Map    │   │   │
│  │  │  Actor   │   │  Actor   │   │  Actor   │   │   │
│  │  └────┬─────┘   └────┬─────┘   └────┬─────┘   │   │
│  │       │              │              │          │   │
│  │  ┌────▼──────────────▼──────────────▼─────┐   │   │
│  │  │      Actor Message Queue (进程内)      │   │   │
│  │  └──────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │         Network Layer (KCP/TCP/WebSocket)      │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘

特点：
• 单进程包含所有服务器逻辑
• Actor 间通过消息传递（进程内）
• 可能多进程部署（进程间 Actor 通信）
• 使用 KCP/TCP/WebSocket 协议
• 每个实体都是一个 Actor
```

## 详细对比

### 1. 服务边界

| 维度 | 传统微服务 | ET Framework |
|------|-----------|--------------|
| **服务粒度** | 按业务功能划分（Auth、Game、Room） | 按实体划分（每个玩家、地图、物品都是 Actor） |
| **通信方式** | HTTP/gRPC 网络调用 | 进程内消息队列 或 进程间 Actor 消息 |
| **进程数量** | 每个服务一个进程 | 单进程多 Actor，可多进程分布 |
| **服务发现** | Consul/CoreDNS | 内置 Actor 寻址机制 |

**示例：玩家移动**

```csharp
// 传统微服务方式
// 1. 客户端 → Gateway HTTP → Game Service API
// 2. Game Service → PostgreSQL 查询玩家位置
// 3. Game Service → 返回响应
[HttpGet("players/{playerId}/move")]
public async Task<ActionResult> MovePlayer(int playerId, [FromBody] MoveRequest request)
{
    // 数据库查询
    var player = await _context.Players.FindAsync(playerId);
    player.Position = request.Position;
    await _context.SaveChangesAsync();
    
    // 可能需要调用其他服务
    await _roomService.NotifyPlayerMove(playerId, request.Position);
    
    return Ok();
}
```

```csharp
// ET Framework 方式（伪代码）
// 1. 客户端 → TCP → Gateway Actor
// 2. Gateway Actor → 发送消息给 World Actor（进程内）
// 3. World Actor → 处理移动逻辑
// 4. 全部在内存中完成，无网络调用
public class PlayerActor : Entity
{
    public async Task OnMove(MoveRequest message)
    {
        // 直接内存操作，无需查询数据库
        this.Position = message.Position;
        
        // 通知其他玩家（也是 Actor 消息）
        await this.GetParent<RoomActor>()
            .BroadcastMove(this.Id, message.Position);
    }
}
```

### 2. 数据流转

#### 传统微服务

```
客户端请求 → HTTP → [网关]
                        ↓
                 [认证验证] → JWT
                        ↓
                 [路由转发] → HTTP/REST
                        ↓
           ┌─────────────┼─────────────┐
           ↓             ↓             ↓
    [Auth Service] [Game Service] [Room Service]
           ↓             ↓             ↓
    [PostgreSQL] ← [Redis] ← [Redis]
           ↓             ↓
    [返回数据] → [返回数据] → [返回数据]
                        ↓
                 [聚合响应] → HTTP
                        ↓
                    返回客户端
```

**特点**：
- 每次服务调用都是网络请求
- 数据需要序列化/反序列化
- 需要共享数据库（增加耦合）
- 延迟较高（网络 + 数据库）

#### ET Framework

```
客户端请求 → TCP → [Gateway Actor (内存)]
                        ↓
                 [验证会话] (内存)
                        ↓
                 [发送消息] (进程内)
                        ↓
           ┌─────────────┼─────────────┐
           ↓             ↓             ↓
    [Player Actor] [World Actor] [Room Actor]
           ↓             ↓             ↓
    (内存操作)     (内存操作)     (内存操作)
           ↓             ↓             ↓
     [内存队列] ← [内存队列] ← [内存队列]
                        ↓
                 [批量持久化] → 数据库
                        ↓
                 [返回响应] → TCP
                        ↓
                    返回客户端
```

**特点**：
- 所有操作在内存中完成
- 消息传递无序列化开销（进程内）
- 无需共享数据库
- 延迟极低（内存操作）

### 3. 扩展性

#### 传统微服务

```yaml
# 扩展策略：水平扩展服务
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: game-service-hpa
spec:
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        averageUtilization: 70

# 扩展时的问题：
# 1. 状态同步困难（玩家在哪个服务实例？）
# 2. 需要分布式缓存（Redis）
# 3. 需要消息队列（RabbitMQ/Kafka）
# 4. 需要分布式锁
```

#### ET Framework

```
扩展策略： Actor 动态分布
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  Process 1   │  │  Process 2   │  │  Process 3   │
│              │  │              │  │              │
│ ┌──────────┐ │  │ ┌──────────┐ │  │ ┌──────────┐ │
│ │Map A     │ │  │ │Map B     │ │  │ │Map C     │ │
│ │Actor     │ │  │ │Actor     │ │  │ │Actor     │ │
│ └──────────┘ │  │ └──────────┘ │  │ └──────────┘ │
│ ┌──────────┐ │  │ ┌──────────┐ │  │ ┌──────────┐ │
│ │100 Players│ │  │ │150 Players│ │  │ │80 Players│ │
│ └──────────┘ │  │ └──────────┘ │  │ └──────────┘ │
└──────────────┘  └──────────────┘  └──────────────┘
        │                  │                  │
        └──────────────────┼──────────────────┘
                           │
                    [Actor 寻址系统]
                    (自动路由消息)
```

**特点**：
- Actor 可以在不同进程间迁移
- 消息自动路由到目标 Actor
- 状态绑定在 Actor 上，无状态同步问题
- 无需分布式缓存和锁

### 4. 性能对比

| 指标 | 传统微服务 | ET Framework | 差异 |
|------|-----------|--------------|------|
| **服务间调用延迟** | 1-10ms (网络) | <0.1ms (进程内) | 10-100倍 |
| **序列化开销** | 每次 JSON/Protobuf | 进程内无需序列化 | 显著降低 |
| **数据库压力** | 每次操作查询/更新 | 批量持久化 | 大幅降低 |
| **内存占用** | 多进程重复资源 | 单进程共享资源 | 更低 |
| **CPU 占用** | 网络序列化开销 | 纯逻辑计算 | 更低 |

### 5. 开发体验

#### 传统微服务

```csharp
// Game.Service/Services/GameService.cs
public class GameService
{
    private readonly DbContext _context;
    private readonly HttpClient _httpClient;
    
    // 需要注入很多依赖
    public GameService(DbContext context, IHttpClientFactory factory)
    {
        _context = context;
        _httpClient = factory.CreateClient();
    }
    
    public async Task<GameStatus> GetGameStatus(int gameId)
    {
        // 查询数据库
        var game = await _context.Games.FindAsync(gameId);
        
        // 调用其他服务（网络请求）
        var roomResponse = await _httpClient.GetAsync(
            $"http://room-service/api/rooms/{game.RoomId}/status"
        );
        var room = await roomResponse.Content.ReadFromJsonAsync<RoomStatus>();
        
        // 聚合数据
        return new GameStatus
        {
            Game = game,
            Room = room,
            Players = await GetPlayers(gameId)
        };
    }
    
    private async Task<List<Player>> GetPlayers(int gameId)
    {
        // 又要查询或调用服务
        // ...
    }
}
```

**问题**：
- 需要管理多个 DbContext
- 网络调用增加复杂度
- 需要处理超时、重试、熔断
- 测试困难（需要 Mock 多个服务）

#### ET Framework

```csharp
// ET Framework 风格
[ActorMessageHandler]
public class GameActor : Entity
{
    // 单一职责，处理游戏逻辑
    public async Task<GetGameStatusResponse> OnAsk(GetGameStatusRequest request)
    {
        var roomActor = this.GetChild<RoomActor>(request.RoomId);
        var roomStatus = await roomActor.Call<RoomStatus>(new GetRoomStatus());
        
        var players = this.GetChild<PlayersComponent>().GetAll();
        
        return new GetGameStatusResponse
        {
            Game = this.GetComponent<GameComponent>(),
            Room = roomStatus,
            Players = players
        };
    }
    
    // 所有数据在内存，无网络调用
}
```

**优势**：
- 代码简洁，无网络调用
- 组件化设计（ECS）
- 易于测试（单进程测试）
- 类型安全的消息传递

## 适用场景对比

### 传统微服务适合

| 场景 | 原因 |
|------|------|
| ✅ **业务系统**（电商、CMS、企业管理） | 业务边界清晰，服务间调用频率低 |
| ✅ **多团队协作** | 服务独立，职责明确 |
| ✅ **不同技术栈混合** | Auth 用 Java，Game 用 C#，Room 用 Go |
| ✅ **已有微服务架构** | 复用现有基础设施 |
| ✅ **跨地域部署** | 服务可独立部署在不同区域 |

### ET Framework 适合

| 场景 | 原因 |
|------|------|
| ✅ **实时游戏**（MMO、MOBA、FPS） | 低延迟要求，高频 Actor 交互 |
| ✅ **高并发系统** | 进程内消息传递，性能优异 |
| ✅ **状态密集型应用** | Actor 绑定状态，无需查询数据库 |
| ✅ **.NET 技术栈团队** | .NET 生态完善 |
| ✅ **小团队快速迭代** | 单进程部署，运维简单 |

## 迁移建议

### 当前项目（传统微服务）→ 保持现状的理由

1. **已投入成本** - 已有完整的微服务和数据库
2. **技术团队熟悉** - 团队熟悉 ASP.NET Core 和微服务
3. **非纯实时游戏** - 如果是 RPG 等可容忍延迟的游戏
4. **多服务生态** - 聊天、排行榜、数据分析等独立服务合理

### 考虑引入 ET 的理由

1. **实时性要求高** - 如果是 MOBA、FPS 等需要 <50ms 延迟
2. **状态密集** - 游戏世界状态复杂，频繁读写
3. **性能瓶颈** - 当前架构无法支撑更多并发玩家
3. **简化架构** - 减少服务间调用和数据库压力

### 混合架构方案

```
┌─────────────────────────────────────────────────────────┐
│                    Ocelot API Gateway                    │
│         (HTTP 接口，适合非实时业务)                      │
└────────────────────┬────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
   ┌────▼──────┐           ┌──────▼─────┐
   │ Realtime  │           │  Backend   │
   │ World     │           │ Services   │
   │ (ET Core) │           │ (Auth/API) │
   └────┬──────┘           └──────┬─────┘
        │                         │
        └────────────┬────────────┘
                     │
              ┌──────▼──────┐
              │   Shared DB  │
              └─────────────┘

说明：
• ET Framework 处理实时游戏逻辑（玩家移动、战斗、技能）
• 传统微服务处理业务逻辑（用户注册、排行榜、商城）
• 通过事件总线或共享数据库同步状态
```

## 代码示例：混合架构

```csharp
// ET Framework - 实时游戏逻辑
[ActorMessageHandler]
public class PlayerActor : Entity
{
    // 处理玩家移动（高频、实时）
    public async Task OnMove(MoveRequest request)
    {
        this.GetComponent<TransformComponent>().Position = request.Position;
        
        // 实时广播给同房间玩家
        var room = this.GetParent<RoomActor>();
        await room.Broadcast(new PlayerMovedEvent { 
            PlayerId = this.Id, 
            Position = request.Position 
        });
        
        // 异步持久化到数据库（低优先级）
        await this.GetComponent<DatabaseComponent>()
            .SaveAsync(() => this.ToEntity<PlayerPersist>());
    }
    
    // 战斗逻辑
    public async Task<AttackResult> OnAttack(AttackRequest request)
    {
        var target = scene.GetComponent<PlayerActor>(request.TargetId);
        var damage = this.GetDamage(target);
        
        if (damage > 0)
        {
            target.GetComponent<HealthComponent>().Reduce(damage);
            
            // 即时返回
            return new AttackResult { Success = true, Damage = damage };
            
            // 战斗记录异步发送到后端服务
            await EventBus.PublishAsync(new CombatRecordEvent
            {
                AttackerId = this.Id,
                TargetId = target.Id,
                Damage = damage
            });
        }
        
        return new AttackResult { Success = false };
    }
}
```

```csharp
// 传统微服务 - 业务逻辑
// Game.Service/Services/LeaderboardService.cs
public class LeaderboardService
{
    private readonly DbContext _context;
    
    // 处理排行榜（低频、可延迟）
    public async Task UpdateLeaderboardAsync(CombatRecordEvent combatEvent)
    {
        // 从数据库计算或缓存中获取
        var leaderboard = await _context.Leaderboards
            .FirstOrDefaultAsync(l => l.Type == "PvP");
            
        // 更新排名
        leaderboard.Update(combatEvent);
        await _context.SaveChangesAsync();
    }
    
    // 提供排行榜 API
    public async Task<List<PlayerRank>> GetTopPlayers(int limit = 100)
    {
        return await _context.Players
            .OrderByDescending(p => p.Score)
            .Take(limit)
            .Select(p => new PlayerRank(p))
            .ToListAsync();
    }
}
```

## 总结

### 关键区别

| 维度 | 传统微服务 | ET Framework |
|------|-----------|--------------|
| **核心思想** | 服务按功能拆分 | 实体即 Actor |
| **通信方式** | 网络 HTTP/gRPC | 进程内/进程间消息 |
| **数据存储** | 共享数据库 | Actor 状态（内存） |
| **性能** | 中等 | 高（内存操作） |
| **延迟** | 毫秒级 | 微秒级（进程内） |
| **扩展性** | 水平扩展服务 | Actor 动态分布 |
| **复杂度** | 中等（运维） | 中等（架构设计） |
| **适用场景** | 业务系统、可容忍延迟 | 实时游戏、高并发 |

### 选择建议

**选择 ET Framework**，如果：
- ✅ 开发实时多人游戏
- ✅ 追求极致性能和低延迟
- ✅ 状态密集，频繁查询/更新
- ✅ 团队熟悉 .NET 和 Actor 模型

**选择传统微服务**，如果：
- ✅ 非实时游戏（回合制、卡牌）
- ✅ 已有微服务基础设施
- ✅ 多团队协作，技术栈混合
- ✅ 需要独立部署和扩展

**考虑混合方案**，如果：
- ✅ 既有实时需求，又有业务逻辑
- ✅ 想要利用两者优势
- ✅ 团队能力支撑双架构

## 推荐阅读

- **ET Framework**: https://github.com/egametang/ET
- **Actor 模型**: https://en.wikipedia.org/wiki/Actor_model
- **Orleans**: .NET 的 Actor 模型实现（微软官方）
- **Akka**: 跨语言的 Actor 框架

**ET Framework 是游戏开发的优秀选择，但你的项目已经有完整的微服务架构。根据实际需求和团队情况，选择是否引入或迁移。**
