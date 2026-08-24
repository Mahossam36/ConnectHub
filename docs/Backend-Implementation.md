# ConnectHub Backend Implementation & Architecture Guide

This document is the authoritative reference for the ConnectHub backend architecture, database schema, design decisions, authentication system, moderation & security pipeline, real-time communications, caching, and API endpoints.

---

## Table of Contents
1. [Architecture Overview](#1-architecture-overview)
2. [Domain Models (ConnectHub.Models)](#2-domain-models-connecthubmodels)
3. [Data Access Layer (ConnectHub.DAL)](#3-data-access-layer-connecthubdal)
4. [Generic Repository & Specific Repositories](#4-generic-repository--specific-repositories)
5. [IQueryable & Deferred Execution Strategy](#5-iqueryable--deferred-execution-strategy)
6. [Unit of Work Pattern](#6-unit-of-work-pattern)
7. [Business Logic Layer (ConnectHub.BLL)](#7-business-logic-layer-connecthubbll)
8. [DTOs & AutoMapper](#8-dtos--automapper)
9. [Result Pattern (Ardalis.Result)](#9-result-pattern-ardalisresult)
10. [Structured Logging (ILogger)](#10-structured-logging-ilogger)
11. [Audit Logging System](#11-audit-logging-system)
12. [File Storage Abstraction](#12-file-storage-abstraction)
13. [Denormalized Counters & Synchronization](#13-denormalized-counters--synchronization)
14. [Authentication & Token System](#14-authentication--token-system)
15. [JWT Access Token Architecture](#15-jwt-access-token-architecture)
16. [Refresh Token Architecture & Hashing](#16-refresh-token-architecture--hashing)
17. [Token Rotation & Reuse Detection](#17-token-rotation--reuse-detection)
18. [Logout & Revocation Workflow](#18-logout--revocation-workflow)
19. [Preparation for Google SSO](#19-preparation-for-google-sso)
20. [Content Safety Moderation (OpenAI API)](#20-content-safety-moderation-openai-api)
21. [XSS Protection & Input Sanitization](#21-xss-protection--input-sanitization)
22. [Real-time Communication (SignalR Hubs)](#22-real-time-communication-signalr-hubs)
23. [In-Memory Caching (IMemoryCache)](#23-in-memory-caching-imemorycache)
24. [API Layer, Routing & Controllers](#24-api-layer-routing--controllers)
25. [Global Exception Handling](#25-global-exception-handling)
26. [Swagger / OpenAPI Testing Guide](#26-swagger--openapi-testing-guide)
27. [End-to-End Pipeline Overview](#27-end-to-end-pipeline-overview)

---

## 1. Architecture Overview

ConnectHub follows a clean **N-Tier Layered Architecture**:

```
┌────────────────────────────────────────────────────────┐
│                   ConnectHub.API                       │
│    (Controllers, SignalR Hubs, Swagger, Middleware)    │
└──────────────────────────┬─────────────────────────────┘
                           │ references
                           ▼
┌────────────────────────────────────────────────────────┐
│                   ConnectHub.BLL                       │
│ (Services, FluentValidation, Moderation, AutoMapper)   │
└──────────────────────────┬─────────────────────────────┘
                           │ references
                           ▼
┌────────────────────────────────────────────────────────┐
│                   ConnectHub.DAL                       │
│    (DbContext, Identity, EF Repositories, UnitOfWork)  │
└──────────────────────────┬─────────────────────────────┘
                           │ references
                           ▼
┌────────────────────────────────────────────────────────┐
│                 ConnectHub.Models                      │
│        (Core Entities, Enums, Relationships)           │
└────────────────────────────────────────────────────────┘
```

---

## 2. Domain Models (ConnectHub.Models)

All entities standardize on `Guid` primary keys (`uniqueidentifier`) and maintain explicit navigation properties for every foreign key.

| Entity | Primary Key | Key Foreign Keys | Purpose |
| :--- | :--- | :--- | :--- |
| **`User`** | `Guid Id` | — | Platform business profile (matches `ApplicationUser.Id`). |
| **`Category`** | `Guid Id` | — | Dynamic, application-managed taxonomy for community groups. |
| **`Tag`** | `Guid Id` | — | Discovery tags for groups (Many-to-Many). |
| **`Group`** | `Guid Id` | `CategoryId`, `CreatedById` | Interest-based community container. |
| **`GroupMember`** | `Guid Id` | `GroupId`, `UserId` | Explicit membership and role (Owner, Admin, Member). |
| **`Post`** | `Guid Id` | `AuthorId`, `GroupId` | Main feed entry in a group. |
| **`PostLike`** | `(PostId, UserId)` | `PostId`, `UserId` | Composite PK tracking 1 like per user on posts. |
| **`Comment`** | `Guid Id` | `AuthorId`, `PostId`, `ParentCommentId` | Threaded discussions with self-referencing reply hierarchy. |
| **`CommentLike`** | `(CommentId, UserId)` | `CommentId`, `UserId` | Composite PK tracking 1 like per user on comments. |
| **`Attachment`** | `Guid Id` | `UploadedById`, `PostId?` | Media/file metadata attached to posts. |
| **`Notification`** | `Guid Id` | `UserId` | In-app alerts for social interactions. |
| **`Report`** | `Guid Id` | `ReportedById` | Polymorphic moderation reports (`TargetType` + `TargetId`). |
| **`RefreshToken`** | `Guid Id` | `UserId` | Persisted SHA-256 hashed refresh tokens for auth rotation. |
| **`AuditLog`** | `Guid Id` | `UserId?` | Security and administrative audit trail. |

---

## 3. Data Access Layer (ConnectHub.DAL)

### `AppDbContext`
Inherits from `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` and configures 14 entity sets:
```csharp
public DbSet<User> DomainUsers => Set<User>();
public DbSet<Category> Categories => Set<Category>();
public DbSet<Tag> Tags => Set<Tag>();
public DbSet<Group> Groups => Set<Group>();
public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
public DbSet<Post> Posts => Set<Post>();
public DbSet<PostLike> PostLikes => Set<PostLike>();
public DbSet<Comment> Comments => Set<Comment>();
public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
public DbSet<Attachment> Attachments => Set<Attachment>();
public DbSet<Notification> Notifications => Set<Notification>();
public DbSet<Report> Reports => Set<Report>();
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
```

---

## 4. Generic Repository & Specific Repositories

### `IGenericRepository<T>`
Provides standard data-access primitives:
* `IQueryable<T> Query()` — Returns deferred `AsNoTracking()` query.
* `Task<T?> GetByIdAsync(Guid id)`
* `Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)`
* `Task AddAsync(T entity)` / `Task AddRangeAsync(IEnumerable<T> entities)`
* `void Update(T entity)` / `void UpdateRange(IEnumerable<T> entities)`
* `void Delete(T entity)` / `void DeleteRange(IEnumerable<T> entities)`

### Specific Repositories
* **`IRefreshTokenRepository` / `RefreshTokenRepository`:** Lookups by `TokenHash`, bulk user token revocation (`ExecuteUpdateAsync`).
* **`IGroupRepository` / `GroupRepository`:** `GetWithDetailsAsync`, `IsUserMemberAsync`, `GetUserRoleAsync`.
* **`IPostRepository` / `PostRepository`:** `GetWithDetailsAsync`, `HasUserLikedPostAsync`, `AddLikeAsync`, `RemoveLikeAsync`.
* **`ICommentRepository` / `CommentRepository`:** `GetPostComments`, `HasUserLikedCommentAsync`, `AddLikeAsync`, `RemoveLikeAsync`.
* **`INotificationRepository` / `NotificationRepository`:** `GetUnreadCountAsync`, `MarkAllAsReadAsync` (`ExecuteUpdateAsync`).
* **`IReportRepository` / `ReportRepository`:** `GetWithDetailsAsync`.

---

## 5. IQueryable & Deferred Execution Strategy

1. Queries remain database-side via `_repository.Query()`.
2. Pagination uses SQL Server `OFFSET ... FETCH NEXT` via `Skip(skip).Take(take)`.
3. Total counts use `CountAsync()`.
4. Immediate execution is deferred until `ToListAsync()` or `FirstOrDefaultAsync()`.

---

## 6. Unit of Work Pattern

`IUnitOfWork` coordinates transactional persistence:
```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}
```
Services inject individual repositories and `IUnitOfWork`, committing multi-entity state transitions in a single atomic database roundtrip.

---

## 7. Business Logic Layer (ConnectHub.BLL)

Services implemented:
1. **`AuthService`:** Local registration, login, JWT token issuance, refresh token rotation, revocation, and Google SSO convergence.
2. **`UserService`:** Profile viewing, updating, and avatar uploads.
3. **`GroupService`:** Group browsing, faceted discovery, group creation/updating/deletion, and memory caching.
4. **`GroupMemberService`:** Group join, leave, role modification, and member kick/removal with counter updates.
5. **`PostService`:** Feed retrieval, post authoring, editing, deletion, pinning, liking, unliking, XSS sanitization, moderation, and SignalR broadcasts.
6. **`CommentService`:** Threaded comments, adding replies, editing, deleting, liking, unliking, moderation, and notifications.
7. **`NotificationService`:** User notification feed, unread badge count, marking read, and real-time SignalR dispatch.
8. **`ReportService`:** Content moderation report submission, listing, and resolution.
9. **`AttachmentService`:** File upload and deletion coordination with `IFileStorageService`.
10. **`AuditService`:** Security and business audit trail recording.
11. **`ContentModerationService`:** OpenAI Moderation API evaluation.
12. **`XssSanitizerService`:** HTML stripping and plain-text normalization.
13. **`FileStorageService`:** Physical file storage implementation.

---

## 8. DTOs & AutoMapper

Centralized in `ConnectHubProfile`:
* `User` → `UserSummaryDto`, `UserProfileResponseDto`
* `Category` → `CategoryDto`
* `Tag` → `TagDto`
* `Group` → `GroupSummaryResponseDto`, `GroupDetailResponseDto`
* `GroupMember` → `GroupMemberResponseDto`
* `Post` → `PostResponseDto`
* `Comment` → `CommentResponseDto`
* `Attachment` → `AttachmentResponseDto`
* `Notification` → `NotificationResponseDto`
* `Report` → `ReportResponseDto`

---

## 9. Result Pattern (Ardalis.Result)

Expected business outcomes are modeled as `Result<T>`:
* `Result.Success(value)`
* `Result.NotFound(error)`
* `Result.Unauthorized(error)`
* `Result.Forbidden(error)`
* `Result.Conflict(error)`
* `Result.Invalid(validationErrors)`

---

## 10. Structured Logging (ILogger)

All services use structured logging:
```csharp
_logger.LogInformation("User {UserId} joined group {GroupId}.", currentUserId, groupId);
```
**Strict security rule:** Passwords, access tokens, refresh tokens, and secrets are strictly excluded from logs.

---

## 11. Audit Logging System

Captures security and administrative events in the `AuditLogs` table:
* `Register`, `Login`, `Logout`, `RefreshToken`
* `CreateGroup`, `UpdateGroup`, `DeleteGroup`
* `JoinGroup`, `LeaveGroup`, `ChangeMemberRole`, `RemoveMember`
* `CreatePost`, `UpdatePost`, `DeletePost`
* `AddComment`, `UpdateComment`, `DeleteComment`
* `SubmitReport`, `ResolveReport`

---

## 12. File Storage Abstraction

`IFileStorageService` manages physical files:
* Physical paths are resolved in `FileStorageService` (default: `wwwroot`).
* Database persists strictly relative paths (e.g. `uploads/attachments/xyz.png`).

---

## 13. Denormalized Counters & Synchronization

| Counter | Parent Entity | Synchronized Operations |
| :--- | :--- | :--- |
| `LikesCount` | `Post` | Post like (+1), Post unlike (-1) |
| `CommentsCount` | `Post` | Add comment (+1), Delete comment (-1) |
| `AttachmentsCount` | `Post` | Post creation with attachments |
| `LikesCount` | `Comment` | Comment like (+1), Comment unlike (-1) |
| `RepliesCount` | `Comment` | Add reply (+1), Delete reply (-1) |
| `CountMembers` | `Group` | Join group (+1), Leave group (-1), Remove member (-1) |
| `PostCount` | `Group` | Create post (+1), Delete post (-1) |

---

## 14. Authentication & Token System

```
                           AUTHENTICATION WORKFLOW

        ┌────────────────────────────────────────────────────────┐
        │                        CLIENT                          │
        └───────┬────────────────────────────────────────▲───────┘
                │ 1. POST /api/auth/login                │ 8. AuthResponseDto
                │    { email, password }                 │    (AccessToken + RefreshToken)
                ▼                                        │
        ┌────────────────────────────────────────────────┴───────┐
        │                     AuthService                        │
        └───────┬────────────────────────────────────────▲───────┘
                │ 2. Validate Credentials                │ 7. Return Result
                ▼                                        │
        ┌─────────────────────────┐                      │
        │ ASP.NET Core Identity   │                      │
        │ (UserManager)           │                      │
        └───────┬─────────────────┘                      │
                │ 3. Succeeded                           │
                ▼                                        │
        ┌─────────────────────────┐                      │
        │ Generate JWT Access     │                      │
        │ Token (HMAC-SHA256)     │                      │
        └───────┬─────────────────┘                      │
                │ 4. AccessToken created                 │
                ▼                                        │
        ┌─────────────────────────┐                      │
        │ Generate 64-byte random │                      │
        │ RefreshToken + SHA-256  │                      │
        └───────┬─────────────────┘                      │
                │ 5. TokenHash calculated                │
                ▼                                        │
        ┌─────────────────────────┐                      │
        │ Persist to Database     │                      │
        │ (RefreshToken Table)    ├──────────────────────┘
        └─────────────────────────┘
          6. SaveChangesAsync()
```

---

## 15. JWT Access Token Architecture

Configured via `appsettings.json` under `Jwt:`:
* `Secret`, `Issuer`, `Audience`, `ExpiryMinutes` (default: 60).
* Emits minimal claims: `sub` / `NameIdentifier`, `email`, `name`, `jti`.

---

## 16. Refresh Token Architecture & Hashing

* Raw token: 64-byte random string generated via `RandomNumberGenerator`.
* Storage: Strictly SHA-256 hash (`TokenHash`) with a unique index.

---

## 17. Token Rotation & Reuse Detection

```
                       REFRESH TOKEN ROTATION FLOW

     Client                        AuthService                    Database
       │                                │                             │
       │─── 1. POST /api/auth/refresh ─►│                             │
       │       { refreshToken }         │── 2. SHA-256 Hash           │
       │                                │── 3. Find by TokenHash ────►│
       │                                │◄── Returns RefreshToken ────│
       │                                │                             │
       │                                │── 4. Check Expired / Revoked│
       │                                │                             │
       │                                │── [IF REVOKED DETECTED]     │
       │                                │   Revoke ALL User Tokens! ─►│
       │                                │   Return 401 Unauthorized   │
       │                                │                             │
       │                                │── 5. Revoke Old Token ─────►│
       │                                │── 6. Generate New JWT       │
       │                                │── 7. Generate New Refresh   │
       │                                │── 8. Persist New Token ────►│
       │◄── 9. AuthResponseDto ─────────│   (Commit SaveChangesAsync) │
       │    (New Access + Refresh)      │                             │
```

---

## 18. Logout & Revocation Workflow

`POST /api/auth/revoke` or `POST /api/auth/logout`:
1. Hashes incoming token.
2. Locates record in database.
3. Sets `RevokedAt = DateTime.UtcNow` and `RevokedReason = "Logged out"`.
4. Commits changes and writes an audit log.

---

## 19. Preparation for Google SSO

Unified pipeline:
```
Local Login (Email/Password) ────┐
                                 ├─► Common GenerateAuthResponseAsync() ─► JWT + Refresh Token
Google Login (ID Token) ─────────┘
```

---

## 20. Content Safety Moderation (OpenAI API)

* `IContentModerationService` / `ContentModerationService` calls `https://api.openai.com/v1/moderations`.
* Configurable via `OpenAI:ApiKey` in `appsettings.json`.
* Evaluates posts, comments, group descriptions, and profile updates.

---

## 21. XSS Protection & Input Sanitization

* `IXssSanitizerService` / `XssSanitizerService` strips dangerous HTML tags and normalizes input before database persistence.

---

## 22. Real-time Communication (SignalR Hubs)

Endpoints:
* `/hubs/notifications`: Personal user notification channel (`ReceiveNotification`).
* `/hubs/groups`: Group and post live feed channels (`PostCreated`, `CommentCreated`).

---

## 23. In-Memory Caching (IMemoryCache)

* `GroupService` caches group detail lookups for 10 minutes.
* Cache is invalidated automatically upon group updates or deletion.

---

## 24. API Layer, Routing & Controllers

| Controller | Route | Verbs | Description |
| :--- | :--- | :--- | :--- |
| **`AuthController`** | `/api/auth` | `POST /register`, `POST /login`, `POST /refresh`, `POST /revoke`, `POST /logout` | Authentication & token lifecycle. |
| **`UsersController`** | `/api/users` | `GET /{id}/profile`, `GET /me`, `PUT /profile`, `POST /avatar` | User profile & avatar operations. |
| **`GroupsController`** | `/api/groups` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`, `GET /{id}/members`, `POST /{id}/join`, `POST /{id}/leave`, `PUT /{id}/members/{targetUserId}/role`, `DELETE /{id}/members/{targetUserId}` | Community groups & membership. |
| **`PostsController`** | `/api/posts` | `GET /api/groups/{groupId}/posts`, `GET /{id}`, `POST /api/groups/{groupId}/posts`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/pin`, `DELETE /{id}/pin`, `POST /{id}/like`, `DELETE /{id}/like` | Feed posts, pinning & likes. |
| **`CommentsController`** | `/api/comments` | `GET /api/posts/{postId}/comments`, `POST /api/posts/{postId}/comments`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/like`, `DELETE /{id}/like` | Discussion comments, replies & likes. |
| **`AttachmentsController`** | `/api/attachments` | `POST /`, `DELETE /{id}` | File uploads and deletions. |
| **`NotificationsController`** | `/api/notifications` | `GET /`, `PUT /{id}/read`, `PUT /read-all`, `DELETE /{id}` | Alerts and read state. |
| **`ReportsController`** | `/api/reports` | `POST /`, `GET /`, `PUT /{id}/resolve` | Moderation reports & review. |

---

## 25. Global Exception Handling

`GlobalExceptionHandlingMiddleware` intercepts unhandled exceptions, logs structured error context, and returns standard RFC 7807 `ProblemDetails` JSON.

---

## 26. Swagger / OpenAPI Testing Guide

1. Start API: `dotnet run --project ConnectHub.API`
2. Navigate to: `https://localhost:7xxx/swagger`
3. Call `POST /api/auth/register` or `POST /api/auth/login` to obtain `accessToken`.
4. Click **Authorize** button at top right of Swagger UI.
5. Enter: `Bearer <your_access_token>`.
6. Test all protected endpoints directly in the browser!
