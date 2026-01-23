using Auction.InvoiceService.Repo;
using Microsoft.EntityFrameworkCore;

namespace Auction.InvoiceService.IRepo;

public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options) { }
    public DbSet<InvoiceEntity> Invoices { get; set; } = null!;
}
