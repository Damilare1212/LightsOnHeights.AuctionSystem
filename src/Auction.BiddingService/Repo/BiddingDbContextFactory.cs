using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace Auction.BiddingService.Repo;

public class BiddingDbContextFactory : IDesignTimeDbContextFactory<BiddingDbContext>
{
    public BiddingDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<BiddingDbContext>();

        var projectDir = Directory.GetCurrentDirectory();
        var dataDir = Path.Combine(projectDir, "data");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "bidding.db");

        builder.UseSqlite($"Data Source={dbPath}");
        return new BiddingDbContext(builder.Options);
    }
}
