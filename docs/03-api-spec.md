# API 说明（当前不提供自建后端）

本项目仅包含 Windows 客户端（WPF）。不再提供或维护自建后端 API；所有生成能力通过第三方 AI Provider 的官方接口直接调用完成（如 OpenAI/Azure/OpenRouter/自建网关等）。

## 客户端调用指引
- 调用方式：使用 `HttpClient` 直接请求第三方 API，解析流式（`text/event-stream`）或分块响应。
- 配置管理：供应商配置（`vendor`、`apiKey`、`baseUrl`、`defaultModel`）在客户端设置页管理，并持久化到本地 SQLite（`AINovelStudio.settings.db`）。
- 取消/重试：客户端负责取消请求与重试策略；不依赖后端推送。

## 迁移与兼容
- 历史文档 `docs/03-api-spec.md` 原后端接口说明已废弃，仅保留此提示，后续以客户端直连第三方 API 为准。
- 若需要自建后端，请在独立仓库维护；客户端可继续通过设置中的 `baseUrl` 指向你的后端网关。