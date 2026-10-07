using Server_Inventory.Contracts;
using Server_Inventory.Models;

namespace Server_Inventory.Services;

internal interface IServerService
{
    Task<IReadOnlyList<ServerResponse>> GetAllAsync(
        string? search, ServerStatus? status, CancellationToken cancellationToken);

    Task<ServerResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ServerSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
    Task<ServerResponse> CreateAsync(SaveServerRequest request, CancellationToken cancellationToken);
    Task<ServerResponse?> UpdateAsync(int id, SaveServerRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
