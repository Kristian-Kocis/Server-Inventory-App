using System.ComponentModel.DataAnnotations;
using Server_Inventory.Models;

namespace Server_Inventory.Contracts;

/// <summary>Fields used to add or replace a server inventory entry.</summary>
public sealed record SaveServerRequest
{
    /// <summary>A friendly name for the server.</summary>
    [Required, StringLength(120)]
    public required string Name { get; init; }

    /// <summary>The network hostname of the server.</summary>
    [Required, StringLength(253)]
    public required string Hostname { get; init; }

    /// <summary>The server's IP address, when known.</summary>
    [StringLength(45)]
    public string? IpAddress { get; init; }

    /// <summary>The installed operating system.</summary>
    [Required, StringLength(120)]
    public required string OperatingSystem { get; init; }

    /// <summary>The environment where the server is used.</summary>
    [Required, StringLength(80)]
    public required string Environment { get; init; }

    /// <summary>The recorded operational state.</summary>
    [EnumDataType(typeof(ServerStatus))]
    public ServerStatus Status { get; init; } = ServerStatus.Unknown;

    /// <summary>The datacenter or region where the server is located.</summary>
    [StringLength(120)]
    public string? Location { get; init; }

    /// <summary>Additional inventory notes.</summary>
    [StringLength(2000)]
    public string? Notes { get; init; }
}
