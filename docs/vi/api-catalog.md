# Danh mục API

URL gốc: `/api/v1` (trừ `/health`). Xác thực: **JWT Bearer**, trừ login/register và health.

Rate limit: **`Fixed`** (100/phút) mặc định; **`Clock`** (300/phút) cho clock-in/clock-out.

## Auth (`/api/v1/auth`)

| Method | Path | Vai trò | Mô tả |
|--------|------|---------|-------|
| POST | `/login` | Anonymous | Đăng nhập, cấp token |
| POST | `/register` | Anonymous | Tự đăng ký (luôn role `User`; Admin tạo user → `POST /users`) |
| POST | `/refresh-token` | Anonymous | Làm mới JWT |
| GET | `/me` | Đã đăng nhập | User hiện tại |
| POST | `/logout` | Đã đăng nhập | Đăng xuất |
| PUT | `/change-password` | Đã đăng nhập | Đổi mật khẩu |
| POST | `/forgot-password` | Anonymous | Quên mật khẩu |
| POST | `/reset-password` | Anonymous | Đặt lại mật khẩu |

## Users (`/api/v1/users`)

| Method | Path | Vai trò | Mô tả |
|--------|------|---------|-------|
| GET | `/` | Admin | Danh sách user (phân trang) |
| GET | `/{id}` | Admin | Chi tiết user |
| POST | `/` | Admin | Tạo user |

## Nền tảng (Foundation)

| Resource | Base | Manager | Admin | Ghi chú |
|----------|------|---------|-------|---------|
| Employees | `/employees` | Đọc/ghi danh sách | Đầy đủ | Xóa = chấm dứt (soft) |
| Locations | `/locations` | Đọc/ghi | Đầy đủ | `GET/PUT /locations/{id}/scheduling-policy` quản lý luật chi nhánh bằng danh sách typed rule có version (`rules[]`: nội dung luật bên trái + giá trị cần điền bên phải). |
| Departments | `/departments` | Đọc/ghi | Đầy đủ | |

## Lập lịch (Scheduling)

| Method | Path | Vai trò | Mô tả |
|--------|------|---------|-------|
| GET/POST | `/schedules` | Admin, Manager | Danh sách / tạo (Draft) |
| GET/PUT/DELETE | `/schedules/{id}` | Admin, Manager | Chi tiết / sửa / xóa Draft |
| POST | `/schedules/{id}/publish` | Admin, Manager | Draft → Published |
| POST | `/schedules/{id}/unpublish` | Admin, Manager | Published → Draft |
| POST | `/schedules/{id}/copy` | Admin, Manager | Copy tuần sang Draft mới |
| GET/POST | `/schedules/{id}/assignments` | Admin, Manager | Danh sách / thêm phân ca |
| DELETE | `/schedules/{id}/assignments/{assignmentId}` | Admin, Manager | Xóa phân ca |
| POST | `/schedules/{id}/suggest` | Admin, Manager | Gợi ý phân ca (không ghi DB; không dùng Bedrock) |
| POST | `/schedules/{id}/apply-suggestions` | Admin, Manager | Áp dụng gợi ý (chỉ Draft) |
| POST | `/schedules/{id}/insights/context` | Admin, Manager | Tạo/refresh snapshot JSON cho insight lịch |
| GET | `/schedules/{id}/insights/context` | Admin, Manager | Đọc context snapshot mới nhất |
| POST | `/schedules/{id}/insights/chat` | Admin, Manager | Hỏi trợ lý Bedrock tùy chọn dựa trên context; không mutate lịch |
| GET/POST/PUT/DELETE | `/shifts` | Admin, Manager | Mẫu ca (shift definition) |

## Self-service nhân viên (`/api/v1/self`) — User (cần Employee)

Khác `GET /api/v1/auth/me` (tài khoản đăng nhập). Các route này cần hồ sơ Employee liên kết.

| Method | Path | Mô tả |
|--------|------|-------|
| GET | `/self/schedule` | Lịch ca của mình (28 ngày, published) |
| GET | `/self/swap-requests` | Yêu cầu đổi ca gửi/nhận |
| GET | `/self/attendance` | Lịch sử chấm công |

## Đổi ca (`/api/v1/swap-requests`)

| Method | Path | Vai trò | Mô tả |
|--------|------|---------|-------|
| POST | `/` | User | Tạo yêu cầu |
| GET | `/` | Admin, Manager | Danh sách (lọc) |
| GET | `/{id}` | Đã đăng nhập | Chi tiết (theo quyền) |
| POST | `/{id}/accept` | User (đối tác) | Chấp nhận + auto đổi ca |
| POST | `/{id}/decline` | User (đối tác) | Từ chối |
| POST | `/{id}/cancel` | User (người gửi) | Hủy |
| POST | `/{id}/override-approve` | Admin, Manager | Manager duyệt |
| POST | `/{id}/override-reject` | Admin, Manager | Manager từ chối |

## Chấm công (`/api/v1/attendance`)

| Method | Path | Vai trò | Rate | Mô tả |
|--------|------|---------|------|-------|
| POST | `/clock-in` | User | Clock | Vào ca |
| POST | `/clock-out` | User | Clock | Ra ca + tính phút |
| GET | `/` | Admin, Manager | Fixed | Danh sách / lọc |
| PUT | `/{id}/adjust` | Admin, Manager | Fixed | Điều chỉnh + ghi chú audit |

## Lương (`/api/v1/payroll`)

| Method | Path | Vai trò | Mô tả |
|--------|------|---------|-------|
| GET | `/summary` | Admin, Manager | Tổng hợp theo department & kỳ |
| GET | `/summary/{employeeId}` | Admin, Manager | Chi tiết từng nhân viên |
| POST | `/summary/export` | Admin | Tải file CSV |

Tham số: `departmentId`, `startDate`, `endDate` (`PayrollPeriodRequest`).

## Chat (`/api/v1/channels`)

| Method | Path | Vai trò | Mô tả |
|--------|------|---------|-------|
| GET | `/` | Đã đăng nhập | Kênh của employee hiện tại |
| POST | `/` | Admin, Manager | Tạo Direct / Group |
| GET | `/{id}/messages` | Member (Admin đọc được mọi kênh) | Phân trang cursor: `?before=&limit=` |
| POST | `/{id}/messages` | Member | Gửi + đẩy SignalR |
| DELETE | `/{id}/messages/{msgId}` | Người gửi hoặc Admin | Xóa mềm |

## Real-time

| Transport | Path | Auth | Sự kiện |
|-----------|------|------|---------|
| SignalR | `/ws/chat?access_token={jwt}` | JWT query | Client: `JoinChannel`, `LeaveChannel` — Server: `ReceiveMessage` |

## Health

| Method | Path | Auth |
|--------|------|------|
| GET | `/health` | Anonymous |

## Envelope phản hồi

```json
{
  "success": true,
  "data": { },
  "message": { "code": "...", "text": "...", "statusCode": 200 },
  "errors": null
}
```

Mã thông báo: `Wokki.Common.Utils.AppMessages`.
