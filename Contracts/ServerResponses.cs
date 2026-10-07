using Server_Inventory.Models;

namespace Server_Inventory.Contracts;

/// <summary>Server details stored in the inventory.</summary>
/// <param name="Id">The unique inventory ID.</param>
/// <param name="Name">A friendly name for the server.</param>
/// <param name="Hostname">The network hostname of the server.</param>
/// <param name="IpAddress">The server's IP address, when known.</param>
/// <param name="OperatingSystem">The installed operating system.</param>
/// <param name="Environment">The environment where the server is used.</param>
/// <param name="Status">The recorded operational state.</param>
/// <param name="Location">The datacenter or region where the server is located.</param>
/// <param name="Notes">Additional inventory notes.</param>
/// <param name="CreatedAt">When the inventory entry was created.</param>
/// <param name="UpdatedAt">When the inventory entry was last changed.</param>
public sealed record ServerResponse(
    int Id,
    string Name,
    string Hostname,
    string? IpAddress,
    string OperatingSystem,
    string Environment,
    ServerStatus Status,
    string? Location,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Counts of inventory entries grouped by operational status.</summary>
/// <param name="Total">The total number of inventory entries.</param>
/// <param name="Running">The number recorded as running.</param>
/// <param name="Stopped">The number recorded as stopped.</param>
/// <param name="Maintenance">The number recorded as under maintenance.</param>
/// <param name="Unknown">The number with an unconfirmed status.</param>
public sealed record ServerSummaryResponse(
    int Total,
    int Running,
    int Stopped,
    int Maintenance,
    int Unknown);
