using System.Collections.Concurrent;

namespace Auction.RoomService.Services;

public class RoomManager
{
    private readonly ConcurrentDictionary<Guid, RoomState> _rooms = new();

    public void AddAuction(Guid roomId, Guid auctionId)
    {
        var room = _rooms.GetOrAdd(roomId, id => new RoomState { RoomId = id });
        room.CurrentAuctionId = auctionId;
        room.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public RoomState? GetRoom(Guid roomId)
    {
        _rooms.TryGetValue(roomId, out var r);
        return r;
    }
}

public class RoomState
{
    public Guid RoomId { get; set; }
    public Guid? CurrentAuctionId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
