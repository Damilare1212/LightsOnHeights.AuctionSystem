using Auction.NotificationService.Repo;

namespace Auction.NotificationService.IRepo;

public interface IEventRepository
{
    Task AddAsync(EventEntity evt);
    Task<IEnumerable<EventEntity>> GetByAuctionAsync(Guid auctionId);
}
