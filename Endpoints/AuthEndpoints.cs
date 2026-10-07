using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server_Inventory.Contracts;
using Server_Inventory.Security;

namespace Server_Inventory.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<CsrfValidationFilter>();

        auth.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return TypedResults.Ok(new CsrfTokenResponse(
                tokens.RequestToken ?? throw new InvalidOperationException("The antiforgery request token was not created.")));
        })
        .WithName("GetCsrfToken")
        .WithSummary("Get a CSRF request token")
        .WithDescription("Issues the antiforgery cookie and returns the matching request token.")
        .Produces<CsrfTokenResponse>(StatusCodes.Status200OK);

        auth.MapPost("/login", async Task<Results<NoContent, UnauthorizedHttpResult>> (
            LoginRequest request,
            HttpContext context,
            IOptions<AdminCredentials> credentials,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Authentication");
            if (!CredentialsMatch(request.Username, request.Password, credentials.Value))
            {
                logger.LogWarning("Cookie sign-in failed for {Username} from {RemoteIp}",
                    request.Username, context.Connection.RemoteIpAddress);
                return TypedResults.Unauthorized();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, credentials.Value.Username),
                new Claim(ClaimTypes.Name, credentials.Value.Username),
                new Claim(ClaimTypes.Role, "Administrator")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false });
            logger.LogInformation("Administrator {Username} signed in using a browser session", credentials.Value.Username);

            return TypedResults.NoContent();
        })
        .WithName("Login")
        .WithSummary("Sign in as the administrator")
        .WithDescription("Validates the configured administrator credentials and starts an HTTP-only cookie session.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .RequireRateLimiting("login");

        app.MapPost("/api/auth/token", Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult> (
            LoginRequest request,
            HttpContext context,
            IOptions<AdminCredentials> credentials,
            IOptions<JwtOptions> jwtOptions,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Authentication");
            if (!CredentialsMatch(request.Username, request.Password, credentials.Value))
            {
                logger.LogWarning("JWT sign-in failed for {Username} from {RemoteIp}",
                    request.Username, context.Connection.RemoteIpAddress);
                return TypedResults.Unauthorized();
            }

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(jwtOptions.Value.AccessTokenMinutes);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, credentials.Value.Username),
                new Claim(ClaimTypes.Name, credentials.Value.Username),
                new Claim(ClaimTypes.Role, "Administrator"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.SigningKey));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: jwtOptions.Value.Issuer,
                audience: jwtOptions.Value.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: signingCredentials);
            var encodedToken = new JwtSecurityTokenHandler().WriteToken(token);

            logger.LogInformation("JWT access token issued for {Username}", credentials.Value.Username);
            return TypedResults.Ok(new AccessTokenResponse(encodedToken, "Bearer", expiresAt));
        })
        .WithName("CreateAccessToken")
        .WithTags("Authentication")
        .WithSummary("Create a JWT access token")
        .WithDescription("Validates the administrator credentials and returns a short-lived bearer token for API clients.")
        .Produces<AccessTokenResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .RequireRateLimiting("login");

        auth.MapGet("/me", (ClaimsPrincipal principal) =>
            TypedResults.Ok(new AuthenticatedUserResponse(
                principal.Identity?.Name ?? throw new InvalidOperationException("The authenticated identity has no username.")))
        )
        .WithName("GetCurrentUser")
        .WithSummary("Get the current administrator")
        .WithDescription("Returns the username associated with the authenticated session.")
        .Produces<AuthenticatedUserResponse>(StatusCodes.Status200OK)
        .RequireAuthorization();

        auth.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return TypedResults.NoContent();
        })
        .WithName("Logout")
        .WithSummary("End the current session")
        .WithDescription("Signs out the current administrator and expires the session cookie.")
        .Produces(StatusCodes.Status204NoContent)
        .RequireAuthorization();
    }

    private static bool CredentialsMatch(string username, string password, AdminCredentials expected)
    {
        var usernameMatches = string.Equals(username, expected.Username, StringComparison.Ordinal);
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected.Password));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash) && usernameMatches;
    }
}
