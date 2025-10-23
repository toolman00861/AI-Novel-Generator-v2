# 本地持久化与存储说明

本文档介绍生成内容的本地持久化实现方案（SQLite），包括数据表结构、服务设计、调用方式与后续扩展建议。

## 概览
- 目标：将 AI 生成的文章持久化到本地数据库（非文件导出）。
- 技术栈：`SQLite` + 现有 `IPersistenceService`/`SqlitePersistenceService`。
- 接入点：在 AI 生成页的“保存到章节”操作中，完成内存模型更新与数据库写入双路径。

## 数据表结构
- `Novels`
  - `Id INTEGER PRIMARY KEY AUTOINCREMENT`
  - `Title TEXT NOT NULL UNIQUE`
  - `Description TEXT`
  - `Author TEXT`
  - `Status TEXT`
  - `TagsJson TEXT`
  - `CreatedAt TEXT`
  - `UpdatedAt TEXT`
- `Chapters`
  - `Id INTEGER PRIMARY KEY AUTOINCREMENT`
  - `NovelId INTEGER NOT NULL`
  - `Title TEXT NOT NULL`
  - `Content TEXT`
  - `OrderIndex INTEGER`
  - `Status TEXT`
  - `Summary TEXT`
  - `CreatedAt TEXT`
  - `UpdatedAt TEXT`
  - 约束：`UNIQUE(NovelId, Title)`；`FOREIGN KEY(NovelId) REFERENCES Novels(Id) ON DELETE CASCADE`

## 服务设计
- 文件：`client/AINovelStudio/Services/NovelStorageService.cs`
- 依赖：`IPersistenceService`（接口）、`SqlitePersistenceService`（实现）
- 能力：
  - `EnsureNovel(title, source?)`：按标题查找或创建小说，返回 `NovelId`。
  - `EnsureChapter(novelId, chapterTitle, source?)`：按小说Id + 章节标题查找或创建章节，返回 `ChapterId`。
  - `SaveGeneratedContent(novelTitle, chapterTitle, content)`：将生成内容写入 `Chapters.Content`，并更新状态与时间戳。
- 初始化：在构造中调用 `EnsureSchema()` 创建/校验表结构。

## 调用与集成
- 文件：`client/AINovelStudio/ViewModels/AIGenerationViewModel.cs`
- 集成点：`SaveToChapter()`
  - 校验：需选定小说与章节且存在输出文本。
  - 内存更新：将 `OutputText` 写入 `SelectedChapter.Content`，保持现有 UI 状态一致。
  - 持久化：调用 `_storageService.SaveGeneratedContent(SelectedNovel.Title, SelectedChapter.Title, OutputText)` 写入数据库。
  - 反馈：成功弹窗提示；异常捕获后弹窗错误信息。

## 使用流程（用户视角）
1. 在 AI 生成页选择小说与章节，生成文本。
2. 点击“保存到章节”。
3. 章节内容更新，同时写入本地 SQLite；重新打开应用仍可读到保存内容（在后续加载逻辑接入后）。

## 后续扩展建议
- 加载持久化数据：
  - 将 `NovelManagementViewModel` 改为从数据库加载小说与章节列表，替换示例数据。
- 元数据：
  - 在 `Chapters` 增加生成来源（提供者、模型名）、提示词概要、生成时间等字段。
- 自动保存与版本：
  - 增加自动保存开关与节流；为章节内容引入版本记录或快照机制。
- 统一状态枚举：
  - 目前 `Status` 为字符串，后续可引入枚举映射或字典表，保证一致性与扩展性。

## 注意事项
- 唯一约束：同一小说下章节标题唯一；若需重名，建议改为以 `OrderIndex` 或 GUID 作为主键关联。
- 时间戳：使用 ISO8601（`DateTime.Now.ToString("o")`）便于跨时区与排序。
- 编译警告：当前存在与可空引用相关的警告，不影响持久化功能运行；建议在后续迭代中治理。