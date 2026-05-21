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
| 6 | [architecture.md](./architecture.md) | Clean Architecture layers and code conventions |
| 7 | [minimal-api.md](./minimal-api.md) | Endpoint module pattern in `Wokki.Api` |

## Product context

**Wokki Shift Ops MVP** is a single-tenant workforce backend: scheduling, shift swaps, attendance, payroll summaries, internal chat, and heuristic schedule suggestions. One company per deployment; no in-app multi-tenant onboarding.

## Document control

| Field | Value |
|-------|--------|
| Product | Wokki Shift Ops MVP |
| Backend repo | `wokki-server` |
| BRD version | 1.0 |
| Last updated | 2026-05-20 |
| Implementation status | Phases 1–5 complete (see `plans/shift-ops-mvp/plan.md`) |
| Source of truth for code | This `docs/` set + `Wokki.Domain` enums + `AppMessages` |

## Related repo paths

- Implementation plan: `plans/shift-ops-mvp/`
- Agent coding rules: `AGENTS.md`, `.cursor/rules/wokki-backend.mdc`
- OpenAPI / Scalar: `/scalar` when `ApiDocs:Enabled` or Development
