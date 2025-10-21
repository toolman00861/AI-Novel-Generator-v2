# 客户端开发计划（MVP）

## 技术栈与目录
- 客户端：`client/`（WPF + .NET + MVVM）
- 持久化：本地 SQLite（`AINovelStudio.settings.db`，首次启动自动创建）

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
- 持久化存储服务
  - 抽象接口 `IPersistenceService` 与实现 `SqlitePersistenceService`，供设置、内容管理等模块复用；可替换为其他存储实现



## 环境与配置
- 客户端配置（WPF）：
  - 配置与设置持久化：使用内嵌 SQLite 文件 `AINovelStudio.settings.db`（`Microsoft.Data.Sqlite`）；首次启动自动创建并在无记录时从 `appsettings.client.json` 迁移；通过 `SettingsService.Load()` / `SettingsService.Save()` 读写
  - Provider：供应商配置（`vendor`、`apiKey`、`baseUrl`、`defaultModel`）由客户端设置并持久化到 SQLite
  - `FeatureFlags`（可选）

## 开发里程碑（建议）
1. 搭建 WPF 客户端骨架：窗口/页面、MVVM、模块化服务（持久化、Provider）
2. 实现基础 CRUD 与列表分页（本地 SQLite）；客户端拉通列表与详情页
3. 实现上下文选择器与聚合逻辑（客户端）
4. 集成第三方 AI Provider（OpenAI/Azure/OpenRouter 等），实现流式输出或分块解析
5. 增强人物设计（关系、口吻、标签）；将角色注入生成上下文
6. 收敛与优化：错误处理、日志与打包发布（Windows）

## 部署方案（初版）
- 客户端：MSIX/ClickOnce/安装包分发；支持自动更新（可选）
- 监控：客户端崩溃/使用统计（可选）

## 测试与质量
- 单元测试：Context 聚合与 Provider 调用（mock）
- 端到端（E2E）：章节编辑页发起生成并保存结果
- 代码规范：.NET Analyzers + EditorConfig；提交前检查（lint/format）