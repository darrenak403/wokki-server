# Wokki Server — Claude Code

@AGENTS.md

Branch workspace rule lives in AGENTS.md: selected branch URL scopes workspace/sidebar actions; Admin creates staff only through `/employees` (User + Employee together; same-org legacy orphan Users are linked there) with department → auto Active LocationMembership; no standalone `/users` staff and no self-serve join. Manager only `LocationManager`-assigned locations. Org package gate: register creates org pending package; PlatformOperator activates/renews via `/api/v1/platform/organizations/{id}/subscription`, otherwise org users get `ORG_PACKAGE_NOT_ACTIVATED` / `ORG_PACKAGE_EXPIRED`.
