using System.Diagnostics;
using System.Net;

namespace ConnectHub.ApiTestRunner;

public sealed class RunnerOptions
{
    public required Uri BaseUrl { get; init; }
    public Guid? CategoryId { get; init; }
    public bool Verbose { get; init; }
    public bool AllowUntrustedDevelopmentCertificate { get; init; }
    public string? CategoryFilter { get; init; }
    public string? EndpointFilter { get; init; }
    public string? WorkflowFilter { get; init; }
    public int Users { get; init; } = 8;
    public int Groups { get; init; } = 3;
    public int Posts { get; init; } = 12;
    public int Comments { get; init; } = 18;
    public int Categories { get; init; } = 1;
    public int MembersPerGroup { get; init; } = 1;
    public int LikesPerPost { get; init; }
    public int LikesPerComment { get; init; }
    public int Reports { get; init; }
    public bool MockData { get; init; }
    public int Iterations { get; init; } = 1;
    public int Seed { get; init; }
    public int SlowRequestMs { get; init; } = 2_000;
}

public sealed record TestResult
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Endpoint { get; init; }
    public required string Method { get; init; }
    public required string Expected { get; init; }
    public required string Actual { get; init; }
    public required string Outcome { get; init; }
    public long DurationMs { get; init; }
    public string? Request { get; init; }
    public string? Response { get; init; }
    public string? Error { get; init; }
    public string? Workflow { get; init; }
    public int? Step { get; init; }
    public string? TestType { get; init; }
    public string? Actor { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ApiResponse(HttpStatusCode StatusCode, string Body, HttpResponseMessage Raw, long DurationMs)
{
    public bool Is(HttpStatusCode expected) => StatusCode == expected;
}

public sealed class TestCase
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Endpoint { get; init; }
    public required string Method { get; init; }
    public string? Workflow { get; init; }
    public int? Step { get; init; }
    public required Func<Task<TestResult>> Execute { get; init; }
}

public sealed class UserSession
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
}

public sealed class ScenarioContext
{
    public UserSession? Owner { get; set; }
    public UserSession? Member { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }
    public Guid? AttachmentId { get; set; }
    public Guid? NotificationId { get; set; }
    public Guid? ReportId { get; set; }
    public List<UserSession> Users { get; } = [];
}

public sealed record MockGroup(Guid Id, UserSession Owner, IReadOnlyList<UserSession> Members);
public sealed record MockPost(Guid Id, MockGroup Group, UserSession Author);
public sealed record MockComment(Guid Id, MockPost Post, UserSession Author);

public sealed record EndpointDefinition(string Method, string Route, bool Protected, string Area);
