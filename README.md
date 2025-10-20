# AI 小说工坊（AI Novel Studio）

一个基于 C#/.NET 的 Windows 桌面应用（WPF）用于 AI 小说生成与管理。支持基础的文章内容管理、联动上下文、人物设定；AI 生成通过第三方 API 或可选后端服务实现。

## 技术选型（建议）
- 客户端：C#（.NET，WPF，MVVM）
- 后端（可选）：ASP.NET Core Web API + EF Core
- 数据库：SQLite（开发便捷）或 SQL Server（生产可选）
- AI 供应商：可插拔（OpenAI/Azure OpenAI/本地大模型服务），通过统一 Provider 接口封装
- 通信：HTTP/JSON + 流式输出（SSE 或流式读取）

## 项目结构（拟）
```
repo-root/
  docs/                      # 文档
    01-product-requirements.md
    02-architecture.md
    03-api-spec.md
    04-data-model.md
    05-frontend-backend-plan.md
  client/                    # WPF 客户端（后续创建）
  server/                    # ASP.NET Core 后端（可选，后续创建）
```

## 功能概览
- 文章内容管理：小说/章节/片段的 CRUD、草稿与发布、标签与状态
- 联动上下文：根据选择的范围（角色设定、世界观、章节摘要、之前段落）汇总上下文喂给模型
- 人物设计：角色档案、性格标签、说话风格、背景关系，支持与章节/片段关联
- AI 生成：支持续写、改写、大纲生成、摘要；支持流式输出与提示词模板
- 权限与协作：预留用户与角色权限（MVP 可先不做）

## 部署建议
- 客户端：Windows 桌面应用（WPF），通过安装包分发（MSIX/ClickOnce/zip）
- 后端（可选）：ASP.NET Core 部署于自有服务器或容器，暴露 REST + SSE 接口
- 配置：后端使用 `appsettings.json`；客户端使用应用配置（API 基址、功能开关）

## 开发里程碑
1. 文档与架构定稿（当前阶段）
2. 搭建（可选）后端骨架：ASP.NET Core 实体、仓储、控制器、AI Provider 接口
3. 搭建 WPF 客户端骨架：窗口/页面、MVVM、数据访问（HttpClient）
4. 实现内容管理与人物设计的 CRUD
5. 打通联动上下文与 AI 生成（流式输出/流式读取）
6. 优化与发布：鉴权、审计、限流、灰度、监控

## 运行（后续）
- 客户端：`cd client && dotnet restore && dotnet run`
- 后端（可选）：`cd server && dotnet restore && dotnet run`

> 当前仓库为文档阶段，代码将在文档确认后创建。