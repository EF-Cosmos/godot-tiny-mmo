# Godot Tiny MMO - 演示文稿

本目录包含 Godot Tiny MMO 项目的演示文稿和架构说明。

## 在线访问

访问 [GitHub Pages](https://ef-cosmos.github.io/godot-tiny-mmo/) 查看演示文稿。

## 本地查看

直接在浏览器中打开任何 HTML 文件即可查看：

```bash
# 打开主索引页
open index.html

# 或打开特定幻灯片
open slide01.html
```

## 文件说明

### 主题演示文稿
- `slide01.html` - GODOT TINY MMO 标题页
- `slide02.html` - 项目介绍与核心特性
- `slide03.html` - 技术架构概述
- `slide04.html` - 网络架构与通信
- `slide05.html` - 游戏功能展示
- `slide06.html` - 微服务后端架构
- `slide07.html` - 开发路线图与未来规划

### 架构文档
- `architecture.html` - 完整的系统架构设计与技术栈说明

### 扩展演示
- `godot-mmo-ppt/slides/` - 包含更多技术细节的完整演示文稿系列

## 技术说明

所有演示文稿采用纯 HTML + CSS 开发，具有以下特性：
- 响应式设计
- 赛博朋克风格视觉效果
- 动画效果和过渡
- 无需外部依赖（字体除外）

## 部署

演示文稿通过 GitHub Actions 自动部署到 GitHub Pages。当 `slides/` 目录中的文件更新并推送到 main 分支时，工作流将自动运行并更新在线版本。

### 手动部署

也可以通过 GitHub Actions 的 "workflow_dispatch" 事件手动触发部署。
