using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace Auction.NotificationService.Repo;

public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<NotificationDbContext>();

        var projectDir = Directory.GetCurrentDirectory();
        var dataDir = Path.Combine(projectDir, "data");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "notifications.db");

        builder.UseSqlite($"Data Source={dbPath}");
        return new NotificationDbContext(builder.Options);
    }
}
