using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace Auction.PaymentService.Repo;

public class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<PaymentDbContext>();

        var projectDir = Directory.GetCurrentDirectory();
        var dataDir = Path.Combine(projectDir, "data");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "payments.db");

        builder.UseSqlite($"Data Source={dbPath}");
        return new PaymentDbContext(builder.Options);
    }
}
