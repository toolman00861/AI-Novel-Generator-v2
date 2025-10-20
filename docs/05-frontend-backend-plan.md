# 前后端拆分与开发计划（MVP）

## 技术栈与目录
- 客户端：`client/`（WPF + .NET + MVVM）
- 后端（可选）：`server/`（ASP.NET Core + EF Core + SQLite/SQL Server）

## 客户端模块与页面
- 页面
  - `/`：仪表盘（最近项目与生成）
  - `/novels`：小说列表与创建
  - `/novels/[id]`：小说详情（章节列表 + 人物概要）
  - `/chapters/[id]`：章节编辑器（上下文侧栏 + 生成面板）
  - `/characters` & `/characters/[id]`：人物档案管理
- 组件
  - 编辑器（富文本/Markdown）、上下文选择器、角色卡片、生成进度（SSE 流）
- 数据访问
  - 基于 HttpClient + Polly：统一请求封装、错误与加载态管理；SSE/流式解析封装

## 后端模块与接口
- Modules：`ContentModule`、`ContextModule`、`GenerationModule`、`ProviderModule`、`AuthModule`（后置）
- Controllers（参考 `docs/03-api-spec.md`）
- Entities：参考 `docs/04-data-model.md`
- Provider 接口：统一 `generate()` 返回流；支持多厂商切换

## 环境与配置
- 后端 `appsettings.json`（ASP.NET Core）：
  - `ConnectionStrings:Default`（SQLite/SQL Server）
  - `Provider:Vendor`（openai|azure|openrouter|custom）
  - `Provider:ApiKey`
  - `Provider:BaseUrl`（可选）
  - `Provider:DefaultModel`（如 `gpt-4o-mini`）
- 客户端配置（WPF）：
  - `ApiBase`（例如 `https://api.example.com`，有后端时）
  - `FeatureFlags`（可选）

## 开发里程碑（建议）
1. 初始化（可选）后端骨架（ASP.NET Core）与核心实体（novel/chapter/character）
2. 实现基础 CRUD 与列表分页；客户端拉通列表与详情页
3. 实现 Context 聚合端点与客户端上下文选择器
4. 集成 AI Provider（OpenAI），实现 `/v1/generate` SSE 与客户端流式展示
5. 增强人物设计（关系、口吻、标签）；将角色注入生成上下文
6. 收敛与优化：错误处理、配额与日志；打包发布（Windows）

## 部署方案（初版）
- 后端（可选）：Docker 容器（`mcr.microsoft.com/dotnet/aspnet:8.0`）+ SQL Server/SQLite；或裸机 IIS/Kestrel
- 客户端：MSIX/ClickOnce/安装包分发；支持自动更新（可选）
- 监控：后端日志与错误告警；客户端崩溃/使用统计（可选）

## 测试与质量
- 单元测试：Context 聚合与 Provider 调用（mock）
- 端到端（E2E）：章节编辑页发起生成并保存结果
- 代码规范：.NET Analyzers + EditorConfig；提交前检查（lint/format）