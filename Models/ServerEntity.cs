namespace Server_Inventory.Models;

internal sealed class ServerEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Hostname { get; set; }
    public string? IpAddress { get; set; }
    public required string OperatingSystem { get; set; }
    public required string Environment { get; set; }
    public ServerStatus Status { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
