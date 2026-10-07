namespace Server_Inventory.Contracts;

/// <summary>A request token used to protect browser requests against CSRF.</summary>
/// <param name="RequestToken">The token to send in the X-CSRF-TOKEN header.</param>
public sealed record CsrfTokenResponse(string RequestToken);

/// <summary>The identity associated with the current authenticated session.</summary>
/// <param name="Username">The signed-in administrator username.</param>
public sealed record AuthenticatedUserResponse(string Username);

/// <summary>An access token for authenticating API requests.</summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="TokenType">The authorization scheme to use, normally Bearer.</param>
/// <param name="ExpiresAt">The UTC time when the access token expires.</param>
public sealed record AccessTokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);
