using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Server_Inventory.Contracts;
using Server_Inventory.Models;

namespace Server_Inventory.Tests;

public sealed class ServerApiIntegrationTests(ServerApiFactory factory)
    : IClassFixture<ServerApiFactory>
{
    [Fact]
    public async Task Jwt_authentication_protects_server_api_and_allows_inventory_management()
    {
        using var client = factory.CreateClient();

        var anonymousResponse = await client.GetAsync("/api/servers/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var invalidTokenResponse = await client.PostAsJsonAsync(
            "/api/auth/token",
            new LoginRequest { Username = ServerApiFactory.Username, Password = "incorrect-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, invalidTokenResponse.StatusCode);

        var tokenResponse = await client.PostAsJsonAsync(
            "/api/auth/token",
            new LoginRequest
            {
                Username = ServerApiFactory.Username,
                Password = ServerApiFactory.Password
            });
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var token = await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.NotNull(token);
        Assert.Equal("Bearer", token.TokenType);
        Assert.True(token.ExpiresAt > DateTimeOffset.UtcNow);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");
        var rejectedToken = await client.GetAsync("/api/servers/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var summaryResponse = await client.GetAsync("/api/servers/summary");
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);

        var createdResponse = await client.PostAsJsonAsync(
            "/api/servers",
            new SaveServerRequest
            {
                Name = "Integration server",
                Hostname = "integration.example.test",
                OperatingSystem = "Ubuntu 24.04",
                Environment = "Test",
                Status = ServerStatus.Running
            });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var created = await createdResponse.Content.ReadFromJsonAsync<ServerResponse>(jsonOptions);
        Assert.NotNull(created);

        var readResponse = await client.GetAsync($"/api/servers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/servers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var deletedResponse = await client.GetAsync($"/api/servers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }
    [Fact]
    public async Task Cookie_sessions_require_csrf_tokens_for_writes()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var csrfResponse = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        var csrf = await csrfResponse.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        Assert.NotNull(csrf);

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Username = ServerApiFactory.Username,
                Password = ServerApiFactory.Password
            })
        };
        loginRequest.Headers.Add("X-CSRF-TOKEN", csrf.RequestToken);
        var loginResponse = await client.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.NoContent, loginResponse.StatusCode);

        var unauthorizedWrite = await client.PostAsJsonAsync(
            "/api/servers",
            new SaveServerRequest
            {
                Name = "Cookie server",
                Hostname = "cookie.example.test",
                OperatingSystem = "Ubuntu 24.04",
                Environment = "Test"
            });
        Assert.Equal(HttpStatusCode.BadRequest, unauthorizedWrite.StatusCode);

        var freshCsrfResponse = await client.GetAsync("/api/auth/csrf");
        var freshCsrf = await freshCsrfResponse.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        Assert.NotNull(freshCsrf);
        using var authorizedWrite = new HttpRequestMessage(HttpMethod.Post, "/api/servers")
        {
            Content = JsonContent.Create(new SaveServerRequest
            {
                Name = "Cookie server",
                Hostname = "cookie.example.test",
                OperatingSystem = "Ubuntu 24.04",
                Environment = "Test"
            })
        };
        authorizedWrite.Headers.Add("X-CSRF-TOKEN", freshCsrf.RequestToken);
        var created = await client.SendAsync(authorizedWrite);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task OpenApi_and_interactive_documentation_are_available_in_development()
    {
        using var client = factory.CreateClient();

        var openApi = await client.GetAsync("/openapi/v1.json");
        var openApiBody = await openApi.Content.ReadAsStringAsync();
        Assert.True(openApi.StatusCode == HttpStatusCode.OK, $"OpenAPI returned {(int)openApi.StatusCode}: {openApiBody}");
        Assert.Contains("/api/auth/token", openApiBody, StringComparison.Ordinal);

        var documentationUi = await client.GetAsync("/scalar/v1");
        var documentationHtml = await documentationUi.Content.ReadAsStringAsync();
        Assert.True(
            documentationUi.StatusCode == HttpStatusCode.OK,
            $"Scalar UI returned {(int)documentationUi.StatusCode}: {documentationHtml}");
        Assert.Contains("Scalar", documentationHtml, StringComparison.OrdinalIgnoreCase);
    }

}
