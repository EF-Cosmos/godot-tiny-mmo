#!/usr/bin/env python3
"""
Godot Tiny MMO Presentation - System Architecture Design Philosophy
A meticulous refinement embodying visual craftsmanship at the highest level.
"""

from reportlab.lib.pagesizes import A4
from reportlab.pdfgen import canvas
from reportlab.lib.units import cm
from reportlab.lib.colors import HexColor, PCMYKColor
import math

# Page dimensions - A4 landscape proportions
WIDTH, HEIGHT = A4
MARGIN = 2.5 * cm

# Refined color palette - chromatic taxonomy
# Each hue serves semantic purpose, calibrated with painstaking precision
COLORS = {
    'void': HexColor('#050A14'),          # Deepest background - foundation
    'slate': HexColor('#0F172A'),         # Primary structural element
    'panel': HexColor('#1E293B'),         # Secondary surface
    'blue': HexColor('#3B82F6'),          # Infrastructure / Gateway
    'cyan': HexColor('#06B6D4'),          # Data flow / Communication
    'violet': HexColor('#8B5CF6'),        # Services / Auth
    'purple': HexColor('#A855F7'),        # Processing / Logic
    'emerald': HexColor('#10B981'),       # Success / Complete
    'amber': HexColor('#F59E0B'),         # In Progress / Warning
    'rose': HexColor('#F43F5E'),          # Error / Critical
    'white': HexColor('#F8FAFC'),         # Primary text
    'muted': HexColor('#64748B'),         # Secondary text
    'subtle': HexColor('#334155'),        # Tertiary text
}

def draw_geometric_accent(c, page_num):
    """Draw subtle geometric elements that create spatial rhythm.
    These marks appear as if placed with painstaking deliberation."""
    # Top-left accent - a calculated geometric intervention
    c.setFillColor(COLORS['blue'])
    alpha = 0.08
    c.rect(0, HEIGHT - 12*cm, 12*cm, 12*cm, fill=1, stroke=0)

    # Bottom-right counterweight - visual balance through asymmetry
    c.setFillColor(COLORS['violet'])
    c.rect(WIDTH - 8*cm, 0, 8*cm, 8*cm, fill=1, stroke=0)

    # Grid pattern dots - systematic observation reference
    # Placed with mathematical precision
    dot_spacing = 1.5 * cm
    dot_radius = 1
    c.setFillColor(COLORS['subtle'])

    for y in range(int(HEIGHT // dot_spacing)):
        for x in range(int(WIDTH // dot_spacing)):
            # Only draw dots in specific zones - restraint is key
            if (x < 3 and y < 3) or (x > 15 and y > 10):
                c.circle(x * dot_spacing + MARGIN, HEIGHT - y * dot_spacing - MARGIN,
                        dot_radius, fill=1)

def draw_header(c, title, page_num):
    """Draw header with typographic precision.
    Every letter positioned with master-level craftsmanship."""
    # Numeric indicator - systematic reference
    c.setFillColor(COLORS['blue'])
    c.setFont('Helvetica-Bold', 32)
    page_str = f"{page_num:02d}"
    c.drawString(MARGIN, HEIGHT - MARGIN + 0.1*cm, page_str)

    # Vertical accent line - spatial division
    c.setFillColor(COLORS['cyan'])
    c.rect(MARGIN + 1.2*cm, HEIGHT - MARGIN - 0.5*cm, 0.1*cm, 0.6*cm, fill=1)

    # Title - positioned with breathing room
    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 20)
    c.drawString(MARGIN + 1.8*cm, HEIGHT - MARGIN - 0.05*cm, title)

    return HEIGHT - MARGIN - 2*cm

def draw_data_point(c, label, value, x, y, color_name='blue'):
    """Draw a data point with chromatic coding.
    Color taxonomy communicates category without explanation."""
    # Label marker
    c.setFillColor(COLORS[color_name])
    c.circle(x, y + 0.12*cm, 0.12*cm, fill=1)

    # Label text - whisper-quiet, essential only
    c.setFillColor(COLORS['muted'])
    c.setFont('Helvetica', 9)
    c.drawString(x + 0.3*cm, y, label)

    # Value - positioned for visual hierarchy
    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 10)
    c.drawString(x + 0.3*cm, y - 0.35*cm, value)

    return y - 0.9*cm

def draw_module(c, title, subtitle, x, y, width, height, color):
    """Draw a module box - the fundamental unit of system architecture.
    Each module is a discrete, carefully considered element."""
    # Module background - calculated radius suggests manufactured precision
    c.setFillColor(color)
    c.roundRect(x, y - height, width, height, 0.25*cm, fill=1, stroke=0)

    # Title - typographic hierarchy
    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 14)
    c.drawString(x + 0.5*cm, y - 0.6*cm, title)

    # Subtitle - secondary information, restrained
    c.setFont('Helvetica', 9)
    c.setFillColor(COLORS['white'])
    c.drawString(x + 0.5*cm, y - 1.1*cm, subtitle)

    return y - height

def draw_connection(c, x1, y1, x2, y2, label=None):
    """Draw a connection line between modules.
    The arrow suggests flow without overwhelming the composition."""
    # Line - precise, minimal
    c.setStrokeColor(COLORS['muted'])
    c.setLineWidth(0.03*cm)
    c.line(x1, y1, x2, y2)

    # Arrowhead - geometric precision
    arrow_size = 0.25*cm
    angle = math.atan2(y2 - y1, x2 - x1)
    p1 = (x2 - arrow_size * math.cos(angle - math.pi/6),
          y2 - arrow_size * math.sin(angle - math.pi/6))
    p2 = (x2 - arrow_size * math.cos(angle + math.pi/6),
          y2 - arrow_size * math.sin(angle + math.pi/6))
    c.setFillColor(COLORS['muted'])
    c.beginPath()
    c.moveTo(x2, y2)
    c.lineTo(*p1)
    c.lineTo(*p2)
    c.closePath()
    c.fill()

    # Label if provided - positioned with care
    if label:
        mid_x, mid_y = (x1 + x2) / 2, (y1 + y2) / 2
        c.setFillColor(COLORS['muted'])
        c.setFont('Helvetica-Oblique', 8)
        label_width = c.stringWidth(label, 'Helvetica-Oblique', 8)
        c.drawString(mid_x - label_width/2, mid_y + 0.2*cm, label)

def slide_01_title(c):
    """Slide 01: Title - The statement of intent.
    Composed with the restraint of a master craftsman."""
    # Background - deep void suggests depth and dimension
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    # Geometric composition - Mondrian-inspired balance
    # Each rectangle placed with painstaking deliberation
    c.setFillColor(COLORS['blue'])
    c.rect(0, HEIGHT - 10*cm, 10*cm, 10*cm, fill=1)

    c.setFillColor(COLORS['violet'])
    c.rect(WIDTH - 7*cm, 0, 7*cm, 7*cm, fill=1)

    # Accent line - creates spatial tension
    c.setFillColor(COLORS['cyan'])
    c.rect(WIDTH - 7*cm, HEIGHT - 10*cm, 0.3*cm, 3*cm, fill=1)

    # Main title - centered with mathematical precision
    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 52)
    title = "GODOT"
    title_width = c.stringWidth(title, 'Helvetica-Bold', 52)
    c.drawString((WIDTH - title_width) / 2, HEIGHT / 2 + 2.5*cm, title)

    c.setFont('Helvetica-Bold', 52)
    title2 = "TINY MMO"
    title_width2 = c.stringWidth(title2, 'Helvetica-Bold', 52)
    c.drawString((WIDTH - title_width2) / 2, HEIGHT / 2 + 1.5*cm, title2)

    # Subtitle - lighter weight, creates visual hierarchy
    c.setFont('Helvetica', 22)
    subtitle = "开源 MMORPG 游戏框架"
    subtitle_width = c.stringWidth(subtitle, 'Helvetica', 22)
    c.drawString((WIDTH - subtitle_width) / 2, HEIGHT / 2 + 0.2*cm, subtitle)

    # Tagline - whispered, not shouted
    c.setFont('Helvetica-Oblique', 12)
    c.setFillColor(COLORS['cyan'])
    tagline = "Godot 4.4+ + .NET 微服务架构"
    tagline_width = c.stringWidth(tagline, 'Helvetica-Oblique', 12)
    c.drawString((WIDTH - tagline_width) / 2, HEIGHT / 2 - 0.4*cm, tagline)

    # Version - bottom anchor, technical reference
    c.setFont('Helvetica', 9)
    c.setFillColor(COLORS['muted'])
    c.drawString(MARGIN, 2.5*cm, "v1.0 | Branch: refactor/microservices-backend")

    # Page number - systematic reference
    c.setFont('Helvetica-Bold', 14)
    c.setFillColor(COLORS['blue'])
    c.drawString(WIDTH - MARGIN - 1*cm, 2.5*cm, "01")

def slide_02_overview(c):
    """Slide 02: Project Overview - Information through spatial arrangement."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    draw_geometric_accent(c, 2)
    y = draw_header(c, "项目概述", 2)

    # Data points - systematic arrangement
    y -= 0.5*cm
    y = draw_data_point(c, "项目性质", "开源 MMORPG 框架实验项目", MARGIN + 1.5*cm, y, 'blue')
    y = draw_data_point(c, "架构模式", "双架构: Godot 原生 + .NET 微服务", MARGIN + 1.5*cm, y, 'violet')
    y = draw_data_point(c, "部署模型", "客户端/服务器分离部署", MARGIN + 1.5*cm, y, 'cyan')
    y = draw_data_point(c, "服务器架构", "三服务器模型: Gateway / Master / World", MARGIN + 1.5*cm, y, 'blue')
    y = draw_data_point(c, "网络协议", "自定义字节打包协议", MARGIN + 1.5*cm, y, 'cyan')

    # Info panel - creates visual anchor
    panel_y = y - 1*cm
    c.setFillColor(COLORS['panel'])
    c.roundRect(MARGIN, panel_y - 2.8*cm, WIDTH - 2*MARGIN, 2.8*cm, 0.3*cm, fill=1)

    # Panel header
    c.setFillColor(COLORS['amber'])
    c.setFont('Helvetica-Bold', 12)
    c.drawString(MARGIN + 0.6*cm, panel_y - 0.6*cm, "CORE METRICS")

    # Metrics grid - systematic display
    c.setFillColor(COLORS['muted'])
    c.setFont('Helvetica', 9)
    metrics = [
        ("Engine", "Godot 4.4 / 4.5"),
        ("Backend", ".NET 10.0 / C#"),
        ("Players/Instance", "200"),
        ("Physics Rate", "10 ticks/sec"),
        ("Status", "Microservices Refactor In Progress"),
    ]

    for i, (label, value) in enumerate(metrics):
        row_y = panel_y - 1.1*cm - (i // 3) * 0.55*cm
        col_x = MARGIN + 0.6*cm + (i % 3) * 5.5*cm
        c.setFillColor(COLORS['cyan'])
        c.drawString(col_x, row_y, f"{label}:")
        c.setFillColor(COLORS['white'])
        c.drawString(col_x + 1.2*cm, row_y, value)

def slide_03_evolution(c):
    """Slide 03: Architecture Evolution - Timeline as visual progression."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "架构演进路线", 3)

    phases = [
        ("Phase 01", "原生 GDScript 服务器", COLORS['panel'], "COMPLETE"),
        ("Phase 02", "AuthService 上线", COLORS['blue'], "COMPLETE"),
        ("Phase 03", "ChatService 上线", COLORS['cyan'], "COMPLETE"),
        ("Phase 04", "RoomService 上线", COLORS['violet'], "IN PROGRESS"),
        ("Phase 05", "GameService 上线", COLORS['amber'], "PLANNED"),
    ]

    box_width = (WIDTH - 2*MARGIN - 1*cm) / 5
    box_height = 3.5*cm

    for i, (phase, desc, color, status) in enumerate(phases):
        x = MARGIN + i * (box_width + 0.25*cm)

        # Phase module
        c.setFillColor(color)
        c.roundRect(x, y - box_height, box_width, box_height, 0.2*cm, fill=1)

        # Phase number - large, creates visual rhythm
        c.setFillColor(COLORS['white'])
        c.setFont('Helvetica-Bold', 11)
        c.drawString(x + 0.3*cm, y - 0.7*cm, phase)

        # Description
        c.setFont('Helvetica', 8)
        c.drawString(x + 0.3*cm, y - 1.3*cm, desc)

        # Status indicator - color-coded taxonomy
        if status == "COMPLETE":
            c.setFillColor(COLORS['emerald'])
            status_text = "✓"
        elif status == "IN PROGRESS":
            c.setFillColor(COLORS['amber'])
            status_text = "→"
        else:
            c.setFillColor(COLORS['muted'])
            status_text = "○"

        c.setFont('Helvetica-Bold', 16)
        c.drawString(x + 0.3*cm, y - 2.2*cm, status_text)

        # Status label
        c.setFont('Helvetica', 7)
        c.drawString(x + 0.8*cm, y - 2.2*cm, status)

def slide_04_godot_arch(c):
    """Slide 04: Godot Native Architecture - Three-server model."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "三服务器模型", 4) - 0.5*cm

    servers = [
        ("GATEWAY", "HTTP REST API", "认证路由 → Master", COLORS['blue']),
        ("MASTER", "中央协调器", "账户管理、网关桥接", COLORS['violet']),
        ("WORLD", "游戏实例托管", "10 ticks/sec | 200 玩家", COLORS['cyan']),
    ]

    for i, (name, type_desc, detail, color) in enumerate(servers):
        y = draw_module(c, name, f"{type_desc} | {detail}",
                       MARGIN, y, WIDTH - 2*MARGIN, 2.2*cm, color)
        y -= 0.4*cm

    # Protocol reference
    c.setFillColor(COLORS['panel'])
    c.roundRect(MARGIN, y - 1.8*cm, WIDTH - 2*MARGIN, 1.8*cm, 0.2*cm, fill=1)

    c.setFillColor(COLORS['amber'])
    c.setFont('Helvetica-Bold', 10)
    c.drawString(MARGIN + 0.5*cm, y - 0.5*cm, "STATE SYNC PROTOCOL")

    c.setFillColor(COLORS['muted'])
    c.setFont('Helvetica', 8)
    c.drawString(MARGIN + 0.5*cm, y - 0.9*cm, "PackedByteArray + PathRegistry | Baseline + Delta | Zone-based Optimization")

def slide_05_microservices(c):
    """Slide 05: Microservices Architecture - Service topology."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "GameBackend 微服务设计", 5) - 0.5*cm

    services = [
        ("ApiGateway", "5000", "Ocelot 网关", COLORS['blue']),
        ("AuthService", "5298/7072", "JWT 认证", COLORS['violet']),
        ("GameService", "—", "游戏逻辑", COLORS['cyan']),
        ("RoomService", "—", "房间管理", COLORS['emerald']),
        ("ChatService", "8090", "SignalR 聊天", COLORS['amber']),
    ]

    box_w = (WIDTH - 2*MARGIN - 1*cm) / 2
    box_h = 2*cm

    for i, (name, port, desc, color) in enumerate(services):
        row, col = i // 2, i % 2
        x = MARGIN + col * (box_w + 1*cm)
        box_y = y - row * (box_h + 0.5*cm)

        c.setFillColor(color)
        c.roundRect(x, box_y - box_h, box_w, box_h, 0.2*cm, fill=1)

        c.setFillColor(COLORS['white'])
        c.setFont('Helvetica-Bold', 13)
        c.drawString(x + 0.3*cm, box_y - 0.5*cm, name)

        c.setFont('Helvetica', 8)
        c.drawString(x + 0.3*cm, box_y - 0.9*cm, f"Port: {port} | {desc}")

    # Consul reference
    consul_y = y - 3.5 * (box_h + 0.5*cm)
    c.setFillColor(COLORS['panel'])
    c.roundRect(MARGIN, consul_y - 1.2*cm, WIDTH - 2*MARGIN, 1.2*cm, 0.2*cm, fill=1)

    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 10)
    c.drawString(MARGIN + 0.4*cm, consul_y - 0.5*cm, "Consul (8500) — 服务发现 | 健康检查 | 负载均衡")

def slide_06_techstack(c):
    """Slide 06: Technology Stack - Chromatic categorization."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "技术栈", 6) - 0.5*cm

    # Left column - Godot ecosystem
    c.setFillColor(COLORS['blue'])
    c.rect(MARGIN, y - 0.7*cm, 2.5*cm, 0.7*cm, fill=1)

    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 11)
    c.drawString(MARGIN + 0.3*cm, y - 0.45*cm, "GODOT")

    y -= 1.1*cm
    items_godot = [
        ("Godot 4.4 / 4.5 Engine", "blue"),
        ("GDScript 原生服务器", "cyan"),
        ("自定义字节打包协议", "blue"),
        ("PackedByteArray 序列化", "cyan"),
    ]

    for item, color in items_godot:
        y = draw_data_point(c, "", item, MARGIN + 1*cm, y, color)

    # Right column - .NET ecosystem
    y_right = y + 3.2*cm
    x_right = WIDTH / 2 + 1*cm

    c.setFillColor(COLORS['violet'])
    c.rect(x_right, y_right - 0.7*cm, 2.5*cm, 0.7*cm, fill=1)

    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 11)
    c.drawString(x_right + 0.3*cm, y_right - 0.45*cm, ".NET")

    y_right -= 1.1*cm
    items_dotnet = [
        (".NET 10.0 / ASP.NET Core", "violet"),
        ("Ocelot API Gateway", "purple"),
        ("PostgreSQL + EF Core", "blue"),
        ("Redis 缓存", "amber"),
        ("SignalR 实时通信", "cyan"),
        ("Consul 服务发现", "violet"),
        ("Docker 容器化", "blue"),
    ]

    for item, color in items_dotnet:
        y_right = draw_data_point(c, "", item, x_right + 1*cm, y_right, color)

    # Bottom tags - visual reference system
    tag_y = min(y, y_right) - 1.5*cm
    tags = [
        ("JWT Auth", COLORS['violet']),
        ("gRPC", COLORS['blue']),
        ("REST", COLORS['cyan']),
        ("SignalR", COLORS['purple']),
        ("OpenTelemetry", COLORS['amber']),
    ]

    tag_w = 3.2*cm
    for i, (tag, color) in enumerate(tags):
        x = MARGIN + i * (tag_w + 0.3*cm)
        c.setFillColor(color)
        c.roundRect(x, tag_y - 0.8*cm, tag_w, 0.8*cm, 0.1*cm, fill=1)
        c.setFillColor(COLORS['white'])
        c.setFont('Helvetica-Bold', 9)
        c.drawString(x + 0.3*cm, tag_y - 0.55*cm, tag)

def slide_07_features(c):
    """Slide 07: Core Features - Information density through systematic arrangement."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "核心特性", 7) - 0.5*cm

    features = [
        ("水平扩展", "动态扩容 + 负载均衡", COLORS['blue']),
        ("服务发现", "Consul 自动注册与检查", COLORS['violet']),
        ("分布式追踪", "OpenTelemetry + Jaeger", COLORS['cyan']),
        ("二进制协议", "自定义字节打包优化", COLORS['emerald']),
        ("状态同步", "Baseline + Delta 增量更新", COLORS['amber']),
        ("内容注册", "ContentRegistryHub 资源索引", COLORS['purple']),
        ("RPC 模式", "DataRequestHandler 统一处理", COLORS['blue']),
    ]

    for title, desc, color in features:
        y = draw_module(c, title, desc, MARGIN, y, WIDTH - 2*MARGIN, 1.6*cm, color)
        y -= 0.3*cm

def slide_08_structure(c):
    """Slide 08: Project Structure - Directory as visual system."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "项目结构", 8) - 0.5*cm

    panel_w = (WIDTH - 2*MARGIN - 1*cm) / 2

    # Source panel
    c.setFillColor(COLORS['panel'])
    c.roundRect(MARGIN, y - 5.5*cm, panel_w, 5.5*cm, 0.2*cm, fill=1)

    c.setFillColor(COLORS['blue'])
    c.setFont('Helvetica-Bold', 11)
    c.drawString(MARGIN + 0.3*cm, y - 0.4*cm, "source/")

    lines = [
        ("client/", "   客户端 (UI, 本地玩家, 网络)", "cyan"),
        ("common/", "   共享代码 (游戏逻辑, 协议)", "blue"),
        ("server/", "   GDScript 服务器", "cyan"),
        ("  ├─ gateway/", "   HTTP REST 认证", "subtle"),
        ("  ├─ master/", "   账户管理, 编排", "subtle"),
        ("  └─ world/", "   实例管理, 游戏处理", "subtle"),
    ]

    for i, (prefix, text, color_name) in enumerate(lines):
        line_y = y - 0.8*cm - i * 0.65*cm
        c.setFillColor(COLORS[color_name])
        c.setFont('Helvetica', 9)
        c.drawString(MARGIN + 0.3*cm, line_y, prefix)
        c.setFillColor(COLORS['muted'])
        c.drawString(MARGIN + 0.3*cm + c.stringWidth(prefix, 'Helvetica', 9), line_y, text)

    # GameBackend panel
    rx = MARGIN + panel_w + 1*cm
    c.setFillColor(COLORS['panel'])
    c.roundRect(rx, y - 5.5*cm, panel_w, 5.5*cm, 0.2*cm, fill=1)

    c.setFillColor(COLORS['violet'])
    c.setFont('Helvetica-Bold', 11)
    c.drawString(rx + 0.3*cm, y - 0.4*cm, "GameBackend/")

    backend_lines = [
        ("Game.ApiGateway/", "   Ocelot API 网关", "amber"),
        ("Game.AuthService/", "   认证服务 (JWT)", "violet"),
        ("Game.GameService/", "   游戏核心逻辑", "cyan"),
        ("Game.RoomService/", "   房间/实例管理", "emerald"),
        ("Game.ChatService/", "   SignalR 实时聊天", "purple"),
        ("Game.Shared/", "   共享代码库", "subtle"),
    ]

    for i, (prefix, text, color_name) in enumerate(backend_lines):
        line_y = y - 0.8*cm - i * 0.65*cm
        c.setFillColor(COLORS[color_name])
        c.setFont('Helvetica', 9)
        c.drawString(rx + 0.3*cm, line_y, prefix)
        c.setFillColor(COLORS['muted'])
        c.drawString(rx + 0.3*cm + c.stringWidth(prefix, 'Helvetica', 9), line_y, text)

def slide_09_quickstart(c):
    """Slide 09: Quick Start - Actions as visual commands."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "快速开始", 9) - 0.5*cm

    # Godot section
    c.setFillColor(COLORS['blue'])
    c.rect(MARGIN, y - 0.7*cm, 2.5*cm, 0.7*cm, fill=1)

    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 11)
    c.drawString(MARGIN + 0.3*cm, y - 0.45*cm, "GODOT")

    y -= 1.1*cm
    y = draw_data_point(c, "编辑器", "Godot 4.4+ 打开项目", MARGIN + 1*cm, y, 'cyan')
    y = draw_data_point(c, "配置", "Feature Tags: gateway-server | master-server | world-server | client", MARGIN + 1*cm, y, 'blue')
    y = draw_data_point(c, "调试", "支持多实例 (4+)", MARGIN + 1*cm, y, 'cyan')

    # .NET section
    y -= 0.3*cm
    c.setFillColor(COLORS['violet'])
    c.rect(MARGIN, y - 0.7*cm, 2.5*cm, 0.7*cm, fill=1)

    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 11)
    c.drawString(MARGIN + 0.3*cm, y - 0.45*cm, ".NET")

    y -= 1.1*cm
    y = draw_data_point(c, "全部服务", "docker-compose up -d", MARGIN + 1*cm, y, 'amber')
    y = draw_data_point(c, "单个服务", "dotnet run --project Game.AuthService", MARGIN + 1*cm, y, 'violet')

    # Command reference box
    cmd_y = y - 1.2*cm
    c.setFillColor(COLORS['panel'])
    c.roundRect(MARGIN, cmd_y - 1.8*cm, WIDTH - 2*MARGIN, 1.8*cm, 0.2*cm, fill=1)

    c.setFillColor(COLORS['cyan'])
    c.setFont('Helvetica-Bold', 10)
    c.drawString(MARGIN + 0.4*cm, cmd_y - 0.5*cm, "$ SERVICE STATUS")

    c.setFillColor(COLORS['muted'])
    c.setFont('Helvetica', 8)
    c.drawString(MARGIN + 0.4*cm, cmd_y - 0.9*cm, "curl http://localhost:8500/v1/agent/services")
    c.drawString(MARGIN + 0.4*cm, cmd_y - 1.25*cm, "http://localhost:16686 (Jaeger Tracing)")

def slide_10_database(c):
    """Slide 10: Data Persistence - Storage as systematic taxonomy."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "数据持久化", 10) - 0.5*cm

    # PostgreSQL
    c.setFillColor(COLORS['blue'])
    c.rect(MARGIN, y - 0.7*cm, 4*cm, 0.7*cm, fill=1)

    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 11)
    c.drawString(MARGIN + 0.3*cm, y - 0.45*cm, "PostgreSQL")

    y -= 1.1*cm
    y = draw_data_point(c, "用户账户", "AuthService", MARGIN + 1*cm, y, 'blue')
    y = draw_data_point(c, "聊天消息", "ChatService", MARGIN + 1*cm, y, 'cyan')
    y = draw_data_point(c, "房间信息", "RoomService", MARGIN + 1*cm, y, 'violet')
    y = draw_data_point(c, "游戏数据", "GameService", MARGIN + 1*cm, y, 'purple')

    # Redis
    y -= 0.3*cm
    c.setFillColor(COLORS['amber'])
    c.rect(MARGIN, y - 0.7*cm, 4*cm, 0.7*cm, fill=1)

    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 11)
    c.drawString(MARGIN + 0.3*cm, y - 0.45*cm, "Redis")

    y -= 1.1*cm
    y = draw_data_point(c, "在线用户", "状态管理", MARGIN + 1*cm, y, 'amber')
    y = draw_data_point(c, "会话缓存", "临时存储", MARGIN + 1*cm, y, 'amber')

    # QAD
    y -= 0.3*cm
    c.setFillColor(COLORS['panel'])
    c.rect(MARGIN, y - 0.7*cm, 4*cm, 0.7*cm, fill=1)

    c.setFillColor(COLORS['white'])
    c.setFont('Helvetica-Bold', 11)
    c.drawString(MARGIN + 0.3*cm, y - 0.45*cm, "QAD")

    y -= 1.1*cm
    y = draw_data_point(c, "PlayerData", "玩家数据", MARGIN + 1*cm, y, 'cyan')
    y = draw_data_point(c, "Guild", "公会数据", MARGIN + 1*cm, y, 'cyan')

    # Tech reference
    ref_y = y - 1.2*cm
    c.setFillColor(COLORS['panel'])
    c.roundRect(MARGIN, ref_y - 1.2*cm, WIDTH - 2*MARGIN, 1.2*cm, 0.2*cm, fill=1)

    c.setFillColor(COLORS['cyan'])
    c.setFont('Helvetica-Bold', 9)
    c.drawString(MARGIN + 0.3*cm, ref_y - 0.4*cm, "EF Core | Code First | Connection Pooling")

def slide_11_network(c):
    """Slide 11: Network Architecture - Flow as visual progression."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "网络架构", 11) - 0.5*cm

    cx = WIDTH / 2

    # Flow diagram - spatial communication
    modules = [
        ("Godot Client", COLORS['blue']),
        ("ApiGateway", COLORS['violet']),
    ]

    my = y
    for name, color in modules:
        c.setFillColor(color)
        c.roundRect(cx - 4.5*cm, my - 1.5*cm, 9*cm, 1.5*cm, 0.2*cm, fill=1)
        c.setFillColor(COLORS['white'])
        c.setFont('Helvetica-Bold', 14)
        c.drawString(cx - c.stringWidth(name, 'Helvetica-Bold', 14)/2, my - 0.9*cm, name)
        my -= 2.2*cm

        if my < y - 2*cm:
            # Connection
            c.setStrokeColor(COLORS['muted'])
            c.setLineWidth(0.05*cm)
            c.line(cx, my + 1.7*cm, cx, my + 0.3*cm)
            # Label
            c.setFillColor(COLORS['muted'])
            c.setFont('Helvetica-Oblique', 8)
            label = "HTTP / SignalR" if name == "Godot Client" else "Service Discovery"
            c.drawString(cx + 0.3*cm, my + 0.9*cm, label)

    # Services row
    services = [("Auth", COLORS['blue']), ("Game", COLORS['cyan']),
                ("Room", COLORS['emerald']), ("Chat", COLORS['amber'])]

    sw = 3.2*cm
    total_w = len(services) * sw + (len(services) - 1) * 0.3*cm
    sx = cx - total_w / 2

    for i, (name, color) in enumerate(services):
        x = sx + i * (sw + 0.3*cm)
        c.setFillColor(color)
        c.roundRect(x, my - 1.5*cm, sw, 1.5*cm, 0.2*cm, fill=1)
        c.setFillColor(COLORS['white'])
        c.setFont('Helvetica-Bold', 11)
        c.drawString(x + sw/2 - c.stringWidth(name, 'Helvetica-Bold', 11)/2, my - 0.9*cm, name)

    # Protocol box
    proto_y = my - 2.5*cm
    c.setFillColor(COLORS['panel'])
    c.roundRect(MARGIN, proto_y - 2.5*cm, WIDTH - 2*MARGIN, 2.5*cm, 0.2*cm, fill=1)

    c.setFillColor(COLORS['amber'])
    c.setFont('Helvetica-Bold', 10)
    c.drawString(MARGIN + 0.3*cm, proto_y - 0.5*cm, "STATE SYNC PROTOCOL")

    c.setFillColor(COLORS['muted'])
    c.setFont('Helvetica', 8)
    details = [
        "PackedByteArray binary format",
        "PathRegistry field ID mapping",
        "Baseline + Delta incremental updates",
        "Zone-based spatial optimization",
    ]
    for i, d in enumerate(details):
        c.drawString(MARGIN + 0.3*cm, proto_y - 0.9*cm - i * 0.4*cm, d)

def slide_12_roadmap(c):
    """Slide 12: Roadmap - Timeline as status taxonomy."""
    c.setFillColor(COLORS['void'])
    c.rect(0, 0, WIDTH, HEIGHT, fill=1)

    y = draw_header(c, "开发路线图", 12) - 0.5*cm

    items = [
        ("AuthService 上线", "COMPLETE", COLORS['emerald']),
        ("ChatService 上线", "COMPLETE", COLORS['emerald']),
        ("RoomService 开发中", "IN PROGRESS", COLORS['amber']),
        ("GameService 迁移中", "IN PROGRESS", COLORS['amber']),
        ("完全微服务化", "PLANNED", COLORS['muted']),
        ("性能优化与压力测试", "PLANNED", COLORS['muted']),
        ("生产环境部署", "PLANNED", COLORS['muted']),
    ]

    for text, status, color in items:
        c.setFillColor(COLORS['panel'])
        c.roundRect(MARGIN, y - 1.2*cm, WIDTH - 2*MARGIN, 1.2*cm, 0.15*cm, fill=1)

        # Status icon
        if status == "COMPLETE":
            icon = "✓"
        elif status == "IN PROGRESS":
            icon = "→"
        else:
            icon = "○"

        c.setFillColor(color)
        c.setFont('Helvetica-Bold', 18)
        c.drawString(MARGIN + 0.5*cm, y - 0.4*cm, icon)

        c.setFillColor(COLORS['white'])
        c.setFont('Helvetica-Bold', 12)
        c.drawString(MARGIN + 1.2*cm, y - 0.4*cm, text)

        c.setFont('Helvetica', 9)
        c.drawString(WIDTH - MARGIN - c.stringWidth(status, 'Helvetica', 9) - 0.5*cm,
                    y - 0.4*cm, status)

        y -= 1.5*cm

def main():
    """Generate the presentation - the culmination of meticulous craftsmanship."""
    output_path = "/home/ef_cosmos/project/game/godot-tiny-mmo/Godot_Tiny_MMO_Presentation.pdf"

    c = canvas.Canvas(output_path, pagesize=A4)

    slide_01_title(c)
    c.showPage()

    slide_02_overview(c)
    c.showPage()

    slide_03_evolution(c)
    c.showPage()

    slide_04_godot_arch(c)
    c.showPage()

    slide_05_microservices(c)
    c.showPage()

    slide_06_techstack(c)
    c.showPage()

    slide_07_features(c)
    c.showPage()

    slide_08_structure(c)
    c.showPage()

    slide_09_quickstart(c)
    c.showPage()

    slide_10_database(c)
    c.showPage()

    slide_11_network(c)
    c.showPage()

    slide_12_roadmap(c)
    c.showPage()

    c.save()

    print(f"Presentation created: {output_path}")
    print("Design Philosophy: System Architecture")
    print("Chromatic taxonomy | Modular precision | Spatial communication")

if __name__ == "__main__":
    main()
