using System.ComponentModel.DataAnnotations;

namespace Server_Inventory.Contracts;

/// <summary>Credentials used to sign in to the inventory.</summary>
public sealed record LoginRequest
{
    /// <summary>The configured administrator username.</summary>
    [Required, StringLength(100)]
    public required string Username { get; init; }

    /// <summary>The configured administrator password.</summary>
    [Required, StringLength(1024, MinimumLength = 1)]
    public required string Password { get; init; }
}
