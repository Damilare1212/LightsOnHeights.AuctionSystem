using Auction.BiddingService.Models;

namespace Auction.BiddingService.IRepo;

public interface IBidRepository
{
    Task AddAsync(BidEntity bid);
    Task<IEnumerable<BidEntity>> GetByAuctionAsync(Guid auctionId);
    Task<BidEntity?> GetHighestByAuctionAsync(Guid auctionId);
    Task<IEnumerable<BidEntity>> GetAllAsync();
}
