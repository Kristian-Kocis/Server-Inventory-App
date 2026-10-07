using Microsoft.EntityFrameworkCore;
using Server_Inventory.Models;

namespace Server_Inventory.Data;

internal sealed class ServerDbContext(DbContextOptions<ServerDbContext> options) : DbContext(options)
{
    public DbSet<ServerEntity> Servers => Set<ServerEntity>();
}
