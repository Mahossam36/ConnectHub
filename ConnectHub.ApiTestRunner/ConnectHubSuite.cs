using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ConnectHub.ApiTestRunner;

public sealed class ConnectHubSuite
{
    private readonly ApiClient _api;
    private readonly RunnerOptions _options;
    private readonly ScenarioContext _context = new();
    private readonly Random _random;
    private readonly string _runId;

    public ConnectHubSuite(ApiClient api, RunnerOptions options)
    {
        _api = api;
        _options = options;
        _random = new Random(options.Seed);
        _runId = $"api_test_{options.Seed}_{_random.Next(100000, 999999)}";
    }

    public IReadOnlyList<TestCase> Build()
    {
        var tests = new List<TestCase>
        {
            Case("API reachability", "Infrastructure", "/api/tags", "GET", Reachability),
            Case("Tags list", "Tags", "/api/tags", "GET", Tags),
            Case("Categories list is public", "Categories", "/api/categories", "GET", Categories),
            Case("Browse groups is public and paginated", "Groups", "/api/groups", "GET", BrowseGroups),
            Case("Register rejects invalid email", "Authentication", "/api/auth/register", "POST", InvalidRegister),
            Case("Register unique owner", "Authentication", "/api/auth/register", "POST", RegisterOwner),
            Case("Create category through HTTP", "Categories", "/api/categories", "POST", CreateCategory),
            Case("Provision deterministic user pool", "Data Setup", "/api/auth/register", "POST", ProvisionUserPool, "Data setup", 1),
            Case("Register duplicate email conflicts", "Authentication", "/api/auth/register", "POST", DuplicateRegister),
            Case("Login rejects invalid password", "Authentication", "/api/auth/login", "POST", InvalidLogin),
            Case("Login valid user", "Authentication", "/api/auth/login", "POST", ValidLogin),
            Case("Refresh rejects malformed token", "Authentication", "/api/auth/refresh", "POST", InvalidRefresh),
            Case("Protected profile rejects anonymous", "Authorization", "/api/users/me", "GET", AnonymousProfile),
            Case("Current profile", "Users", "/api/users/me", "GET", CurrentProfile),
            Case("Profile by ID", "Users", "/api/users/{id}/profile", "GET", ProfileById),
            Case("Update profile validation", "Users", "/api/users/profile", "PUT", InvalidProfile),
            Case("Update profile", "Users", "/api/users/profile", "PUT", UpdateProfile),
            Case("Attachment upload validation", "Attachments", "/api/attachments", "POST", EmptyAttachment),
            Case("Group workflow", "Groups", "/api/groups", "POST", GroupWorkflow, "Groups", 1),
            Case("Post workflow", "Posts", "/api/Posts/api/groups/{groupId}/posts", "POST", PostWorkflow, "Posts", 1),
            Case("Comment workflow", "Comments", "/api/Comments/api/posts/{postId}/comments", "POST", CommentWorkflow, "Comments", 1),
            Case("Notification workflow", "Notifications", "/api/notifications", "GET", NotificationWorkflow, "Notifications", 1),
            Case("Report workflow", "Reports", "/api/reports", "POST", ReportWorkflow, "Reports", 1),
            Case("Attachment workflow", "Attachments", "/api/attachments", "POST", AttachmentWorkflow, "Attachments", 1),
            Case("Generate configured group/post/comment dataset", "Data Setup", "/api/groups", "POST", GenerateDataset, "Data setup", 2),
        };

        foreach (var endpoint in EndpointCatalog.All.Where(endpoint => endpoint.Protected))
        {
            tests.Add(Case($"Anonymous rejected: {endpoint.Method} {endpoint.Route}", "Authentication", endpoint.Route, endpoint.Method,
                () => AuthBoundary(endpoint, null, "Anonymous")));
            tests.Add(Case($"Malformed token rejected: {endpoint.Method} {endpoint.Route}", "Authentication", endpoint.Route, endpoint.Method,
                () => AuthBoundary(endpoint, "not.a.jwt", "Malformed bearer token")));
        }

        return tests;
    }

    public IReadOnlyList<TestCase> BuildMockData() =>
    [
        Case("API reachability", "Infrastructure", "/api/tags", "GET", Reachability),
        Case("Register mock-data owner", "Mock Data", "/api/auth/register", "POST", RegisterOwner),
        Case("Create comprehensive mock dataset", "Mock Data", "multiple endpoints", "HTTP", GenerateMockData, "Mock data", 1)
    ];

    private async Task<TestResult> AuthBoundary(EndpointDefinition endpoint, string? token, string kind)
    {
        var route = Materialize(endpoint.Route);
        var response = await _api.SendAsync(new HttpMethod(endpoint.Method), route, token: token);
        var result = Assert($"{kind} rejected: {endpoint.Method} {endpoint.Route}", "Authentication", endpoint.Route, endpoint.Method, response, HttpStatusCode.Unauthorized);
        return result with { TestType = kind, Actor = kind };
    }

    private static string Materialize(string route) => route
        .Replace("{id}", Guid.NewGuid().ToString())
        .Replace("{groupId}", Guid.NewGuid().ToString())
        .Replace("{postId}", Guid.NewGuid().ToString())
        .Replace("{userId}", Guid.NewGuid().ToString());

    private TestCase Case(string name, string category, string endpoint, string method, Func<Task<TestResult>> execute, string? workflow = null, int? step = null) =>
        new() { Name = name, Category = category, Endpoint = endpoint, Method = method, Execute = execute, Workflow = workflow, Step = step };

    private async Task<TestResult> Reachability()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "/api/tags");
        return Assert("API reachability", "Infrastructure", "/api/tags", "GET", response, HttpStatusCode.OK);
    }

    private async Task<TestResult> Tags()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "/api/tags");
        return Assert("Tags list", "Tags", "/api/tags", "GET", response, HttpStatusCode.OK);
    }

    private async Task<TestResult> Categories()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "/api/categories");
        return Assert("Categories list is public", "Categories", "/api/categories", "GET", response, HttpStatusCode.OK);
    }

    private async Task<TestResult> BrowseGroups()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "/api/groups?skip=0&take=1");
        return Assert("Browse groups is public and paginated", "Groups", "/api/groups", "GET", response, HttpStatusCode.OK);
    }

    private async Task<TestResult> InvalidRegister()
    {
        var request = new { email = "not-an-email", password = "x", firstName = "", lastName = "" };
        var response = await _api.SendAsync(HttpMethod.Post, "/api/auth/register", request);
        return Assert("Register rejects invalid email", "Authentication", "/api/auth/register", "POST", response, HttpStatusCode.BadRequest, request);
    }

    private async Task<TestResult> RegisterOwner()
    {
        var response = await RegisterAsync("owner");
        return Assert("Register unique owner", "Authentication", "/api/auth/register", "POST", response, HttpStatusCode.Created);
    }

    private async Task<TestResult> CreateCategory()
    {
        if (_context.Owner is null) return Skip("Create category through HTTP", "Categories", "/api/categories", "POST", "Owner setup failed.");
        if (_options.CategoryId.HasValue)
        {
            _context.CategoryId = _options.CategoryId;
            return Pass("Create category through HTTP", "Categories", "/api/categories", "POST", "Supplied category ID", "Using the optional supplied category ID.");
        }

        var request = new { name = $"Category {_runId}" };
        var response = await _api.SendAsync(HttpMethod.Post, "/api/categories", request, _context.Owner.AccessToken);
        if (response.Is(HttpStatusCode.Created))
            _context.CategoryId = ApiClient.RequiredGuid(ApiClient.Json(response.Body), "id");
        return Assert("Create category through HTTP", "Categories", "/api/categories", "POST", response, HttpStatusCode.Created, request);
    }

    private async Task<TestResult> ProvisionUserPool()
    {
        if (_context.Owner is null) return Skip("Provision deterministic user pool", "Data Setup", "/api/auth/register", "POST", "Owner setup failed.");
        for (var index = 1; index < _options.Users; index++)
        {
            var response = await CreateSessionAsync($"member_{index:00}");
            if (!response.Is(HttpStatusCode.Created))
                return Assert("Provision deterministic user pool", "Data Setup", "/api/auth/register", "POST", response, HttpStatusCode.Created, workflow: "Data setup", step: 1);
        }
        return Pass("Provision deterministic user pool", "Data Setup", "/api/auth/register", "POST", "201", "Created configured user sessions.", "Data setup", 1);
    }

    private async Task<TestResult> DuplicateRegister()
    {
        if (_context.Owner is null) return Skip("Register duplicate email conflicts", "Authentication", "/api/auth/register", "POST", "Owner setup failed.");
        var request = new { email = _context.Owner.Email, password = _context.Owner.Password, firstName = "Owner", lastName = "Duplicate" };
        var response = await _api.SendAsync(HttpMethod.Post, "/api/auth/register", request);
        return Assert("Register duplicate email conflicts", "Authentication", "/api/auth/register", "POST", response, HttpStatusCode.Conflict, request);
    }

    private async Task<TestResult> InvalidLogin()
    {
        var request = new { email = _context.Owner?.Email ?? "nobody@example.test", password = "WrongPassword9" };
        var response = await _api.SendAsync(HttpMethod.Post, "/api/auth/login", request);
        return Assert("Login rejects invalid password", "Authentication", "/api/auth/login", "POST", response, HttpStatusCode.Unauthorized, request);
    }

    private async Task<TestResult> ValidLogin()
    {
        if (_context.Owner is null) return Skip("Login valid user", "Authentication", "/api/auth/login", "POST", "Owner setup failed.");
        var request = new { email = _context.Owner.Email, password = _context.Owner.Password };
        var response = await _api.SendAsync(HttpMethod.Post, "/api/auth/login", request);
        return Assert("Login valid user", "Authentication", "/api/auth/login", "POST", response, HttpStatusCode.OK, request);
    }

    private async Task<TestResult> InvalidRefresh()
    {
        var request = new { refreshToken = "not-a-valid-refresh-token" };
        var response = await _api.SendAsync(HttpMethod.Post, "/api/auth/refresh", request);
        return Assert("Refresh rejects malformed token", "Authentication", "/api/auth/refresh", "POST", response, HttpStatusCode.Unauthorized, request);
    }

    private async Task<TestResult> AnonymousProfile()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "/api/users/me");
        return Assert("Protected profile rejects anonymous", "Authorization", "/api/users/me", "GET", response, HttpStatusCode.Unauthorized);
    }

    private async Task<TestResult> CurrentProfile()
    {
        if (_context.Owner is null) return Skip("Current profile", "Users", "/api/users/me", "GET", "Owner setup failed.");
        var response = await _api.SendAsync(HttpMethod.Get, "/api/users/me", token: _context.Owner.AccessToken);
        return Assert("Current profile", "Users", "/api/users/me", "GET", response, HttpStatusCode.OK);
    }

    private async Task<TestResult> ProfileById()
    {
        if (_context.Owner is null) return Skip("Profile by ID", "Users", "/api/users/{id}/profile", "GET", "Owner setup failed.");
        var response = await _api.SendAsync(HttpMethod.Get, $"/api/users/{_context.Owner.UserId}/profile");
        return Assert("Profile by ID", "Users", "/api/users/{id}/profile", "GET", response, HttpStatusCode.OK);
    }

    private async Task<TestResult> InvalidProfile()
    {
        if (_context.Owner is null) return Skip("Update profile validation", "Users", "/api/users/profile", "PUT", "Owner setup failed.");
        var request = new { firstName = "", lastName = "", bio = "bio" };
        var response = await _api.SendAsync(HttpMethod.Put, "/api/users/profile", request, _context.Owner.AccessToken);
        return Assert("Update profile validation", "Users", "/api/users/profile", "PUT", response, HttpStatusCode.BadRequest, request);
    }

    private async Task<TestResult> UpdateProfile()
    {
        if (_context.Owner is null) return Skip("Update profile", "Users", "/api/users/profile", "PUT", "Owner setup failed.");
        var request = new { firstName = "Owner", lastName = _runId, bio = "Automated external API test." };
        var response = await _api.SendAsync(HttpMethod.Put, "/api/users/profile", request, _context.Owner.AccessToken);
        return Assert("Update profile", "Users", "/api/users/profile", "PUT", response, HttpStatusCode.OK, request);
    }

    private async Task<TestResult> EmptyAttachment()
    {
        if (_context.Owner is null) return Skip("Attachment upload validation", "Attachments", "/api/attachments", "POST", "Owner setup failed.");
        using var multipart = new MultipartFormDataContent();
        var response = await _api.SendAsync(HttpMethod.Post, "/api/attachments", token: _context.Owner.AccessToken, content: multipart);
        return Assert("Attachment upload validation", "Attachments", "/api/attachments", "POST", response, HttpStatusCode.BadRequest);
    }

    private async Task<TestResult> GroupWorkflow()
    {
        if (!await EnsureGroupAsync()) return Skip("Group workflow", "Groups", "/api/groups", "POST", CategorySetupMessage());
        var owner = _context.Owner!;
        var get = await _api.SendAsync(HttpMethod.Get, $"/api/groups/{_context.GroupId}", token: owner.AccessToken);
        if (!get.Is(HttpStatusCode.OK)) return Assert("Group workflow", "Groups", "/api/groups/{id}", "GET", get, HttpStatusCode.OK);
        var update = new { name = $"Updated {_runId}", description = "updated through HTTP", categoryId = _context.CategoryId, tagIds = Array.Empty<Guid>(), coverImageUrl = (string?)null };
        var updated = await _api.SendAsync(HttpMethod.Put, $"/api/groups/{_context.GroupId}", update, owner.AccessToken);
        return Assert("Group workflow", "Groups", "/api/groups/{id}", "PUT", updated, HttpStatusCode.OK, update, "Groups", 1);
    }

    private async Task<TestResult> PostWorkflow()
    {
        if (!await EnsureGroupAsync()) return Skip("Post workflow", "Posts", "/api/Posts/api/groups/{groupId}/posts", "POST", CategorySetupMessage());
        var owner = _context.Owner!;
        var invalid = await _api.SendAsync(HttpMethod.Post, $"/api/Posts/api/groups/{_context.GroupId}/posts", new { content = "", attachmentIds = Array.Empty<Guid>() }, owner.AccessToken);
        if (!invalid.Is(HttpStatusCode.BadRequest)) return Assert("Post workflow", "Posts", "/api/Posts/api/groups/{groupId}/posts", "POST", invalid, HttpStatusCode.BadRequest);
        if (!await EnsurePostAsync()) return Skip("Post workflow", "Posts", "/api/Posts/api/groups/{groupId}/posts", "POST", "Post creation failed.");
        var feed = await _api.SendAsync(HttpMethod.Get, $"/api/Posts/api/groups/{_context.GroupId}/posts?skip=0&take=20", token: owner.AccessToken);
        if (!feed.Is(HttpStatusCode.OK)) return Assert("Post workflow", "Posts", "/api/Posts/api/groups/{groupId}/posts", "GET", feed, HttpStatusCode.OK);
        var get = await _api.SendAsync(HttpMethod.Get, $"/api/Posts/api/posts/{_context.PostId}", token: owner.AccessToken);
        if (!get.Is(HttpStatusCode.OK)) return Assert("Post workflow", "Posts", "/api/Posts/api/posts/{id}", "GET", get, HttpStatusCode.OK);
        var update = await _api.SendAsync(HttpMethod.Put, $"/api/Posts/api/posts/{_context.PostId}", new { content = "Updated test post" }, owner.AccessToken);
        return Assert("Post workflow", "Posts", "/api/Posts/api/posts/{id}", "PUT", update, HttpStatusCode.OK, new { content = "Updated test post" }, "Posts", 1);
    }

    private async Task<TestResult> CommentWorkflow()
    {
        if (!await EnsurePostAsync()) return Skip("Comment workflow", "Comments", "/api/Comments/api/posts/{postId}/comments", "POST", CategorySetupMessage());
        var owner = _context.Owner!;
        var created = await _api.SendAsync(HttpMethod.Post, $"/api/Comments/api/posts/{_context.PostId}/comments", new { content = "Automated test comment", parentCommentId = (Guid?)null }, owner.AccessToken);
        if (!created.Is(HttpStatusCode.Created)) return Assert("Comment workflow", "Comments", "/api/Comments/api/posts/{postId}/comments", "POST", created, HttpStatusCode.Created);
        _context.CommentId = ApiClient.RequiredGuid(ApiClient.Json(created.Body), "id");
        var list = await _api.SendAsync(HttpMethod.Get, $"/api/Comments/api/posts/{_context.PostId}/comments?skip=0&take=20", token: owner.AccessToken);
        if (!list.Is(HttpStatusCode.OK)) return Assert("Comment workflow", "Comments", "/api/Comments/api/posts/{postId}/comments", "GET", list, HttpStatusCode.OK);
        var update = await _api.SendAsync(HttpMethod.Put, $"/api/Comments/api/comments/{_context.CommentId}", new { content = "Updated automated comment" }, owner.AccessToken);
        if (!update.Is(HttpStatusCode.OK)) return Assert("Comment workflow", "Comments", "/api/Comments/api/comments/{id}", "PUT", update, HttpStatusCode.OK);
        var like = await _api.SendAsync(HttpMethod.Post, $"/api/Comments/api/comments/{_context.CommentId}/like", token: owner.AccessToken);
        if (!like.Is(HttpStatusCode.OK)) return Assert("Comment workflow", "Comments", "/api/Comments/api/comments/{id}/like", "POST", like, HttpStatusCode.OK);
        var unlike = await _api.SendAsync(HttpMethod.Delete, $"/api/Comments/api/comments/{_context.CommentId}/like", token: owner.AccessToken);
        return Assert("Comment workflow", "Comments", "/api/Comments/api/comments/{id}/like", "DELETE", unlike, HttpStatusCode.OK, workflow: "Comments", step: 1);
    }

    private async Task<TestResult> NotificationWorkflow()
    {
        if (_context.Owner is null) return Skip("Notification workflow", "Notifications", "/api/notifications", "GET", "Owner setup failed.");
        var response = await _api.SendAsync(HttpMethod.Get, "/api/notifications?skip=0&take=20", token: _context.Owner.AccessToken);
        return Assert("Notification workflow", "Notifications", "/api/notifications", "GET", response, HttpStatusCode.OK, workflow: "Notifications", step: 1);
    }

    private async Task<TestResult> ReportWorkflow()
    {
        if (!await EnsurePostAsync()) return Skip("Report workflow", "Reports", "/api/reports", "POST", CategorySetupMessage());
        var owner = _context.Owner!;
        var request = new { targetType = 1, targetId = _context.PostId, reason = "Automated test report reason." };
        var create = await _api.SendAsync(HttpMethod.Post, "/api/reports", request, owner.AccessToken);
        if (!create.Is(HttpStatusCode.Created)) return Assert("Report workflow", "Reports", "/api/reports", "POST", create, HttpStatusCode.Created, request);
        _context.ReportId = ApiClient.RequiredGuid(ApiClient.Json(create.Body), "id");
        var list = await _api.SendAsync(HttpMethod.Get, "/api/reports?skip=0&take=20", token: owner.AccessToken);
        if (!list.Is(HttpStatusCode.OK)) return Assert("Report workflow", "Reports", "/api/reports", "GET", list, HttpStatusCode.OK);
        var resolve = await _api.SendAsync(HttpMethod.Put, $"/api/reports/{_context.ReportId}/resolve", new { status = 2 }, owner.AccessToken);
        return Assert("Report workflow", "Reports", "/api/reports/{id}/resolve", "PUT", resolve, HttpStatusCode.OK, new { status = 2 }, "Reports", 1);
    }

    private async Task<TestResult> AttachmentWorkflow()
    {
        if (_context.Owner is null) return Skip("Attachment workflow", "Attachments", "/api/attachments", "POST", "Owner setup failed.");
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent("ConnectHub automated test"u8.ToArray()) { Headers = { ContentType = new("text/plain") } }, "file", "test.txt");
        var upload = await _api.SendAsync(HttpMethod.Post, "/api/attachments", token: _context.Owner.AccessToken, content: multipart);
        if (!upload.Is(HttpStatusCode.Created)) return Assert("Attachment workflow", "Attachments", "/api/attachments", "POST", upload, HttpStatusCode.Created);
        _context.AttachmentId = ApiClient.RequiredGuid(ApiClient.Json(upload.Body), "id");
        var delete = await _api.SendAsync(HttpMethod.Delete, $"/api/attachments/{_context.AttachmentId}", token: _context.Owner.AccessToken);
        return Assert("Attachment workflow", "Attachments", "/api/attachments/{id}", "DELETE", delete, HttpStatusCode.OK, workflow: "Attachments", step: 1);
    }

    private async Task<TestResult> GenerateDataset()
    {
        if (!await EnsureGroupAsync()) return Skip("Generate configured group/post/comment dataset", "Data Setup", "/api/groups", "POST", CategorySetupMessage());
        var owner = _context.Owner!;
        for (var groupIndex = 1; groupIndex < _options.Groups; groupIndex++)
        {
            var groupResponse = await CreateGroupAsync($"Group {groupIndex} {_runId}", $"Generated dataset group {groupIndex}", _context.CategoryId!.Value, owner);
            if (!groupResponse.Is(HttpStatusCode.Created)) return Assert("Generate configured group/post/comment dataset", "Data Setup", "/api/groups", "POST", groupResponse, HttpStatusCode.Created, workflow: "Data setup", step: 2);
        }
        for (var postIndex = 1; postIndex <= _options.Posts; postIndex++)
        {
            var post = new { content = $"Generated post {postIndex} for {_runId}", attachmentIds = Array.Empty<Guid>() };
            var postResponse = await _api.SendAsync(HttpMethod.Post, $"/api/Posts/api/groups/{_context.GroupId}/posts", post, owner.AccessToken);
            if (!postResponse.Is(HttpStatusCode.Created)) return Assert("Generate configured group/post/comment dataset", "Data Setup", "/api/Posts/api/groups/{groupId}/posts", "POST", postResponse, HttpStatusCode.Created, post, "Data setup", 2);
            var postId = ApiClient.RequiredGuid(ApiClient.Json(postResponse.Body), "id");
            var commentCount = Math.Max(1, _options.Comments / _options.Posts);
            for (var commentIndex = 1; commentIndex <= commentCount; commentIndex++)
            {
                var comment = new { content = $"Generated comment {commentIndex} for post {postIndex}", parentCommentId = (Guid?)null };
                var commentResponse = await _api.SendAsync(HttpMethod.Post, $"/api/Comments/api/posts/{postId}/comments", comment, owner.AccessToken);
                if (!commentResponse.Is(HttpStatusCode.Created)) return Assert("Generate configured group/post/comment dataset", "Data Setup", "/api/Comments/api/posts/{postId}/comments", "POST", commentResponse, HttpStatusCode.Created, comment, "Data setup", 2);
            }
        }
        return Pass("Generate configured group/post/comment dataset", "Data Setup", "/api/groups", "POST", "201", $"Created {_options.Groups} groups, {_options.Posts} posts, and approximately {_options.Comments} comments.", "Data setup", 2);
    }

    private async Task<TestResult> GenerateMockData()
    {
        if (_context.Owner is null)
            return Skip("Create comprehensive mock dataset", "Mock Data", "multiple endpoints", "HTTP", "Owner setup failed.");

        while (_context.Users.Count < _options.Users)
        {
            var response = await CreateSessionAsync($"mock_user_{_context.Users.Count:000}");
            if (!response.Is(HttpStatusCode.Created))
                return Assert("Create comprehensive mock dataset", "Mock Data", "/api/auth/register", "POST", response, HttpStatusCode.Created);
        }

        var categoryIds = new List<Guid>();
        if (_options.CategoryId.HasValue)
            categoryIds.Add(_options.CategoryId.Value);
        else
        {
            for (var index = 0; index < _options.Categories; index++)
            {
                var request = new { name = $"Mock Category {index + 1:000} {_runId}" };
                var response = await _api.SendAsync(HttpMethod.Post, "/api/categories", request, _context.Owner.AccessToken);
                if (!response.Is(HttpStatusCode.Created))
                    return Assert("Create comprehensive mock dataset", "Mock Data", "/api/categories", "POST", response, HttpStatusCode.Created, request);
                categoryIds.Add(ApiClient.RequiredGuid(ApiClient.Json(response.Body), "id"));
            }
        }

        var groups = new List<MockGroup>();
        for (var index = 0; index < _options.Groups; index++)
        {
            var owner = _context.Users[index % _context.Users.Count];
            var response = await CreateGroupAsync(
                $"Mock Group {index + 1:0000} {_runId}",
                $"Mock community {index + 1} created through the ConnectHub HTTP API.",
                categoryIds[index % categoryIds.Count],
                owner);
            if (!response.Is(HttpStatusCode.Created))
                return Assert("Create comprehensive mock dataset", "Mock Data", "/api/groups", "POST", response, HttpStatusCode.Created);

            var groupId = ApiClient.RequiredGuid(ApiClient.Json(response.Body), "id");
            var members = new List<UserSession> { owner };
            var targetMembers = Math.Min(_options.MembersPerGroup, _context.Users.Count - 1);
            for (var memberOffset = 1; memberOffset <= targetMembers; memberOffset++)
            {
                var member = _context.Users[(index + memberOffset) % _context.Users.Count];
                if (member.UserId == owner.UserId) continue;
                var join = await _api.SendAsync(HttpMethod.Post, $"/api/groups/{groupId}/join", token: member.AccessToken);
                if (!join.Is(HttpStatusCode.OK))
                    return Assert("Create comprehensive mock dataset", "Mock Data", "/api/groups/{id}/join", "POST", join, HttpStatusCode.OK);
                members.Add(member);
            }

            if (members.Count > 1)
            {
                var promote = await _api.SendAsync(HttpMethod.Put, $"/api/groups/{groupId}/members/{members[1].UserId}/role", new { role = 2 }, owner.AccessToken);
                if (!promote.Is(HttpStatusCode.OK))
                    return Assert("Create comprehensive mock dataset", "Mock Data", "/api/groups/{id}/members/{userId}/role", "PUT", promote, HttpStatusCode.OK);
            }

            groups.Add(new MockGroup(groupId, owner, members));
        }

        var posts = new List<MockPost>();
        for (var index = 0; index < _options.Posts; index++)
        {
            var group = groups[index % groups.Count];
            var author = group.Members[index % group.Members.Count];
            var request = new { content = $"Mock post {index + 1:000000} for {group.Id} generated through HTTP.", attachmentIds = Array.Empty<Guid>() };
            var response = await _api.SendAsync(HttpMethod.Post, $"/api/Posts/api/groups/{group.Id}/posts", request, author.AccessToken);
            if (!response.Is(HttpStatusCode.Created))
                return Assert("Create comprehensive mock dataset", "Mock Data", "/api/Posts/api/groups/{groupId}/posts", "POST", response, HttpStatusCode.Created, request);
            posts.Add(new MockPost(ApiClient.RequiredGuid(ApiClient.Json(response.Body), "id"), group, author));
        }

        foreach (var post in posts)
        {
            var likerCount = Math.Min(_options.LikesPerPost, post.Group.Members.Count - 1);
            for (var offset = 1; offset <= likerCount; offset++)
            {
                var liker = post.Group.Members[(MemberIndex(post.Group.Members, post.Author) + offset) % post.Group.Members.Count];
                var like = await _api.SendAsync(HttpMethod.Post, $"/api/Posts/api/posts/{post.Id}/like", token: liker.AccessToken);
                if (!like.Is(HttpStatusCode.OK))
                    return Assert("Create comprehensive mock dataset", "Mock Data", "/api/Posts/api/posts/{id}/like", "POST", like, HttpStatusCode.OK);
            }
        }

        var comments = new List<MockComment>();
        var rootsByPost = new Dictionary<Guid, List<MockComment>>();
        for (var index = 0; index < _options.Comments; index++)
        {
            var post = posts[index % posts.Count];
            var author = post.Group.Members[(index + 1) % post.Group.Members.Count];
            rootsByPost.TryGetValue(post.Id, out var roots);
            var useReply = index % 5 == 4 && roots is { Count: > 0 };
            var parentCommentId = useReply ? roots![index % roots.Count].Id : (Guid?)null;
            var request = new { content = $"Mock comment {index + 1:000000} generated through HTTP.", parentCommentId };
            var response = await _api.SendAsync(HttpMethod.Post, $"/api/Comments/api/posts/{post.Id}/comments", request, author.AccessToken);
            if (!response.Is(HttpStatusCode.Created))
                return Assert("Create comprehensive mock dataset", "Mock Data", "/api/Comments/api/posts/{postId}/comments", "POST", response, HttpStatusCode.Created, request);
            var comment = new MockComment(ApiClient.RequiredGuid(ApiClient.Json(response.Body), "id"), post, author);
            comments.Add(comment);
            if (!useReply)
            {
                if (!rootsByPost.TryGetValue(post.Id, out roots)) rootsByPost[post.Id] = roots = [];
                roots.Add(comment);
            }
        }

        foreach (var comment in comments)
        {
            var likerCount = Math.Min(_options.LikesPerComment, comment.Post.Group.Members.Count - 1);
            for (var offset = 1; offset <= likerCount; offset++)
            {
                var liker = comment.Post.Group.Members[(MemberIndex(comment.Post.Group.Members, comment.Author) + offset) % comment.Post.Group.Members.Count];
                var like = await _api.SendAsync(HttpMethod.Post, $"/api/Comments/api/comments/{comment.Id}/like", token: liker.AccessToken);
                if (!like.Is(HttpStatusCode.OK))
                    return Assert("Create comprehensive mock dataset", "Mock Data", "/api/Comments/api/comments/{id}/like", "POST", like, HttpStatusCode.OK);
            }
        }

        for (var index = 0; index < Math.Min(_options.Reports, posts.Count); index++)
        {
            var post = posts[index];
            var reporter = post.Group.Members[(MemberIndex(post.Group.Members, post.Author) + 1) % post.Group.Members.Count];
            var request = new { targetType = 1, targetId = post.Id, reason = $"Mock moderation report {index + 1:0000}." };
            var response = await _api.SendAsync(HttpMethod.Post, "/api/reports", request, reporter.AccessToken);
            if (!response.Is(HttpStatusCode.Created))
                return Assert("Create comprehensive mock dataset", "Mock Data", "/api/reports", "POST", response, HttpStatusCode.Created, request);
        }

        return Pass("Create comprehensive mock dataset", "Mock Data", "multiple endpoints", "HTTP", "201", $"Created {_context.Users.Count} users, {categoryIds.Count} categories, {groups.Count} groups, {posts.Count} posts, {comments.Count} comments, post/comment likes, memberships, admins, and {Math.Min(_options.Reports, posts.Count)} reports.", "Mock data", 1);
    }

    private async Task<ApiResponse> RegisterAsync(string label)
    {
        var response = await CreateSessionAsync(label);
        if (response.Is(HttpStatusCode.Created)) _context.Owner = _context.Users.Last();
        return response;
    }

    private async Task<ApiResponse> CreateSessionAsync(string label)
    {
        var email = $"{label}_{_runId}_{_random.Next(100000, 999999)}@example.test";
        const string password = "TestPassword9";
        var request = new { email, password, firstName = label, lastName = "Runner" };
        var response = await _api.SendAsync(HttpMethod.Post, "/api/auth/register", request);
        if (response.Is(HttpStatusCode.Created))
        {
            var body = ApiClient.Json(response.Body);
            _context.Users.Add(new UserSession { UserId = ApiClient.RequiredGuid(body, "userId"), Email = email, Password = password, AccessToken = ApiClient.RequiredString(body, "accessToken"), RefreshToken = ApiClient.RequiredString(body, "refreshToken") });
        }
        return response;
    }

    private async Task<bool> EnsureGroupAsync()
    {
        if (_context.GroupId.HasValue) return true;
        if (_context.Owner is null) return false;
        if (!_context.CategoryId.HasValue)
        {
            if (_options.CategoryId.HasValue)
                _context.CategoryId = _options.CategoryId;
            else if (!await EnsureCategoryAsync())
                return false;
        }
        var response = await CreateGroupAsync($"Group {_runId}", "External HTTP test group", _context.CategoryId!.Value, _context.Owner);
        if (!response.Is(HttpStatusCode.Created)) return false;
        _context.GroupId = ApiClient.RequiredGuid(ApiClient.Json(response.Body), "id");
        return true;
    }

    private async Task<ApiResponse> CreateGroupAsync(string name, string description, Guid categoryId, UserSession owner)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(name), "Name" },
            { new StringContent(description), "Description" },
            { new StringContent(categoryId.ToString()), "CategoryId" }
        };

        return await _api.SendAsync(HttpMethod.Post, "/api/groups", token: owner.AccessToken, content: form);
    }

    private async Task<bool> EnsureCategoryAsync()
    {
        if (_context.CategoryId.HasValue) return true;
        if (_context.Owner is null) return false;

        var response = await _api.SendAsync(HttpMethod.Post, "/api/categories", new { name = $"Category {_runId}" }, _context.Owner.AccessToken);
        if (!response.Is(HttpStatusCode.Created)) return false;
        _context.CategoryId = ApiClient.RequiredGuid(ApiClient.Json(response.Body), "id");
        return true;
    }

    private async Task<bool> EnsurePostAsync()
    {
        if (_context.PostId.HasValue) return true;
        if (!await EnsureGroupAsync()) return false;
        var response = await _api.SendAsync(HttpMethod.Post, $"/api/Posts/api/groups/{_context.GroupId}/posts", new { content = "Automated external API test post", attachmentIds = Array.Empty<Guid>() }, _context.Owner!.AccessToken);
        if (!response.Is(HttpStatusCode.Created)) return false;
        _context.PostId = ApiClient.RequiredGuid(ApiClient.Json(response.Body), "id");
        return true;
    }

    private string CategorySetupMessage() => "Skipped: category creation through HTTP failed.";

    private static int MemberIndex(IReadOnlyList<UserSession> members, UserSession member)
    {
        for (var index = 0; index < members.Count; index++)
        {
            if (members[index].UserId == member.UserId)
                return index;
        }

        return 0;
    }

    private static TestResult Assert(string name, string category, string endpoint, string method, ApiResponse response, HttpStatusCode expected, object? request = null, string? workflow = null, int? step = null) =>
        new()
        {
            Name = name, Category = category, Endpoint = endpoint, Method = method,
            Expected = ((int)expected).ToString(), Actual = ((int)response.StatusCode).ToString(),
            Outcome = response.Is(expected) ? "Passed" : "Failed", DurationMs = response.DurationMs,
            Request = request is null ? null : JsonSerializer.Serialize(request), Response = response.Body,
            Error = response.Is(expected) ? null : "Unexpected HTTP status. Investigate the API controller, validator, or BLL result mapping.", Workflow = workflow, Step = step
        };

    private static TestResult Pass(string name, string category, string endpoint, string method, string actual, string message, string? workflow = null, int? step = null) =>
        new() { Name = name, Category = category, Endpoint = endpoint, Method = method, Expected = "Success", Actual = actual, Outcome = "Passed", DurationMs = 0, Error = message, Workflow = workflow, Step = step, TestType = "Workflow" };

    private static TestResult Skip(string name, string category, string endpoint, string method, string reason) =>
        new() { Name = name, Category = category, Endpoint = endpoint, Method = method, Expected = "n/a", Actual = "n/a", Outcome = "Skipped", DurationMs = 0, Error = reason };
}
