# Danh mục API

URL gốc: `/api/v1` (trừ `/health`). Xác thực: **JWT Bearer**, trừ login/register và health.

Rate limit: **`Fixed`** (100/phút) mặc định; **`Clock`** (300/phút) cho clock-in/clock-out.

## Auth (`/api/v1/auth`)

| Method | Path               | Vai trò      | Mô tả                                                                                                        |
| ------ | ------------------ | ------------ | ------------------------------------------------------------------------------------------------------------ |
| POST   | `/login`           | Anonymous    | Đăng nhập, cấp token                                                                                         |
| POST   | `/register`        | Anonymous    | Tự đăng ký org: `email`, `password`, `organizationName` → Org Admin + JWT; org mới ở trạng thái NotActivated |
| POST   | `/refresh-token`   | Đã đăng nhập | Làm mới JWT                                                                                                  |
| GET    | `/me`              | Đã đăng nhập | User hiện tại                                                                                                |
| POST   | `/logout`          | Đã đăng nhập | Đăng xuất                                                                                                    |
| PUT    | `/change-password` | Đã đăng nhập | Đổi mật khẩu                                                                                                 |
| POST   | `/forgot-password` | Anonymous    | Quên mật khẩu                                                                                                |
| POST   | `/reset-password`  | Anonymous    | Đặt lại mật khẩu                                                                                             |

## Users (`/api/v1/users`)

| Method | Path    | Vai trò | Mô tả                       |
| ------ | ------- | ------- | --------------------------- |
| GET    | `/`     | Admin   | Danh sách user (phân trang) |
| GET    | `/{id}` | Admin   | Chi tiết user               |
| POST   | `/`     | Admin   | Deprecated/bị chặn cho staff org; dùng `POST /employees` để tạo tài khoản + Employee cùng lúc |

## Nền tảng (Foundation)

| Resource    | Base           | Manager                                              | Admin  | Ghi chú                                                                                                                       |
| ----------- | -------------- | ---------------------------------------------------- | ------ | ----------------------------------------------------------------------------------------------------------------------------- |
| Employees   | `/employees`   | Đọc danh sách theo scope                             | Đầy đủ | Tạo User + Employee cùng lúc; User legacy cùng org chưa có Employee sẽ được liên kết. Manager thấy nhân viên có Active membership trong chi nhánh được gán. Xóa = chấm dứt (soft) |
| Locations   | `/locations`   | Đọc theo scope; `PUT /{id}` trong chi nhánh được gán | Đầy đủ | Manager chỉ thấy chi nhánh được gán; có thể cập nhật metadata chi nhánh trong scope (drawer Tổ chức). Ghi policy: Admin only. |
| Departments | `/departments` | Đọc theo scope; `PUT /{id}` trong chi nhánh được gán | Đầy đủ | Manager chỉ thấy phòng ban thuộc chi nhánh được gán; có thể cập nhật phòng ban trong scope                                    |

## Membership chi nhánh (`/api/v1/location-memberships`)

| Method | Path                          | Vai trò                   | Mô tả                                                             |
| ------ | ----------------------------- | ------------------------- | ----------------------------------------------------------------- |
| GET    | `/my`                         | Đã đăng nhập (+ Employee) | Membership **Active** hiện tại (tự tạo khi Admin tạo nhân viên)   |
| GET    | `/locations/{id}/memberships` | Admin, Manager            | Danh sách membership chi nhánh; Manager phải quản lý chi nhánh đó |

**Đã bỏ (2026-05-29):** luồng tự gửi yêu cầu tham gia — `POST /request`, `GET /pending`, `PATCH /{id}/review`.

## Điều chuyển workspace (`/api/v1/workspace`)

| Method | Path                   | Vai trò        | Mô tả                                                                                                                                                                  |
| ------ | ---------------------- | -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| POST   | `/location/transfer`   | Admin, Manager | Chuyển nhân viên sang chi nhánh khác trong phạm vi được quản lý.                                                                                                       |
| POST   | `/department/transfer` | Admin, Manager | Chuyển nhân viên sang phòng ban thuộc chi nhánh Active hiện tại của nhân viên. Nếu phòng ban khác chi nhánh, trả `WS_EMPLOYEE_WRONG_LOCATION`; chuyển chi nhánh trước. |

## Lập lịch (Scheduling)

| Method              | Path                                         | Vai trò        | Mô tả                                                           |
| ------------------- | -------------------------------------------- | -------------- | --------------------------------------------------------------- |
| GET/POST            | `/schedules`                                 | Admin, Manager | Danh sách / tạo (Draft)                                         |
| GET/PUT/DELETE      | `/schedules/{id}`                            | Admin, Manager | Chi tiết / sửa / xóa Draft                                      |
| POST                | `/schedules/{id}/publish`                    | Admin, Manager | Draft → Published                                               |
| POST                | `/schedules/{id}/unpublish`                  | Admin, Manager | Published → Draft                                               |
| POST                | `/schedules/{id}/copy`                       | Admin, Manager | Copy tuần sang Draft mới                                        |
| GET/POST            | `/schedules/{id}/assignments`                | Admin, Manager | Danh sách / thêm phân ca                                        |
| DELETE              | `/schedules/{id}/assignments/{assignmentId}` | Admin, Manager | Xóa phân ca                                                     |
| POST                | `/schedules/{id}/suggest`                    | Admin, Manager | Gợi ý phân ca (không ghi DB; không dùng Bedrock)                |
| POST                | `/schedules/{id}/apply-suggestions`          | Admin, Manager | Áp dụng gợi ý (chỉ Draft)                                       |
| POST                | `/schedules/{id}/insights/context`           | Admin, Manager | Tạo/refresh snapshot JSON cho insight lịch                      |
| GET                 | `/schedules/{id}/insights/context`           | Admin, Manager | Đọc context snapshot mới nhất                                   |
| POST                | `/schedules/{id}/insights/chat`              | Admin, Manager | Hỏi trợ lý Bedrock tùy chọn dựa trên context; không mutate lịch |
| GET/POST/PUT/DELETE | `/shifts`                                    | Admin, Manager | Mẫu ca (shift definition)                                       |

## Self-service nhân viên (`/api/v1/self`) — User (cần Employee)

Khác `GET /api/v1/auth/me` (tài khoản đăng nhập). Các route này cần hồ sơ Employee liên kết.

| Method | Path                  | Mô tả                                 |
| ------ | --------------------- | ------------------------------------- |
| GET    | `/self/schedule`      | Lịch ca của mình (28 ngày, published) |
| GET    | `/self/swap-requests` | Yêu cầu đổi ca gửi/nhận               |
| GET    | `/self/attendance`    | Lịch sử chấm công                     |

## Đổi ca (`/api/v1/swap-requests`)

| Method | Path                     | Vai trò          | Mô tả                   |
| ------ | ------------------------ | ---------------- | ----------------------- |
| POST   | `/`                      | User             | Tạo yêu cầu             |
| GET    | `/`                      | Admin, Manager   | Danh sách (lọc)         |
| GET    | `/{id}`                  | Đã đăng nhập     | Chi tiết (theo quyền)   |
| POST   | `/{id}/accept`           | User (đối tác)   | Chấp nhận + auto đổi ca |
| POST   | `/{id}/decline`          | User (đối tác)   | Từ chối                 |
| POST   | `/{id}/cancel`           | User (người gửi) | Hủy                     |
| POST   | `/{id}/override-approve` | Admin, Manager   | Manager duyệt           |
| POST   | `/{id}/override-reject`  | Admin, Manager   | Manager từ chối         |

## Chấm công (`/api/v1/attendance`)

| Method | Path           | Vai trò        | Rate  | Mô tả                      |
| ------ | -------------- | -------------- | ----- | -------------------------- |
| POST   | `/clock-in`    | User           | Clock | Vào ca                     |
| POST   | `/clock-out`   | User           | Clock | Ra ca + tính phút          |
| GET    | `/`            | Admin, Manager | Fixed | Danh sách / lọc            |
| PUT    | `/{id}/adjust` | Admin, Manager | Fixed | Điều chỉnh + ghi chú audit |

## Lương (`/api/v1/payroll`)

| Method | Path                    | Vai trò        | Mô tả                         |
| ------ | ----------------------- | -------------- | ----------------------------- |
| GET    | `/summary`              | Admin, Manager | Tổng hợp theo department & kỳ |
| GET    | `/summary/{employeeId}` | Admin, Manager | Chi tiết từng nhân viên       |
| POST   | `/summary/export`       | Admin          | Tải file CSV                  |

Tham số: `departmentId`, `startDate`, `endDate` (`PayrollPeriodRequest`).

## Chat (`/api/v1/channels`)

| Method | Path                     | Vai trò                          | Mô tả                                |
| ------ | ------------------------ | -------------------------------- | ------------------------------------ |
| GET    | `/`                      | Đã đăng nhập                     | Kênh của employee hiện tại           |
| POST   | `/`                      | Admin, Manager                   | Tạo Direct / Group                   |
| GET    | `/{id}/messages`         | Member (Admin đọc được mọi kênh) | Phân trang cursor: `?before=&limit=` |
| POST   | `/{id}/messages`         | Member                           | Gửi + đẩy SignalR                    |
| DELETE | `/{id}/messages/{msgId}` | Người gửi hoặc Admin             | Xóa mềm                              |

## Stats

| Method | Path              | Vai trò          | Mô tả                                                 |
| ------ | ----------------- | ---------------- | ----------------------------------------------------- |
| GET    | `/platform/stats` | PlatformOperator | Tổng số org, user, chi nhánh, nhân viên toàn platform |
| GET    | `/org/stats`      | Admin, Manager   | Số liệu vận hành trong org hiện tại                   |

## Platform admin (`/api/v1/platform`) — PlatformOperator

| Method | Path                                                   | Mô tả                                                   |
| ------ | ------------------------------------------------------ | ------------------------------------------------------- |
| GET    | `/users?page=&pageSize=&organizationId=&role=&search=` | Danh sách user toàn hệ thống, có org name nếu thuộc org |
| GET    | `/organizations?page=&pageSize=&search=`               | Danh sách org kèm trạng thái gói và số liệu             |
| PUT    | `/organizations/{id}/subscription`                     | Bật/tắt hoặc gia hạn gói org                            |

Body `PUT /platform/organizations/{id}/subscription`:

```json
{
  "enabled": true,
  "durationDays": 90
}
```

`durationDays` optional trên API (`1..3650`). FE platform gửi số ngày admin chọn (không hardcode 30). Bỏ trống → BE dùng `subscriptionDurationDays` đã lưu của org. Khi bật: `subscriptionExpiresAt = now + durationDays`. Khi tắt, org không dùng được nhưng không xóa dữ liệu.

Mã gate gói cho user org:

| HTTP | Code                        | Ý nghĩa                                    |
| ---- | --------------------------- | ------------------------------------------ |
| 403  | `ORG_PACKAGE_NOT_ACTIVATED` | Org chưa từng kích hoạt gói hoặc đã bị tắt |
| 402  | `ORG_PACKAGE_EXPIRED`       | Gói org hết hạn; cần Wokki admin gia hạn   |

## Real-time

| Transport | Path                          | Auth      | Sự kiện                                                          |
| --------- | ----------------------------- | --------- | ---------------------------------------------------------------- |
| SignalR   | `/ws/chat?access_token={jwt}` | JWT query | Client: `JoinChannel`, `LeaveChannel` — Server: `ReceiveMessage` |

## Health

| Method | Path      | Auth      |
| ------ | --------- | --------- |
| GET    | `/health` | Anonymous |

## Envelope phản hồi

```json
{
  "success": true,
  "data": {},
  "message": {"code": "...", "text": "...", "statusCode": 200},
  "errors": null
}
```

Mã thông báo: `Wokki.Common.Utils.AppMessages`.
