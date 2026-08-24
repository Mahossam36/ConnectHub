namespace ConnectHub.ApiTestRunner;

public static class EndpointCatalog
{
    public static readonly IReadOnlyList<EndpointDefinition> All =
    [
        new("POST", "/api/auth/register", false, "Auth"), new("POST", "/api/auth/login", false, "Auth"), new("POST", "/api/auth/refresh", false, "Auth"), new("POST", "/api/auth/revoke", true, "Auth"), new("POST", "/api/auth/logout", true, "Auth"),
        new("GET", "/api/users/{id}/profile", false, "Users"), new("GET", "/api/users/me", true, "Users"), new("PUT", "/api/users/profile", true, "Users"), new("POST", "/api/users/avatar", true, "Users"),
        new("GET", "/api/tags", false, "Tags"),
        new("GET", "/api/categories", false, "Categories"), new("POST", "/api/categories", true, "Categories"),
        new("GET", "/api/groups", false, "Groups"), new("GET", "/api/groups/{id}", false, "Groups"), new("POST", "/api/groups", true, "Groups"), new("PUT", "/api/groups/{id}", true, "Groups"), new("DELETE", "/api/groups/{id}", true, "Groups"),
        new("GET", "/api/groups/{id}/members", true, "Group Members"), new("POST", "/api/groups/{id}/join", true, "Group Members"), new("POST", "/api/groups/{id}/leave", true, "Group Members"), new("PUT", "/api/groups/{id}/members/{userId}/role", true, "Group Members"), new("DELETE", "/api/groups/{id}/members/{userId}", true, "Group Members"),
        new("GET", "/api/Posts/api/groups/{groupId}/posts", true, "Posts"), new("POST", "/api/Posts/api/groups/{groupId}/posts", true, "Posts"), new("GET", "/api/Posts/api/posts/{id}", true, "Posts"), new("PUT", "/api/Posts/api/posts/{id}", true, "Posts"), new("DELETE", "/api/Posts/api/posts/{id}", true, "Posts"), new("POST", "/api/Posts/api/posts/{id}/pin", true, "Posts"), new("DELETE", "/api/Posts/api/posts/{id}/pin", true, "Posts"), new("POST", "/api/Posts/api/posts/{id}/like", true, "Posts"), new("DELETE", "/api/Posts/api/posts/{id}/like", true, "Posts"),
        new("GET", "/api/Comments/api/posts/{postId}/comments", true, "Comments"), new("POST", "/api/Comments/api/posts/{postId}/comments", true, "Comments"), new("PUT", "/api/Comments/api/comments/{id}", true, "Comments"), new("DELETE", "/api/Comments/api/comments/{id}", true, "Comments"), new("POST", "/api/Comments/api/comments/{id}/like", true, "Comments"), new("DELETE", "/api/Comments/api/comments/{id}/like", true, "Comments"),
        new("POST", "/api/attachments", true, "Attachments"), new("DELETE", "/api/attachments/{id}", true, "Attachments"),
        new("GET", "/api/notifications", true, "Notifications"), new("PUT", "/api/notifications/{id}/read", true, "Notifications"), new("PUT", "/api/notifications/read-all", true, "Notifications"), new("DELETE", "/api/notifications/{id}", true, "Notifications"),
        new("POST", "/api/reports", true, "Reports"), new("GET", "/api/reports", true, "Reports"), new("PUT", "/api/reports/{id}/resolve", true, "Reports")
    ];
}
