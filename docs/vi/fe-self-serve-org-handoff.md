# Bàn giao FE: Self-serve Organization

**Bản đầy đủ:** [docs/fe/self-serve-org-handoff.md](../fe/self-serve-org-handoff.md)

## Thay đổi lớn nhất

1. **Register** tạo org + Org Admin (`organizationName` bắt buộc).
2. **`admin@gmail.com`** = PlatformOperator → chỉ `/platform`.
3. **Org mới trống** — onboarding Admin tạo chi nhánh → phòng ban → nhân viên.
4. **Nhân viên** do Admin tạo (`POST /employees`) → login vào `/app` **trực tiếp**.
5. **Bỏ hẳn** `/join`, `/pending`, duyệt yêu cầu tham gia chi nhánh.

## Routing nhanh

| JWT | Đi tới |
|-----|--------|
| `PlatformOperator` | `/platform` |
| `Admin` | `/app` (+ onboarding nếu chưa có chi nhánh) |
| `Manager` | `/app` |
| `User` | `/app` (ca, đổi ca, chấm công) |

Màn vận hành theo chi nhánh dùng `/{orgId}/{locationId}/{role}/...`. Sidebar và workspace thao tác theo chi nhánh đang chọn; `/{orgId}/{role}/workspace` chỉ redirect/chọn chi nhánh, không render toàn bộ chi nhánh của org.

## Tạo nhân viên

Admin chọn **phòng ban** khi tạo → BE tự gán Active membership chi nhánh. Gửi `temporaryPassword` cho nhân viên login.

## API đã bỏ

- `POST /location-memberships/request`
- `GET /location-memberships/pending`
- `PATCH /location-memberships/{id}/review`
