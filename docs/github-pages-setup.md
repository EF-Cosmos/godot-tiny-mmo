# GitHub Pages 配置指南

本文档说明如何为 Godot Tiny MMO 项目配置 GitHub Pages 以展示演示文稿。

## 自动部署配置

本项目已配置 GitHub Actions 工作流，可自动将 `slides/` 目录部署到 GitHub Pages。

### 启用 GitHub Pages

1. **进入仓库设置**
   - 访问 https://github.com/EF-Cosmos/godot-tiny-mmo/settings/pages

2. **配置 Pages 源**
   - **Source (源)**: 选择 "GitHub Actions"
   - （不要选择 "Deploy from a branch"）

3. **保存配置**
   - 配置会自动保存

### 工作流说明

工作流文件位于 `.github/workflows/deploy-slides.yml`，具有以下特性：

- **触发条件**:
  - 当 `slides/` 目录中的文件被推送到 `main` 或 `master` 分支时
  - 当工作流文件本身被修改时
  - 可以手动触发（workflow_dispatch）

- **部署内容**:
  - 整个 `slides/` 目录
  - 包括所有 HTML 文件、子目录和资源

- **权限**:
  - `contents: read` - 读取仓库内容
  - `pages: write` - 写入 Pages
  - `id-token: write` - 用于部署认证

### 访问演示文稿

配置完成后，演示文稿将在以下地址可用：

- **主页**: https://ef-cosmos.github.io/godot-tiny-mmo/
- **幻灯片索引**: https://ef-cosmos.github.io/godot-tiny-mmo/index.html
- **特定幻灯片**: https://ef-cosmos.github.io/godot-tiny-mmo/slide01.html

### 手动触发部署

如需手动触发部署：

1. 访问 Actions 页面: https://github.com/EF-Cosmos/godot-tiny-mmo/actions
2. 选择 "Deploy Slides to GitHub Pages" 工作流
3. 点击 "Run workflow" 按钮
4. 选择分支（通常是 main）
5. 点击 "Run workflow"

## 验证部署

部署完成后，您可以：

1. **查看工作流状态**
   - 访问 https://github.com/EF-Cosmos/godot-tiny-mmo/actions
   - 查看最新的 "Deploy Slides to GitHub Pages" 运行

2. **访问网站**
   - 打开 https://ef-cosmos.github.io/godot-tiny-mmo/
   - 确认所有幻灯片都能正常加载

3. **检查样式和资源**
   - 验证 CSS 样式正确加载
   - 检查字体和图片显示正常

## 故障排除

### 部署失败

如果部署失败：

1. 检查 Actions 日志查看错误信息
2. 确认 GitHub Pages 在仓库设置中已启用
3. 确认分支名称正确（main 或 master）
4. 检查权限配置是否正确

### 页面显示 404

如果访问页面时显示 404：

1. 等待几分钟，GitHub Pages 部署可能需要一些时间
2. 检查 URL 是否正确
3. 确认工作流已成功完成
4. 在仓库设置中查看 Pages 配置

### 样式或资源未加载

如果样式未正确显示：

1. 检查浏览器控制台的错误信息
2. 确认所有资源路径使用相对路径
3. 验证外部资源（如 Google Fonts）可访问

## 更新演示文稿

要更新演示文稿：

1. 修改 `slides/` 目录中的 HTML 文件
2. 提交并推送到 main 分支
3. GitHub Actions 将自动部署更新

```bash
cd slides
# 编辑文件...
git add .
git commit -m "Update slides"
git push origin main
```

## 目录结构

```
slides/
├── index.html              # 主索引页
├── README.md              # 说明文档
├── slide01.html           # 标题页
├── slide02.html           # 项目介绍
├── slide03.html           # 技术架构
├── slide04.html           # 网络架构
├── slide05.html           # 游戏功能
├── slide06.html           # 微服务架构
├── slide07.html           # 开发路线
├── architecture.html      # 系统架构
└── godot-mmo-ppt/        # 扩展演示
    └── slides/
        ├── slide1.html
        ├── slide2.html
        └── ...
```

## 自定义域名（可选）

如果要使用自定义域名：

1. 在 `slides/` 目录中创建 `CNAME` 文件
2. 文件内容为您的域名（如 `slides.example.com`）
3. 在域名提供商处配置 DNS 记录指向 GitHub Pages
4. 在仓库设置中的 Pages 部分配置自定义域名

更多信息请参考：https://docs.github.com/pages/configuring-a-custom-domain-for-your-github-pages-site
