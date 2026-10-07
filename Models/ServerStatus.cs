namespace Server_Inventory.Models;

/// <summary>Describes the recorded operational state of a server.</summary>
public enum ServerStatus
{
    /// <summary>The operational state has not been confirmed.</summary>
    Unknown,

    /// <summary>The server is recorded as operating normally.</summary>
    Running,

    /// <summary>The server is recorded as stopped.</summary>
    Stopped,

    /// <summary>The server is undergoing planned maintenance.</summary>
    Maintenance
}
