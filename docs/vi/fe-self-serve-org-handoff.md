# Bàn giao FE: Self-serve Organization

**Bản đầy đủ:** [docs/fe/self-serve-org-handoff.md](../fe/self-serve-org-handoff.md)  
**FE code map (wokki-client):** [wokki-client/docs/fe-implementation-guide.md](../../../wokki-client/docs/fe-implementation-guide.md)

## Thay đổi lớn nhất

1. **Register** tạo org + Org Admin (`organizationName` bắt buộc).
2. Org mới **chưa có gói sử dụng**; Wokki admin bật/gia hạn trong `/platform`.
3. **`admin@gmail.com`** = PlatformOperator → chỉ `/platform`.
4. **Org mới trống** — sau khi gói active, onboarding Admin tạo chi nhánh → phòng ban → nhân viên.
5. **Nhân viên** do Admin tạo (`POST /employees`) → login vào `/app` **trực tiếp**.
6. **Bỏ hẳn** `/join`, `/pending`, duyệt yêu cầu tham gia chi nhánh.

## Routing nhanh

| JWT | Đi tới |
|-----|--------|
| `PlatformOperator` | `/platform` |
| `Admin` | `/app` (+ onboarding nếu chưa có chi nhánh) |
| `Manager` | `/app` |
| `User` | `/app` (ca, đổi ca, chấm công) |

Nếu login/refresh/API trả:

| Code | Copy FE |
|------|---------|
| `ORG_PACKAGE_NOT_ACTIVATED` | "Bạn chưa có gói sử dụng hệ thống." |
| `ORG_PACKAGE_EXPIRED` | "Bạn phải gia hạn để tiếp tục dùng hệ thống." |

Màn vận hành theo chi nhánh dùng `/{orgId}/{locationId}/{role}/...`. Sidebar và workspace thao tác theo chi nhánh đang chọn; `/{orgId}/{role}/workspace` chỉ redirect/chọn chi nhánh, không render toàn bộ chi nhánh của org.

## Tạo nhân viên

Admin chọn **phòng ban** khi tạo → BE tự gán Active membership chi nhánh. Gửi `temporaryPassword` cho nhân viên login.

## Platform gói org

- `GET /api/v1/platform/users`
- `GET /api/v1/platform/organizations`
- `PUT /api/v1/platform/organizations/{id}/subscription` — Wokki admin **chọn số ngày** trên UI `/platform` (`durationDays`); FE **không** hardcode mặc định 30 ngày.

Ví dụ khi bật/gia hạn (số ngày do admin nhập):

```json
{ "enabled": true, "durationDays": 90 }
```

`durationDays`: `1..3650`, **bắt buộc khi `enabled: true`**. Ví dụ admin set **50 ngày** → sau 50 ngày mọi account trong org (mọi chi nhánh) không login/API được → FE hiển thị `/org-package?reason=expired` với copy gia hạn org.

## API đã bỏ

- `POST /location-memberships/request`
- `GET /location-memberships/pending`
- `PATCH /location-memberships/{id}/review`
