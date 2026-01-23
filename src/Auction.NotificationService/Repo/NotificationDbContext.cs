using Microsoft.EntityFrameworkCore;

namespace Auction.NotificationService.Repo;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }
    public DbSet<EventEntity> Events { get; set; } = null!;
}
