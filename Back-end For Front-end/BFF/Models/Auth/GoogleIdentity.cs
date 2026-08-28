namespace BFF.Models.Auth;

public sealed record GoogleIdentity(
    string Subject,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl);

public sealed record GoogleIdentityResult(GoogleIdentity? Identity, GoogleIdentityFailure Failure)
{
    public static GoogleIdentityResult Success(GoogleIdentity identity) => new(identity, GoogleIdentityFailure.None);
    public static GoogleIdentityResult AuthenticationFailed() => new(null, GoogleIdentityFailure.AuthenticationFailed);
    public static GoogleIdentityResult IncompleteProfile() => new(null, GoogleIdentityFailure.IncompleteProfile);
}

public enum GoogleIdentityFailure
{
    None,
    AuthenticationFailed,
    IncompleteProfile
}
