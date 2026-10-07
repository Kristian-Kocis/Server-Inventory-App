using Microsoft.AspNetCore.Http.HttpResults;
using Server_Inventory.Contracts;
using Server_Inventory.Models;
using Server_Inventory.Security;
using Server_Inventory.Services;

namespace Server_Inventory.Endpoints;

internal static class ServerEndpoints
{
    public static void MapServerEndpoints(this WebApplication app)
    {
        var servers = app.MapGroup("/api/servers")
            .WithTags("Servers")
            .RequireAuthorization()
            .AddEndpointFilter<CsrfValidationFilter>();

        servers.MapGet("/", async (
            string? search,
            ServerStatus? status,
            IServerService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAllAsync(search, status, cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("GetServers")
        .WithSummary("List servers")
        .WithDescription("Returns inventory entries, optionally filtered by a search term and operational status.");

        servers.MapGet("/summary", async (
            IServerService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetSummaryAsync(cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("GetServerSummary")
        .WithSummary("Get server inventory summary")
        .WithDescription("Returns the total number of inventory entries grouped by operational status.");

        servers.MapGet("/{id:int}", async Task<Results<Ok<ServerResponse>, NotFound>> (
            int id,
            IServerService service,
            CancellationToken cancellationToken) =>
        {
            var server = await service.GetByIdAsync(id, cancellationToken);
            return server is null ? TypedResults.NotFound() : TypedResults.Ok(server);
        })
        .WithName("GetServerById")
        .WithSummary("Get a server")
        .WithDescription("Returns one inventory entry by its ID.")
        .Produces<ServerResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        servers.MapPost("/", async (
            SaveServerRequest request,
            IServerService service,
            CancellationToken cancellationToken) =>
        {
            var server = await service.CreateAsync(request, cancellationToken);
            return TypedResults.Created($"/api/servers/{server.Id}", server);
        })
        .WithName("CreateServer")
        .WithSummary("Add a server")
        .WithDescription("Adds a server to the inventory and returns its resource URL.")
        .RequireAuthorization("ManageServers")
        .Produces<ServerResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        servers.MapPut("/{id:int}", async Task<Results<Ok<ServerResponse>, NotFound>> (
            int id,
            SaveServerRequest request,
            IServerService service,
            CancellationToken cancellationToken) =>
        {
            var server = await service.UpdateAsync(id, request, cancellationToken);
            return server is null ? TypedResults.NotFound() : TypedResults.Ok(server);
        })
        .WithName("UpdateServer")
        .WithSummary("Update a server")
        .WithDescription("Replaces the stored details for one inventory entry.")
        .RequireAuthorization("ManageServers")
        .Produces<ServerResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        servers.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (
            int id,
            IServerService service,
            CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteAsync(id, cancellationToken);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        })
        .WithName("DeleteServer")
        .WithSummary("Remove a server")
        .WithDescription("Deletes one inventory entry.")
        .RequireAuthorization("ManageServers")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
