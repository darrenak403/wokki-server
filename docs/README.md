# Wokki Server — Documentation Index

Read these **before** implementing or changing behavior. Technical how-to stays in linked files; business intent lives in the BRD set.

**Tiếng Việt:** [vi/README.md](./vi/README.md) — BRD, quy tắc nghiệp vụ, luồng xử lý, danh mục API.

## For AI agents (recommended order)

| Order | Document | Purpose |
|-------|----------|---------|
| 1 | [brd.md](./brd.md) | Business Requirements Document — objectives, stakeholders, scope, FR/NFR |
| 2 | [business-rules.md](./business-rules.md) | Locked rules with IDs (`BR-xxx`) — must not violate in code |
| 3 | [process-flows.md](./process-flows.md) | End-to-end flows and state machines |
| 4 | [api-catalog.md](./api-catalog.md) | REST + WebSocket surface by role |
| 5 | [glossary.md](./glossary.md) | Domain terms and enums |
| 6 | [fe/self-serve-org-handoff.md](./fe/self-serve-org-handoff.md) | **FE handoff (2026-05-29):** self-serve org, register, platform vs org app, package gate |
| 7 | [fe/2026-05-29-feat-platform-org-subscription.md](./fe/2026-05-29-feat-platform-org-subscription.md) | Concise FE contract for platform user/org list and org package activation |
| 8 | [architecture.md](./architecture.md) | Clean Architecture layers and code conventions |
| 9 | [minimal-api.md](./minimal-api.md) | Endpoint module pattern in `Wokki.Api` |

## Product context

**Wokki Shift Ops** — workforce backend với **logical multi-tenant (Organization)**: self-serve register, org-scoped data, platform operator riêng. Scheduling, swap, attendance, payroll, chat.

## Document control

| Field | Value |
|-------|--------|
| Product | Wokki Shift Ops MVP |
| Backend repo | `wokki-server` |
| BRD version | 1.0 |
| Last updated | 2026-05-29 |
| Implementation status | Phases 1–5 complete (see `plans/shift-ops-mvp/plan.md`) |
| Source of truth for code | This `docs/` set + `Wokki.Domain` enums + `AppMessages` |

## Related repo paths

- Implementation plan: `plans/shift-ops-mvp/`
- Agent coding rules: `AGENTS.md`, `.cursor/rules/wokki-backend.mdc`
- OpenAPI / Scalar: `/scalar` when `ApiDocs:Enabled` or Development
