using Auction.BiddingService.IRepo;
using Auction.BiddingService.Models;
using Microsoft.EntityFrameworkCore;

namespace Auction.BiddingService.Repo;

public class EfBidRepository : IBidRepository
{
    private readonly BiddingDbContext _db;

    public EfBidRepository(BiddingDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(BidEntity bid)
    {
        _db.Bids.Add(bid);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<BidEntity>> GetByAuctionAsync(Guid auctionId)
    {
        return await _db.Bids.Where(b => b.AuctionId == auctionId).ToListAsync();
    }

    public async Task<BidEntity?> GetHighestByAuctionAsync(Guid auctionId)
    {
        return await _db.Bids.Where(b => b.AuctionId == auctionId).OrderByDescending(b => b.Amount).FirstOrDefaultAsync();
    }
}
