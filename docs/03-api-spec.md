# 后端 API 规格（草案）

基于 REST + SSE（后端 ASP.NET Core，可选），客户端为 WPF；后端域名示例 `https://api.example.com`。

## 通用说明
- 鉴权：MVP 可不做；后续通过 `Authorization: Bearer <token>`
- Content-Type：`application/json`
- 错误格式：`{ code: string, message: string, details?: any }`

## 客户端对接（WPF）
- REST：使用 `HttpClient` 统一请求封装与错误处理。
- 流式生成：解析 `text/event-stream` 或按 chunk 读取，按事件渲染进度。
- 取消生成：客户端断开连接或调用后端取消端点（如提供）。

## 资源定义
### Novel（小说）
- 字段：`id`、`title`、`description`、`tags[]`、`status`、`coverUrl`、`createdAt`、`updatedAt`
- 列表：`GET /v1/novels?query=&status=&page=&pageSize=`
- 创建：`POST /v1/novels`
- 详情：`GET /v1/novels/{id}`
- 更新：`PUT /v1/novels/{id}`
- 删除：`DELETE /v1/novels/{id}`

### Chapter（章节）
- 字段：`id`、`novelId`、`title`、`order`、`status`、`summary`、`content`、`createdAt`、`updatedAt`
- 列表：`GET /v1/novels/{novelId}/chapters`
- 创建：`POST /v1/novels/{novelId}/chapters`
- 详情：`GET /v1/chapters/{id}`
- 更新：`PUT /v1/chapters/{id}`
- 删除：`DELETE /v1/chapters/{id}`

### Character（人物）
- 字段：`id`、`novelId`、`name`、`alias`、`age`、`role`、`traits[]`、`voice`、`background`、`relations[]`
- 列表：`GET /v1/novels/{novelId}/characters`
- 创建：`POST /v1/novels/{novelId}/characters`
- 详情：`GET /v1/characters/{id}`
- 更新：`PUT /v1/characters/{id}`
- 删除：`DELETE /v1/characters/{id}`

### Context（上下文）
- 聚合：`POST /v1/context/aggregate`
  - 请求：
    ```json
    {
      "novelId": "...",
      "chapterId": "...",
      "include": {
        "characters": ["id1", "id2"],
        "world": true,
        "chapterSummary": true,
        "previousContent": { "maxTokens": 1200 }
      },
      "weights": { "characters": 1.0, "world": 0.7, "chapterSummary": 0.8, "previousContent": 0.6 }
    }
    ```
  - 响应：`{ context: string, tokens: number, sources: [...] }`

### Prompt Template（提示模板）
- 列表：`GET /v1/prompts`
- 创建：`POST /v1/prompts`
- 更新：`PUT /v1/prompts/{id}`
- 删除：`DELETE /v1/prompts/{id}`

### Generation（生成）
- 生成（SSE）：`POST /v1/generate`（返回 `text/event-stream`）
  - 请求（示例）：
    ```json
    {
      "mode": "continue|rewrite|outline|summarize",
      "novelId": "...",
      "chapterId": "...",
      "promptTemplateId": "...",
      "model": "gpt-4o-mini",
      "params": { "maxTokens": 1024, "temperature": 0.8 },
      "userInput": "续写这里：……",
      "contextOptions": { "characters": ["c1"], "chapterSummary": true, "previousContent": { "maxTokens": 800 } }
    }
    ```
  - 响应（SSE 事件流）：
    - `event: chunk` data: `{"text":"..."}`
    - `event: usage` data: `{ "promptTokens": 123, "completionTokens": 456 }`
    - `event: done` data: `{ "ok": true }`
  - 取消生成：客户端断开连接即可。

### Provider 配置
- 列表/更新：`GET/PUT /v1/provider-config`
- 字段：`vendor`（openai/azure/openrouter/custom）、`apiKey`、`baseUrl`、`defaultModel`

## 示例响应
- 成功：`200 { data: {...}, meta: {...} }`
- 错误：`4xx/5xx { code: "VALIDATION_ERROR", message: "...", details: {...} }`

## 约定与说明
- 所有可分页列表：`page` 从 1 开始，`pageSize` 默认 20；返回 `meta.total`、`meta.page`、`meta.pageSize`。
- 时间字段统一 ISO 字符串；服务器统一使用 UTC 存储。
- SSE 连接超时与心跳由后端维持；前端断线自动重连可后续实现。