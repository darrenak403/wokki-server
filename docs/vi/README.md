# Wokki Server — Mục lục tài liệu (Tiếng Việt)

Đọc các tài liệu này **trước** khi triển khai hoặc thay đổi hành vi nghiệp vụ. Phần kỹ thuật (code) vẫn tham chiếu bản tiếng Anh khi cần.

## Cho AI agents & lập trình viên

| Thứ tự | Tài liệu | Mục đích |
|--------|----------|----------|
| 1 | [brd.md](./brd.md) | Tài liệu yêu cầu nghiệp vụ (BRD) — mục tiêu, stakeholder, phạm vi, FR/NFR |
| 2 | [business-rules.md](./business-rules.md) | Quy tắc nghiệp vụ khóa (`BR-xxx`) — không được vi phạm trong code |
| 3 | [process-flows.md](./process-flows.md) | Luồng xử lý và máy trạng thái |
| 4 | [api-catalog.md](./api-catalog.md) | REST + WebSocket theo vai trò |
| 5 | [glossary.md](./glossary.md) | Thuật ngữ và enum |
| 6 | **[fe-integration-guide.md](./fe-integration-guide.md)** | **Bàn giao FE — 7 main flow, auth, SignalR** |
| 7 | [../fe/2026-05-29-feat-branch-workspace-scope.md](../fe/2026-05-29-feat-branch-workspace-scope.md) | Bàn giao FE cho branch scope, membership, manager scope và React Flow |
| 8 | [../architecture.md](../architecture.md) | Kiến trúc Clean Architecture (EN) |
| 9 | [../minimal-api.md](../minimal-api.md) | Mẫu Minimal API (EN) |

**Bản tiếng Anh (đồng bộ với code):** [../README.md](../README.md)

## Bối cảnh sản phẩm

**Wokki Shift Ops MVP** là backend vận hành ca làm việc cho **một doanh nghiệp / một instance**: lập lịch, đổi ca, chấm công, tổng hợp lương, chat nội bộ và gợi ý lịch heuristic. Không đa tenant trong cùng một database.

## Kiểm soát tài liệu

| Trường | Giá trị |
|--------|---------|
| Sản phẩm | Wokki Shift Ops MVP |
| Repository | `wokki-server` |
| Phiên bản BRD | 1.0 (vi) |
| Cập nhật | 2026-05-20 |
| Trạng thái triển khai | Phase 1–5 hoàn tất (`plans/shift-ops-mvp/plan.md`) |
| Nguồn sự thật khi code | `docs/vi/` + `docs/` (EN) + `Wokki.Domain` + `AppMessages` |

## Đường dẫn liên quan

- Kiểm tra luồng & smoke: `plans/fe-handoff-flow-verification/`
- Kế hoạch triển khai: `plans/shift-ops-mvp/`
- Quy tắc agent: `AGENTS.md`, `.cursor/rules/wokki-backend.mdc`
- Tài liệu API: `/scalar` khi bật `ApiDocs:Enabled` hoặc môi trường Development
