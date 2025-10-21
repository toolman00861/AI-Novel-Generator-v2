# Repository Guidelines

## 项目结构与模块组织
仓库主体是位于 `client/AINovelStudio` 的 WPF 客户端，文档集中在 `docs/`。客户端遵循 MVVM 分层：`Models/` 负责配置与领域实体，`Services/` 提供 AI 调用、SQLite 持久化及配置迁移，`ViewModels/` 管理可观察状态，`Views/`、`Resources/` 与 `Styles/` 构建界面。调试产物保留在 `bin/`、`obj/`，提交前应清理。新增资源请放入现有分类，例如通用命令置于 `Commands/`，转换器置于 `Converters/`。

## 构建、测试与开发命令
- `cd client/AINovelStudio`
- `dotnet restore`：首次拉取依赖。
- `dotnet build`：编译 WinExe 目标并输出分析器警告。
- `dotnet run`：以 `appsettings.client.json` 启动桌面客户端。
- `dotnet publish -c Release -o publish`：生成可分发的精简包。

## 代码风格与命名约定
项目目标框架为 .NET 8，启用可空引用类型。统一使用四个空格缩进；公共类型与方法采用 PascalCase，字段与局部变量使用 camelCase，异步成员以 `Async` 结尾。命令、服务应通过依赖注入保持可测试性。已启用 `EnableNETAnalyzers` 与 `AnalysisLevel=latest`，所有警告需修复或在 PR 中说明理由。提交前运行 `dotnet format` 对齐 C# 与 XAML 风格。

## 测试指引
当前尚未建立自动化测试。新增功能时建议在 `client/AINovelStudio.Tests` 创建 xUnit 工程（`dotnet new xunit`），纳入解决方案后执行 `dotnet test`。测试类命名遵循 `目标类型名Tests`，方法采用 `方法_场景_结果` 格式。在没有自动化覆盖前，请在 PR 描述中记录手动验证流程，如设置重置、AI 生成流程、SQLite 数据刷新等。

## 提交与拉取请求规范
遵循现有 Conventional Commits 风格，例如 `feat(持久化): 描述`。合并前应压缩临时修复提交。PR 需关联任务或 Issue，概述用户可见改动，界面改动附带截图，并列出执行的关键命令（如 `dotnet build`、`dotnet run`）。若有分析器抑制、数据库迁移或配置结构调整，请在说明中突出。

## 配置与安全注意事项
默认配置保存在 `appsettings.client.json`，启动后迁移至 `AINovelStudio.settings.db`，切勿提交本地生成的 `.db` 文件。敏感密钥请使用本地 JSON 或环境变量覆盖，严禁在代码或 XAML 中硬编码。扩展新的模型服务时，遵循现有 `Provider` 配置结构以便复用 UI 与验证逻辑。
