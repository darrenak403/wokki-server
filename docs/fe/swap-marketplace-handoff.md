# FE Handoff: Swap Marketplace

**Backend:** `/api/v1/swap-posts` (replaces removed `/api/v1/swap-requests`)  
**Route (User):** `/{orgId}/{locationId}/user/swap`  
**Route (Admin/Manager audit):** `/{orgId}/{locationId}/admin/swap` or `.../manager/swap`

## Preconditions

- Schedule for the week must be **`Draft`** — feed disabled + banner when Published.
- User must belong to the schedule's **department** and have active **location membership**.

## Implemented FE (wokki-client)

| Area | Component / hook |
|------|------------------|
| User marketplace | `SwapMarketplacePanel`, `SwapFeedCard`, `CreateSwapPostDialog`, `AcceptCoverDialog`, `AcceptCrossSwapDialog` |
| Admin/Manager audit | `SwapAdminPanel` |
| API | `fetchSwapPosts`, `fetchSelf.getDraftWeekAssignments` |
| React Query | `useSwapPosts` (`useSwapPostFeedQuery`, `useMySwapPostsQuery`, mutations, audit) |

## Screens

### 1. Feed (`GET /api/v1/swap-posts/feed?scheduleId=`)

- List cards: author, type (Nhường ca / Đổi chéo), shift date/name/time, note, `canAccept`.
- **Cover:** button "Nhận ca" → modal xác nhận (preview chỗ trống) → `POST /{id}/accept` (empty body, **không** `acceptorAssignmentId`).
- **CrossSwap:** button "Đổi với tôi" → modal chọn **ca của mình** + preview → `POST /{id}/accept` with `{ acceptorAssignmentId }`.
- Hide Accept on own posts (`isMine` / `canAccept`).

### 2. Create post (`POST /api/v1/swap-posts`)

- Modal: type picker + pick **my assignment** from `GET /self/schedule/draft/{weekStartDate}/assignments`.
- Body: `{ authorAssignmentId, type: 0|1, note? }` (`0` Cover, `1` CrossSwap).

### 3. My posts (`GET /api/v1/self/swap-posts/mine`)

- Filter by `scheduleId`; **Cancel** when `canCancel` → `POST /{id}/cancel`.

### 4. Admin/Manager moderation

- **Bảng tin (read-only):** `GET /api/v1/swap-posts/admin/feed?locationId=&weekStartDate=&departmentId=` — Pending posts on Draft schedules; filter by branch + optional department (`Tất cả` = omit `departmentId`).
- **Nhật ký:** `GET /api/v1/swap-posts/audit` — same filters + optional `departmentId`.
- No create/accept actions.

| Area | Component |
|------|-----------|
| Admin/Manager | `SwapAdminPanel` (tabs Bảng tin / Nhật ký) |

- Week navigator defaults to **next Monday** (same as đăng ký ca — Draft is usually the upcoming week).

## Empty / locked states

| State | UX |
|-------|-----|
| Draft, no posts | CTA "Đăng ca" |
| Published | Banner "Lịch đã công bố — không thể đổi ca"; disable feed |
| Accept taken | Toast `SWAP_POST_ALREADY_TAKEN`; refresh feed |
| Policy fail | Show server message from preview/accept |

## Email

- Accept success only → `swap_post.completed` to author + accepter (no broadcast on new post).

## Removed (legacy)

- `/api/v1/swap-requests`, `/api/v1/self/swap-requests`, `/api/v1/self/swap-targets`
- FE: `SwapPanel`, `SwapRequestList`, `SwapInboxPanel`, `useSwapRequests`, `fetchSwapRequests`
