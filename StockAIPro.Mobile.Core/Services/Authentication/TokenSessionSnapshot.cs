namespace StockAIPro.Mobile.Services.Authentication;

/// <summary>
/// An access token, refresh token, and session id captured together as one
/// internally-consistent triple - see ITokenStore.GetSnapshotAsync for why
/// this must be obtained as a single atomic operation rather than three
/// independent Get*Async calls. Reading the three fields separately leaves
/// a window between each call in which a concurrent logout and/or login
/// could complete, producing a "torn" combination that belongs to no
/// session that ever actually existed (e.g. one account's access token
/// paired with a different account's session id) - exactly the shape of
/// bug this type exists to make structurally impossible.
/// </summary>
public sealed record TokenSessionSnapshot(string? AccessToken, string? RefreshToken, string? SessionId);
