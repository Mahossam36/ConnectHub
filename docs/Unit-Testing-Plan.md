# ConnectHub Backend — Unit Testing Plan

This document defines the comprehensive unit testing strategy and test case catalog for the ConnectHub backend, focusing on the Business Logic Layer (**`ConnectHub.BLL`**).

---

## 1. Test Project Structure & Frameworks

### Recommended Test Project
Create a single dedicated test project for BLL unit tests:
* **Project Name:** `ConnectHub.BLL.Tests` (Target: `.NET 10.0`)
* **Solution Path:** `ConnectHub.BLL.Tests/ConnectHub.BLL.Tests.csproj`

```
ConnectHub.slnx
├── ConnectHub.Models
├── ConnectHub.DAL
├── ConnectHub.BLL
├── ConnectHub.API
└── ConnectHub.BLL.Tests
    ├── Common/
    │   ├── AutoMapperFixture.cs         (Shared AutoMapper instance with ConnectHubProfile)
    │   └── MockHelpers.cs               (UserManager / Mock setup helpers)
    ├── Services/
    │   ├── AuthServiceTests.cs
    │   ├── PostServiceTests.cs
    │   ├── CommentServiceTests.cs
    │   ├── GroupServiceTests.cs
    │   ├── GroupMemberServiceTests.cs
    │   ├── UserServiceTests.cs
    │   ├── AttachmentServiceTests.cs
    │   ├── NotificationServiceTests.cs
    │   ├── ReportServiceTests.cs
    │   └── ContentModerationServiceTests.cs
    └── Utilities/
        └── XssSanitizerServiceTests.cs
```

### Recommended Packages
* **`xUnit`** (`xunit`, `xunit.runner.visualstudio`): Standard, modern test framework for .NET.
* **`Moq`** (`Moq`): Powerful mocking library for interface dependencies.
* **`FluentAssertions`** (`FluentAssertions`): Expressive, readable assertions (e.g., `result.Status.Should().Be(ResultStatus.Ok)`).
* **`Microsoft.NET.Test.Sdk`**: Test engine integration.

---

## 2. Mocking Strategy & Boundaries

### What to Mock in Unit Tests
| Dependency | Interface | Mock Purpose |
| :--- | :--- | :--- |
| **Repositories** | `IGenericRepository<T>`, `IPostRepository`, `IGroupRepository`, `ICommentRepository`, `IRefreshTokenRepository`, `INotificationRepository`, `IReportRepository`, `IAttachmentRepository` | Isolate database persistence; return pre-canned in-memory entity lists or entities. |
| **Unit of Work** | `IUnitOfWork` | Verify `SaveChangesAsync()` invocation count on state transitions. |
| **Identity User Manager** | `UserManager<ApplicationUser>` | Mock `FindByEmailAsync`, `CheckPasswordAsync`, `CreateAsync`, `FindByIdAsync`. |
| **File Storage** | `IFileStorageService` | Mock `SaveFileAsync` (return fake relative path) and `DeleteFileAsync`. |
| **Content Moderation** | `IContentModerationService` | Mock `IsContentSafeAsync` (return safe `true` or flagged `false`). |
| **XSS Sanitizer** | `IXssSanitizerService` | Mock or use real `XssSanitizerService` (pure string operations). |
| **Audit Service** | `IAuditService` | Verify `LogAsync` was called with correct action and entity details. |
| **Real-time Notifier** | `IRealTimeNotificationService` | Verify SignalR push events dispatched to user/group channels. |
| **In-Memory Cache** | `IMemoryCache` | Mock `TryGetValue` and `Set` or use real `MemoryCache` instance. |
| **Logger** | `ILogger<T>` | Verify log events or use `NullLogger<T>.Instance`. |

### What Should NOT Be Unit Tested (Integration Test Scope)
1. **Actual SQL Server & Database Queries:** Real queries with EF Core execution belong in Integration Tests (`WebApplicationFactory` + In-Memory or Testcontainers SQL Server).
2. **ASP.NET Core Middleware & Routing:** HTTP request pipeline, JWT token validation middleware, and global exception handling.
3. **SignalR WebSocket Transports:** Raw socket connections, client connection IDs, and Hub lifetime.
4. **Physical Disk I/O:** Actual byte reading/writing to `wwwroot` directories.
5. **External OpenAI Network Calls:** Unit tests must not make live HTTP calls to OpenAI endpoints.

---

## 3. Test Naming Convention

Follow the standard **`MethodName_Condition_ExpectedResult`** convention:
* `RegisterAsync_WhenEmailAlreadyExists_ReturnsConflictResult`
* `CreatePostAsync_WhenUserIsNotMember_ReturnsForbiddenResult`
* `LikePostAsync_WhenPostExistsAndNotLiked_IncrementsLikesCountAndSaves`
* `RefreshTokenAsync_WhenTokenIsRevoked_RevokesAllTokensAndReturnsUnauthorized`

---

## 4. Comprehensive BLL Test Cases Catalog

### 4.1 `AuthServiceTests` (P0 — Critical)
* `RegisterAsync_WhenValidInput_CreatesIdentityUserDomainUserAndReturnsTokenPair`
* `RegisterAsync_WhenEmailExists_ReturnsConflictResult`
* `RegisterAsync_WhenIdentityFails_ReturnsInvalidResultWithErrors`
* `LoginAsync_WhenCredentialsValid_ReturnsAuthResponseDtoWithTokens`
* `LoginAsync_WhenUserNotFound_ReturnsUnauthorized`
* `LoginAsync_WhenPasswordInvalid_ReturnsUnauthorized`
* `LoginAsync_WhenUserIsDeactivated_ReturnsForbidden`
* `RefreshTokenAsync_WhenTokenValid_RevokesOldTokenPersistsNewTokenAndReturnsRotatedDto`
* `RefreshTokenAsync_WhenTokenRevoked_DetectsReuseRevokesAllUserTokensAndReturnsUnauthorized`
* `RefreshTokenAsync_WhenTokenExpired_ReturnsUnauthorized`
* `RefreshTokenAsync_WhenTokenNotFound_ReturnsUnauthorized`
* `RevokeTokenAsync_WhenActiveTokenProvided_RevokesTokenAndSaves`
* `RevokeTokenAsync_WhenTokenBelongsToAnotherUser_ReturnsForbidden`
* `RevokeTokenAsync_WhenTokenNotFound_ReturnsNotFound`

### 4.2 `PostServiceTests` (P0 — Critical)
* `GetGroupFeedAsync_WhenUserIsMember_ReturnsPagedPostsWithLikedStatus`
* `GetGroupFeedAsync_WhenUserNotMember_ReturnsForbidden`
* `GetPostByIdAsync_WhenPostExistsAndUserMember_ReturnsPostResponseDto`
* `GetPostByIdAsync_WhenPostNotFound_ReturnsNotFound`
* `CreatePostAsync_WhenValid_SanitizesModeratesAddsPostIncrementsGroupPostCountAndBroadcasts`
* `CreatePostAsync_WhenContentFlaggedByModeration_ReturnsInvalidResultWithoutSaving`
* `CreatePostAsync_WhenUserNotMember_ReturnsForbidden`
* `CreatePostAsync_WhenAttachmentsReferenced_LinksPostIdAndUpdatesAttachmentsCount`
* `UpdatePostAsync_WhenAuthor_SanitizesModeratesUpdatesAndSaves`
* `UpdatePostAsync_WhenNotAuthor_ReturnsForbidden`
* `DeletePostAsync_WhenAuthorOrGroupAdmin_DeletesPostDecrementsGroupPostCountAndSaves`
* `DeletePostAsync_WhenRegularMemberNotAuthor_ReturnsForbidden`
* `PinPostAsync_WhenGroupAdminOrOwner_PinsPostAndSaves`
* `PinPostAsync_WhenRegularMember_ReturnsForbidden`
* `UnpinPostAsync_WhenGroupAdminOrOwner_UnpinsPostAndSaves`
* `LikePostAsync_WhenNotYetLiked_AddsLikeIncrementsLikesCountAndSaves`
* `LikePostAsync_WhenAlreadyLiked_ReturnsConflictResult`
* `UnlikePostAsync_WhenLiked_RemovesLikeDecrementsLikesCountAndSaves`
* `UnlikePostAsync_WhenNotLiked_ReturnsNotFound`

### 4.3 `CommentServiceTests` (P0 — Critical)
* `GetPostCommentsAsync_WhenUserIsMember_ReturnsThreadedCommentsAndReplies`
* `GetPostCommentsAsync_WhenPostNotFound_ReturnsNotFound`
* `AddCommentAsync_WhenRootComment_AddsCommentIncrementsPostCommentsCountAndNotifiesAuthor`
* `AddCommentAsync_WhenReply_AddsReplyIncrementsParentRepliesCountAndNotifiesParentAuthor`
* `AddCommentAsync_WhenContentFlagged_ReturnsInvalidWithoutSaving`
* `AddCommentAsync_WhenParentCommentDoesNotBelongToPost_ReturnsInvalid`
* `UpdateCommentAsync_WhenAuthor_UpdatesContentAndSaves`
* `UpdateCommentAsync_WhenNotAuthor_ReturnsForbidden`
* `DeleteCommentAsync_WhenAuthorOrAdmin_DeletesCommentDecrementsCountersAndSaves`
* `DeleteCommentAsync_WhenUnauthorizedUser_ReturnsForbidden`
* `LikeCommentAsync_WhenNotLiked_AddsLikeIncrementsLikesCountAndSaves`
* `LikeCommentAsync_WhenAlreadyLiked_ReturnsConflict`
* `UnlikeCommentAsync_WhenLiked_RemovesLikeDecrementsLikesCountAndSaves`
* `UnlikeCommentAsync_WhenNotLiked_ReturnsNotFound`

### 4.4 `GroupMemberServiceTests` (P0 — Critical)
* `GetMembersAsync_WhenGroupExists_ReturnsPagedMembers`
* `GetMembersAsync_WhenGroupNotFound_ReturnsNotFound`
* `JoinGroupAsync_WhenNotMember_AddsMembershipIncrementsGroupMemberCountAndSaves`
* `JoinGroupAsync_WhenAlreadyActiveMember_ReturnsConflict`
* `JoinGroupAsync_WhenRejoiningInactive_ReactivatesMembershipIncrementsCounterAndSaves`
* `LeaveGroupAsync_WhenMember_DeactivatesMembershipDecrementsCounterAndSaves`
* `LeaveGroupAsync_WhenOwner_ReturnsConflict`
* `LeaveGroupAsync_WhenNotMember_ReturnsNotFound`
* `ChangeMemberRoleAsync_WhenOwnerChangesMember_UpdatesRoleAndSaves`
* `ChangeMemberRoleAsync_WhenAdminTriesToPromoteToOwner_ReturnsForbidden`
* `ChangeMemberRoleAsync_WhenRegularMemberTriesToChangeRole_ReturnsForbidden`
* `RemoveMemberAsync_WhenAdminOrOwner_DeactivatesMembershipDecrementsCounterAndSaves`
* `RemoveMemberAsync_WhenAdminTriesToRemoveAdminOrOwner_ReturnsForbidden`

### 4.5 `GroupServiceTests` (P1 — Important)
* `BrowseGroupsAsync_WithSearchAndTagFilter_ReturnsFilteredPagedResult`
* `GetGroupByIdAsync_WhenCached_ReturnsFromMemoryCacheWithoutQueryingRepository`
* `GetGroupByIdAsync_WhenNotCached_QueriesRepoCachesAndReturnsDetail`
* `GetGroupByIdAsync_WhenNotFoundOrInactive_ReturnsNotFound`
* `CreateGroupAsync_WhenValid_SanitizesModeratesCreatesGroupAssignsOwnerAndSaves`
* `CreateGroupAsync_WhenCategoryNotFound_ReturnsInvalid`
* `UpdateGroupAsync_WhenOwnerOrAdmin_UpdatesDetailsInvalidatesCacheAndSaves`
* `UpdateGroupAsync_WhenRegularMember_ReturnsForbidden`
* `DeleteGroupAsync_WhenOwner_SoftDeletesGroupInvalidatesCacheAndSaves`
* `DeleteGroupAsync_WhenNotOwner_ReturnsForbidden`

### 4.6 `UserServiceTests` (P1 — Important)
* `GetProfileAsync_WhenUserExists_MapsUserAndIdentityEmailToDto`
* `GetProfileAsync_WhenNotFound_ReturnsNotFound`
* `UpdateProfileAsync_WhenValid_UpdatesBioAndNameAndSaves`
* `UpdateProfileAsync_WhenUserNotFound_ReturnsNotFound`
* `UpdateAvatarAsync_WhenValidStream_DeletesOldFileSavesNewAndUpdatesProfileImagePath`
* `UpdateAvatarAsync_WhenStreamEmpty_ReturnsInvalid`

### 4.7 `AttachmentServiceTests` (P1 — Important)
* `UploadAsync_WhenValidFile_DelegatesToStorageServiceCreatesAttachmentEntityAndSaves`
* `UploadAsync_WhenEmptyStreamOrZeroSize_ReturnsInvalid`
* `UploadAsync_WhenMissingContentTypeOrName_ReturnsInvalid`
* `DeleteAsync_WhenUploader_DeletesPhysicalFileDeletesEntityAndSaves`
* `DeleteAsync_WhenNotUploader_ReturnsForbidden`
* `DeleteAsync_WhenNotFound_ReturnsNotFound`

### 4.8 `NotificationServiceTests` (P2 — Optional/Standard)
* `GetNotificationsAsync_ReturnsPagedItemsAndUnreadCount`
* `MarkAsReadAsync_WhenOwner_SetsIsReadTrueAndSaves`
* `MarkAsReadAsync_WhenNotOwner_ReturnsForbidden`
* `MarkAllAsReadAsync_DelegatesToRepoBulkUpdate`
* `DeleteNotificationAsync_WhenOwner_DeletesAndSaves`
* `DispatchNotificationAsync_CreatesEntitySavesAndPushesToRealTimeService`

### 4.9 `ReportServiceTests` (P2 — Optional/Standard)
* `SubmitReportAsync_WhenPostTargetExists_CreatesPendingReportAndSaves`
* `SubmitReportAsync_WhenCommentTargetExists_CreatesPendingReportAndSaves`
* `SubmitReportAsync_WhenTargetNotFound_ReturnsNotFound`
* `GetReportsAsync_ReturnsPagedReports`
* `ResolveReportAsync_WhenReportExists_UpdatesStatusAndReviewedAtAndSaves`
* `ResolveReportAsync_WhenNotFound_ReturnsNotFound`

### 4.10 `ContentModerationServiceTests` & `XssSanitizerServiceTests` (P1 — Important)
* `IsContentSafeAsync_WhenApiKeyMissing_ReturnsSuccessTrueAsFallback`
* `IsContentSafeAsync_WhenApiReturnsFlaggedTrue_ReturnsInvalidResult`
* `IsContentSafeAsync_WhenApiReturnsFlaggedFalse_ReturnsSuccessTrue`
* `Sanitize_WhenHtmlTagsPresent_StripsTagsAndDecodesEntities`
* `Sanitize_WhenPlainTextWithoutHtml_PreservesContentUnchanged`

---

## 5. Prioritized Implementation Roadmap

```
┌────────────────────────────────────────────────────────┐
│  P0 (Critical Business Logic & Security)               │
│  • AuthService (14 tests)                              │
│  • PostService (18 tests)                              │
│  • CommentService (14 tests)                           │
│  • GroupMemberService (13 tests)                       │
│  Total: ~59 tests                                      │
└──────────────────────────┬─────────────────────────────┘
                           ▼
┌────────────────────────────────────────────────────────┐
│  P1 (Important Operations & Safety)                    │
│  • GroupService (10 tests)                             │
│  • UserService (6 tests)                               │
│  • AttachmentService (6 tests)                         │
│  • ContentModeration & XSS Sanitizer (5 tests)         │
│  Total: ~27 tests                                      │
└──────────────────────────┬─────────────────────────────┘
                           ▼
┌────────────────────────────────────────────────────────┐
│  P2 (Supporting Workflows)                             │
│  • NotificationService (6 tests)                       │
│  • ReportService (6 tests)                             │
│  Total: ~12 tests                                      │
└────────────────────────────────────────────────────────┘
```

---

## 6. Estimated Test Count Summary

| Service / Component | P0 (Critical) | P1 (Important) | P2 (Optional) | Total Tests |
| :--- | :---: | :---: | :---: | :---: |
| **`AuthService`** | 14 | — | — | **14** |
| **`PostService`** | 18 | — | — | **18** |
| **`CommentService`** | 14 | — | — | **14** |
| **`GroupMemberService`** | 13 | — | — | **13** |
| **`GroupService`** | — | 10 | — | **10** |
| **`UserService`** | — | 6 | — | **6** |
| **`AttachmentService`** | — | 6 | — | **6** |
| **`ContentModerationService`** | — | 3 | — | **3** |
| **`XssSanitizerService`** | — | 2 | — | **2** |
| **`NotificationService`** | — | — | 6 | **6** |
| **`ReportService`** | — | — | 6 | **6** |
| **TOTALS** | **59** | **27** | **12** | **98** |

### Recommended Minimum Test Set (Student Project Quick Win)
If working with tight time/token budgets, implement the **Top 25 Core Tests**:
1. `AuthService`: Login valid, Login invalid, Refresh rotation, Refresh revoked reuse (4 tests).
2. `PostService`: Feed query, Create post, Moderate flag, Like increment, Unlike decrement (5 tests).
3. `CommentService`: Add comment post counter +1, Add reply reply counter +1, Delete comment counter -1 (3 tests).
4. `GroupMemberService`: Join group member count +1, Leave group member count -1, Owner leave rejection, Role change authorization (4 tests).
5. `GroupService`: Create group owner assignment, Update permission check, Delete soft-delete (3 tests).
6. `AttachmentService`: Upload valid, Upload empty rejection, Delete authorization check (3 tests).
7. `XssSanitizerService`: HTML tag stripping (1 test).
8. `ContentModerationService`: Flagged content rejection (2 tests).
