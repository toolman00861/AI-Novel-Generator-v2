# 数据模型（草案）

## 实体与关系
- `users`（可后置）
  - `id`, `email`, `passwordHash`, `role`, `createdAt`
- `novels`
  - `id`, `title`, `description`, `tags[]`, `status`, `coverUrl`, `createdAt`, `updatedAt`
- `chapters`
  - `id`, `novelId (FK novels.id)`, `title`, `order`, `status`, `summary`, `content`, `createdAt`, `updatedAt`
- `characters`
  - `id`, `novelId (FK novels.id)`, `name`, `alias`, `age`, `role`, `traits[]`, `voice`, `background`, `createdAt`, `updatedAt`
- `relationships`（人物关系）
  - `id`, `novelId`, `fromCharacterId`, `toCharacterId`, `type`, `description`
- `context_items`（世界观/设定/手动摘要等）
  - `id`, `novelId`, `type (world|note|rule|summary)`, `title`, `content`, `weight`, `createdAt`, `updatedAt`
- `prompt_templates`
  - `id`, `name`, `description`, `system`, `user`, `injectionStrategy`, `createdAt`, `updatedAt`
- `generation_runs`
  - `id`, `novelId`, `chapterId`, `mode`, `model`, `params(json)`, `promptTokens`, `completionTokens`, `result`, `status`, `createdAt`
- `provider_config`
  - `id`, `vendor`, `apiKey`, `baseUrl`, `defaultModel`, `updatedAt`

## 关系示意
- `novels 1 - n chapters`
- `novels 1 - n characters`
- `characters n - n characters`（通过 `relationships`）
- `novels 1 - n context_items`
- `chapters 1 - n generation_runs`

## 索引建议
- `chapters`: (`novelId`, `order`)、`status`
- `characters`: (`novelId`, `name`)、`role`
- `context_items`: (`novelId`, `type`)
- `generation_runs`: (`chapterId`, `createdAt`)

## 上下文聚合逻辑（简述）
1. 收集用户选择的来源：人物设定、世界观、章节摘要、前文内容
2. 根据权重构造候选片段列表，按优先级排序：人物>章节摘要>世界观>前文
3. 逐个片段累加，超过 `maxTokens` 时：
   - 对低权重片段做摘要（调用简单摘要模板）
   - 或截断末尾低价值部分
4. 输出 `context` 字符串与 `sources`（包含每个片段的来源与裁剪信息）

## 字段约束与校验
- 文本字段最大长度根据数据库与模型限制设置（例如 `content` 采用长文本类型）
- `order` 为章节排序，整型、>=1；
- `status` 遵循枚举：`draft|published|archived`；
- `traits[]` 可使用字符串数组或 JSONB；
- 关系外键删除策略：MVP 采用 `RESTRICT` 或 `CASCADE`（需谨慎）。