using Microsoft.EntityFrameworkCore;
using Server_Inventory.Contracts;
using Server_Inventory.Data;
using Server_Inventory.Models;

namespace Server_Inventory.Services;

internal sealed class ServerService(
    ServerDbContext dbContext,
    ILogger<ServerService> logger) : IServerService
{
    public async Task<IReadOnlyList<ServerResponse>> GetAllAsync(
        string? search, ServerStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Servers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(server =>
                server.Name.ToLower().Contains(term) ||
                server.Hostname.ToLower().Contains(term) ||
                (server.IpAddress != null && server.IpAddress.ToLower().Contains(term)) ||
                server.OperatingSystem.ToLower().Contains(term) ||
                server.Environment.ToLower().Contains(term) ||
                (server.Location != null && server.Location.ToLower().Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(server => server.Status == status.Value);
        }

        var servers = await query
            .OrderBy(server => server.Name)
            .ToListAsync(cancellationToken);

        return servers.Select(ToResponse).ToArray();
    }

    public async Task<ServerResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var server = await dbContext.Servers
            .AsNoTracking()
            .SingleOrDefaultAsync(server => server.Id == id, cancellationToken);

        return server is null ? null : ToResponse(server);
    }

    public async Task<ServerSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var statuses = await dbContext.Servers
            .AsNoTracking()
            .GroupBy(server => server.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

        var running = statuses.GetValueOrDefault(ServerStatus.Running);
        var stopped = statuses.GetValueOrDefault(ServerStatus.Stopped);
        var maintenance = statuses.GetValueOrDefault(ServerStatus.Maintenance);
        var unknown = statuses.GetValueOrDefault(ServerStatus.Unknown);

        return new ServerSummaryResponse(
            statuses.Values.Sum(),
            running,
            stopped,
            maintenance,
            unknown);
    }

    public async Task<ServerResponse> CreateAsync(
        SaveServerRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var server = new ServerEntity
        {
            Name = request.Name.Trim(),
            Hostname = request.Hostname.Trim(),
            IpAddress = NormalizeOptional(request.IpAddress),
            OperatingSystem = request.OperatingSystem.Trim(),
            Environment = request.Environment.Trim(),
            Status = request.Status,
            Location = NormalizeOptional(request.Location),
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Servers.Add(server);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created server inventory entry {ServerId} for {ServerName}", server.Id, server.Name);
        return ToResponse(server);
    }

    public async Task<ServerResponse?> UpdateAsync(
        int id, SaveServerRequest request, CancellationToken cancellationToken)
    {
        var server = await dbContext.Servers
            .SingleOrDefaultAsync(server => server.Id == id, cancellationToken);

        if (server is null)
        {
            return null;
        }

        server.Name = request.Name.Trim();
        server.Hostname = request.Hostname.Trim();
        server.IpAddress = NormalizeOptional(request.IpAddress);
        server.OperatingSystem = request.OperatingSystem.Trim();
        server.Environment = request.Environment.Trim();
        server.Status = request.Status;
        server.Location = NormalizeOptional(request.Location);
        server.Notes = NormalizeOptional(request.Notes);
        server.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated server inventory entry {ServerId} for {ServerName}", server.Id, server.Name);
        return ToResponse(server);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var server = await dbContext.Servers
            .SingleOrDefaultAsync(server => server.Id == id, cancellationToken);

        if (server is null)
        {
            return false;
        }

        dbContext.Servers.Remove(server);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted server inventory entry {ServerId} for {ServerName}", server.Id, server.Name);
        return true;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ServerResponse ToResponse(ServerEntity server) => new(
        server.Id,
        server.Name,
        server.Hostname,
        server.IpAddress,
        server.OperatingSystem,
        server.Environment,
        server.Status,
        server.Location,
        server.Notes,
        server.CreatedAt,
        server.UpdatedAt);
}
