using Auction.NotificationService.IRepo;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Auction.NotificationService.Repo;

public class EfEventRepository : IEventRepository
{
    private readonly NotificationDbContext _db;

    public EfEventRepository(NotificationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(EventEntity evt)
    {
        _db.Events.Add(evt);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<EventEntity>> GetByAuctionAsync(Guid auctionId)
    {
        return await _db.Events.Where(e => e.AuctionId == auctionId).ToListAsync();
    }
}
