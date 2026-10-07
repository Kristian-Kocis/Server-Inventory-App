using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Server_Inventory.Data;

namespace Server_Inventory.Tests;

public sealed class ServerApiFactory : WebApplicationFactory<Program>
{
    public const string Username = "test-admin";
    public const string Password = "integration-test-password-at-least-16";
    public const string JwtSigningKey = "integration-test-signing-key-at-least-32-bytes-long";

    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"server-inventory-tests-{Guid.NewGuid():N}.db");
    private string TestConnectionString => $"Data Source={databasePath};Pooling=False";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Username"] = Username,
                ["Authentication:Password"] = Password,
                ["Jwt:Issuer"] = "ServerInventory.Tests",
                ["Jwt:Audience"] = "ServerInventory.Tests.Client",
                ["Jwt:SigningKey"] = JwtSigningKey,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:ServerInventory"] = TestConnectionString
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ServerDbContext>>();
            services.RemoveAll<ServerDbContext>();
            services.AddDbContext<ServerDbContext>(options =>
                options.UseSqlite(TestConnectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            var path = databasePath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
