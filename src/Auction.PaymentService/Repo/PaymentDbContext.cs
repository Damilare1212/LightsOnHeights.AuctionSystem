using Microsoft.EntityFrameworkCore;
using Auction.PaymentService.Models;

namespace Auction.PaymentService.Repo;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }
    public DbSet<PaymentRecord> Payments { get; set; } = null!;
}
