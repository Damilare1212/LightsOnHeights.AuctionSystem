using Auction.InvoiceService.IRepo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace Auction.InvoiceService.Repo;

public class InvoiceDbContextFactory : IDesignTimeDbContextFactory<InvoiceDbContext>
{
    public InvoiceDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<InvoiceDbContext>();

        var projectDir = Directory.GetCurrentDirectory();
        var dataDir = Path.Combine(projectDir, "data");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "invoices.db");

        builder.UseSqlite($"Data Source={dbPath}");
        return new InvoiceDbContext(builder.Options);
    }
}
