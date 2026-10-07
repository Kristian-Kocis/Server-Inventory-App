using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Server_Inventory.Contracts;
using Server_Inventory.Data;
using Server_Inventory.Models;
using Server_Inventory.Services;

namespace Server_Inventory.Tests;

public sealed class ServerServiceTests
{
    [Fact]
    public async Task Create_search_update_and_delete_manage_server_records()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new ServerDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var service = new ServerService(dbContext, NullLogger<ServerService>.Instance);
        var request = new SaveServerRequest
        {
            Name = "Unit test server",
            Hostname = "unit.example.test",
            OperatingSystem = "Debian 12",
            Environment = "Test",
            Status = ServerStatus.Running
        };

        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.Id > 0);

        var searchResults = await service.GetAllAsync("unit", null, CancellationToken.None);
        Assert.Single(searchResults);
        Assert.Equal(created.Id, searchResults[0].Id);

        var updated = await service.UpdateAsync(
            created.Id,
            request with { Status = ServerStatus.Maintenance },
            CancellationToken.None);
        Assert.Equal(ServerStatus.Maintenance, updated?.Status);

        Assert.True(await service.DeleteAsync(created.Id, CancellationToken.None));
        Assert.Null(await service.GetByIdAsync(created.Id, CancellationToken.None));
    }
}
