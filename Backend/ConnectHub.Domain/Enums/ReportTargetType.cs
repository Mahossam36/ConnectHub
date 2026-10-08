namespace ConnectHub.Models.Enums;

/// <summary>
/// Discriminates the type of content being reported.
/// Together with <c>TargetId</c>, it identifies the exact reported content without requiring
/// separate FK columns for each content type.
/// </summary>
public enum ReportTargetType
{
    Post = 1,
    Comment = 2,
}
