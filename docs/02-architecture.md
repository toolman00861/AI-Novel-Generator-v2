# 系统架构与技术选型

## 架构总览
- 客户端（C#/.NET WPF + MVVM）：
  - 窗口/页面：仪表盘、小说列表/详情、章节编辑器、人物设计、AI 生成面板
  - 数据：通过 HttpClient 访问可选后端 REST/SSE；统一错误与加载态
  - 持久化服务：抽离为通用模块（`IPersistenceService` + `SqlitePersistenceService`），统一数据库连接与初始化
- 后端（可选，ASP.NET Core + EF Core）：
  - 模块：Auth（可后置）、Content（小说/章节/人物）、Context、AI Provider、Admin
  - 接口：REST + SSE（流式生成）
  - Provider：封装 OpenAI/Azure/OpenRouter/自建模型等，统一生成接口
- 数据库（SQLite 开发模式 / SQL Server 生产）：
  - 表：novels、chapters、characters、relationships、context_items、prompt_templates、generation_runs 等
- 部署：客户端 Windows 安装包；后端 ASP.NET Core 部署（容器/裸机）（可选）

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
4. Streaming（SSE）
    - 后端将生成结果按 token/句子流式推送，客户端订阅并实时渲染
    - 客户端通过 HttpClient 解析 `text/event-stream` 或按 chunk 读取
    - 支持中途停止（取消请求）与再次续写（接续上下文）

## 数据流（示意）
1. 客户端在章节编辑页选择上下文范围与生成模式
2. 调用后端 `/v1/generate`（或直接调用第三方 API），后端 Context 模块聚合上下文
3. 后端调用 AI Provider 并以 SSE 推送增量结果
4. 客户端消费流并显示；用户点击“接受”后保存为章节内容或片段

## 技术选型说明
- WPF：适合 Windows 桌面复杂 UI，MVVM 模式易于维护
- ASP.NET Core：模块化清晰、统一 .NET 技术栈、易部署（可选）
- SQLite/SQL Server：SQLite 开发便捷；生产可选 SQL Server，关系型建模清晰
- 客户端设置持久化：内嵌 SQLite 文件（`AINovelStudio.settings.db`），首启自动从 `appsettings.client.json` 迁移；由 `SettingsService` 提供读写接口

## 横切关注点
- 鉴权与配额：JWT + rate limit（后置）
- 审计与日志：记录生成请求、消耗、错误码
- 缓存与优化：上下文聚合结果缓存（基于 novel/chapter/character 哈希）
- 异常处理：统一错误包装；AI Provider 重试策略（指数退避）