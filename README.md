# ConnectHub — Yalla Social Collaboration Platform

> A full-stack, production-ready social collaboration platform built with a clean **4-layer .NET backend**, a security-hardened **Backend-for-Frontend (BFF)** proxy layer, and a modern **Angular 22 SPA** — all wired through a **WSO2 Integration Layer**.

---

## Table of Contents

- [Project Overview](#project-overview)
- [High-Level Architecture](#high-level-architecture)
- [Technology Stack](#technology-stack)
- [Repository Structure](#repository-structure)
- [Backend — ConnectHub API](#backend--connecthub-api)
- [Backend-for-Frontend — Yalla BFF](#backend-for-frontend--yalla-bff)
- [Frontend — Yalla Angular SPA](#frontend--yalla-angular-spa)
- [Domain Model](#domain-model)
- [Real-Time Features](#real-time-features)
- [Security Overview](#security-overview)
- [Configuration Reference](#configuration-reference)
- [Getting Started](#getting-started)
- [Testing](#testing)
- [Contributing](#contributing)

---

## Project Overview

**ConnectHub** (branded **Yalla** on the frontend) is a modern social collaboration platform where users can create posts, join communities (groups), interact through comments and likes, receive real-time notifications, and discover other users and content — all within a safe, moderated environment powered by OpenAI content moderation.

The system is architected across three independently deployable tiers that communicate through a WSO2 API Integration Layer, following **separation of concerns**, **clean architecture principles**, and **zero-trust security** for the browser.

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          CLIENT TIER                                        │
│                                                                             │
│    ┌──────────────────────────────────────────────────────────────────┐    │
│    │  Angular 22 SPA  (Yalla)                                         │    │
│    │  • No tokens stored in browser                                   │    │
│    │  • HttpOnly cookie authentication                                │    │
│    └────────────────────────┬─────────────────────────────────────────┘    │
└─────────────────────────────┼───────────────────────────────────────────────┘
                              │ HttpOnly Yalla.Session Cookie
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          BFF TIER  (.NET 10)                                │
│                                                                             │
│    ┌──────────────────────────────────────────────────────────────────┐    │
│    │  Yalla BFF                                                       │    │
│    │  • Session management (Redis)                                    │    │
│    │  • Google OIDC authentication                                    │    │
│    │  • OpenAI content moderation gate                                │    │
│    │  • Controlled proxy to Integration Layer                         │    │
│    └───────┬───────────────────┬──────────────────┬───────────────────┘    │
└────────────┼───────────────────┼──────────────────┼────────────────────────┘
             │                   │                  │
             ▼                   ▼                  ▼
        Redis Cache        Google OIDC         OpenAI Moderation
        (Sessions +        (Identity)          (omni-moderation-latest)
        Shared Token)
             │
             ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      INTEGRATION / API GATEWAY TIER                         │
│                                                                             │
│    ┌──────────────────────────────────────────────────────────────────┐    │
│    │  WSO2 Integration Layer                                          │    │
│    │  • Validates shared Integration Bearer Token                     │    │
│    │  • Forwards user Access-Token to backend                         │    │
│    │  • Applies forwarding policy                                     │    │
│    └────────────────────────┬─────────────────────────────────────────┘    │
└─────────────────────────────┼───────────────────────────────────────────────┘
                              │ Access-Token (JWT)
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          BACKEND TIER  (.NET)                               │
│                                                                             │
│   ┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────────┐   │
│   │  API Layer │──▶│  BLL Layer │──▶│  DAL Layer │──▶│  Domain Layer  │   │
│   │(Controllers│   │ (Services, │   │(Repos, EF  │   │(Entities,Enums)│   │
│   │  SignalR)  │   │ Validators)│   │  Context)  │   │                │   │
│   └────────────┘   └────────────┘   └────────────┘   └────────────────┘   │
│                                            │                                │
│                                            ▼                                │
│                                    SQL Server (MSSQL)                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Technology Stack

### Backend (ConnectHub API)

| Category | Technology |
|---|---|
| Framework | ASP.NET Core (.NET) |
| ORM | Entity Framework Core |
| Database | Microsoft SQL Server |
| Authentication | JWT Bearer Tokens (Access + Refresh) |
| Real-time | SignalR (WebSockets) |
| Validation | FluentValidation |
| Object Mapping | AutoMapper |
| API Documentation | Swagger / OpenAPI |
| Content Moderation | OpenAI API |
| Input Sanitization | XSS Sanitizer |

### Backend-for-Frontend (Yalla BFF)

| Category | Technology |
|---|---|
| Framework | ASP.NET Core .NET 10 |
| Session Store | Redis |
| Identity Provider | Google OpenID Connect (OIDC) |
| Content Moderation | OpenAI `omni-moderation-latest` |
| API Gateway | WSO2 Integration Layer |
| Testing | xUnit |

### Frontend (Yalla SPA)

| Category | Technology |
|---|---|
| Framework | Angular 22 |
| Language | TypeScript 6 |
| Styling | SCSS |
| HTTP | Angular HttpClient |
| Real-time | SignalR Client |
| Package Manager | npm 11 |
| Testing | Vitest |
| Formatter | Prettier |

---

## Repository Structure

```
Entertainment/
├── Backend/                          # Core .NET API — 4 clean architecture layers
│   ├── ConnectHub.API/               # Presentation layer (Controllers, SignalR Hubs)
│   ├── ConnectHub.BLL/               # Business Logic Layer (Services, DTOs, Validators)
│   ├── ConnectHub.DAL/               # Data Access Layer (Repositories, EF Context, Migrations)
│   └── ConnectHub.Domain/            # Domain Layer (Entities, Enums)
│
├── Back-end For Front-end/           # BFF security layer
│   └── BFF/
│       ├── Controllers/              # AuthController, ProxyController
│       ├── Services/                 # Authentication, Google, Integration, Moderation, Sessions
│       ├── Middleware/               # Global error handling
│       ├── Health/                   # Redis health check
│       ├── Configuration/            # BffOptions
│       ├── Models/                   # Auth and session models
│       ├── BFF.Tests/                # xUnit unit tests
│       └── BFF_ARCHITECTURE.md       # Detailed BFF architecture docs
│
├── Front-end Project/                # Angular 22 SPA
│   └── yalla/
│       └── src/
│           └── app/
│               ├── core/             # Guards, interceptors, services, models
│               ├── features/         # Feature modules (auth, home, profile, communities, …)
│               └── main-layout/      # Shared shell layout
│
├── ConnectHub.ApiTestRunner/         # API integration test runner
├── docs/                             # Additional documentation
└── ConnectHub.slnx                   # Solution file
```

---

## Backend — ConnectHub API

The backend follows a strict **4-layer clean architecture** with clear dependency direction: `API → BLL → DAL → Domain`.

### Layers

#### `ConnectHub.Domain` — Domain Layer

Pure domain models with zero external dependencies.

- **Entities:** `User`, `Post`, `Comment`, `Group`, `GroupMember`, `Tag`, `Category`, `Attachment`, `Notification`, `Report`, `PostLike`, `CommentLike`, `RefreshToken`, `AuditLog`
- **Enums:** Role enums, status types, report categories, and more

#### `ConnectHub.DAL` — Data Access Layer

Persistence infrastructure built on Entity Framework Core.

- **Context:** `ConnectHubDbContext` (EF Core + ASP.NET Identity)
- **Repositories:** `GenericRepository<T>`, specialized repositories for Posts, Comments, Groups, Notifications, Reports, Attachments, RefreshTokens
- **Unit of Work:** Transaction-scoped commit pattern
- **Configurations:** Fluent API entity configurations
- **Migrations:** Full EF Core migration history

#### `ConnectHub.BLL` — Business Logic Layer

All domain and application rules live here.

| Service | Responsibility |
|---|---|
| `AuthService` | Registration, login, JWT issuance, refresh token lifecycle |
| `PostService` | CRUD, likes, feed generation, moderation hooks |
| `CommentService` | Threaded comments, likes, moderation |
| `GroupService` | Community creation, settings, member management |
| `GroupMemberService` | Invite, join, leave, role assignment |
| `NotificationService` | Event-driven notification creation |
| `UserService` | Profile management, user lookup |
| `CategoryService` | Content categorization |
| `TagService` | Tagging system |
| `AttachmentService` | File attachment handling |
| `ReportService` | Content reporting and moderation workflow |
| `ContentModerationService` | OpenAI API integration for text moderation |
| `FileStorageService` | File upload and storage |
| `AuditService` | Audit trail logging |
| `XssSanitizerService` | Input sanitization against XSS |

#### `ConnectHub.API` — Presentation Layer

ASP.NET Core Web API with SignalR real-time support.

**Controllers:**

| Controller | Endpoints |
|---|---|
| `AuthController` | Register, Login, Refresh Token, Logout |
| `PostsController` | CRUD posts, likes, feed |
| `CommentsController` | CRUD comments, likes |
| `GroupsController` | CRUD communities, membership management |
| `UsersController` | User profiles, search |
| `NotificationsController` | Notification retrieval, mark-as-read |
| `CategoriesController` | Category listing |
| `TagsController` | Tag management |
| `AttachmentsController` | File uploads |
| `ReportsController` | Content reporting |

**SignalR Hubs:**

| Hub | Purpose |
|---|---|
| `NotificationHub` — `/hubs/notifications` | Push real-time notifications to connected users |
| `GroupHub` — `/hubs/groups` | Real-time updates within group/community feeds |

**Authentication:**
- JWT Bearer via `Authorization` header **or** `Access-Token` header (for BFF-forwarded requests)
- `access_token` query string support for SignalR WebSocket upgrades

---

## Backend-for-Frontend — Yalla BFF

The BFF is the **sole browser-facing service**. It acts as a security boundary that keeps all tokens server-side, away from the browser's JavaScript environment.

> See `Back-end For Front-end/BFF/BFF_ARCHITECTURE.md` for full architecture details and sequence diagrams.

### Responsibilities

| Responsibility | Details |
|---|---|
| **Session management** | Opaque `Yalla.Session` HttpOnly cookie; session data stored in Redis |
| **Token lifecycle** | Access/refresh tokens stored only in Redis, never sent to browser |
| **Google OIDC** | Full OAuth 2.0 / OpenID Connect flow with Google |
| **Integration token** | Shared WSO2 Integration Bearer Token cached and refreshed in Redis |
| **Content moderation** | OpenAI `omni-moderation-latest` called before 4 configured write routes |
| **Controlled proxy** | All `/api/**` traffic proxied to WSO2 with server-owned headers |
| **Health checks** | `/health` (liveness) and `/health/ready` (Redis readiness) |

### BFF Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/auth/login` | Authenticate user; create Redis session; set cookie |
| `POST` | `/auth/register` | Register user; create Redis session; set cookie |
| `POST` | `/auth/logout` | Clear Redis session and cookie |
| `POST` | `/auth/revoke` | Revoke refresh token; clear session |
| `GET` | `/auth/me` | Return safe session profile (no tokens) |
| `GET` | `/auth/google` | Start Google OIDC challenge |
| `GET` | `/auth/google/callback` | Google OIDC redirect handler (middleware-owned) |
| `GET` | `/auth/google/complete` | Post-OIDC session creation |
| `GET` | `/health` | Liveness probe |
| `GET` | `/health/ready` | Redis readiness probe |
| `*` | `/api/{**path}` | Authenticated proxy to Integration Layer |

### Request Flow (Proxy)

```
Angular → [HttpOnly Cookie] → BFF → [Validate Session (Redis)]
    → [Refresh token if needed] → [Moderate if configured route]
    → [Attach Integration Token + Access-Token] → WSO2
    → [Forward policy] → ConnectHub Backend
    → [Response streamed back to Angular]
```

---

## Frontend — Yalla Angular SPA

A standalone Angular 22 SPA using the modern **standalone component** architecture, **signal-based reactivity**, and lazy-loaded feature modules.

### Feature Modules

| Feature | Routes | Description |
|---|---|---|
| **Auth** | `/login`, `/register` | Email/password login, registration, Google OAuth entry |
| **Home** | `/home` | Social feed with posts, likes, comments |
| **Profile** | `/profile` | User profile view and editing |
| **Communities** | `/communities` | User's joined communities |
| **Discover** | `/discover` | Explore and join public communities |
| **Community** | `/community/:id` | Individual community page with group feed |
| **Notifications** | `/notifications` | Real-time notification center |

### Core Module

| Directory | Contents |
|---|---|
| `core/guards/` | `authGuard` — Route protection for authenticated pages |
| `core/interceptors/` | HTTP interceptors (credential handling, error mapping) |
| `core/services/` | Shared application services |
| `core/models/` | TypeScript interfaces and types |
| `core/auth/` | Authentication state management |
| `core/utils/` | Utility helpers |

### Routing

All application routes (except `/login` and `/register`) are protected by `authGuard`. The `MainLayoutComponent` shell wraps all authenticated routes, rendering the shared navbar and side panel once. The `CommunityComponent` is self-contained and rendered outside the main layout shell to avoid duplicating navigation elements.

---

## Domain Model

```
User
 ├──< Post >──── PostLike
 │      ├──< Comment >── CommentLike
 │      ├──< Attachment >
 │      ├──< Tag >
 │      └──< Category >
 │
 ├──< GroupMember >──── Group ──< Post (group posts)
 │
 ├──< Notification >
 ├──< Report >
 ├──< AuditLog >
 └──< RefreshToken >
```

---

## Real-Time Features

ConnectHub uses **ASP.NET Core SignalR** for bidirectional WebSocket communication.

| Hub | Endpoint | Events |
|---|---|---|
| `NotificationHub` | `/hubs/notifications` | New notifications pushed to user connections |
| `GroupHub` | `/hubs/groups` | Live group feed updates (new posts, member events) |

**Token delivery for WebSocket connections:**
- Standard requests: `Access-Token` header
- SignalR WebSocket upgrades: `?access_token=<token>` query parameter

---

## Security Overview

| Layer | Mechanism |
|---|---|
| **Browser → BFF** | Opaque HttpOnly cookie (`Yalla.Session`); no tokens in browser |
| **BFF → WSO2** | Shared Integration Bearer Token (server-side, cached in Redis) |
| **WSO2 → Backend** | User `Access-Token` JWT forwarded by WSO2 policy |
| **Backend** | JWT validation (`HS256`), role-based authorization, FluentValidation, XSS sanitization |
| **Content** | OpenAI moderation gate on write operations (posts, comments) |
| **Identity** | Native credentials + Google OpenID Connect (OIDC) |
| **Transport** | HTTPS enforced; `SameSite=None; Secure` cookie policy |
| **Proxy Safety** | Path validation rejects `://`, blank, `.`, `..` segments; header allowlisting |

---

## Configuration Reference

### Backend — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=ConnectHubDb;..."
  },
  "Jwt": {
    "Secret": "<min-32-char-signing-key>",
    "Issuer": "ConnectHub",
    "Audience": "ConnectHubClients",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "OpenAI": {
    "ApiKey": "<openai-api-key>"
  },
  "FileStorage": {
    "BasePath": "<upload-directory>"
  }
}
```

### BFF — `appsettings.json`

```json
{
  "Integration": {
    "BaseUrl": "<wso2-base-url>",
    "ConnectHubPath": "<api-path>",
    "TokenPath": "<token-endpoint>",
    "TokenRevokePath": "<revoke-endpoint>",
    "IntegrationTokenExpirationSafetyMarginSeconds": 30
  },
  "Authentication": {
    "LoginPath": "<login-endpoint>",
    "RegisterPath": "<register-endpoint>",
    "ExternalLoginPath": "<external-login-endpoint>",
    "RefreshPath": "<refresh-endpoint>"
  },
  "Session": {
    "CookieName": "Yalla.Session",
    "ExpirationMinutes": 60,
    "SecurePolicy": "Always",
    "SameSite": "None"
  },
  "Redis": {
    "ConnectionString": "<redis-connection-string>"
  },
  "Cors": {
    "AllowedOrigins": ["https://your-frontend-domain.com"]
  },
  "Google": {
    "ClientId": "<google-client-id>",
    "ClientSecret": "<google-client-secret>",
    "CallbackPath": "/auth/google/callback",
    "RedirectUri": "<bff-callback-uri>",
    "FrontendRedirectUri": "<spa-uri-after-login>"
  },
  "OpenAI": {
    "Moderation": {
      "Model": "omni-moderation-latest",
      "TimeoutSeconds": 10
    }
  }
}
```

---

## Getting Started

### Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8+ (BFF requires .NET 10) |
| Node.js | 20+ |
| npm | 11+ |
| SQL Server | LocalDB / Express / Full |
| Redis | 7+ |
| Angular CLI | 22+ |

### 1. Backend Setup

```bash
# Navigate to Backend
cd Backend

# Restore packages
dotnet restore

# Apply EF Core migrations
dotnet ef database update --project ConnectHub.DAL --startup-project ConnectHub.API

# Run the API
dotnet run --project ConnectHub.API
```

The API will be available at `https://localhost:<port>` with Swagger at `/swagger`.

### 2. BFF Setup

```bash
# Navigate to BFF
cd "Back-end For Front-end/BFF"

# Restore packages
dotnet restore

# Configure appsettings.Development.json with your Redis, WSO2, and Google credentials

# Run the BFF
dotnet run
```

### 3. Frontend Setup

```bash
# Navigate to frontend
cd "Front-end Project/yalla"

# Install dependencies
npm install

# Start the dev server
npm start
```

The Angular app will be available at `http://localhost:4200`.

---

## Testing

### Backend — API Integration Tests

```bash
cd ConnectHub.ApiTestRunner
dotnet test
```

### BFF — Unit Tests

```bash
cd "Back-end For Front-end/BFF"
dotnet test BFF.Tests
```

Tests cover:
- `IntegrationTokenServiceTests` — token caching and refresh logic
- `OpenAiContentModerationServiceTests` — moderation decision handling

### Frontend — Unit Tests

```bash
cd "Front-end Project/yalla"
npm test
```

Powered by **Vitest** for fast, native ESM test execution.

---

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Commit your changes following [Conventional Commits](https://www.conventionalcommits.org/)
4. Open a Pull Request targeting `main`

Please ensure all tests pass and new features include appropriate test coverage before submitting a PR.

---

<div align="center">

**ConnectHub / Yalla** — Built with .NET, Angular, Redis & WSO2

</div>
