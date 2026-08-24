# Endpoint inventory used by the runner

The runner was derived from controller attributes, DTO validators, BLL authorization checks, and `Ardalis.Result` mappings.

| Area | HTTP surface | Authentication / key behavior |
| --- | --- | --- |
| Auth | `POST /api/auth/register`, `/login`, `/refresh`, `/revoke`, `/logout` | Register/login/refresh anonymous; revoke/logout require the token owner. |
| Users | `GET /api/users/{id}/profile`, `/me`; `PUT /profile`; `POST /avatar` | Public profile; current/update/avatar protected. |
| Tags | `GET /api/tags` | Public. |
| Groups | `GET/POST /api/groups`, `GET/PUT/DELETE /api/groups/{id}` | Browse/details public; create authenticated; update owner/admin; delete owner. |
| Membership | `GET /members`, `POST /join`, `POST /leave`, `PUT /members/{userId}/role`, `DELETE /members/{userId}` | Membership required to list; owner/admin role controls management. |
| Posts | group feed/create; get/update/delete/pin/unpin/like/unlike | Membership required for feed/create/reactions; author or admin/owner moderates; admin/owner pins. |
| Comments | list/create; update/delete/like/unlike | Membership required to list/create/reactions; author or admin/owner moderates. |
| Attachments | `POST /api/attachments`, `DELETE /api/attachments/{id}` | Authenticated; uploader-only delete. |
| Notifications | list/read/read-all/delete | Authenticated and owner-scoped. |
| Reports | create/list/resolve | Authenticated; implementation currently allows every authenticated user to list/resolve. |

`400` is produced by request validators, while expected business outcomes map from `Ardalis.Result`: `401`, `403`, `404`, and `409`.

## Empty database constraint

Groups require a category. The API does not expose category creation or lookup, so no HTTP-only runner can create its own category in an entirely empty database. The runner records category-dependent workflows as skipped until a category GUID is supplied. This deliberately exposes the missing API setup surface instead of accessing EF Core or SQL directly.
