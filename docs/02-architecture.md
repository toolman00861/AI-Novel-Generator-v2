# 系统架构与技术选型

## 架构总览
- 客户端（C#/.NET WPF + MVVM）：
  - 窗口/页面：仪表盘、小说列表/详情、章节编辑器、人物设计、AI 生成面板
  - 数据：直接调用第三方 AI Provider API（可选）；统一错误与加载态（客户端）
  - 持久化服务：抽离为通用模块（`IPersistenceService` + `SqlitePersistenceService`），统一数据库连接与初始化
- 数据库（客户端本地 SQLite）：
  - 文件：`AINovelStudio.settings.db`（首次启动自动创建）
- 部署：客户端 Windows 安装包（仅客户端）

## 关键模块设计
1. Content 模块
   - 负责小说/章节/人物的 CRUD 与查询
   - 提供章节摘要生成（可调用 AI 或手工录入）
2. Context 模块
   - 根据用户选择的范围聚合上下文，做长度控制与裁剪
   - 支持权重与优先级（人物>章节摘要>世界观>前文段落）
3. AI Provider 模块
   - 标准接口：`generate({ system, user, context, mode, model, params }): Stream`，返回流
   - 适配具体供应商：密钥读取、请求参数映射、错误处理与重试
4. Streaming（供应商 API）
    - 供应商 API 返回流式文本或 chunk，客户端解析并实时渲染
    - 客户端通过 HttpClient 解析 `text/event-stream` 或按 chunk 读取
    - 支持中途停止（取消请求）与再次续写（接续上下文）

## 数据流（示意）
1. 客户端在章节编辑页选择上下文范围与生成模式
2. 客户端聚合上下文并直接调用第三方 AI Provider API
3. 客户端解析流式/分块响应并实时渲染
4. 用户点击“接受”后保存为章节内容或片段（本地 SQLite）

## 技术选型说明
- WPF：适合 Windows 桌面复杂 UI，MVVM 模式易于维护
- 客户端本地 SQLite：轻量、易部署；统一通过 `SqlitePersistenceService` 访问
- 客户端设置持久化：内嵌 SQLite 文件（`AINovelStudio.settings.db`），首次启动自动创建并在无记录时从 `appsettings.client.json` 迁移；由 `SettingsService` 提供读写接口

## 横切关注点
- 密钥与安全：客户端安全输入与存储（避免硬编码）；基础输入校验
- 审计与日志：记录生成请求、消耗、错误码（客户端）
- 缓存与优化：上下文聚合结果缓存（基于 novel/chapter/character 哈希）
- 异常处理：统一错误包装；AI Provider 重试策略（指数退避）