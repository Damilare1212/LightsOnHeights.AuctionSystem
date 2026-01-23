using Auction.BiddingService.Models;
using Microsoft.EntityFrameworkCore;

namespace Auction.BiddingService.Repo;

public class BiddingDbContext : DbContext
{
    public BiddingDbContext(DbContextOptions<BiddingDbContext> options) : base(options) { }
    public DbSet<BidEntity> Bids { get; set; } = null!;
}
