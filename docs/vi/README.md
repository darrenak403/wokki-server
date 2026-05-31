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
| 6 | [../fe/self-serve-org-handoff.md](../fe/self-serve-org-handoff.md) | Bàn giao FE self-serve org, platform shell, package gate |
| 7 | [../fe/2026-05-29-feat-platform-org-subscription.md](../fe/2026-05-29-feat-platform-org-subscription.md) | Contract FE cho list user/org platform và bật/tắt gói |
| 8 | [../fe/2026-05-29-feat-branch-workspace-scope.md](../fe/2026-05-29-feat-branch-workspace-scope.md) | Bàn giao FE cho branch scope, membership, manager scope và React Flow |
| 9 | **[fe-integration-guide.md](./fe-integration-guide.md)** | Luồng FE cũ — chỉ tham khảo phần chưa bị thay bởi self-serve org |
| 10 | [../architecture.md](../architecture.md) | Kiến trúc Clean Architecture (EN) |
| 11 | [../minimal-api.md](../minimal-api.md) | Mẫu Minimal API (EN) |

**Bản tiếng Anh (đồng bộ với code):** [../README.md](../README.md)

## Bối cảnh sản phẩm

**Wokki Shift Ops MVP** là backend vận hành ca làm việc đa tổ chức (`Organization` là tenant root): self-serve register, dữ liệu scope theo org, Wokki admin quản lý platform và gói sử dụng org.

## Kiểm soát tài liệu

| Trường | Giá trị |
|--------|---------|
| Sản phẩm | Wokki Shift Ops MVP |
| Repository | `wokki-server` |
| Phiên bản BRD | 1.0 (vi) |
| Cập nhật | 2026-05-29 |
| Trạng thái triển khai | Phase 1–5 hoàn tất (`plans/shift-ops-mvp/plan.md`) |
| Nguồn sự thật khi code | `docs/vi/` + `docs/` (EN) + `Wokki.Domain` + `AppMessages` |

## Đường dẫn liên quan

- Kiểm tra luồng & smoke: `plans/fe-handoff-flow-verification/`
- Kế hoạch triển khai: `plans/shift-ops-mvp/`
- Quy tắc agent: `AGENTS.md`, `.cursor/rules/wokki-backend.mdc`
- Tài liệu API: `/scalar` khi bật `ApiDocs:Enabled` hoặc môi trường Development
